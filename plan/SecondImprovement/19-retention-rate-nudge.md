# 19 — User Retention: Rate-Change Session Nudge

**Priority:** Medium  
**Status:** ❌ Not started  
**Goal:** Give users a reason to reopen the app when interest rates change — a passive re-engagement hook that requires no push notification permissions.

---

## Problem

Users set up their loan once and never return unless they're actively searching. The app has no "reason to come back" between sessions. Rate changes are frequent (central bank decisions) and directly relevant — they're a natural re-engagement trigger.

---

## Design

On session start (inside `LoanView.LoadDataSet`, after `MarkInitializationComplete`):

1. Read the `InterestRate` saved in the ViewModel (loaded from disk).
2. Read the `LastSessionInterestRate` stored in `Preferences`.
3. If they differ by ≥ 0.10 percentage points AND the user manually changed the rate since the last session, show a banner.

```
┌──────────────────────────────────────────────────────────┐
│  📈  Your rate changed                              [×]  │
│  Last session: 5.50% → Now: 5.75%                        │
│  New monthly repayment: $3,120 (was $3,050)              │
└──────────────────────────────────────────────────────────┘
```

The banner uses the existing `DisclaimerBannerView` control (Information type, already used in `LoanView.xaml`).

### Persistence

```csharp
// Save after each session's rate is committed
Preferences.Set("LastSessionInterestRate", _viewModel.InterestRate);
Preferences.Set("LastSessionInterestRateDate", DateTime.UtcNow.ToString("O"));
```

Load in `LoanView.LoadDataSet`:

```csharp
var lastRate = Preferences.Get("LastSessionInterestRate", 0.0);
var currentRate = _viewModel.InterestRate;
if (Math.Abs(currentRate - lastRate) >= 0.10 && lastRate > 0)
{
    _viewModel.ShowRateChangeNotice = true;
    _viewModel.RateChangePreviousRate = lastRate;
}
```

### ViewModel additions

```csharp
[JsonIgnore] public bool ShowRateChangeNotice { get; set; }
[JsonIgnore] public double RateChangePreviousRate { get; set; }
[JsonIgnore] public string RateChangeSummary =>
    $"Last session: {RateChangePreviousRate:0.##}% → Now: {InterestRate:0.##}%";
public ICommand DismissRateChangeCommand { get; } // sets ShowRateChangeNotice = false
```

---

## Files to Touch

| File | Change |
|------|--------|
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` | Add rate-change notice properties + dismiss command |
| `src/LoanCalculator/View/LoanView.xaml` | Wire `DisclaimerBannerView` visibility to `ShowRateChangeNotice` |
| `src/LoanCalculator/View/LoanView.xaml.cs` | Save `LastSessionInterestRate` to Preferences on `OnDisappearing` |

---

## Dismissal logic

- Banner dismissed per session (not persisted). If the rate is still different next session, it shows again.
- No more than once per 24 hours per rate change (use a `Preferences` date stamp).

---

## Verification

1. Set rate to 5.50% → close app → change rate to 5.75% in a fresh session → banner appears.
2. Dismiss banner → does not reappear in same session.
3. If rate unchanged between sessions → banner does not appear.
4. Banner shows correct previous rate and new monthly repayment.
