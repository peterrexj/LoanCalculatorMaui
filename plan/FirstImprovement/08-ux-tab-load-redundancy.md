# 08 — UX: Redundant OnAppearing Data Reloads

**Priority:** High  
**Status:** ✅ DONE  
**Symptoms:** Every time the user taps back to the Loan/Income/Expense tab, the whole data set reloads from disk — causing a visible flash, charts re-animating, and the scroll position resetting.

---

## What Was Done

- `_hasLoadedOnce` flag in `LoanView` — first appearance does full `LoadDataSet`, subsequent appearances do nothing unless dirty flags are set.
- Dirty flags (`IsIncomeDirty`, `IsExpenseDirty`, `IsLoanDirty`) added to `SharedServiceCore` with `Mark*` / `Clear*` helpers.
- `LoanView.OnAppearing` checks `IsIncomeDirty || IsExpenseDirty` and calls `RefreshCrossTabSummaries()` (targeted reload) instead of full `LoadDataSet`.
- `RefreshCrossTabSummaries()` prefers live in-memory singleton ViewModels; falls back to disk only if the tab hasn't been visited yet (`HasInitialized == false`).
- `LoanView.OnDisappearing` calls `MarkLoanDirty()` so Income/Expense tabs can react if needed.
- Scroll position preservation not yet implemented — still resets on re-appear.

---

## Problem

All three views (`LoanView`, `IncomeView`, `ExpenseView`) call their full `LoadDataSet()` method on **every `OnAppearing`**. This reloads from disk, re-runs calculations, re-fires all `OnPropertyChanged` notifications, and re-renders the entire view — even if nothing has changed since the last visit.

This means:
- Tapping Income → tapping back to Loan → the Loan tab reloads everything.
- If the user entered income data, it correctly shows in Loan after reload. But if they just switched tabs momentarily, the full reload is unnecessary.
- `SfDataGrid` (amortization table) and Syncfusion charts re-animate on every tab return.

### Root cause

MAUI's `ShellContent.ContentTemplate="{DataTemplate ...}"` creates pages lazily on first tab and then **keeps them alive** in memory. But because MAUI fires `OnAppearing` on every tab switch (not just first load), the full `LoadDataSet()` runs every time.

---

## Fix

### Strategy — Track what needs reloading

Add a dirty-flag mechanism to `SharedServiceCore` that views can check. A tab only needs to reload its cross-tab data (Income/Expense) when **another tab has actually saved new data** since this tab last loaded.

```csharp
// SharedServiceCore.cs — add dirty flags
public static bool IsExpenseDataDirty { get; private set; }
public static bool IsIncomeDataDirty { get; private set; }
public static bool IsLoanDataDirty { get; private set; }

public static void MarkExpenseDirty() => IsExpenseDataDirty = true;
public static void MarkIncomeDirty() => IsIncomeDataDirty = true;
public static void MarkLoanDirty() => IsLoanDataDirty = true;

public static void ClearExpenseDirty() => IsExpenseDataDirty = false;
public static void ClearIncomeDirty() => IsIncomeDataDirty = false;
public static void ClearLoanDirty() => IsLoanDataDirty = false;
```

Call `MarkExpenseDirty()` inside `ExpenseViewModel` when a transaction is added/deleted. Call `MarkIncomeDirty()` in `IncomeViewModel` similarly.

### In `LoanView.OnAppearing`

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();

    if (!_hasLoadedOnce)
    {
        // First load — always do a full load
        _hasLoadedOnce = true;
        await LoadDataSet();
    }
    else if (SharedServiceCore.IsExpenseDataDirty || SharedServiceCore.IsIncomeDataDirty)
    {
        // Cross-tab data changed — reload only the summary data, not the full ViewModel
        await RefreshCrossTabSummaries();
        SharedServiceCore.ClearExpenseDirty();
        SharedServiceCore.ClearIncomeDirty();
    }
    // else: nothing changed — no reload, no flicker
}

private bool _hasLoadedOnce = false;

private async Task RefreshCrossTabSummaries()
{
    _viewModel.ExpenseSummary = await SharedServiceCore.GetExpenseSummaryAsync();
    _viewModel.IncomeSummary = await SharedServiceCore.GetIncomeSummaryAsync();
    _viewModel.HasIncomeExpensesRecorded =
        _viewModel.ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 &&
        _viewModel.IncomeSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;
    _viewModel.TriggerPropertyChangedOnPageLevel();
}
```

Apply the same pattern to `IncomeView` and `ExpenseView` — they only need to reload if the user made a change in `LoanView` that affects projection inputs (which is currently not tracked — mark `IsLoanDataDirty` on relevant `LoanViewModel` changes).

### Preserve scroll position and tab selection

MAUI Shell keeps pages alive but does reset `ScrollView` position on re-layout. Add `Padding` and scroll anchoring:

```xml
<!-- In LoanView.xaml — the main ScrollView -->
<ScrollView x:Name="MainScrollView">
```

```csharp
// Save and restore scroll position across OnDisappearing/OnAppearing
private double _savedScrollY = 0;

protected override void OnDisappearing()
{
    base.OnDisappearing();
    _savedScrollY = MainScrollView.ScrollY;
}

protected override async void OnAppearing()
{
    // ... load logic ...
    if (_hasLoadedOnce && !dataReloaded)
    {
        await MainScrollView.ScrollToAsync(0, _savedScrollY, false);
    }
}
```

---

## Verification

1. Open app → navigate to Loan tab (full load).
2. Tap Income tab → tap back to Loan tab → **no flash, no chart re-animation**, scroll position preserved.
3. Add an income entry → tap back to Loan → Affordability value updates (dirty flag caused a targeted refresh).
4. Add a loan expense → tap to Income tab → no reload (loan changes don't dirty income data).
