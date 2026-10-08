# UI test cases — objectives, steps, and what to do when they fail

One entry per test. Each gives the **objective** (the regression it exists to catch), the
**steps** it actually performs, the **expected result**, the exact command to **run just that
test on either platform**, and **what to check when it fails**.

- Setup, architecture and conventions → [README.md](README.md)
- Failures organised by symptom → [TROUBLESHOOTING.md](TROUBLESHOOTING.md)

**39 cases across 36 test methods.** Theme/font/currency selection is iOS-only — Syncfusion
drop-downs cannot be opened on Android; see [TC-5.1](#tc-51).

**Test data is managed centrally.** `Infrastructure/TestData.cs` seeds a loan and a budget at most
once per run and hands the same data to any test that asks, so **no test depends on another having
run first**. The two destructive tests restore the baseline in a `TearDown`. Fixed figures (asset
650,000 / deposit 130,000 / income 6,000 / expense 1,500) keep failures comparable between runs.

---

## How to run

### Both platforms, same command shape

```bash
cd src/Tests/LoanCalculator.UITests
npm install && npm run drivers          # first time only

./run-uitests.sh --ios                  # full suite, ~24 min
./run-uitests.sh --android              # full suite, ~13 min
```

### By category

```bash
./run-uitests.sh --ios     --filter "TestCategory=Smoke"        # ~4 min — run per change
./run-uitests.sh --android --filter "TestCategory=Smoke"        # ~3 min
./run-uitests.sh --ios     --filter "TestCategory=Calculation"
./run-uitests.sh --ios     --filter "TestCategory=Settings"
./run-uitests.sh --ios     --filter "TestCategory=EdgeCase"     # boundary inputs + empty states
```

### A single test

```bash
./run-uitests.sh --ios     --filter "Name=AppLaunchesToTheLoanTab"
./run-uitests.sh --android --filter "Name=AppLaunchesToTheLoanTab"
```

### A single fixture

```bash
./run-uitests.sh --ios --filter "FullyQualifiedName~LoanCalculationTests"
```

### Platform-specific notes

| | iOS | Android |
|---|---|---|
| Device | iPhone 17 Pro simulator, iOS 26.5 (`--device "iPad Pro 13-inch (M5)"` to change) | first available AVD (`--avd <name>` to pick) |
| First run | Builds WebDriverAgent once — several minutes, not hung | Installs the UiAutomator2 server once |
| Speed | ~1 min/test | ~30 s/test, but the app build is slower |
| Prerequisite | Xcode, licence accepted | Android SDK + JDK 17+ |
| Known gap | none | theme selection (TC-5.1, TC-5.2) reported as *ignored* |

Useful flags: `--no-build` (reuse the installed app — warns if XAML is newer), `--fresh`
(uninstall first, so the app starts with no saved data).

---

## What every test does first

`UITestBase.BaseSetUp` runs before **each** test:

1. Constructs the page objects.
2. **Terminates and relaunches the app** (`UITEST_RESTART_PER_TEST`, default on) so no test
   inherits UI state from another.
3. `AppLaunch.WaitUntilReady()` — waits out the animated splash, then clears whichever
   first-run gates are present:
   - the **launch disclaimer** (`DisclaimerAcceptButton`), and
   - the **Quick Setup wizard** (`WizardCancelButton`), which only appears on a fresh install.
4. Waits for the Loan tab's `FabQuickInput` to confirm the app is usable.

`BaseTearDown` writes a screenshot and the full element tree to
`TestResults/uitest-artifacts/` on failure.

**So a failure inside setup is never about the test's own subject** — see
[TC-0](#tc-0-shared-setup-failures).

---

## TC-0. Shared setup failures

Not a test, but the most common thing you will see, because it fails *every* test at once.

> `The app did not reach the Loan tab within 90s.`

| Check | Why |
|---|---|
| **Android: `adb shell settings get global animator_duration_scale`** | If `0`, MAUI animations never complete, the splash never hands off, and nothing works. Set it to `1.0`. This is the single most likely cause |
| The failure **screenshot** | MAUI splash = `PreWarmAsync` stalled. Native splash (plain icon) = never rendered. Normal app = the *anchor* is wrong, not the app |
| A popup with no `AutomationId` | It cannot be dismissed, so setup never completes |
| Genuinely slow cold start | Raise `UITEST_LAUNCH_TIMEOUT_SECONDS` before concluding it hung |

---

## 1. SmokeTests — `TestCategory=Smoke`

The regression net for "a change broke a screen". Run these on every change; they catch what
unit tests structurally cannot.

### TC-1.1 `AppLaunchesToTheLoanTab`

- **Objective:** the app starts and lands on its default tab. The cheapest possible "is it
  alive" check, and the canary for startup regressions, DI wiring faults and splash hangs.
- **Steps:** (setup launches and clears first-run gates) → assert the Loan tab is showing.
- **Expected:** `FabQuickInput` is visible.
- **Run:** `--filter "Name=AppLaunchesToTheLoanTab"`
- **On failure:** see [TC-0](#tc-0-shared-setup-failures). If every other test also fails, fix
  this one first — the rest are almost certainly the same cause.

### TC-1.2 `EveryTabRenders` — 4 cases: Loan, Budget, WhatIf, Settings

- **Objective:** every Shell tab still builds and renders its content. Catches a XAML change,
  missing resource key or binding error that breaks one page — the classic "it compiled, but
  that screen is now blank".
- **Steps:** navigate to the tab → assert its anchor element is visible.
- **Expected:** each tab's anchor renders: Loan `FabQuickInput`, Budget `BudgetTabIncome`,
  What If `WhatIfPageTitle`, Settings `SettingsPageTitle`.
- **Run:** `--filter "Name=EveryTabRenders"` (all four), or
  `--filter "FullyQualifiedName~EveryTabRenders(Budget)"` for one.
- **On failure:** only that tab is broken. Read the artifact `.xml` — if the anchor is absent
  the page failed to build (check the logcat/simulator log for a binding or resource
  exception); if present but not visible, the layout changed and the anchor needs revisiting.

### TC-1.3 `TabsCanBeVisitedInSequenceWithoutCrashing`

- **Objective:** pages survive being **re-entered**. This app coordinates cross-tab data
  through dirty flags and `RefreshCrossTabSummaries()`, and that path only runs on revisit —
  so disposal and rebind faults hide here and nowhere else.
- **Steps:** walk Budget → WhatIf → Settings → Loan → Settings → Budget → Loan, asserting the
  correct tab is showing at each step. Note Settings, Budget and Loan are each visited twice.
- **Expected:** every hop lands and renders.
- **Run:** `--filter "Name=TabsCanBeVisitedInSequenceWithoutCrashing"`
- **On failure:** note **which** hop failed — it is almost always the *second* visit to a tab,
  which points at `OnAppearing`/`OnDisappearing` or the dirty-flag refresh rather than the page
  itself. Compare against TC-1.2: if TC-1.2 passes for that tab and this fails, the bug is in
  re-entry.

### TC-1.4 `LoanPageInPageTabsAllRender`

- **Objective:** the Loan page's inner `SfTabView` tabs all render, including the heavy ones
  (a data grid and a chart), and returning to the first tab restores it.
- **Steps:** Loan tab → open Amortisation (anchor `LoanAmortisationGrid`) → open Insights
  (anchor `LoanInsightsExportButton`) → back to Asset → assert `FabQuickInput` visible.
- **Expected:** all three render; Asset is restored at the end.
- **Run:** `--filter "Name=LoanPageInPageTabsAllRender"`
- **On failure:** `SfTabView` keeps **all** tab content in the element tree, so "not found" here
  usually means not *visible*. The driver reports which case. Insights is anchored on the export
  button, not the chart, because the chart sits inside a collapsed expander and is never visible
  on arrival — if you re-anchor it, pick something at the top of the tab.

### TC-1.5 `BudgetInPageTabsAllRender`

- **Objective:** same for the Budget page's four inner tabs, including the Projection tab's
  slider and the Summary tab.
- **Steps:** Budget → Income → Expenses → Summary → Projection → Income again → assert
  `BudgetIncomeAddFab` visible.
- **Expected:** all four render and Income is restored.
- **Run:** `--filter "Name=BudgetInPageTabsAllRender"`
- **On failure:** Summary is anchored on `BudgetSummaryCaption`, *not* a total — with an empty
  budget the Summary tab shows a "No budget data yet" placeholder instead of the totals, so a
  total would be a data-dependent anchor. If you change that anchor, keep it data-independent.

### TC-1.6 `QuickInputPopupOpensAndCloses`

- **Objective:** the Loan Details page opens and closes. It is the only place asset, deposit and
  loan amount can be typed, so this is a cheap guard on the app's main data-entry route.
  (The name is historical: this was an `SfPopup`, then a separate Quick Input page, before Quick
  Setup and Quick Input merged into one modal `ContentPage`.)
- **Steps:** Loan tab → tap `FabWizard` → assert `AssetEntry` visible → tap `QuickInputApplyButton`
  → assert `AssetEntry` gone.
- **Expected:** opens, then closes.
- **Run:** `--filter "Name=QuickInputPopupOpensAndCloses"`
- **On failure:** if it will not open, check `FabWizard` still exists — it is also the Loan tab's
  readiness anchor in `ShellTabs.AnchorOf`, so losing it fails every test, not just this one.

### TC-2.1 `RaisingTheDepositReducesTheLoanByTheSameAmount`

- **Objective:** the core arithmetic — a larger deposit reduces the borrowed amount one-for-one.
  Protects the binding chain from entry → view model → displayed loan amount.
- **Steps:** Loan tab → open Quick Input → set asset 800,000 / deposit 100,000, read the settled
  loan → set asset 800,000 / deposit 150,000, read again.
- **Expected:** second loan == first − 50,000 (±1).
- **Run:** `--filter "Name=RaisingTheDepositReducesTheLoanByTheSameAmount"`
- **On failure:** if the delta is wrong but non-zero, the calculation changed — check
  `HomeLoanCalculator` and the unit tests. If the loan did not move at all, the binding or the
  debounced save broke. If the number looks like *only* the upfront costs, the deposit was
  silently set to 100% of the asset (the coupling above).

### TC-2.2 `RaisingTheAssetValueRaisesTheLoanByTheSameAmount`

- **Objective:** the mirror invariant — a larger asset raises the borrowed amount one-for-one,
  with the deposit held constant.
- **Steps:** set asset 600,000 / deposit 120,000, read → set asset 700,000 / deposit 120,000
  (deposit rewritten because changing the asset re-derives it), read again.
- **Expected:** second loan == first + 100,000 (±1).
- **Run:** `--filter "Name=RaisingTheAssetValueRaisesTheLoanByTheSameAmount"`
- **On failure:** as TC-2.1. If the value jumps to roughly the asset amount, the deposit reset
  to 100% — that is the known coupling, and the reason this test rewrites the deposit.

### TC-2.3 `LoanAmountIsNeverMoreThanAssetPlusUpfrontCosts`

- **Objective:** a sanity bound — the deposit must always come off the borrowed figure. Catches
  a sign error or a dropped deposit term that the delta tests could still satisfy.
- **Steps:** set asset 650,000 / deposit 130,000 → read the loan amount.
- **Expected:** loan > 0 **and** loan < 780,000 (asset + deposit).
- **Run:** `--filter "Name=LoanAmountIsNeverMoreThanAssetPlusUpfrontCosts"`
- **On failure:** the deposit is not being subtracted, or upfront costs are being added twice.

### TC-2.4 `AmountsAreSpelledOutInWords`

- **Objective:** the amount-in-words display still renders. It is a separate formatting path
  from the numeric label and has its own currency/locale handling.
- **Steps:** set asset 650,000 / deposit 130,000 → read `LoanWords`.
- **Expected:** non-empty, and matches `thousand|lakh|million` (case-insensitive — the app
  supports Indian numbering, so "lakh" is valid).
- **Run:** `--filter "Name=AmountsAreSpelledOutInWords"`
- **On failure:** if empty, the words converter threw or the binding broke. If non-empty but
  unmatched, the wording or numbering system changed — widen the regex rather than deleting the
  test.

### TC-2.5 `QuickInputValuesSurviveReopeningThePopup`

- **Objective:** entered values **persist**. This exercises the 600 ms debounced save and the
  reload path — the most likely place for "I typed it and it vanished" bugs.
- **Steps:** open Quick Input → set asset 725,000 / deposit 145,000 → wait for the loan to
  recalculate (proving the save was triggered) → Apply → reopen Quick Input → read both fields.
- **Expected:** asset 725,000 and deposit 145,000 still shown.
- **Run:** `--filter "Name=QuickInputValuesSurviveReopeningThePopup"`
- **On failure:** most likely the debounced save did not flush before the popup closed — check
  `FlushPendingSave` is called on dismissal. Note the test deliberately waits for a
  recalculation *before* applying, so a failure here is a real persistence bug, not a race in
  the test.

### TC-2.6 `CompletingTheSetupWizardRecordsTheLoan`

- **Objective:** first-run setup is what every new user sees, and the suite previously only ever
  *cancelled* it. This checks that the values it collects reach the loan.
- **Steps:** tap `FabWizard` → asset 900,000, deposit 180,000 → dismiss keyboard, scroll to
  `WizardCalculateButton` → Calculate → reopen and read both fields.
- **Expected:** asset 900,000 and deposit 180,000 recorded.
- **Run:** `--filter "Name=CompletingTheSetupWizardRecordsTheLoan"`
- **On failure:**
  - `WizardCalculateButton` not found — it sits at the end of the scrollable content, *below* the
    forced-present software keyboard, so it must be scrolled to. See TROUBLESHOOTING on scrolls
    that type digits.
  - Values do not reach the loan — `WizardViewModel.Commit()` is not writing them. Note it writes
    expenses first, then the asset, then **one** side of the deposit/loan split last; the order is
    load-bearing.
  - Only asset/deposit are filled on purpose. Upfront, running cost, income and expenses are shown
    **only while empty**, so they may legitimately be absent.
---

## 2b. CrossScreenTotalsTests — `TestCategory=Calculation`

The two TECH-DEBT D1 regressions. Both mechanisms are already pinned by the unit suite; these
cover what unit tests cannot — real disk persistence, the splash pre-warm, the page lifecycle and
the bindings.

**Everything here is an invariant or a delta, never an absolute total.** Budget data persists
between runs, and both defects are "a figure changed when it should not have" — which a delta says
directly, and which is immune to pre-existing data, the currency symbol and the unsigned net
display.

### TC-2b.1 `AffordabilityIsAvailableOnTheFirstLoanPageAfterARestart`

- **Objective:** TECH-DEBT D1a. The splash pre-warm loaded income/expenses without summing them,
  and the flag gating the affordability box reads the yearly totals, which are **not persisted**.
  The box therefore stayed in its "record your income and expenses" state for the whole session
  until something recalculated.
- **Steps:** `TestData.EnsureBudget` → `RestartApp` → `WaitUntilReady` → **Loan tab first** →
  read `LoanSummaryBox`.
- **Expected:** the summary box mentions "estimate" and not "record".
- **Run:** `--filter "Name=AffordabilityIsAvailableOnTheFirstLoanPageAfterARestart"`
- **On failure:**
  - **Do not add a Budget visit before the Loan tab to "stabilise" it.** Visiting Budget
    recalculates and masks the entire defect. The Loan tab being first is the test.
  - Box says "record" — `BudgetViewModel.LoadIncomeAsync`/`LoadExpenseAsync` are not summing, or
    `HasIncomeExpensesRecorded` is gating on a non-persisted field again.
  - Asserts a *word*, not an amount, on purpose: the caption switches on exactly the flag under
    test and survives differences an amount would not.

### TC-2b.2 `IncomeAfterExpensesToggleDoesNotChangeOtherScreens`

- **Objective:** TECH-DEBT D1. The Income tab's "income after expenses" switch is a *display*
  preference, but it used to write its deduction into the summary object shared by every screen —
  so Budget's Net subtracted the expenses twice and the Expenses tab's "Monthly Income" showed a
  reduced, sometimes negative, figure.
- **Steps:** `EnsureBudget` → record Summary Net, Summary income, and both tabs' summary blocks →
  tap `BudgetIncomeAfterExpenseSwitch` → confirm the Income tab's block **changed** → re-read the
  others.
- **Expected:** the Income tab's own figures change; Summary Net, Summary income and the whole
  Expenses tab block are **unchanged**.
- **Run:** `--filter "Name=IncomeAfterExpensesToggleDoesNotChangeOtherScreens"`
- **On failure:**
  - **Inconclusive** (`Assume` failed, Income tab unchanged) — the switch tap did not register.
    `SfSwitch` draws its own state rather than exposing it, so the test verifies the tap by its
    effect; it cannot read the switch. Check the tap landed, and see TROUBLESHOOTING on Syncfusion
    iOS touch overlays.
  - Net changed — the deduction is reaching shared state again. `SumUpData` must take no
    deduction parameter; an "after expenses" figure belongs where it is displayed.
  - The toggle **persists**. The fixture's `[TearDown]` turns it back off; if a run dies partway,
    turn it off by hand or later tests start from a flipped state.
  - `BudgetTabSummary` not found — you are probably **on the Loan page**. `TestData.EnsureBudget`
    latches on a static `_budgetSeeded` and returns *before* `tabs.GoTo(AppTab.Budget)` once any
    earlier test has seeded, so it does not reliably leave you on Budget. Navigate explicitly, as
    `BudgetTests` does in its `[SetUp]`. This is what the fixture's first run failed on, and the
    error ("missing an AutomationId in XAML") points the wrong way — check the artifact's element
    list for `LoanTab*` ids before touching any XAML.

---

## 3. BudgetTests — `TestCategory=Calculation`

Budget data **persists between runs**, so these assert on the *change* an action causes, never
an absolute total. That keeps them correct whether the device starts empty or with data from an
earlier run.

Two of them call `EnsureSomeBudgetData()` first, because an empty budget shows a placeholder
instead of the totals. The Summary totals are reached via `ScrollToTotals()` — required on
Android, where the accessibility tree *omits* far-off-screen ScrollView children entirely.

> **Frequency caveat:** the add form's frequency defaults to whatever the view model last held,
> so an entry may be weekly or fortnightly and contribute a *converted* figure to the monthly
> total. That is why TC-3.1 and TC-3.2 assert direction, not an exact amount — which still
> catches the regression that matters: an entry that silently does not register.

### TC-3.1 `AddingIncomeRaisesTheMonthlyIncomeTotal`

- **Objective:** adding income flows through to the Summary total — the whole add → save → list
  → aggregate chain.
- **Steps:** Budget → read monthly income (0 if empty) → Income tab → tap `+` → enter a
  timestamped unique name and amount 4000 → Save → Summary → scroll to totals → wait for the
  total to exceed the baseline.
- **Expected:** monthly income total strictly increased.
- **Run:** `--filter "Name=AddingIncomeRaisesTheMonthlyIncomeTotal"`
- **On failure:** if the popup did not open, check `BudgetIncomeAddFab`. If it opened but Save
  did nothing, check the add/update command and that the amount parsed. If the total did not
  move, the aggregate or the list refresh broke. Names are timestamped, so repeat runs cannot
  collide — a duplicate-name failure means the uniqueness helper changed.

### TC-3.2 `AddingAnExpenseRaisesTheMonthlyExpenseTotal`

- **Objective:** the same chain on the expense side, which has its own popup, list and aggregate.
- **Steps:** as TC-3.1 via the Expenses tab, amount 250.
- **Expected:** monthly expense total strictly increased.
- **Run:** `--filter "Name=AddingAnExpenseRaisesTheMonthlyExpenseTotal"`
- **On failure:** as TC-3.1. If income works and expense does not, the bug is expense-specific —
  compare the two popups' handlers.

### TC-3.3 `NetMonthlyEqualsIncomeMinusExpenses`

- **Objective:** internal consistency of the Summary tab. Catches an aggregate that is computed
  from stale or differently-filtered data than the figures beside it.
- **Steps:** ensure some budget data exists → Summary → scroll to totals → read income, expenses
  and net.
- **Expected:** `net == |income − expenses|` (±1). Magnitudes are compared because the displayed
  net is unsigned.
- **Run:** `--filter "Name=NetMonthlyEqualsIncomeMinusExpenses"`
- **On failure:** a genuine reconciliation bug, and worth chasing — the three numbers are drawn
  from the same screen, so they cannot legitimately disagree. Check whether one includes asset
  expenses and another does not (the Budget tab has "include asset expenses" switches).

### TC-3.4 `YearlyNetIsTwelveTimesMonthlyNet`

- **Objective:** the monthly → yearly conversion.
- **Steps:** ensure data → Summary → scroll to totals → read monthly net and yearly net.
- **Expected:** `yearly == monthly × 12` (±12, allowing rounding).
- **Run:** `--filter "Name=YearlyNetIsTwelveTimesMonthlyNet"`
- **On failure:** if the ratio is 52 or 26, a frequency conversion is being applied twice. If it
  is close but outside tolerance, rounding changed — widen the tolerance only if the new
  behaviour is intended.

### TC-3.5 `DeletingAnIncomeRowRemovesItAndLowersTheTotal`

- **Objective:** delete, which had no coverage. Also feeds the empty-state class — deleting the last
  row is how a screen ends up empty.
- **Steps:** add an income row with a unique space-free name → confirm it appears → read the monthly
  total → tap that row's delete button → confirm the row is gone and the total fell.
- **Run:** `--filter "Name=DeletingAnIncomeRowRemovesItAndLowersTheTotal"`
- **Note:** row buttons are addressed as `IncomeDelete_<name>`, which is why names avoid spaces.
- **On failure:** if the row will not delete, check `OnBudgetIncomeDelete` — it takes the row from
  `btn.BindingContext`. It used to parse `btn.AutomationId` as a Guid, which made the button
  untaggable; do not reintroduce that.

### TC-3.6 `EditingAnIncomeRowAmountUpdatesTheTotal`

- **Objective:** edit, the third CRUD operation. Exercises the edit form pre-populating from an
  existing row and saving back over it, rather than creating a new one.
- **Steps:** add a row at 1,000 → read the monthly total → open that row's edit button → change the
  amount to 5,000 → Save → wait for the total to rise.
- **Run:** `--filter "Name=EditingAnIncomeRowAmountUpdatesTheTotal"`
- **On failure:** if the total rises by the *full* 5,000 rather than the difference, the edit created
  a duplicate row instead of updating in place — check `AddOrUpdateEntryFromView`.

---

## 4. WhatIfTests — `TestCategory=Calculation`

Every What If scenario is driven off the loan recorded on the Loan tab, so **each test seeds
that first** — which also exercises the cross-tab refresh path, where stale summaries show up.
All inputs on this page are `−`/`+` steppers; there are no text fields.

### TC-4.1 `ScenarioCardsAppearOnceALoanExists`

- **Objective:** the page's gating works — cards are hidden with no loan and appear once one
  exists. This is the cross-tab data handover, the most likely place for a stale empty state.
- **Steps:** Loan tab → enter asset 650,000 / deposit 130,000 → Apply → What If tab.
- **Expected:** the "no loan data" placeholder is **gone** and `WhatIfRateDeltaValue` is visible.
- **Run:** `--filter "Name=ScenarioCardsAppearOnceALoanExists"`
- **On failure:** if the placeholder is still showing, the What If page did not pick up the new
  loan — check the dirty-flag refresh and `HasLoanData`. This failing while the Loan tests pass
  means the calculation is fine and the *handover* is broken.

### TC-4.2 `RaisingTheRateDeltaRaisesTheProjectedRepayment`

- **Objective:** directional correctness of the rate-change scenario. A sign error here would be
  invisible to unit tests of the formula if the wiring is what is inverted.
- **Steps:** seed the loan → What If → read the baseline monthly repayment → tap `+` on the rate
  delta 3 times → wait for the repayment to exceed the baseline.
- **Expected:** projected monthly repayment strictly increased.
- **Run:** `--filter "Name=RaisingTheRateDeltaRaisesTheProjectedRepayment"`
- **On failure:** if it moved *down*, the sign is inverted. If it did not move, the stepper taps
  are not registering — check the `+` button is hittable and the recalculation is triggered.

### TC-4.3 `LoweringTheRateDeltaLowersTheProjectedRepayment`

- **Objective:** the opposite direction. Kept as a separate test so an inverted sign fails one
  and passes the other, which localises the bug immediately.
- **Steps:** as TC-4.2 but tapping `−` 3 times.
- **Expected:** projected monthly repayment strictly decreased.
- **Run:** `--filter "Name=LoweringTheRateDeltaLowersTheProjectedRepayment"`
- **On failure:** if TC-4.2 passes and this fails, suspect a clamp at zero — three steps down may
  be hitting a floor. Check the step size in Settings and whether the delta can go negative.

### TC-4.4 `RateDeltaStepsBackToZero`

- **Objective:** the stepper is symmetric — stepping up then down changes the displayed value
  back. Catches a stepper that only increments, or one that clamps asymmetrically.
- **Steps:** seed → What If → `+` twice, read the delta → `−` twice, read again.
- **Expected:** the two readings differ.
- **Run:** `--filter "Name=RateDeltaStepsBackToZero"`
- **On failure:** the `−` button is not working or is clamped. Note this asserts the value
  *changed*, not that it returned to exactly zero — the starting delta comes from Settings and
  is not assumed to be zero.

### TC-4.5 `StressTestUnlocksOnceIncomeAndExpensesExist`

- **Objective:** the affordability stress test needs a loan **and** budget data, making it a
  second cross-tab handover — and the one most likely to show a stale prompt, since it depends on two
  other tabs rather than one.
- **Steps:** seed a loan → seed income and expenses on the Budget tab → What If → scroll to the
  stress-test card.
- **Expected:** the results panel is showing, not the "record your income & expenses" prompt.
- **Run:** `--filter "Name=StressTestUnlocksOnceIncomeAndExpensesExist"`
- **On failure:** if the prompt persists, the What If page did not pick up the budget change — check
  the dirty-flag refresh. TC-4.1 passing while this fails narrows it to the *budget* handover.

### TC-4.6 `StressTestPromptsWhenThereIsNoBudgetData`

- **Objective:** the inverse gate — with no income or expenses it must prompt, not show results
  computed from nothing. Catches a card that renders zeros as if they were real figures.
- **Steps:** seed a loan → Settings → delete income and expense data → What If → scroll to the card.
- **Expected:** the prompt is showing.
- **Run:** `--filter "Name=StressTestPromptsWhenThereIsNoBudgetData"`
- **Destructive:** deletes budget data. `TestData` re-seeds on the next request.
- **On failure:** if results show, the gate is checking the wrong condition — a zero total is being
  treated as "available".

---

## 5. SettingsTests — `TestCategory=Settings`

### TC-5.1 `EveryThemeRendersEveryTab` <a id="tc-51"></a>

- **Objective:** **the highest-value test in the suite.** Theme resources are embedded XAML
  resolved *by key at runtime*, so a key missing from one theme file fails only when that theme
  is selected **and** a page using the key is drawn. The unit test
  `ThemeFiles_ShouldHave_EqualKeys` proves the key *sets* match; this proves they actually
  *resolve*. Nothing else catches this class of bug.
- **Steps:** for each of Dark, Light, Forest, Warm — select the theme, then visit all four Shell
  tabs and assert each renders. Restores Dark in a `finally` block so later tests and manual runs
  start predictably.
- **Expected:** 4 themes × 4 tabs = 16 successful renders.
- **Run:** `--filter "Name=EveryThemeRendersEveryTab"` — **iOS only**
- **Android:** reported as **ignored**. Tapping an `SfComboBox` through UiAutomator2 does not
  open its drop-down (the list never enters the element tree), so no theme can be selected.
- **On failure:** the message names the theme and the tab. That pair is your bug: open that theme
  file and diff its keys against `Theme.CommonStyles.xaml` and the other three. Also run the
  `ThemeFiles_ShouldHave_EqualKeys` unit test — if it fails too, the key is simply missing; if it
  passes, the key exists but its *value* is unresolvable in that theme.

### TC-5.2 `AppRelaunchesCleanlyIntoAPersistedTheme` <a id="tc-52"></a>

- **Objective:** a saved non-default theme is loaded from embedded XAML **during startup, before
  any page is drawn**. If that load fails the app breaks on launch — the worst possible failure,
  and invisible to every other test.
- **Steps:** select Forest → restart the app → wait for ready → visit all four tabs. Restores
  Dark in a `finally` block.
- **Expected:** the app relaunches and all four tabs render under the persisted theme.
- **Run:** `--filter "Name=AppRelaunchesCleanlyIntoAPersistedTheme"` — **iOS only**
- **Android:** ignored, same reason as TC-5.1.
- **Note:** it deliberately does **not** assert *which* theme is showing. A collapsed
  `SfComboBox`'s value cannot be read on iOS (Syncfusion draws it), so the observable assertion
  is "the app relaunches and renders" — which is the real risk anyway.
- **On failure:** if setup times out after the restart, the persisted theme is breaking startup.
  Check the app's crash log and the theme file that was saved.

### TC-5.3 `EveryTabRendersAfterChangingTheFont`

- **Objective:** the font picker loads 13 TTFs and applies the choice through a `DefaultFontFamily`
  `DynamicResource` — structurally the same risk as the theme system, so it gets the same treatment.
  A missing or unregistered font should fail loudly rather than silently falling back.
- **Steps:** Settings → select Merriweather → visit all four tabs. Restores Lato in a `finally`.
- **Run:** `--filter "Name=EveryTabRendersAfterChangingTheFont"` — **iOS only**
- **Android:** ignored (drop-downs cannot be opened).
- **On failure:** the named tab uses a font resource that does not resolve; check the font is
  registered in `MauiProgram.ConfigureFonts` and the TTF is present.

### TC-5.4 `ChangingTheCurrencyChangesTheSymbolOnDisplayedAmounts`

- **Objective:** the chosen currency's symbol is pushed through `Helper.CurrencySymbol` into every
  formatted amount in the app, so a break is visible on every screen at once. It also drives the
  amount-in-words numbering system (hence "lakh" being valid in TC-2.4).
- **Steps:** Settings → select Euro → Loan tab → Quick Input → enter asset/deposit → read the
  formatted loan amount. Restores US Dollar in a `finally`.
- **Expected:** the formatted amount contains `€`.
- **Run:** `--filter "Name=ChangingTheCurrencyChangesTheSymbolOnDisplayedAmounts"` — **iOS only**
- **Android:** ignored (drop-downs cannot be opened).
- **On failure:** if the symbol never changes, `CurrencySymbol` is read once at startup rather than
  observed — the amounts will be correct only after a restart.

### TC-5.5 `SettingsPageRendersItsAppearancePickers`

- **Objective:** the three appearance pickers exist. A cheap structural guard — if a picker
  disappears from the page, TC-5.1's failure would be confusing without this.
- **Steps:** Settings tab → assert `SettingsThemeCombo`, `SettingsFontCombo` and
  `SettingsCurrencyCombo` are all present.
- **Expected:** all three visible.
- **Run:** `--filter "Name=SettingsPageRendersItsAppearancePickers"` — runs on **both** platforms.
- **On failure:** a picker was removed, renamed, or its `AutomationId` was dropped in a XAML
  edit. Confirm against the artifact `.xml`.

---

## 6. EdgeCaseTests — `TestCategory=EdgeCase`

Boundary inputs and empty states — **the two places this app has actually broken**. A deposit
covering the whole asset crashed it (`Chunk(0)` on a zero principal), and wiping the device exposed
two screens that assumed data existed. These lock both classes down rather than rediscovering them
by accident.

### TC-6.1 `DepositCoveringTheWholeAssetDoesNotCrash`

- **Objective:** lock the regression that produced `ArgumentOutOfRangeException('size')`. With
  deposit == asset the principal is 0, `CalculateHomeLoan` returns an empty `PaymentOutput`, and
  `Chunk(0)` threw — surfacing as "An unexpected error occurred".
- **Steps:** Quick Input → asset 500,000, deposit 500,000 → Apply → assert the Loan tab still responds.
- **Run:** `--filter "Name=DepositCoveringTheWholeAssetDoesNotCrash"`
- **On failure:** the guard in `UpdateLoanPaymentAmortizationDataByYear` was removed or bypassed.
  The unit tests `UpdateLoanPaymentAmortizationDataBy*_ZeroPaymentsPerYear_DoesNotThrow` cover the
  same cause more cheaply — run those first.

### TC-6.2 `ADepositLargerThanTheAssetDoesNotCrash`

- **Objective:** the same boundary, overshot. A negative principal must not reach the calculator.
- **Steps:** asset 400,000, deposit 600,000 → Apply → assert the Loan tab still responds.
- **Run:** `--filter "Name=ADepositLargerThanTheAssetDoesNotCrash"`
- **On failure:** if TC-6.1 passes and this fails, the guard checks `== 0` rather than `<= 0`.

### TC-6.3 `AZeroPrincipalStillLetsEveryTabRender`

- **Objective:** a zero principal must not break *other* screens. Amortisation and Insights derive
  from the payment schedule, which is empty here — exactly where the two guarded methods are reached.
- **Steps:** set deposit == asset → visit all four Shell tabs → open the Amortisation and Insights
  in-page tabs.
- **Run:** `--filter "Name=AZeroPrincipalStillLetsEveryTabRender"`
- **On failure:** the failing tab names the view that does not tolerate an empty schedule.

### TC-6.4 `AZeroInterestRateIsCalculatedWithoutDividingByZero`

- **Objective:** `CalculateHomeLoan` has a dedicated `InterestRate == 0` branch that skips the
  amortisation formula entirely. A separate code path with no other UI coverage.
- **Steps:** seed a loan → tap the rate label (which swaps in an entry) → set the rate to 0 → read
  the summary → open the Amortisation tab.
- **Run:** `--filter "Name=AZeroInterestRateIsCalculatedWithoutDividingByZero"`
- **On failure:** a blank summary means the zero-rate branch stopped producing a payment. A hang or
  crash on the Amortisation tab means the schedule builder divided by zero.

### TC-6.5 `EmptyingTheLoanDataLeavesEveryTabUsable`

- **Objective:** deleting *just* the loan must leave every screen usable, including What If, which
  is derived entirely from it.
- **Steps:** seed a loan → Settings → open "Delete your data" → Delete Loan → visit all four tabs.
- **Run:** `--filter "Name=EmptyingTheLoanDataLeavesEveryTabUsable"`
- **Destructive:** restores the baseline in `TearDown`.
- **On failure:** the failing tab is showing results computed from deleted data, or crashed instead
  of falling back to its empty state.

### TC-6.6 `EmptyingAllDataLeavesEveryTabUsable`

- **Objective:** **the broadest empty-state test.** Wiping everything is how a real user resets the
  app, and it puts every screen into its empty state at once — where the Summary placeholder and
  What If no-data panel problems were originally found by accident. Also relaunches, because that is
  when data is re-read from disk.
- **Steps:** seed loan + budget → Settings → Delete All Data → visit all four tabs → restart the app
  → assert it comes back to the Loan tab.
- **Run:** `--filter "Name=EmptyingAllDataLeavesEveryTabUsable"`
- **Destructive:** restores the baseline in `TearDown`. Skips itself if the All Data button is
  hidden (`IsAllDataDeleteVisible` is false on that device).
- **On failure:** if the tabs pass but the restart fails, the app cannot start from genuinely empty
  storage — the worst variant, since it is what a new install looks like.

### TC-6.7 `SwitchingRepaymentFrequencyChangesTheSummary`

- **Objective:** the Monthly/Fortnightly/Weekly segmented control needed `SyncfusionIosTouchFix` to
  be tappable at all. If that regresses the taps are swallowed **silently** — no error, nothing
  happens — so this asserts the summary actually moved.
- **Steps:** seed a loan → select Monthly, read the summary block → select Fortnightly → wait for
  the summary text to differ.
- **Run:** `--filter "Name=SwitchingRepaymentFrequencyChangesTheSummary"`
- **On failure:** if the summary never changes, the taps are being swallowed by a Syncfusion drawing
  overlay — check `SyncfusionIosTouchFix.ApplyToSegmentedControl` still runs in `OnAppearing`. This
  is the test most likely to break after a Syncfusion upgrade.

---

## Quick reference — anchors used for navigation

If a navigation step fails, these are the elements being waited for.

| Target | Anchor `AutomationId` |
|---|---|
| Shell → Loan | `FabQuickInput` |
| Shell → Budget | `BudgetTabIncome` |
| Shell → What If | `WhatIfPageTitle` |
| Shell → Settings | `SettingsPageTitle` |
| Loan → Asset | `FabQuickInput` |
| Loan → Amortisation | `LoanAmortisationGrid` |
| Loan → Insights | `LoanInsightsExportButton` |
| Budget → Income | `BudgetIncomeAddFab` |
| Budget → Expenses | `BudgetExpenseAddFab` |
| Budget → Summary | `BudgetSummaryCaption` |
| Budget → Projection | `BudgetProjectionYearsSlider` |

Shell tabs themselves carry no `AutomationId` (MAUI does not add one) and are matched by title,
disambiguated by taking the bottom-most match on screen. If a tab title changes in
`AppShell.xaml`, update `ShellTabs.TitleOf`.

---

## General triage order

1. **Did many tests fail with the same message?** Fix that one cause first — it is almost
   certainly setup ([TC-0](#tc-0-shared-setup-failures)), not the individual subjects.
2. **Read the failure message.** The driver distinguishes "never found", "present but not
   visible", and "the driver itself kept erroring" — that alone usually names the cause.
3. **Open the artifact `.xml`** in `TestResults/uitest-artifacts/` and search for the
   `AutomationId` the test wanted. This settles "real regression" versus "test needs updating"
   faster than anything else.
4. **Did you add an `AutomationId` and run with `--no-build`?** It does not exist until the app
   is rebuilt and reinstalled. The script warns, but the warning is easy to scroll past.
5. **Compare platforms.** A failure on one platform only is usually a harness assumption, not an
   app bug — see TROUBLESHOOTING for the known asymmetries (`resource-id` vs
   `accessibilityIdentifier`, off-screen tree omission, keyboard dismissal closing popups).
6. **Still stuck?** `TestResults/uitest-artifacts/appium-server.log` has the full protocol
   traffic for the run.
