# 14 — Feature: Loan Comparison (Side-by-Side Scenarios)

**Priority:** High  
**Status:** ❌ Not started  
**Goal:** Let users save their current loan inputs as a named scenario and compare two scenarios side by side — answering "what if I get a better rate" or "20% vs 10% deposit" without re-typing.

---

## Why This Matters

Currently users must memorise old numbers before changing inputs. The app has no memory of "where they started". A comparison feature directly addresses the #1 use case for a loan calculator: comparing options. It drives return sessions (user sets scenario A, thinks about it, comes back to try scenario B).

---

## Design

### Data model — `LoanScenario`

A lightweight snapshot of just the inputs (not the full `LoanViewModel`):

```csharp
// src/LoanCalculator.Core/Models/LoanScenario.cs
public class LoanScenario
{
    public string Label { get; set; } = "Scenario";
    public double PropertyAmount { get; set; }
    public double DepositAmountDirectInput { get; set; }
    public double InterestRate { get; set; }
    public int LoanTermInYears { get; set; }
    public int RepaymentFrequency { get; set; } // payments per year
}
```

Persisted via `ILocalStorage` as `scenarioA.json` / `scenarioB.json`.

### UI — "Save as Scenario" button on Asset tab

- Small secondary button or icon near the loan summary header: "Save as A" / "Save as B"
- Tapping saves current inputs to the scenario slot
- No modal — just a `Toast`-style confirmation ("Scenario A saved")

### UI — Comparison panel

A collapsible bottom sheet or a new card below the repayment summary on the Asset tab:

```
┌─────────────────────────────────────────────────┐
│  Compare Scenarios                         [x]  │
├─────────────────────┬───────────────────────────┤
│                     │  Scenario A  │ Scenario B  │
│ Property Value      │  $650,000    │  $700,000   │
│ Loan Amount         │  $520,000    │  $560,000   │
│ Interest Rate       │  5.50%       │  5.25%      │
│ Monthly Repayment   │  $2,950      │  $3,090     │
│ Total Interest      │  $242,000    │  $253,000   │
└─────────────────────┴─────────────────┴──────────┘
```

### ViewModel changes

Add to `LoanViewModel`:

```csharp
[JsonIgnore] public LoanScenario? ScenarioA { get; set; }
[JsonIgnore] public LoanScenario? ScenarioB { get; set; }
[JsonIgnore] public bool IsComparisonVisible { get; set; }

public ICommand SaveScenarioACommand { get; }
public ICommand SaveScenarioBCommand { get; }
public ICommand ToggleComparisonCommand { get; }
```

Each save command snapshots current inputs into the slot and persists via `ILocalStorage`. Comparison is purely computed from the two saved scenarios — no live recalculation needed.

---

## Files to Touch

| File | Change |
|------|--------|
| `src/LoanCalculator.Core/Models/LoanScenario.cs` | New file |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` | Add scenario properties + commands |
| `src/LoanCalculator/View/LoanView.xaml` | Add "Save as A/B" buttons + comparison card |
| `src/LoanCalculator/Services/NameValueDataService.cs` | Persist scenario slots |

---

## Premium gating

Show the comparison panel to all users. Saving and comparing are free features — they drive engagement and conversions. The Insights tab (where affordability lives) is already premium-gated.

---

## Verification

1. Enter loan values → tap "Save as A" → change interest rate → tap "Save as B" → open comparison → both scenarios shown correctly.
2. Close and reopen the app — scenarios are restored from disk.
3. Tap "X" to dismiss comparison — it collapses; values in main view unchanged.
