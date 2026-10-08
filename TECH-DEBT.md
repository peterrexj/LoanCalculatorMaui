# Technical debt register

Known compromises, why they exist, and what they will cost. Read alongside `CLAUDE.md` (which
describes how things *work*) — this file records where the design is knowingly weak.

**The dangerous failure mode in this app is not a crash, it is a plausible wrong number.** It is a
financial calculator; users act on the output. Items below are ranked with that in mind: silent
miscalculation outranks a visible break, because nobody reports it.

Last reviewed: 2026-10-07.

---

## D1 — RESOLVED 2026-10-07: `SumUpData` no longer carries a deduction

**Was: severity high, produced wrong figures silently.**

`IncomeExpenseBase.SumUpData(monthly, yearly)` subtracted a caller-supplied deduction from the
totals it had just computed and left it in `IncomeExpenseSummary.TotalMonthly`/`TotalYearly` —
fields shared by the Income tab, Expense tab, Budget page, Loan/affordability box, Wizard and the
PDF. Those fields therefore meant net-or-gross depending on which screen refreshed last, so one
screen's *display toggle* changed another screen's arithmetic.

**What was wrong, with the symptoms it produced:**

- **Budget's Net double-subtracted the expenses.** Flipping "income after expenses" on Budget's
  Income tab made the Summary tab compute `(14000 − 3000) − 3000` instead of `14000 − 3000`, and it
  stayed wrong until the next `RecalculateSummary`.
- **`SumUpData` was not atomic**, so two concurrent passes applied the deduction twice. The
  `ShowIncomeAfterExpense` setter launches an un-awaitable async refresh that races any synchronous
  one; interleaved, income went to `gross − d − d`. This presented as an intermittent test failure
  before it was understood.
- The Budget Expenses tab showed a negative "Monthly Income", and a negative yearly total made the
  Loan page claim no income had been recorded.

**The fix:** `SumUpData()` takes no parameters and always reports the plain sum of the entries. An
"after expenses" figure is computed where it is displayed — `IncomeViewModel.TotalMonthlyIncomeValue`
subtracts `TotalMonthlyExpense` in a getter, and `PdfDataInsightsModel` uses plain subtraction. This
also removed:

- the `TotalMonthlyGross`/`TotalYearlyGross` pair, which existed only to work around the ambiguity
  and were `[JsonIgnore]` (hence 0 after any load — see D1a below);
- the three defensive `SumUpData()` resets in `LoanViewModel.AffordabilityRawValue`, the two in
  `BuildInsights`, the two in `PdfDataInsightsModel.InitializeLocalDataSet`, and the one in
  `PdfInsightsGenerator`;
- `PdfDataInsightsModel.IncomeModel.TransactionRecordsWithExpense{,IncludingProperty}`, two getters
  that called `SumUpData(deduction)` on the **live shared** records and returned them, so rendering
  a PDF mutated view-model state. Replaced by the pure `IncomeExpenseScaling.ScaleToNet`.

**Guarded by:** `AffordabilityOrderDependenceTests` (affordability must not change by a cent),
`IncomeExpenseGrossTotalsTests` (drives the real toggle and asserts no leakage onto the Expenses
tab or Budget's Net), `IncomeExpenseScalingTests` (the PDF breakdown, including that the summary
total agrees with the rows), and `PdfInsightsGeneratorTests`, which **generates a real PDF and
asserts on the text extracted from it**. 963 tests green across 3 consecutive runs.

**On the PDF tests, two things worth keeping.** They assert the error handler recorded nothing,
because `GeneratePdf` wraps everything in `catch { HandleException(e); }` — a broken report
otherwise produces no file and no visible error. And their fixture uses deliberately awkward
figures (9,137 / 2,513): with round numbers the net 6,500 also appeared as 78,000/12 elsewhere in
the report, so the assertion passed even with the subtraction mutated to an addition. Both the
sign mutation and removing the expense recompute are confirmed to fail these tests.

**Do not reintroduce a deduction parameter on `SumUpData`.**

## D1a — RESOLVED 2026-10-07: income/expense totals were zero after a cold start

**Severity was high: the affordability box silently did not appear.**

`BudgetViewModel.LoadIncomeAsync`/`LoadExpenseAsync` loaded via
`LoadDataFile` + `CopyPropertiesFrom` + `InitializeViewData`, none of which sums the entries. The
`*Gross` fields were `[JsonIgnore]`, so they arrived as 0 whatever the saved file contained, and
`LoanViewModel.HasIncomeExpensesRecorded` gates on `TotalYearlyGross > 0`. `SplashPage` pre-warms
through this path, so **the affordability box was hidden on the first Loan page appearance of every
launch** until the user happened to visit Budget.

