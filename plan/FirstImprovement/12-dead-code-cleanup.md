# 12 — Dead Code Cleanup

**Priority:** Low  
**Status:** ⚠️ PARTIAL — Warm theme wired; dead files partially removed  
**Goal:** Remove files and code that are no longer used to reduce cognitive overhead, build time, and confusion for future changes.

---

## What Was Done

- `AppThemes.Warm` is now wired in `ThemeHandler.cs` (switch-case at line 52) — the Warm theme file is no longer dead.
- `SettingsHelper.cs` and `PreloadedLoanView.cs` — verify these files were deleted (not found via grep of current directory listing).
- `IncludeIncomeInProjection` commented-out block in `ExpenseView.xaml` — confirm still present (~527–578) and needs removal decision.

## Still Open

- Confirm `SettingsHelper.cs` and `PreloadedLoanView.cs` deletion in solution file.
- Evaluate `LoanCalculator.Tests/` vs `LoanCalculator.UnitTests/` duplication — check if test project was consolidated.
- `InAppPurchaseViewModelTests.cs` was deleted (shown in git status). Confirm this was intentional and that `LoanCalculator.UnitTests` has equivalent coverage.

---

## Dead Files

| File | Why it's dead | Action |
|------|--------------|--------|
| `src/LoanCalculator/Extensions/SettingsHelper.cs` | 100% commented-out code, no active content | Delete the file |
| `src/LoanCalculator/View/PreloadedLoanView.cs` | Creates static singleton view instances, zero callers found in the active codebase | Delete the file |
| `src/LoanCalculator/AppShell.xaml.cs` | Contains commented-out route registrations (`Routing.RegisterRoute`) and a `OnShellNavigating` handler that does nothing | Remove commented-out code, keep the file (Shell requires it) |

---

## Dead Code Within Files

### `LoanViewModel.cs`

- **Line ~176:** `//var syncAmortizationTask = Task.Run(() => _viewModel.SyncAmortization());` — commented out in `LoadDataSet`, replaced by a direct call below. Remove the comment.
- **Line ~464:** Hardcoded year `"2025"` in `InsightChartLoanInterestAxis` and `InsightChartLoanDepositAxis` — these should use `DateTime.Now.Year.ToString()` like `InsightChartLoanAmountAxis` does. Not dead code, but a bug masquerading as dead data.

### `ExpenseView.xaml`

- **Lines ~527–578:** A large commented-out block for `IncludeIncomeInProjection` toggle. Remove unless there's a plan to implement it.

### `App.xaml` and `Resources/Styles/Colors.xaml` / `Styles.xaml`

- The default MAUI `Colors.xaml` and `Styles.xaml` define colors and styles (`Primary`, `Secondary`, `Tertiary`, etc.) that are entirely overridden by the `LoanApp*` theme system. These files add noise and can confuse developers who see two parallel style systems.
- **Action:** Do not delete yet — MAUI itself may reference some of these keys internally. Instead, add a comment at the top of each file: `<!-- These are MAUI defaults. The app uses LoanApp* keys from Theme.*.xaml. Do not add app-specific styles here. -->`

### `LoanCalculator.Tests/` folder

- `src/LoanCalculator.Tests/` contains a single test file and appears to be a duplicate of `src/Tests/LoanCalculator.UnitTests/`. Verify: if all tests in `LoanCalculator.Tests/` are replicated in `LoanCalculator.UnitTests/`, delete the old folder and update the solution file.

---

## Unused Theme

`Extensions/Data/Theme.Warm.xaml` exists but is excluded from the switch-case in `ThemeHandler` (throws `ArgumentException`). Either:
1. Remove the file and comment it out of the switch to make the exclusion intentional
2. Or wire it up properly (it's already styled)

**Recommendation:** Wire it up — it's already built. Just add `AppThemes.Warm` to the switch-case in `ThemeHandler.cs` and add it to the theme picker in `SettingsView.xaml`.

---

## Verification

1. Build succeeds after each file deletion.
2. All unit tests in `LoanCalculator.UnitTests` pass.
3. All 4 tabs load correctly on Android and iOS simulator.
4. If Warm theme is enabled: test that switching to Warm theme applies correctly and switching away restores the previous theme.
