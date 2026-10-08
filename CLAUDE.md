# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Known weaknesses are catalogued in [`TECH-DEBT.md`](TECH-DEBT.md)** — read it before changing
> cross-tab data flow, the numeric entry fields, or anything touching Syncfusion. It also lists the
> two **release blockers** (the premium override, and exposed credentials). This file describes how
> things work; that one records where the design is knowingly weak and why.

> **Reference implementation.** The sibling app at
> `/Users/josephpe/Dev/Git/peterrexj/new/WikiExtractor/src/Maui` is the canonical reference for
> patterns used here. When asked to add or fix something that "we already solved in the other
> repo", look there first.

---

## Build & Run Commands

All commands run from `src/LoanCalculator/` unless noted.

> **Do not build or run the app unless the user explicitly asks.** Builds here are slow
> (MAUI multi-target) and the user typically builds/runs themselves. After making code
> changes, stop and report what changed — do not kick off `dotnet build`, `dotnet run`,
> `run-ios.sh`, `run-android.ps1`, or deploy to a simulator/emulator on your own. Only do so
> when the user says to build, run, test, or deploy.

### Build

```bash
dotnet build LoanCalculatorMaui.csproj -f net10.0-ios26.5 -c Debug
dotnet build LoanCalculatorMaui.csproj -f net10.0-android36.0 -c Debug
dotnet build LoanCalculatorMaui.csproj -f net10.0-maccatalyst -c Debug
```

### Run on iOS Simulator

```bash
./run-ios.sh                          # iPhone 17 Pro (default, iOS 26.5)
./run-ios.sh --ipad                   # iPad Pro 13-inch (M5)
./run-ios.sh --device "iPad mini (A17 Pro)"
```

The script builds, installs to the simulator, launches with `--console-pty`, and filters log output to app-relevant lines only. `Console.WriteLine` output appears in the terminal.

### Run on Android

```bash
pwsh ./run-android.ps1                # Medium_Phone_API_36.1
pwsh ./run-android.ps1 -Tablet
pwsh ./run-android.ps1 -Avd <name>
```

### Run Tests

```bash
dotnet test src/Tests/LoanCalculator.UnitTests/LoanCalculator.UnitTests.csproj
```

Unit tests need no device. Active coverage: stamp duty bracket calculations, theme XAML key consistency.

### Run UI Tests (Appium — drives a real simulator/emulator)

```bash
cd src/Tests/LoanCalculator.UITests
npm install && npm run drivers          # first time only
./run-uitests.sh --ios  --no-build --filter "TestCategory=Smoke"
./run-uitests.sh --android --no-build --filter "TestCategory=Smoke"
```

**One C# suite drives both platforms** — tests address the UI by `AutomationId` and the
driver layer resolves it per platform. Never add platform conditionals in `Tests/`; put any
platform difference in `Infrastructure/`.

The mapping is **not symmetric**, which is the single biggest gotcha: on iOS `AutomationId`
becomes `accessibilityIdentifier` (queried as accessibility-id / `@name`), but on Android it
becomes the view's **`resource-id`**, reported package-prefixed as
`com.pj.loan.afford.calc:id/<AutomationId>`, and `contentDescription` is **not set at all**.
Querying Android by accessibility-id therefore matches nothing.

The suite is opt-in via `UITEST_PLATFORM`, so a solution-wide `dotnet test` skips it rather
than failing.

- `src/Tests/LoanCalculator.UITests/README.md` — setup, running, architecture, writing a test
- `src/Tests/LoanCalculator.UITests/TEST-CASES.md` — every test case: objective, steps, expected
  result, how to run it alone on each platform, and what to check when it fails
- `src/Tests/LoanCalculator.UITests/TROUBLESHOOTING.md` — **read this before debugging a
  failure**; it is organised by symptom and covers the traps that have really cost time here

Two things to know before touching it:

- **The run script installs the app, not Appium.** The driver gets only a bundle id. If
  Appium owns installation, `noReset` beats `enforceAppInstall` and it silently tests the
  previously installed build — which presents as a broken locator.
- **Adding a test usually means adding an `AutomationId` to XAML first.** Convention is
  `<Page><Thing><Role>`, e.g. `BudgetIncomeSaveButton`, `WhatIfRateDeltaPlus`. Shell tabs
  have no `AutomationId` and are matched by title in `Pages/ShellTabs.cs`.
