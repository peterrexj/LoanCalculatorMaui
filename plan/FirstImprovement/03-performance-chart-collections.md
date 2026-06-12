# 03 — Performance: Chart ObservableCollections Rebuilt on Every Get

**Priority:** High  
**Status:** ✅ DONE  
**Symptoms:** Charts flicker or re-animate on every property update; excessive garbage collection pauses; binding engine does redundant diff work.

---

## What Was Done

- `InsightChartLoanAmountAxis`, `InsightChartLoanInterestAxis`, `InsightChartLoanDepositAxis` all converted to backing-field properties — no longer `new()` on every get.
- `UpdateInsightCharts()` method added — called from `TriggerPropertyChangedOnPropertyTab` (only when `IsInsightsTabActive`) and `BuildInsights()`.
- Amortization chart collections (`_amortizationChartPrincipal`, `_amortizationChartInterest`, `_amortizationChartAreaPrincipal`, `_amortizationChartAreaInterest`) are backing fields, updated by `UpdateAmortizationCharts()` via `RefillCollection()`.
- `RefillCollection` does `Clear()` + `Add()` per item (avoids new collection alloc per get, trades for a clear-on-update).

### Minor gap: `ReplaceChartPoint` still does `Clear()` + `Add()` instead of in-place update

`ChartDataModel` may not implement `INotifyPropertyChanged`. The current `ReplaceChartPoint` calls `Clear()` then `Add()` which still causes one chart animation per update. In-place update (`col[0].Name = ...; col[0].Value = ...`) would avoid the animation if `ChartDataModel` is made to implement `INotifyPropertyChanged`.

---

## Problem

Several chart data source properties in `LoanViewModel` are computed properties (no backing field) that create a **brand-new `ObservableCollection<ChartDataModel>`** on every access:

```csharp
// LoanViewModel.cs — every binding evaluation creates a new collection
[JsonIgnore]
public ObservableCollection<ChartDataModel> InsightChartLoanAmountAxis =>
    new(new List<ChartDataModel>
    {
        new(name: DateTime.Now.Year.ToString(), value: HomeLoanInfo?.LoanAmountDirectInput ?? 0)
    }.AsEnumerable());

[JsonIgnore]
public ObservableCollection<ChartDataModel> InsightChartLoanInterestAxis
{
    get { return new ObservableCollection<ChartDataModel>(...); }
}
// ... and InsightChartLoanDepositAxis, all four AmortizationChart* collections, etc.
```

Because `TriggerPropertyChangedOnPropertyTab` fires `OnPropertyChanged` for all of these on every keystroke, the binding engine re-evaluates these getters, gets back a new collection instance each time, and forces Syncfusion charts to fully re-render.

### Affected properties (all in `LoanViewModel.cs`)

- `InsightChartLoanAmountAxis`
- `InsightChartLoanInterestAxis`
- `InsightChartLoanDepositAxis`
- `AmortizationChartPrincipalAmountAxis`
- `AmortizationChartInterestAmountAxis`
- `AmortizationChartAreaPrincipalAmountAxis`
- `AmortizationChartAreaInterestAmountAxis`

---

## Fix

Replace inline `new ObservableCollection<>(...)` computed properties with **backing fields that are updated in-place**.

### Pattern for each chart property

```csharp
// Before — creates new collection every time
public ObservableCollection<ChartDataModel> InsightChartLoanAmountAxis =>
    new(new List<ChartDataModel>
    {
        new(DateTime.Now.Year.ToString(), HomeLoanInfo?.LoanAmountDirectInput ?? 0)
    });

// After — backing field, updated in-place
[JsonIgnore]
private ObservableCollection<ChartDataModel> _insightChartLoanAmountAxis = new();

[JsonIgnore]
public ObservableCollection<ChartDataModel> InsightChartLoanAmountAxis => _insightChartLoanAmountAxis;

private void UpdateInsightCharts()
{
    var loanAmount = HomeLoanInfo?.LoanAmountDirectInput ?? 0;
    var interest = HomeLoanInfo?.PaymentSummary?.Payment?.TotalInterestPayment ?? 0;
    var deposit = HomeLoanInfo?.DepositAmountDirectInput ?? 0;
    var year = DateTime.Now.Year.ToString();

    SetChartPoint(_insightChartLoanAmountAxis, year, loanAmount);
    SetChartPoint(_insightChartLoanInterestAxis, year, interest);
    SetChartPoint(_insightChartLoanDepositAxis, year, deposit);
}

private static void SetChartPoint(ObservableCollection<ChartDataModel> col, string name, double value)
{
    if (col.Count == 0)
        col.Add(new ChartDataModel(name, value));
    else
    {
        col[0].Name = name;
        col[0].Value = value;
        // ChartDataModel must implement INotifyPropertyChanged for in-place update
        // If it doesn't, clear and re-add (still only one allocation per update, not per get)
    }
}
```

Call `UpdateInsightCharts()` from `RefreshInsightsTabPropertyChanged()` and `TriggerPropertyChangedOnPropertyTab()` instead of firing `OnPropertyChanged` for each chart axis.

### For amortization charts (larger data sets)

The amortization axis collections contain one entry per year/term. Rather than recreating the whole collection, update existing items in-place where possible, and only `Clear()` + `AddRange()` when the number of periods changes (loan term changed):

```csharp
private void UpdateAmortizationCharts(List<AmortizationRow> rows)
{
    SyncCollection(_amortChartPrincipal, rows, r => new ChartDataModel(r.Period, r.Principal));
    SyncCollection(_amortChartInterest, rows, r => new ChartDataModel(r.Period, r.Interest));
}

private static void SyncCollection<T>(ObservableCollection<T> col, List<T> source)
{
    // If same length, update in-place; if different, replace all
    if (col.Count != source.Count) { col.Clear(); foreach (var item in source) col.Add(item); }
    else for (int i = 0; i < source.Count; i++) col[i] = source[i];
}
```

---

## Note on `ChartDataModel`

Check whether `ChartDataModel` (`src/LoanCalculator.Core/Models/Charts/ChartDataModel.cs`) implements `INotifyPropertyChanged`. If it does, in-place property updates will work. If it doesn't, implement it or use the clear-and-re-add approach above (still a large improvement over new collection per get).

---

## Verification

1. Open the Insights tab. Change the property amount. The chart should update without re-animating from zero.
2. Profile with dotnet-trace or Instruments — `ObservableCollection` allocations during slider drag should drop by ~95%.
3. The amortization table should not flash/reload when switching between Yearly/Term segment.
