# 15 — Feature: Extra Repayment & Time-Saved Calculator

**Priority:** High  
**Status:** ❌ Not started  
**Goal:** Show users how much time and interest they save by making extra repayments — one of the most motivating and frequently-searched features for home loan apps.

---

## Why This Matters

"How much sooner can I pay off my loan if I pay an extra $200/month?" is the question. Every mortgage holder thinks about this. Right now the app has no answer. Adding it:
- Gives users a reason to revisit (change the extra amount, see impact)
- Creates a strong emotional hook ("you could save $45,000 in interest")
- Differentiates from basic web calculators

---

## Design

### New input — "Extra repayment per period"

A single numeric input on the Amortization tab (or as an expandable section on the Asset tab):

```
┌─────────────────────────────────────────┐
│  Extra Repayment                        │
│  [$  200  /month]  [+]  [-]            │
│                                         │
│  Time saved:   3 years 4 months         │
│  Interest saved:  $38,200               │
└─────────────────────────────────────────┘
```

### Calculation

Add to `HomeLoanCalculator.cs`:

```csharp
public static (int monthsSaved, double interestSaved) CalculateExtraRepaymentImpact(
    HomeLoanRepaymentRequest request,
    double extraRepaymentPerPeriod)
{
    // Run amortization with extra repayment, compare payoff date to standard
    // Return months difference and total interest difference
}
```

### ViewModel

Add to `LoanViewModel`:

```csharp
public double ExtraRepaymentAmount { get; set; }  // persisted

[JsonIgnore] public string TimeSavedFormatted { get; }   // "3 yrs 4 mos"
[JsonIgnore] public string InterestSavedFormatted { get; } // "$38,200"
[JsonIgnore] public bool HasExtraRepayment => ExtraRepaymentAmount > 0;
```

`ExtraRepaymentAmount` setter triggers recalculation via `TriggerPropertyChangedOnAmortizationTab()`. Since the calculation is CPU-bound, run it in `Task.Run` and marshal results back to main thread (same pattern as `UpdateProjectionDataAsync`).

---

## Files to Touch

| File | Change |
|------|--------|
| `src/LoanCalculator.Core/HomeLoanCalculator.cs` | Add `CalculateExtraRepaymentImpact` overload |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` | Add `ExtraRepaymentAmount`, `TimeSavedFormatted`, `InterestSavedFormatted` |
| `src/LoanCalculator/View/LoanView.xaml` | Add extra repayment input + result display on Amortization tab |

---

## Premium gating

Show the input on the Amortization tab for all users. The result summary (time saved + interest saved) can be teased for trial users ("Unlock to see your savings") — it converts well because the hook is clear.

---

## Verification

1. Enter $500,000 loan, 6% rate, 30 years, $200/month extra → verify "time saved" and "interest saved" are mathematically correct.
2. Set extra to $0 → result row hides or shows "--".
3. Value persists across app restarts.
4. Changing main loan inputs re-calculates extra repayment impact automatically.