- **A test that still fails after you fixed the bug it reports is usually a stale build.**
  `--no-build` tests whatever was built last, so a fix that is not in that build fails as *the
  very bug it fixes* — and the assertion message, written to describe that bug, reads as proof
  the fix did not work. Compare mtimes before re-diagnosing anything:
  ```bash
  stat -f '%Sm %N' -t '%F %T' src/LoanCalculator/bin/Debug/net10.0-android36.0/*-Signed.apk
  stat -f '%Sm %N' -t '%F %T' <the file you changed>
  ```
  If the APK is older, the run proved nothing — the fix is *untested*, not disproven. The script
  warns about this for XAML and C#, but heed it: it has cost real time here twice.

### Run UI Tests in parallel (sharded across devices)

```bash
cd src/Tests/LoanCalculator.UITests
./run-uitests-parallel.sh --ios              # 2 shards (default)
./run-uitests-parallel.sh --ios --shards 3
./run-uitests-parallel.sh --both             # iOS and Android concurrently
```

**Sharding is not the first lever, and 2 is the default on purpose.** A measured 41-test iOS run
took 100 minutes — but **65 of those were three failures**, because a degraded WebDriverAgent cost
240 s per wedged command (Appium's default) and each failure hit five or six of them. That budget
is now capped at 60 s (`UITEST_WDA_CONNECTION_TIMEOUT_SECONDS`), which recovers more wall clock
than 3× sharding does. The remaining ~36 min of healthy run time is what sharding attacks: ~15 min
at 2 shards.

Raise the shard count only with the host in mind. This suite's worst failure mode is
**load-sensitive** — a stressed simulator stops completing animations, which is what strands the
app on its splash — so three simulators plus three WDAs plus three Appium servers can mean *more*
flake, not just less wall clock. Pulling the other way, the degradation is cumulative (nothing
failed in the first 44 minutes of that run) and sharding cuts restarts per WDA from 41 to ~14.
Measure at 2 before going higher.

Each shard gets its own Appium port, artifacts directory, device, and `UITEST_SHARD_INDEX` — which
offsets `wdaLocalPort` (iOS) and `systemPort` (Android). **Appium requires those ports to be unique
per concurrent session**; sharing them does not error, it surfaces as random element-not-found
failures. A shard must also never share a device: budget data persists on the device and
`TestData` latches its seeding in a static, so two shards on one device corrupt each other.

Shards are whole **fixtures**, round-robined, discovered from the `[TestFixture]` classes in
`Tests/*.cs`. Note `dotnet test --list-tests` is no use for this — it prints bare method names with
no class or namespace.

### Playground (PDF generation dry-run, no device needed)

```bash
dotnet run --project src/LoanCalculator.Playground/LoanCalculator.Playground.csproj
```

---

## Project Structure

| Project | Purpose |
|---|---|
| `src/LoanCalculator/` | .NET MAUI app — views, platform services, DI wiring, shell |
| `src/LoanCalculator.Core/` | Class library — all business logic, ViewModels, models, service interfaces, PDF, theme handler |
| `src/Tests/LoanCalculator.UnitTests/` | NUnit tests targeting `LoanCalculator.Core` only |
| `src/Tests/LoanCalculator.UITests/` | Appium UI tests — one suite, runs on both iOS and Android |
| `src/LoanCalculator.Playground/` | Console sandbox for PDF generation |

Platform-specific implementations live in `src/LoanCalculator/Platforms/{Android,iOS,MacCatalyst,Windows}/`.

---

## Architecture

### Dependency Injection

DI is wired in `src/LoanCalculator/MauiProgram.cs`. Platform services are selected with `#if ANDROID / IOS / MACCATALYST / WINDOWS`. MacCatalyst reuses the iOS implementations.

In addition to constructor injection, `ServiceLocator` (in Core) is a static wrapper around `IServiceProvider` initialised in `App.xaml.cs`. It is used inside `SharedServiceCore` and Core classes where constructor injection is impractical.

All four primary ViewModels (`LoanViewModel`, `ExpenseViewModel`, `IncomeViewModel`, `SettingsViewModel`) are registered as **singletons** and shared across pages.

### ViewModel Hierarchy

```
BasePropertyChangeModel  (INotifyPropertyChanged)
  └── BaseViewModel      (IsBusy, IsPageBusy, IsActive, IsFree)
        └── ViewModelUiBase   (CurrencySymbol, ScheduleSave/FlushPendingSave 600ms debounce, isUpdating guard)
              └── ExpenseEntryViewBaseModel  (shared add/edit form fields, HasInitialized)
                    ├── LoanViewModel
                    ├── ExpenseViewModel
                    └── IncomeViewModel
```

Key conventions:
- Property setters guard with `if (!HasInitialized) return` during load and `isUpdating` for reentrancy.
- `MarkInitializationComplete()` is called after the first data load; only then do setters fire saves and recalculations.
- `ScheduleSave(() => SharedServiceCore.SaveData(this))` in trigger methods debounces disk writes. Call `FlushPendingSave(...)` in `OnDisappearing` and after explicit add/delete.

### Data Persistence

`SharedServiceCore.SaveData<T>` is fire-and-forget (`Task.Run`, no `.Wait()`). Reads are async via `SharedServiceCore.LoadDataFile<T>()`. JSON serialisation uses `System.Text.Json` with a custom `DoubleDefaultConverter`.

Platform storage paths:
- iOS/MacCatalyst: `Environment.SpecialFolder.MyDocuments`
- Android: `Environment.SpecialFolder.LocalApplicationData`
- Windows: `%LOCALAPPDATA%/LoanCalculator/`

Named JSON files: `homeloandata.json` (Loan), `incomedata.json`, `expensedata.json`, `settingsdata.json`, `namevaluedata.json`, `themeselectdata.json`.

### Cross-Tab Data Coordination

`SharedServiceCore` holds dirty flags (`IsIncomeDirty`, `IsExpenseDirty`, `IsLoanDirty`). `BudgetView` marks income/expense dirty at each mutation site (add/update/delete, next to the `FlushPendingSave` call), and `SettingsViewModel`'s delete helpers mark dirty after clearing a JSON file — clearing the file does not touch the caches below. First-ever load is detected with a `_hasLoadedOnce` flag per view.

**`LoanViewModel.HasIncomeExpensesRecorded` is a cached flag, not a computed property**, and `IsAffordabilityAvailable` gates on it while `MonthlySurplus` gates on that in turn. So **every page that reads affordability must refresh it on appearing** — not just the Loan page:

- `LoanViewModel.RefreshIncomeExpenseSummariesAsync(income, expense)` is the single shared implementation. It re-points `IncomeSummary`/`ExpenseSummary` and recomputes the flag, and deliberately **does not** clear the dirty flags so one tab's refresh cannot rob another of the notification pass it still needs.
- `LoanView.OnAppearing` calls it via `RefreshCrossTabSummaries()`, then clears the flags and fires its notification pass.
- `WhatIfView.OnAppearing` calls it before `SetLoanViewModel()` (which triggers `Recalculate()`), because the affordability stress test reads `IsAffordabilityAvailable`/`MonthlySurplus`. Without this, going Budget → What If computed the stress test from a stale cache in *both* directions: the unlock prompt after income/expenses were added, stale results after they were deleted.

**Who owns the data, and why the read order matters.** `BudgetViewModel.Income`/`.Expense` are **its own instances**, not the DI singletons that `LoanView`/`WhatIfView` inject (whose pages are commented out of the shell, so they are never initialised). The Budget copies are what the user actually edits, so `RefreshIncomeExpenseSummariesAsync` reads them in this order:

1. `BudgetViewModel.Income`/`.Expense` when `HasInitialized` — the authoritative in-memory copy
2. the injected DI singletons when `HasInitialized` — legacy `IncomeView`/`ExpenseView` path
3. a snapshot from disk — only before any tab has loaded

**Never reorder that to read disk first.** `SharedServiceCore.SaveData` is fire-and-forget (`Task.Run`, nothing can await it), so reading income/expense back from disk is a race by construction — the test suite would go intermittently red with no app change. Reading the in-memory copy is both current and deterministic.

The corollary: **deleting data must empty the in-memory copy too, not just the JSON.** `SettingsViewModel.DiscardDeletedData` calls `TransactionRecords.DeleteAll()` on the Budget instances and marks the flags dirty. Without that, a delete leaves Budget still listing the rows and Loan/What If still computing affordability from them. Deleting *loan* data deliberately clears only the file — blanking the live loan form mid-session would wipe inputs the user can still see.

### Premium / Trial

`SharedServiceCore.TESTING_PREMIUM_OVERRIDE = true` currently bypasses all trial restrictions. **Comment this out before any App Store release.** Premium status is stored in `SecureStorage` under key `"IsPremium"`. `IsPremiumUser()` has both a sync overload (uses `.GetAwaiter().GetResult()` — safe only when the override is active) and `IsPremiumUserAsync()`.

### Australian mode is retired from the UI

The Australian-mode switch and its dependent stamp-duty switch are **hidden** in Settings
(`IsVisible="False"`), and `SettingsViewModel.LoadAustralianModeSetting` resets the stored
preference to false — so a device that had it on is brought back off rather than left in a state
the user can no longer see.

**The feature code and the eight per-state stamp duty tables are kept, with their unit tests.** The
reason for retiring it is maintenance, not correctness: jurisdiction-specific tax rules need
updating on every rule change, and this app is global. To bring it back, restore the two Settings
rows and delete the reset in `LoadAustralianModeSetting`.

### Theme System

Four themes: `Dark` (default), `Light`, `Forest`, `Warm`. Theme XAML files are **EmbeddedResources** in the main app assembly at `src/LoanCalculator/Extensions/Data/`. `ThemeHandler.LoadDefaultStyle()` loads `Theme.CommonStyles.xaml`, `Theme.CommonDataGridStyles.xaml`, and the selected theme file from embedded streams, clears all `LoanApp`-prefixed keys from `Application.Current.Resources`, and adds the new dictionaries. All views reference theme resources with `{DynamicResource LoanApp...}`.

The unit test `ThemeFiles_ShouldHave_EqualKeys` enforces that all four theme files define identical resource key sets — run it after adding any new theme key.

### Font System

Calibri was removed — it is proprietary Microsoft IP and cannot be redistributed in App Store apps. It was replaced with a selectable font system (same pattern as the WikiExtractor reference repo).

**When adding UI, always use `{DynamicResource DefaultFontFamily}` for `FontFamily`** — never `StaticResource` and never a hard-coded font name, or that control will not follow the user's choice.

- UI fonts in `src/LoanCalculator/Resources/Fonts/`: Lato, Nunito, Quicksand, Raleway, Merriweather, SourceSerif4, PlayfairDisplay, Pacifico (all `-Regular`).
- **NotoSans and OpenSans are PDF-only — never offer them in the UI picker.**
- Default is `Lato`, defined in `src/LoanCalculator.Core/Constants/RegisteredFonts.cs` (canonical list) and used as the Preferences fallback in `SharedServiceCore.GetAppFontFamilyAsync()`.

Runtime change flow: Settings picker → `SettingsViewModel.SelectedFontFamily` setter → `ChangeFontFamilyAsync` → `SharedServiceCore.SetAppFontFamilyAsync` (persists to Preferences) → `ApplyFontFamilyAsync` writes `Application.Current.Resources["DefaultFontFamily"]`, and every `DynamicResource` binding updates app-wide. On launch, `ThemeHandler.LoadDefaultStyle` → `LoadSavedFontFamilyAsync()` restores it.

### Navigation & Shell

Four-tab Shell: **Loan, Budget, What If, Settings**. Startup: `App` sets `MainPage = SplashPage`; `SplashPage` runs a hand-rolled animation sequence plus a background pre-warm, then swaps to `AppShell`.

**The splash has no entrance animation and no exit fade, deliberately.** It is composed at its resting state in `SplashPage.xaml` — nothing starts at `Opacity="0"`, scaled or offset — so it is simply visible when it renders. Duration is `max(PreWarmAsync, MinimumVisible)`, where the 700 ms floor exists only to stop a fast pre-warm turning the splash into a flicker. That is ~0.7–1 s, down from ~2.9 s.

Both animations were removed on 2026-10-06 because they cost ~1.8 s and bought nothing:

- The entrance ran every element in from `Opacity="0"`, so **~1.55 s elapsed before the pre-warm was even started**.
- The exit `FadeTo(0)` **could not cross-fade into anything.** `NavigateToShell` sets `Windows[0].Page` — an instant swap — and it runs *after* the fade. Those 280 ms faded the splash to blank before a hard cut. The dark brand gradient dissolving to the bare window is what a "washed-out splash" screenshot actually shows.

Removing them also removed a hang class. **Never `await` a MAUI animation on the startup path.** The platform animator may render an animation and never signal its task, so the await blocks forever and `NavigateToShell()` is never reached — the same mechanism as the Android `animator_duration_scale=0` hang below, and **not Android-specific**: the iOS suite hit it stuck on a fully-faded splash after the theme tests cycled `terminate_app`/`activate_app` a dozen times.

Two wrong shapes already tried, so they are not re-tried:

1. **Outer deadline only.** A single 6 s `SplashDeadline` around the whole sequence does bound the hang, but the hang was in the *last* step, so it converts "stuck forever" into "always waits 6 s on a blank splash" — strictly more visible to the user.
2. **Per-animation caps.** Racing each animation against its own duration works, but it keeps 1.8 s of animation nobody asked for on the critical path.

What remains: `SplashDeadline` (6 s) bounds `PreWarmAsync` only. The dots pulse is the one surviving animation — fire-and-forget, with a 30 s hard lifetime.

**Third wrong shape, and the worst: capping the dots loop.** Racing each dot's fade against a timeout looks like an improvement (a lost completion would otherwise freeze the dots mid-pulse). It is not. When completions never arrive, the loop abandons the in-flight animation and starts a new one every ~340 ms, three dots, without end — and every abandoned MAUI animation stays registered with the animation ticker. An 18-minute run accumulated roughly **9,000** of them, which starves the UI thread.

That **defeats `SplashDeadline`**, because it is reached by `await Task.WhenAny(...)` with the UI `SynchronizationContext` captured: the continuation can only run on that thread, so a flooded thread means `NavigateToShell()` never runs. The cap destroyed the very deadline meant to rescue the hang, and turned a benign stall into a permanent one. Diagnosis: splash stuck at **full opacity** (washed-out means the old exit fade, which no longer exists) with the deadline never firing.

So: **cosmetic animation must never compete with the handoff.** A bare `await` on a dead animation parks the dots loop on one pending task, which costs nothing. Earlier revisions of this file claimed a "3 s timeout" that was never in the code.

No push navigation and no registered routes. The only navigation is **modal**, pushed from `LoanView` with `Navigation.PushModalAsync`, for the two data-entry pages below. Each page exposes a `Task<bool> Completion` that the launcher awaits, so the caller can react after dismissal without the page knowing anything about LoanView.

A modal page must handle **every** dismissal route or its launch button dies silently for the session: the explicit button, `OnBackButtonPressed` (Android BACK), `OnDisappearing` as the fallback for the iPad/MacCatalyst sheet swipe, and a `finally` that clears the launcher's re-entry latch. `LoanDetailsPage` does all four — copy that shape for any new modal.

`IncomeView` and `ExpenseView` still exist in the repo but their `ShellContent` entries are
**commented out** in `AppShell.xaml` — they were merged into `BudgetView`. They are therefore
unreachable, which is *why* the DI singleton `IncomeViewModel`/`ExpenseViewModel` normally report
`HasInitialized == false`: nothing initialises them. Do not assume those singletons hold live data
(see **Cross-Tab Data Coordination**).

### Data entry lives on one page, not popups

**`View/LoanDetailsPage.xaml` + `WizardViewModel` (Core) is the single data-entry page.** Opened from the ⚡ `FabWizard`, by tapping the asset / deposit / loan figures on the Asset tab (three `OnAssetValueTapped` sites), and auto-launched on first run after the disclaimer.

It replaced two overlapping pages — Quick Setup (`WizardPage`) and Quick Input (`QuickInputPage`) — which were themselves `SfPopup` + `DataTemplate` blocks inside `LoanView`. Those were moved out because every defect under **Platform Gotchas** involves `SfPopup`; the extraction removed ~840 lines from `LoanView` and, because `x:Name` works outside a `DataTemplate`, deleted the `Loaded=`-capture-into-backing-field pattern both had needed.

**The organising rule: the page shows only figures you can change there.**

| Section | Fields | Behaviour |
|---|---|---|
| A — always shown | asset, deposit, loan amount | **Writes the model live.** No other screen lets you type these; the Asset tab shows them read-only. |
| B — only while empty | upfront costs, running cost, income, expenses | **Deferred to Apply.** Each has its own editing screen, so once a value exists the field is hidden and `WizardInheritedNote` names it in muted text instead. |

`IsVisible` on Section B binds the `Wizard*NeedsInput` inverses. Note `WizardUpfrontNeedsInput` reads `OtherExpenseTotalAmount`, which includes auto-computed stamp duty — so that field hides as soon as an asset price exists when stamp duty is active.

**Commit order is load-bearing — see `WizardViewModel.Commit()`.** Expenses, then the asset, then **exactly one** side of the deposit/loan split last, chosen by `LastEditedSplit`. The model has two degrees of freedom, not three (see **Cost increases grow the loan** below), so whichever amount is written last wins.

**The page does not bind `Entry.Text`, and must not start.** The handlers write `entry.Text` themselves, and the model rewrites the other two figures on every change, so a binding would be a competing write path. Grouping is applied on `Unfocused` only — reformatting mid-keystroke moved the caret backwards on Android, because the native `EditText` applies replacement text asynchronously and the caret index is computed against the old length. `BudgetView`'s amount entries use the same pattern.

### Cost increases grow the loan, never the deposit

`HomeLoanInformation` stores a total (`PropertyAmount + OtherExpenseTotalAmount`) plus **one split point**. Every amount setter rewrites both sides via `ProcessDepositCalc`, so asset/deposit/loan can never be set independently.

`LoanViewModel.PropertyAmount` and all six expense setters force a recompute by self-assigning **`HomeLoanInfo.DepositAmountDirectInput`** — preserving the deposit and re-deriving the loan. **Do not change these to self-assign the loan.** They used to, which meant the loan never moved and every cost rise was credited to the user's savings:

```
asset 600k, deposit 100k, loan 500k  →  +20k upfront  →  deposit 120k, loan 500k   ✗
                                     →  +50k asset    →  deposit 170k, loan 500k   ✗
```

The deposit is money the user has; the loan is what they must borrow. Three tests in `LoanViewModelTests` pin this (`Raising*_GrowsTheLoanAndLeavesTheDepositAlone`).

### Horizontal page inset is 12, set in one place per page

Every page and tab container uses `Padding="12,0"` (or `12,…` where vertical differs) and **cards carry no horizontal margin** — `ExpanderCardStyle` is `Margin="0,5"`, vertical only. Container padding and card margin used to stack, giving 16–32pt per side depending on the screen. If the inset needs changing, change the container value; do not add margins to cards.

**Before zeroing a card's margin, confirm the container actually has the padding.** Settings had
none — its content `VerticalStackLayout` was bare and the inset lived *entirely* in each card's
`Margin="16,0"`. Zeroing those to honour the rule above left every card flush to both screen
edges. `SettingsView.xaml:196` now carries the `Padding="12,0"`, which is what makes the rule true
there rather than assumed.

Two other things that look like the same attribute but are not:

- A `BoxView` separator's `Margin="16,0"` **inside** a card is row alignment — it matches the
  `Padding="16,10"` of the rows it divides and has nothing to do with the page edge. Settings has
  16 of these and 6 real cards, all reading `Margin="16,0"`; a single find-and-replace hits both.
- Section header labels sit 4pt further in than the cards (`Margin="4,…"` on top of the
  container's 12 = 16). That offset is deliberate, so it has to be re-derived, not preserved
  verbatim, whenever the container value changes.

An indentation-based ancestor walk is not good enough to audit this — multi-line attributes and
closed-but-indented siblings both mislead it. Parse the XAML and sum `Padding` down the real
element tree, skipping `SfListView`/`DataTemplate` subtrees (list items are inset by the list).

### SfPopup ContentTemplate Binding

Still relevant for the popups that remain (`AddAssetExpensePopup`, `UpfrontCostsPopup` on the Loan page, and the Budget add/edit popups).

`SfPopup.ContentTemplate` DataTemplate does **not** inherit the page's `BindingContext` automatically. Always set it explicitly on the root element inside the template:

```xml
<sfPopup:SfPopup.ContentTemplate>
    <DataTemplate>
        <VerticalStackLayout
            BindingContext="{Binding Source={x:Reference thisPage}, Path=BindingContext}">
```

This requires `x:Name="thisPage"` on the `ContentPage`. Without this, `SfComboBox` and interactive controls inside the popup will not bind correctly.

`SfComboBox` inside `SfTextInputLayout` inside `SfPopup` does not render its selected value text — use `SfComboBox` directly (with a plain `Label` above it) instead. Also set `AutoSizeMode="Height"` on the `SfPopup` so content is not clipped.

### SfNumericEntry Quirk

`SfNumericEntry.ValueChanged` fires on Enter, spin button, or **focus change only** — not on every keystroke. Inside `SfPopup`, tapping Save before unfocusing the field means the new value is never written back. Use a plain `Entry` with `Keyboard="Numeric"` and bind to a string property (`IncomeEntryAmountText`) that parses to `double` on set. This is already in place for the add/edit forms.

---

## Platform Gotchas (hard-won — read before debugging a platform oddity)

### Syncfusion iOS drawing overlays swallow taps

**Symptom:** a Syncfusion control does nothing when tapped on **iOS only** — no error, no exception. Setting `SelectedIndex` programmatically works, and controls *inside* the content work, so only touch delivery to the control's own chrome is broken.

**Cause (Syncfusion.Maui 33.2.6 defect).** These controls add a drawing layer (`LayoutViewExt` / `PlatformGraphicsViewExt` wrapping a `NativePlatformGraphicsView`) as the **last sibling**, covering the same rect as the real interactive view. On iOS last sibling = topmost, and the layer has `UserInteractionEnabled = true` but **zero gesture recognizers** — so iOS hit-testing stops there, accepts the touch, and handles nothing. Android dispatches touches differently, hence iOS-only.

**Fix:** `src/LoanCalculator/Extensions/SyncfusionIosTouchFix.cs` walks the native subtree and sets `UserInteractionEnabled = false` on any such overlay that has no gesture recognizers. `ApplyToTabView` (depth ≤ 3, height ≤ 120 so tab *content* charts/grids are untouched) and `ApplyToSegmentedControl` (scoped to that control's subtree — charts must keep graphics-view touches for tooltips). Called from `OnAppearing` in `LoanView.xaml.cs` and `BudgetView.xaml.cs` under `#if IOS || MACCATALYST`. **Re-check this on any Syncfusion upgrade** — it may become unnecessary, or the internal class names may change.

Known still-unwired: `UpfrontAustraliaStates` (the other `SfSegmentedControl`, `LoanView.xaml:449`) lives inside a popup `DataTemplate`, so XAML generates no code-behind field. If it cannot be toggled, walk the visual tree when the popup opens.

**Third instance — `SfExpander` (fixed 2026-10-06), and the one that breaks the pattern.** Tapping an expander header did nothing on the Loan page while Settings' expanders worked, with *identical* markup and *identical* native trees. The measured tree of a collapsed expander:

```
LayoutViewExt (402x30)                      expander root
| LayoutViewExt (402x30) gestures=4         header — the real tap target
| LayoutViewExt (402x0)  gestures=0         content, zero-height while collapsed
| LayoutViewExt (402x30) gestures=5         ← drawing layer, LAST sibling = topmost
| | NativePlatformGraphicsView  touch=False
```

**The important lesson: gesture-recognizer count is NOT a reliable test for "decorative" in Syncfusion's iOS views.** The tab-strip and segmented-control overlays carry zero recognizers, so both earlier fixes match on `GestureRecognizers.Length == 0`. This overlay reports **five** — so every matcher built on that assumption skipped the one view that mattered, across three attempts. `ApplyToExpanders` therefore matches on *structure*: a layout wrapper whose **only child is a graphics view**. It inspects only the expander's **direct subviews**, so content charts and grids keep the touches they need.

Two dead ends recorded so they are not repeated: the Settings/Loan asymmetry looks like proof this is *not* the overlay defect — it is not proof, the overlay is present on both pages. And `ApplyToTabView` is not to blame for it; a dump showed every view still `touch=True`.

**How this was actually found, after three failed guesses from reading code:** `window.HitTest` at the header centre, plus a dump of the hit view's ancestor chain and a reference check for whether the hit was even inside the expander. The ancestry named the interceptor in one run. Reach for that probe early — it is in the git history of `SyncfusionIosTouchFix.cs` if it is needed again.

**Debugging technique that cracked it:** `UIWindow.HitTest(point, null)` to ask iOS which view *would* receive the touch, plus a recursive subview dump logging class / frame / `UserInteractionEnabled` / recognizer count. Note `run-ios.sh`'s log filter only passes lines matching `error|Error|exception|…`, so plain `Console.WriteLine` is invisible unless you use `--logs` or add your tag to the filter. **Also: probing at `OnAppearing` shows inner views with zero frames** because layout has not happened — a measurement artifact, not the bug. Re-probe a few seconds later.

**Wrong turns not worth repeating:** "`NavBarIsVisible="False"` puts headers under the status bar" is FALSE (MAUI already applies the iOS safe-area inset); a lingering full-screen `SfPopup` overlay was ruled out.

### Syncfusion controls that *draw* their content are invisible to accessibility

Because the value is painted into a graphics view rather than rendered as a native text node, **no automation tool can read it**:

- A collapsed `SfComboBox` exposes no text child on iOS. Its accessibility label reports control *state* (literally `"Drop down button pressed"`), and its subtree holds only `<AutomationId>_DownButtonView`.
- It can be *driven* but not *read back* (the drop-down's rows ARE real accessible nodes). **Assert on what a selection changes, not on what the combo displays.**
- On **Android** the drop-down never opens under UiAutomator2 at all, so theme/currency selection cannot be driven there — the UI tests skip those cases with a stated reason. Reading the value *does* work on Android, from an `android.widget.EditText` child.
- Expect the same for charts (`SfCartesianChart` is in the tree but carries no readable value).

### iOS: you cannot type into any field — check the simulator keyboard FIRST

**Before suspecting a single line of app code**, check this:

```bash
defaults read com.apple.iphonesimulator ConnectHardwareKeyboard   # 0 means your Mac keyboard sends NOTHING
```

With it `0`, typing on the Mac keyboard does nothing in any field, in any page, anywhere in the
app. Fix: focus Simulator and press **⇧⌘K** (I/O → Keyboard → Connect Hardware Keyboard) — takes
effect immediately, no restart.

**This cost most of a day.** It presents as a convincing app bug and defeats code reasoning,
because every observation fits an app-side cause:

- The field focuses normally and shows the iOS **Paste / Select All / AutoFill** menu, because that
  menu only needs first responder, not a keyboard.
- A `Focused` event fires; no `TextChanged` ever does.
- `IsReadOnly`, `IsEnabled` and `InputTransparent` all read correct.
- **Android works perfectly**, because the Android emulator maps the host keyboard by default — so
  it looks exactly like an iOS-only platform defect, of which this repo genuinely has several.
- Other controls on the same page (sliders, buttons) work, so touch delivery is clearly fine.
- Appium tests pass, because `EnterText` injects via XCUITest and never needs a keyboard — so the
  suite cannot catch it and "the tests pass" does not mean a human can type.

Sibling trap to `animator_duration_scale` below: both are **simulator/emulator settings that
masquerade as app bugs** and survive reinstalling the app. When input or animation behaves
impossibly, suspect the device configuration before the code, and **measure before theorising** —
instrument a `TextChanged`/`Focused` handler and look at the log rather than reasoning from source.

### Android: `animator_duration_scale = 0` hangs the app on the native splash

**Not an app bug.** MAUI's `FadeTo`/`ScaleTo`/`TranslateTo` are driven by the platform animator; at scale 0 they never signal completion, so `SplashPage.RunSplashAsync` never finishes awaiting, `NavigateToShell()` never runs, and no frame is ever rendered.

Signature: stuck on the **native** splash (not the MAUI one); `adb shell dumpsys window | grep mCurrentFocus` → `null` while `mFocusedApp` *is* MainActivity; main thread logs `Explicit concurrent mark compact GC freed ~64KB` every ~1.6 s forever (that is `AnimateDotsAsync` spinning); Appium then fails every lookup with `Timed out waiting for the root AccessibilityNodeInfo`, because an unfocused window exposes no accessibility tree.

Fix all three in one go — checking only `animator_duration_scale` has missed this twice:

```bash
for s in animator_duration_scale window_animation_scale transition_animation_scale; do
  adb shell settings put global $s 1.0
done
```

**These settings are global** — they survive reinstall, uninstall and emulator reboot, so the app looks permanently broken long after whatever set it. Ways it gets set: manually as a "make tests less flaky" step; Appium's `appium:disableWindowAnimation=true`, which zeroes all three (deliberately **not** used — see the comment in `AppDriver.BuildAndroidOptions`); an emulator snapshot captured mid-test-run; and, until 2026-10-06, `run-uitests.sh` itself.

**The script bug worth knowing about, because it latched.** `run-uitests.sh` zeroes the two *system* scales for a run and restores them on exit. It used to capture the current value as "the original" — so after any run that died without its trap firing (SIGKILL, closed terminal, emulator snapshot), the scales were already 0, the next run recorded `0` as the original, and dutifully restored `0`. Once corrupted it stayed corrupted and every later run re-confirmed it. It now **repairs all three to 1.0 on startup if any is zero, and always restores 1.0** rather than a captured value. A corrupted device therefore heals itself on the next run.

### Android build: stale `obj/` referencing a dropped ABI

`error XAWAS7023 DirectoryNotFoundException … android/assets/x86_64/he/Microsoft.Maui.Controls.resources.dll` — delete the Android `obj/` and `bin/` subdirectories and rebuild. Worth trying whenever an Android build fails inside `MonoAndroidHelper`.

---

## Key Files Quick Reference

| File | What it does |
|---|---|
| `src/LoanCalculator/MauiProgram.cs` | DI registration, Sentry, Syncfusion license |
| `src/LoanCalculator/App.xaml.cs` | Global exception handlers, purchase restore, splash handoff |
| `src/LoanCalculator.Core/Services/SharedServiceCore.cs` | Static helpers: SaveData, LoadDataFile, premium check, dirty flags, currency list |
| `src/LoanCalculator.Core/Models/ViewModels/ExpenseEntryViewBaseModel.cs` | Shared add/edit form state (entry fields, frequency, amount text, add/update logic) |
| `src/LoanCalculator.Core/Themes/ThemeHandler.cs` | Theme loading from embedded resources |
| `src/LoanCalculator/Extensions/Data/Theme.CommonStyles.xaml` | Shared styles + typography token system (`FontSizeSmall/Body/Medium/Large/Heading`, `MarginSection`) |
| `src/LoanCalculator/run-ios.sh` | iOS simulator build + launch script |