This was collateral from the partial Gross mitigation, not from D1 itself. Both loaders now call
`SumUpData()`. Guarded by `BudgetColdStartLoadTests`, which mocks `ILocalStorage` to return the
deserialized shape (entries present, totals zero) and was verified red before the fix.

Also fixed on this path: `PdfDataInsightsModel.InitializeLocalDataSet` now recomputes all three
record sets up front. The PDF reaches it via `PdfGeneratorBase` → `LoadDataFile`, which never sums,
so the loan running costs and expense totals were previously whatever was last written to disk —
only income was ever corrected.

## D1b — projection terms are still shared mutable state

**Severity: medium. Same root cause as D1, deliberately out of its scope.**

`HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear` writes `ProjectionTerms` onto the
**shared** `IncomeExpenseSummary`, and its `personalExpense` argument is driven by the Income tab's
`IncludeExpenses` toggle. `BudgetViewModel.RecalculateProjection` reads the same `ProjectionTerms`
via `Income.IncomeProjectList`, so **Budget's projection chart still moves when the user flips a
toggle on the Income tab**. D1 fixed the totals, not the projections — do not read "D1 resolved" as
covering this.

**Fix:** same shape as D1 — have the projection return its series rather than writing it into shared
state, and let each screen apply its own deduction.

## D1c — every view model ever constructed stays on a static event forever

**Severity: medium. Was a suite-wide booby trap; still an unbounded subscription.**

`ViewModelUiBase`'s constructor does `Helper.CurrencySymbolChanged += OnCurrencySymbolChanged` and
**nothing ever unsubscribes** — the type has no `Dispose` and no removal path. So every
`IncomeViewModel`/`ExpenseViewModel`/`LoanViewModel`/`BudgetViewModel`/`WizardViewModel` instance
accumulates on a static event for the life of the process. Bounded in the app (the primary VMs are
DI singletons) but not for the transient ones — `BudgetViewModel` constructs its own Income/Expense
pair, and the tests construct thousands.

**Fixed 2026-10-07, the dangerous half:** the handler called
`MainThread.BeginInvokeOnMainThread`, which throws outside a MAUI app. A .NET event aborts its
invocation list on the first exception **and propagates to the caller**, so a single leaked view
model turned `Helper.CurrencySymbol = x` into a throwing statement for everyone. `ViewModelUiBase`
now applies the change directly when there is no UI thread to marshal to.

This was masked purely by **alphabetical fixture ordering**: every VM-constructing fixture lived
under `Models.Pdf.*`/`Models.ViewModels.*`, which sort *after* `Models.HelperAndExtensionsTests`.
Adding a fixture named `AffordabilityOrderDependenceTests` put a VM-constructing fixture first and
broke two unrelated currency tests instantly. NUnit order is undeclared here (no `[Order]`, no
parallelism attributes), so that ordering was never a guarantee.

**Still outstanding:** the missing unsubscribe. Give `ViewModelUiBase` a disposal path, or make the
currency notification pull-based instead of a static event push.

## D1d — `TotalMonthly`/`TotalYearly` are still serialized

**Severity: low. Deliberately deferred.**

They are derived data, so persisting them is redundant; `[JsonIgnore]` would be tidier. Deferred
because after D1 nothing writes a net figure, so a stale persisted value is merely redundant rather
than wrong, and it self-corrects on the next `SumUpData`. Doing it changes the on-disk contract and
needs verifying against a device holding a pre-existing data file — cost without a correctness gain.

Note if it is ever done: `DeepCloneObject` is a JSON round-trip (`Exts/JsonExts.cs`), so marking
these `[JsonIgnore]` makes every clone's totals arrive as 0. `IncomeExpenseScaling.ScaleToNet`
already assigns them explicitly after cloning, which is what would keep the PDF correct.

## D2 — Income/Expense exist as two different instances

**Severity: high. Causes edits to appear to vanish.**

`BudgetViewModel.Income`/`.Expense` are its own `new IncomeViewModel()`/`new ExpenseViewModel()`
field initialisers — **not** the DI singletons that `LoanView`/`WhatIfView` inject. `SplashPage`
pre-warms the Budget copies from disk on every launch, so those report `HasInitialized` while the
singletons never do.

This produced two real bugs in one day:

1. The What If stress test computed from a stale cache in both directions.
2. The Quick Setup wizard wrote income/expense into the **singletons**, so the next page appearance
   overwrote the commit with Budget's copy and the affordability box the user had just unlocked
   disappeared.

