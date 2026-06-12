# 02 — Performance: SaveData on Every Keystroke

**Priority:** Critical  
**Status:** ✅ DONE  
**Symptoms:** Typing in any numeric field causes noticeable lag; every slider drag writes to disk on each frame; battery drain on mobile; potential write corruption under rapid input.

---

## What Was Done

- `ScheduleSave(Action)` debounce helper added to `ViewModelUiBase` (600ms timer, cancels on each new call).
- `FlushPendingSave(Action)` added to `ViewModelUiBase` — called from `OnDisappearing` and after add/delete events.
- All `TriggerPropertyChangedOnPropertyTab`, `TriggerPropertyChangedOnAmortizationTab`, `RefreshExpenseTabPropertyChanged`, `RefreshInsightsTabPropertyChanged` in `LoanViewModel` now call `ScheduleSave(...)` instead of `SharedServiceCore.SaveData(this)` directly.
- `LoanView.OnDisappearing` calls `FlushPendingSave` + `MarkLoanDirty`.
- `AddNewIncome_Clicked` and `btnDeleteEntry_Clicked` in `LoanView` call `FlushPendingSave` immediately (no debounce for explicit mutations).

---

## Problem

`SharedServiceCore.SaveData(this)` is called at the end of every property setter trigger chain:

- `TriggerPropertyChangedOnPropertyTab()` — called by `PropertyAmount`, `InterestRate`, `DepositPercentage`, `LoanTermInYears`, `LoanAmountDirectInput`, `DepositAmountDirectInput`, `LoanAmountPercentage` setters
- `TriggerPropertyChangedOnAmortizationTab()` — called by amortization segment selection
- `RefreshExpenseTabPropertyChanged()` — called by expense entry changes
- `RefreshInsightsTabPropertyChanged()` — called by insights tab activation

This means **every character typed** or **every pixel of slider drag** triggers a full JSON serialization + `File.WriteAllTextAsync`. Even with the `IsFormLoading` guard, during normal use the user moving a slider fires 30–60 save calls per second.

### Affected locations

| File | Method | Approx line |
|------|--------|-------------|
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` | `TriggerPropertyChangedOnPropertyTab` | ~680 |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` | `TriggerPropertyChangedOnAmortizationTab` | ~710 |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` | `RefreshExpenseTabPropertyChanged` | ~730 |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` | `RefreshInsightsTabPropertyChanged` | ~750 |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/IncomeViewModel.cs` | `TriggerPropertyChangedOnProjectionTab` | (similar pattern) |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/ExpenseViewModel.cs` | `TriggerPropertyChangedOnProjectionTab` | (similar pattern) |

---

## Fix

Add a **debounce mechanism** in `ViewModelUiBase` (the shared base for all ViewModels). A 600ms debounce is long enough to avoid per-keystroke saves while still saving quickly after the user pauses.

### Step 1 — Add debounce helper to `ViewModelUiBase`

File: `src/LoanCalculator.Core/Models/ViewModels/ViewModelUiBase.cs`

```csharp
private CancellationTokenSource? _saveCts;

protected void ScheduleSave(Action saveAction)
{
    _saveCts?.Cancel();
    _saveCts = new CancellationTokenSource();
    var token = _saveCts.Token;

    Task.Delay(600, token).ContinueWith(t =>
    {
        if (!t.IsCanceled) saveAction();
    }, TaskScheduler.Default);
}
```

### Step 2 — Replace direct `SaveData` calls with `ScheduleSave`

In `LoanViewModel.TriggerPropertyChangedOnPropertyTab()` and similar trigger methods:

```csharp
// Before
private void TriggerPropertyChangedOnPropertyTab()
{
    // ... ~20 OnPropertyChanged calls ...
    SharedServiceCore.SaveData(this);
}

// After
private void TriggerPropertyChangedOnPropertyTab()
{
    // ... same OnPropertyChanged calls — unchanged ...
    ScheduleSave(() => SharedServiceCore.SaveData(this));
}
```

Apply the same pattern to:
- `TriggerPropertyChangedOnAmortizationTab`
- `RefreshExpenseTabPropertyChanged`
- `RefreshInsightsTabPropertyChanged`
- Equivalent methods in `IncomeViewModel` and `ExpenseViewModel`

### Step 3 — Force-save on explicit user actions

Some actions must save immediately without debounce (add/delete transaction entry, navigation away). Call `SharedServiceCore.SaveData(this)` directly (non-debounced) in:

- `LoanViewModel.AddOrUpdateEntryFromView()` — after successful add
- `LoanViewModel.Delete(...)` — after delete
- The equivalent in `IncomeViewModel` and `ExpenseViewModel`
- `OnDisappearing` override in each view (as a final flush)

---

## Verification

1. Open the Loan tab, drag the deposit slider rapidly for 3 seconds.
2. Check file system: the save file should update once ~600ms after you stop dragging (not dozens of times during dragging).
3. Verify data is not lost when switching tabs immediately after editing — the `OnDisappearing` force-save catches this edge case.
4. Type a property amount and immediately close the app — reopen and verify the value was saved (force-save on `OnDisappearing` covers this).
