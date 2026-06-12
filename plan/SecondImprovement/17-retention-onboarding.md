# 17 — User Retention: First-Launch Onboarding Walkthrough

**Priority:** High  
**Status:** ❌ Not started  
**Goal:** Guide first-time users to the app's core value (affordability insight) in 3 steps, reducing drop-off before they enter any data.

---

## Problem

New users open the app, see a blank loan calculator, and don't understand the full value (affordability, expense projection, income comparison). Without guidance, they type one number, don't know where to go next, and close the app. The existing `HasShowAppLaunchDisclaimer` pattern proves the infrastructure for first-launch gating already exists — we just need to use it for a positive guide rather than just a disclaimer.

---

## Design — 3-step coach mark overlay

Each step highlights one UI region with a translucent overlay, an icon, a headline, a 1-line instruction, and a "Next →" button.

```
Step 1: "Enter your property value"
  → Highlight the property amount input area
  → Body: "Start with the price of the home you're considering."

Step 2: "Set your deposit"
  → Highlight the deposit slider / input
  → Body: "Your deposit affects your loan amount and repayments."

Step 3: "Check Insights for affordability"
  → Highlight the Insights tab
  → Body: "Add your income and expenses to see if you can afford this loan."
```

Last step has "Get Started" instead of "Next".

### Implementation

#### State flag

Add to `NameValueDataModel`:

```csharp
public bool HasCompletedOnboarding { get; set; }
```

#### Control — `OnboardingOverlayView`

A new `ContentView` placed at `ZIndex="200"` in `LoanView.xaml`, `IsVisible` bound to `!HasCompletedOnboarding`:

```xml
<Grid IsVisible="{Binding IsOnboardingVisible}"
      BackgroundColor="#CC000000"
      ZIndex="200">
    <!-- Coach mark card positioned via absolute layout or grid -->
    <Frame ...>
        <StackLayout>
            <Image Source="{Binding OnboardingIcon}" />
            <Label Text="{Binding OnboardingHeadline}" Style="{DynamicResource LabelTitleStyle}" />
            <Label Text="{Binding OnboardingBody}" Style="{DynamicResource LabelBodyStyle}" />
            <SfButton Text="{Binding OnboardingButtonLabel}"
                      Command="{Binding OnboardingNextCommand}" />
            <Button Text="Skip" Command="{Binding OnboardingSkipCommand}"
                    TextColor="{DynamicResource LoanAppSecondaryTextColor}" />
        </StackLayout>
    </Frame>
</Grid>
```

#### ViewModel properties (add to `LoanViewModel`)

```csharp
[JsonIgnore] public bool IsOnboardingVisible { get; set; }
[JsonIgnore] public int OnboardingStep { get; set; }
[JsonIgnore] public string OnboardingHeadline { get; }    // derived from step
[JsonIgnore] public string OnboardingBody { get; }
[JsonIgnore] public string OnboardingButtonLabel { get; } // "Next" or "Get Started"
public ICommand OnboardingNextCommand { get; }
public ICommand OnboardingSkipCommand { get; }
```

`OnboardingNextCommand` advances the step. At step 3, it sets `HasCompletedOnboarding = true`, persists via `NameValueDataService.SaveNameValueData()`, and sets `IsOnboardingVisible = false`.

#### Trigger

In `LoanView.LoadDataSet()`, after `MarkInitializationComplete()`:

```csharp
if (!SharedServiceCore.NameValueDataService.NameValueDataModel.HasCompletedOnboarding)
{
    _viewModel.IsOnboardingVisible = true;
    _viewModel.OnboardingStep = 0;
}
```

---

## Files to Touch

| File | Change |
|------|--------|
| `src/LoanCalculator.Core/Models/ViewModels/NameValueDataModel.cs` | Add `HasCompletedOnboarding` |
| `src/LoanCalculator.Core/Models/ViewModels/PrimaryModels/LoanViewModel.cs` | Add onboarding properties + commands |
| `src/LoanCalculator/View/LoanView.xaml` | Add `OnboardingOverlayView` grid |

---

## Verification

1. Fresh install (or clear `NameValueDataModel`) → onboarding overlay appears on first app launch.
2. Tap "Skip" on any step → overlay dismissed, `HasCompletedOnboarding` saved, overlay never shows again.
3. Complete all 3 steps → "Get Started" dismisses, `HasCompletedOnboarding` saved.
4. Restart app → overlay does not appear again.