**Current state:** `LoanViewModel.ResolveAuthoritativeIncome/ResolveAuthoritativeExpense` gives
readers and writers one agreed precedence order (Budget copy → singleton → disk, writers never
disk). That is **a patch over the design, not a fix** — and it is now load-bearing in three call
sites.

**Fix:** one owner for that data. Either Budget uses the DI singletons, or nothing else may hold a
reference and all access goes through one service.

## D3 — `HasIncomeExpensesRecorded` is a cached flag, not a computed property

**Severity: medium. Features silently lock or unlock.**

`IsAffordabilityAvailable` gates on it and `MonthlySurplus` gates on that, so **every page that
reads affordability must remember to refresh it on appearing**. There are three such sites today
(`LoanView`, `WhatIfView`, `WizardViewModel.Commit`). The fourth page someone adds will reproduce
the original bug, and the compiler will not help.

**Fix:** make it computed from the resolved income/expense records. Depends on D2 — once there is a
single owner, a computed property is cheap and cannot go stale.

## D4 — `SaveData` is fire-and-forget and cannot be awaited

**Severity: medium. Risk of silent data loss.**

`SharedServiceCore.SaveData<T>` runs `Task.Run(...)` and discards the task, so no caller can know
whether a write landed. Consequences:

- Data typed just before the app is killed may never reach disk.
- Any read-back from disk is a race by construction; this is why
  `RefreshIncomeExpenseSummariesAsync` must prefer the in-memory copy.
- Tests could not assert a save happened without a hole in production API:
  `SharedServiceCore.LastSaveCompleted` exists **only** so tests can await the write.

**Fix:** return the `Task` and let callers that care await it, keeping a fire-and-forget wrapper for
the debounced hot path. Then delete `LastSaveCompleted`.

## D5 — `SyncfusionIosTouchFix` depends on Syncfusion's private view tree

**Severity: medium. Breaks invisibly on upgrade.**

`src/LoanCalculator/Extensions/SyncfusionIosTouchFix.cs` walks the native iOS subtree matching
internal class names (`LayoutViewExt`, `PlatformGraphicsViewExt`, `NativePlatformGraphicsView`)
with `depth <= 3` and `height <= 120` heuristics, pinned to **Syncfusion 33.2.6**. Syncfusion ships
monthly.

**Three controls now need it** — `SfTabView` headers, `SfSegmentedControl`, and `SfExpander`
(2026-10-06). That it keeps growing is the real signal: this is a defect family in the library, so
assume the next drawing-backed control will need the same treatment. Note the three do **not** share
one matcher — the first two key off `GestureRecognizers.Length == 0`, which the expander overlay
defeats by reporting five. Any future case must be measured, not assumed; see CLAUDE.md for the
hit-test probe that identifies the interceptor in one run.

On upgrade this either becomes unnecessary or silently stops matching — and the symptom is "taps do
nothing, no error, iOS only", which is expensive to diagnose from scratch.

**Mitigation, not fix:** treat any Syncfusion version bump as requiring a manual tap-test of the
Loan tab headers and the repayment-frequency segmented control. The UI suite does not cover this,
because Appium taps by coordinate and does not care which view consumes the touch.

## D6 — Numeric entries hand-roll what a binding would do

**Severity: low-medium. Easy to "fix" back into a bug.**

`LoanDetailsPage` does not bind `Entry.Text`; it populates imperatively under a
`_suppressTextChanged` flag. This is deliberate and documented in `CLAUDE.md`, but it is
unenforceable — a future contributor will reasonably add `Text="{Binding …}"` and reintroduce a
competing write path. There is no test that fails if they do.

`_suppressTextChanged` in `LoanView` is also shared between the Upfront Costs popup and the
(removed) Quick Input flow — one boolean guarding unrelated code paths.

**Resolved 2026-10-05:** the per-keystroke `entry.Text` + `CursorPosition` rewrite is gone.
Grouping now happens on `Unfocused`. That rewrite made the caret jump backwards on Android, because
the native `EditText` applies replacement text asynchronously and the caret index we computed was
one short once a separator was inserted.

## D7 — Dead code with live tests

**Severity: low. Wastes effort and misleads.**

- `IncomeView`/`ExpenseView` remain in the repo with their `ShellContent` entries commented out of
  `AppShell.xaml`. Unreachable.
- `ExpenseViewModel` still carries its own `Wizard*HasValue/Editable/Summary` triplet that the
  wizard never uses, with unit tests covering it. Its semantics diverge from the ones actually used
  (`TotalMonthly > 0` + `"/mo"` vs `Any(Amount > 0)`).
