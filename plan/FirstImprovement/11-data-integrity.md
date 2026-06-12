# 11 — Data Integrity: Save/Load Ordering and Stale Cross-Tab Data

**Priority:** Critical  
**Status:** ✅ DONE  
**Symptoms:** Data entered on one tab not reflected on another; data appears to reset after app restart; incorrect affordability calculation shown (using stale income/expense from a previous session).

---

## What Was Done

- **Fix A (live VM injection) — DONE:** `LoanView` now injects `IncomeViewModel` and `ExpenseViewModel` singletons. `LoadDataSet` and `RefreshCrossTabSummaries` prefer the in-memory singletons; falls back to disk only when `HasInitialized == false`.
- **Fix B (CopyPropertiesFrom replaced) — DONE for LoanViewModel:** `LoanViewModel.CopyPropertiesFrom` now directly assigns `_homeLoanInfo` and `TransactionRecords` backing fields — no reflection, no property setter side-effects during load. `ExpenseViewModel` and `IncomeViewModel` still use reflection-based `CopyPropertiesFrom`.
- **Fix C (PageHelper guard normalized) — DONE:** `PageHelper.PageIsLoading()` / `PageLoadingComplete()` now form a matched try/finally pair in `LoadDataSet`. Redundant calls removed.
- **Fix D (FlushPendingSave on navigate away) — DONE:** `LoanView.OnDisappearing` calls `FlushPendingSave`.

### Remaining: `ExpenseViewModel.CopyPropertiesFrom` / `IncomeViewModel.CopyPropertiesFrom` still use reflection

These are fine for now since income/expense load paths don't have the same derived-property side-effect problem as `LoanViewModel`. Convert to explicit field assignment if data integrity issues are observed on those tabs.

---

## Problems

### Issue A — `ExpenseSummary` / `IncomeSummary` loaded from disk, not from the live ViewModel

In `LoanView.LoadDataSet()`:

```csharp
var expenseSummaryTask = Task.Run(() => SharedServiceCore.ExpenseSummary);
var incomeSummaryTask = Task.Run(() => SharedServiceCore.IncomeSummary);
```

`SharedServiceCore.ExpenseSummary` reads the **saved JSON file** for `ExpenseViewModel`, not the in-memory `ExpenseViewModel` singleton. If the user added expense entries earlier in the same session but the debounced save hasn't flushed yet (see plan 02), the Loan tab will show old data.

### Issue B — Stale data window between save and cross-tab read

The save is fire-and-forget (after plan 01 fix). If the user: enters an expense → switches immediately to Loan tab → the Loan tab loads the cross-tab summary before the expense save completes. The affordability calculation is wrong until next load.

### Issue C — `CopyPropertiesFrom` bypasses property setters

In `LoadDataSet`, when data is loaded from disk:

```csharp
_viewModel.CopyPropertiesFrom(data);
```

This uses reflection to copy all non-`[JsonIgnore]` properties, **bypassing all property setters**. This means:
- `isUpdating` and `HasInitialized` guards are not respected
- Computed side effects triggered by setters (stamp duty recalculation, chart update) do not run
- The ViewModel ends up in a state where stored values are set but derived values are stale

### Issue D — `PageHelper.PageIsLoading` / `PageLoadingComplete` called multiple times

`LoadDataSet` calls `PageHelper.PageIsLoading()` at the start and `PageHelper.PageLoadingComplete()` twice (in the main body and in `finally`). The `finally` in `OnAppearing` also calls `PageLoadingComplete()`. This triple-call means the loading guard (`IsFormLoading`) may be turned off before loading actually completes.

---

## Fix

### Fix A — Read from live ViewModel when available, fall back to disk

Inject `ExpenseViewModel` and `IncomeViewModel` directly into `LoanView` (they are already registered as singletons in DI). Use the in-memory singleton instead of re-reading from disk:

```csharp
// LoanView.xaml.cs — inject the live ViewModels
public LoanView(
    IErrorHandlingService errorHandlingService,
    LoanViewModel viewModel,
    IncomeViewModel incomeViewModel,
    ExpenseViewModel expenseViewModel,
    IThemeHandler themeHandler)
{
    _incomeViewModel = incomeViewModel;
    _expenseViewModel = expenseViewModel;
    // ...
}

// In LoadDataSet:
_viewModel.ExpenseSummary = _expenseViewModel;
_viewModel.IncomeSummary = _incomeViewModel;
```

This eliminates the disk read entirely for cross-tab summaries during a session. On first cold launch (before any data is loaded), the ViewModel singletons are still populated from disk during their own `OnAppearing` — which happens before `LoanView.OnAppearing` in shell tab ordering.

If the Income/Expense tabs have not been visited yet in this session (ViewModel is still empty), fall back to reading from disk once:

```csharp
if (_incomeViewModel.HasInitialized)
    _viewModel.IncomeSummary = _incomeViewModel;
else
    _viewModel.IncomeSummary = await SharedServiceCore.GetIncomeSummaryAsync();
```

### Fix B — Replace `CopyPropertiesFrom` with explicit initialization

After disk load, instead of blind reflection copy, call a purpose-built method that respects initialization order:

```csharp
// In LoanViewModel — replace CopyPropertiesFrom usage with:
public void LoadFromSaved(LoanViewModel saved)
{
    // Set the underlying model objects directly (no setter side effects needed during load)
    _homeLoanInfo = saved._homeLoanInfo ?? new HomeLoanInformation { ... };
    TransactionRecords = saved.TransactionRecords ?? new Incomes { IncomeExpenseEntries = [] };
    // Copy other persisted scalar fields...

    // After all values are set, trigger a single full recalculation
    MarkInitializationComplete();
    TriggerPropertyChangedOnPropertyTab();
    SyncAmortization();
}
```

This is safer than reflection and makes the initialization sequence explicit.

### Fix C — Normalize `PageHelper` guard usage

`PageHelper.PageIsLoading()` and `PageLoadingComplete()` should form a matched pair per operation. Currently there are 3 calls to `PageLoadingComplete()` for 1 call to `PageIsLoading()`:

```csharp
// Correct pattern — single entry, single exit via finally
private async Task LoadDataSet()
{
    PageHelper.PageIsLoading();
    try
    {
        // ... all loading work ...
    }
    finally
    {
        PageHelper.PageLoadingComplete();  // Only here
    }
}
```

Remove the redundant `PageHelper.PageLoadingComplete()` call in the body of `LoadDataSet` (line ~142) and the one in `OnAppearing.finally`. Only the `LoadDataSet.finally` should call it.

### Fix D — Force-save before cross-tab reads

When a save is pending (debounced, not yet flushed), and the user navigates away, trigger an immediate save:

```csharp
// In each view's OnDisappearing
protected override void OnDisappearing()
{
    base.OnDisappearing();
    _viewModel.FlushPendingSave();  // cancels debounce timer, saves immediately
    SharedServiceCore.MarkViewModelDirty<ExpenseViewModel>(); // or Income/Loan
}
```

`FlushPendingSave` in `ViewModelUiBase`:

```csharp
public void FlushPendingSave()
{
    _saveCts?.Cancel();
    SharedServiceCore.SaveData(this);  // immediate, non-debounced
}
```

---

## Verification

1. Enter a property value → enter income → switch to Loan tab → Affordability shows the correct income figure (not zero or stale).
2. Restart the app — all data restored correctly.
3. Force-quit the app immediately after typing a value (no tab switch) — restart shows the value (OnDisappearing flush saved it).
4. Load with no saved data → app shows default values and does not crash.
