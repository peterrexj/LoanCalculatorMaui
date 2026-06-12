# 20 — UX Modern Style: Visual Refresh, Micro-Animations, Haptics

**Priority:** Medium  
**Status:** ❌ Not started  
**Goal:** Bring the visual language in line with 2025 mobile trends — refined typography scale, purposeful motion, tactile feedback — without a full redesign.

---

## Current gaps vs. 2025 trends

| Trend | Current State | Gap |
|-------|--------------|-----|
| Floating/minimal cards | Flat bordered `Frame` elements | Needs subtle shadow + rounded corners (16dp), glassmorphism option |
| Typography hierarchy | Partial token system (plan 09) | Heading sizes inconsistent; weight (Bold vs Medium) not systematically applied |
| Haptic feedback | None | Tapping buttons, completing add-entry, and toggling sliders should have light haptic |
| Micro-animations | None | Value changes (repayment amount) should have a brief number-count animation |
| Skeleton loading | Activity indicator only | Cards should show shimmer skeleton while loading instead of blank |
| Dynamic colour | Fixed theme colours | Respect iOS 18 / Android 15 dynamic colour APIs where possible |
| Bottom sheet modals | Full-page popups for upfront costs, quick input | Should be iOS-native style bottom sheets |

---

## Fix A — Card elevation and radius

Audit all `Frame` elements in `LoanView.xaml`, `IncomeView.xaml`, `ExpenseView.xaml`. Apply:
- `CornerRadius="16"` (current is inconsistent: 8, 10, 12)
- `Shadow` property (MAUI 8+): `<Frame.Shadow><Shadow Brush="#22000000" Radius="8" Offset="0,2" /></Frame.Shadow>`
- Remove hard `BorderColor` where shadow makes it redundant

Add a `CardStyle` to `Theme.CommonStyles.xaml`:

```xml
<Style x:Key="CardStyle" TargetType="Frame">
    <Setter Property="CornerRadius" Value="16" />
    <Setter Property="Padding" Value="16" />
    <Setter Property="HasShadow" Value="False" />
    <Setter Property="BackgroundColor" Value="{DynamicResource LoanAppCardBackgroundColor}" />
    <Setter Property="Shadow">
        <Setter.Value>
            <Shadow Brush="{DynamicResource LoanAppShadowColor}" Radius="8" Opacity="0.15" Offset="0,2" />
        </Setter.Value>
    </Setter>
</Style>
```

---

## Fix B — Haptic feedback

Add haptic feedback at key interaction points. MAUI `HapticFeedback.Perform()` is available without additional packages.

```csharp
// In LoanView.xaml.cs — after AddNewIncome_Clicked succeeds
HapticFeedback.Perform(HapticFeedbackType.Click);

// After OnUpfrontDone, OnQuickInputDone
HapticFeedback.Perform(HapticFeedbackType.LongPress); // heavier = "saved"

// After btnDeleteEntry_Clicked
HapticFeedback.Perform(HapticFeedbackType.Click);
```

Wrap in `try/catch` — some platforms/devices do not support haptics.

---

## Fix C — Number count animation on repayment value

When `TermPaymentMonthly` changes (user releases slider or finishes typing), animate the displayed value from its old value to the new value over 400ms instead of snapping instantly. This makes the update feel computed rather than arbitrary.

Implementation options:
1. Use `Animation` in the MAUI animation API on the `Label` displaying the monthly repayment.
2. Use a custom `CountingLabel` control that interpolates the numeric value over a duration.

This is a display-only change — the underlying ViewModel value is correct immediately.

---

## Fix D — Skeleton loading shimmer

Replace `ActivityIndicator` (spinning ring) with a shimmer skeleton layout while `IsPageBusy = true`. MAUI has `Skeleton` support via `Syncfusion.Maui.Core` (already a dependency) or via a simple `AnimationBehavior`.

The skeleton should mirror the shape of the loaded content — e.g., two placeholder rows for the repayment summary, a placeholder chart rectangle.

---

## Fix E — Bottom sheet modals

The "Upfront Costs" and "Quick Input" popups are currently implemented as `AbsoluteLayout` overlays that slide in from the bottom. Convert to a proper bottom sheet pattern:

- Use `CommunityToolkit.Maui.BottomSheet` (if already a dependency) or replicate with `PanGestureRecognizer` to drag-dismiss.
- Match iOS 17 detent-based sheet behaviour on iPhone.

---

## Files to Touch

| File | Change |
|------|--------|
| `src/LoanCalculator/Extensions/Data/Theme.CommonStyles.xaml` | Add `CardStyle`, `LoanAppCardBackgroundColor`, `LoanAppShadowColor` |
| `src/LoanCalculator/Extensions/Data/Theme.*.xaml` (all theme files) | Add `LoanAppCardBackgroundColor` and `LoanAppShadowColor` tokens per theme |
| `src/LoanCalculator/View/LoanView.xaml` | Apply `CardStyle`, add haptic calls |
| `src/LoanCalculator/View/IncomeView.xaml` | Apply `CardStyle` |
| `src/LoanCalculator/View/ExpenseView.xaml` | Apply `CardStyle` |
| `src/LoanCalculator/View/LoanView.xaml.cs` | Add `HapticFeedback.Perform` calls |

---

## Verification

1. Cards show a subtle shadow in both Light and Dark themes.
2. Tapping "Add expense" produces a `Click` haptic on devices that support it.
3. Changing the loan amount animates the monthly repayment label (Fix C).
4. Page load shows shimmer skeleton before data appears (Fix D).