- The matching `IncomeViewModel` triplet was **deleted 2026-10-07** with its 6 tests, as part of
  D1: it read `TotalYearly`, so it was a D1-shaped reader and not worth carrying forward.
  `WizardViewModel` supersedes both by reading the entries directly, which is correct even before
  `SumUpData` has run.

**Fix:** delete the views and those VM members with their tests. Left in place only because
removing them is churn with no behavioural gain.

## D8 — Wizard preview labels show committed values, not typed ones

**Severity: low, but user-visible and confusing.**

**Resolved 2026-10-06.** The labels still read committed loan state, but Section A of
`LoanDetailsPage` writes the model live and now calls `WizardViewModel.NotifyPreviewLabels()` on
each keystroke — a narrow notifier, deliberately not `Refresh()`, which also calls `SumUpData` on
three record sets and notifies ~25 properties.

## D9 — No upper bound on monetary input

**Severity: low.**

`12,323,232,334` is accepted as an asset price. There is no maximum, so amortisation and chart code
can be handed absurd values. Related: a deposit above the asset value is a known live-lock, see D10.

## D10 — Quarantined: deposit above asset value live-locks the app

**Severity: high when hit, but gated behind invalid input.**

`ADepositLargerThanTheAssetDoesNotCrash` is `[Ignore]`d. The app spins at ~100% CPU indefinitely.
Profiled twice: all main-thread samples in `-[UIKit_UIControlEventProxy BridgeSelector]`, i.e. a
UIControl event storm, not a calculator loop. Ruled out: amortisation guards, the clamp mismatch,
`ProcessDepositCalc`, the payment-schedule loop.

Key insight for whoever picks this up: the `_suppressTextChanged`-style guards are **synchronous**,
but iOS delivers these events **asynchronously**, so a guard is already cleared when the re-entrant
pass arrives. Two attempted fixes were reverted (slider `Maximum` 99→100 broke a pre-existing test
asserting the model's deliberate 100 clamp; a value-equality guard did not touch the hot path).

## D11 — The test suites do not currently gate releases

**Severity: high, because it multiplies every item above.**

- The unit suite was failing **2–4 tests per run** at `HEAD` before 2026-10-02. "Green" had not
  meant anything for some time. Now deterministic across 10 consecutive runs, but see D1 — part of
  that was mitigation; D1 is now cured at the source.
- The UI suite takes **40–90 minutes**, which is too slow to run per-change. It will drift toward
  "run before release, panic, skip".
- **Appium passes where a human cannot type.** `EnterText` injects via XCUITest without a keyboard,
  so the suite cannot detect an unusable input field. Green does not mean usable.

**Fix:** keep the fast suite deterministic and make it a merge gate. Coverage matters less than
trust — an ignored suite has negative value, because it costs time and buys no confidence.

---

## Not debt — do not "clean these up"

Worth separating so effort goes to the right places:

- **Per-platform locator chains** in `AppDriver`. iOS and Android genuinely map `AutomationId`
  differently; this is correct, not a hack.
- **Newest-runtime simulator resolver** in `run-ios.sh`/`run-uitests.sh`. Without it the scripts
  silently pick the *oldest* installed iOS runtime — which is how the suite ran on iOS 18.4 against
  a 26.5 build SDK for weeks.
- **Keyboard-aware `SwipePage`** and the wizard's large bottom padding. The keyboard overlays the
  `ScrollView` rather than insetting it, so 233pt of viewport is dead space MAUI does not account
  for. Both are correct responses to that.
- **Separate `SessionTimeout` / `LaunchTimeout`.** They budget different things; sharing one number
  is what made a cold WebDriverAgent build look like a dead simulator.
- **Moving Quick Setup and Quick Input out of `SfPopup` onto modal pages.** This *removed* debt:
  ~840 lines out of `LoanView` and an escape from the construct behind every platform gotcha.

## Release blockers — not debt, do not ship without these

1. **`TESTING_PREMIUM_OVERRIDE = true`** (`SharedServiceCore.cs:230`) bypasses every trial gate.
   A comment is not a guard: add a Release-configuration assertion or a unit test that fails when
   it is true outside Debug.
2. **Exposed credentials.** A GitHub PAT is embedded in the `origin` remote URL; Android signing
   `StorePass`/`KeyPass` are plaintext in the tracked `.csproj` (lines 83–84) and have been in git
   history since `baabd49`; **3 keystores are committed**. Removing them now does not help —
   rotate the PAT and the keystore passwords, and rewrite history.
