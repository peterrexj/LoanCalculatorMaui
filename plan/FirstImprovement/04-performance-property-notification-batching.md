# 04 — Performance: 20+ OnPropertyChanged Calls Per Keystroke

**Priority:** High  
**Status:** ✅ DONE  
**Symptoms:** UI stutters when typing; perceived lag between input and result update; battery drain.

---

## What Was Done

- `IsAmortizationTabActive` and `IsInsightsTabActive` flags added to `LoanViewModel` — set by `TabView_OnSelectionChanging` in `LoanView.xaml.cs`.
- `TriggerPropertyChangedOnPropertyTab` now conditionally calls `UpdateAmortizationCharts()` (only when `IsAmortizationTabActive`) and `UpdateInsightCharts()` (only when `IsInsightsTabActive`).
- Replaced `OnPropertyChanged(nameof(HomeLoanInfo))` bulk notification approach — property still notified (binding sub-tree still resolves) but chart updates are now gated.
- Save debounce integrated via `ScheduleSave` (plan 02 combined).

### Open: equality guards on `DepositPercentage` / `LoanAmountPercentage`

The commented-out equality check on these setters was not restored. Slider can still fire the full notification chain on unchanged values during initial binding evaluation. Low priority since debounce absorbs the save cost.

---

## Problem

Every property setter that changes a loan value calls `TriggerPropertyChangedOnPropertyTab()`, which fires `OnPropertyChanged` for approximately 20–25 properties simultaneously. Since MAUI's binding engine processes each notification synchronously on the UI thread, this causes a cascade of layout passes on every keystroke.

### What `TriggerPropertyChangedOnPropertyTab` currently does (LoanViewModel, ~line 680)

```csharp
private void TriggerPropertyChangedOnPropertyTab()
{
    OnPropertyChanged(nameof(PropertyAmount));
    OnPropertyChanged(nameof(LoanAmount));
    OnPropertyChanged(nameof(LoanAmountStrFormatted));
    OnPropertyChanged(nameof(DepositAmountStrFormatted));
    OnPropertyChanged(nameof(InterestRateFormatted));
    OnPropertyChanged(nameof(PropertyTotalAmount));
    OnPropertyChanged(nameof(StampDutyAmount));
    OnPropertyChanged(nameof(StampDutyAmountFormatted));
    OnPropertyChanged(nameof(OtherExpenseTotalAmountFormatted));
    OnPropertyChanged(nameof(TotalRepaymentFormatted));
    OnPropertyChanged(nameof(TermPaymentFormatted));
    OnPropertyChanged(nameof(TermPaymentMonthlyFormatted));
    OnPropertyChanged(nameof(TotalInterestPaymentFormatted));
    OnPropertyChanged(nameof(HomeLoanInfo));          // <-- triggers entire HomeLoanInfo sub-tree
    OnPropertyChanged(nameof(ChartPropertyValueWithInterestPayment));
    OnPropertyChanged(nameof(InsightChartLoanAmountAxis));
    OnPropertyChanged(nameof(InsightChartLoanInterestAxis));
    OnPropertyChanged(nameof(InsightChartLoanDepositAxis));
    OnPropertyChanged(nameof(Affordability));
    OnPropertyChanged(nameof(AffordabilityTextDescription));
    OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
    OnPropertyChanged(nameof(IsAffordabilityAvailable));
    SharedServiceCore.SaveData(this);                // disk write — covered in plan 02
}
```

The `HomeLoanInfo` notification causes the binding engine to re-evaluate every sub-property bound as `{Binding HomeLoanInfo.PaymentSummary.Payment.TermPaymentRoundedWithComma}` etc. — this is particularly expensive.

---

## Fix

### Strategy 1 — Split by tab (low risk, immediate gain)

`TriggerPropertyChangedOnPropertyTab` fires notifications for all tabs, including the Amortization tab (which may not even be visible). Split into targeted methods already partially done in the codebase:

- **Asset tab only:** `PropertyAmount`, `LoanAmount`, `LoanAmountStrFormatted`, `DepositAmountStrFormatted`, `InterestRateFormatted`, `PropertyTotalAmount`, `StampDutyAmount*`, `OtherExpenseTotalAmount*`, `TermPayment*`, `TotalRepayment*`, `HomeLoanInfo`
- **Insights tab only:** `InsightChart*`, `Affordability*`, `IsAffordabilityAvailable`
- **Amortization tab:** Only fire when the Amortization tab is active (check a `_isAmortizationTabVisible` flag)

```csharp
// In LoanViewModel — add a visibility flag set by the tab switch event
[JsonIgnore] public bool IsAmortizationTabActive { get; set; }
[JsonIgnore] public bool IsInsightsTabActive { get; set; }

private void TriggerPropertyChangedOnPropertyTab()
{
    // Always fire — these are visible on the Asset tab
    OnPropertyChanged(nameof(LoanAmount));
    OnPropertyChanged(nameof(LoanAmountStrFormatted));
    // ... core display properties ...

    // Only fire if the Insights tab is currently visible
    if (IsInsightsTabActive)
    {
        UpdateInsightCharts(); // see plan 03
        OnPropertyChanged(nameof(Affordability));
        // ...
    }

    ScheduleSave(() => SharedServiceCore.SaveData(this)); // see plan 02
}
```

### Strategy 2 — Remove redundant `HomeLoanInfo` notification

`OnPropertyChanged(nameof(HomeLoanInfo))` notifies the binding engine that the entire object changed. Since individual sub-properties like `HomeLoanInfo.PropertyAmount` are also notified separately, this causes double evaluation. Remove the `OnPropertyChanged(nameof(HomeLoanInfo))` bulk notification and rely on specific property notifications.

### Strategy 3 — Guard no-change setters

Some property setters are missing equality guards. Add them:

```csharp
// DepositPercentage setter — currently commented out equality guard
public double DepositPercentage
{
    get => HomeLoanInfo?.DepositPercentage ?? 0;
    set
    {
        if (isUpdating || !HasInitialized) return;
        if (HomeLoanInfo.DepositPercentage == value) return; // uncomment/add this
        // ...
    }
}
```

The commented-out equality check on `DepositPercentage` (line ~337) and `LoanAmountPercentage` (line ~373) means the slider fires the full notification chain even when the value hasn't changed (e.g. during initial binding evaluation).

---

## Verification

1. Type in the Property Amount field. Measure responsiveness — should feel immediate.
2. Drag the deposit slider. No stutter or dropped frames.
3. Switch to the Amortization tab. Change interest rate. The amortization table should update (proving the Amortization tab still notifies when active).
4. Switch back to Asset tab — asset summary should still be correct.
