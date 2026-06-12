# 07 — UX: Splash Screen Timing and Load State Feedback

**Priority:** High  
**Status:** ✅ DONE  
**Symptoms:** Blank or frozen screen after splash ends; users see a partially loaded UI before data appears; no indication that the app is loading.

---

## What Was Done

- **Fix A (animation-driven splash) — DONE:** `SplashPage` listens to `LottieView_OnAnimationCompleted` and navigates as soon as the animation fires. 3-second `Task.Delay` kept only as a fallback with `_hasNavigated` guard preventing double navigation.
- **Fix B (BindingContext in constructor) — DONE:** `BindingContext = _viewModel` set in `LoanView` constructor with `IsPageBusy = true` — no blank-field flash.
- **Fix D (OnAppearing awaiting) — DONE:** `LoadDataSet` is directly `await`ed in the `try` block; `finally` correctly resets `IsPageBusy = false` after load completes.
- **Fix C (loading overlay)** — `IsPageBusy` binding exists on `IsBusy`. Whether a visual overlay/spinner is shown depends on the XAML. Verify the loading overlay is visible when `IsPageBusy = true` on all four tabs.

---

## Problem

### Issue A — Splash delay is hardcoded to match animation frame count

`SplashPage.OnAppearing` uses `Task.Delay(2600)` to wait for the Lottie animation to finish before swapping to `AppShell`. If the animation stutters or takes longer to load on a slow device, the shell appears mid-animation. If the animation completes early (e.g. on fast devices), the user stares at a static logo for the remaining time.

File: `src/LoanCalculator/View/SplashPage.xaml.cs`

### Issue B — `BindingContext` is deferred until after data loads, causing a visible blank flash

In `LoanView.LoadDataSet()`, `BindingContext ??= _viewModel` is set at line ~140, after all async data loading is complete. Until this line runs, the page has no binding context — every `{Binding ...}` expression shows its fallback (empty string or zero). Users briefly see empty fields.

### Issue C — No loading indicator during tab data load

When the app launches and opens the Loan tab, `IsBusy = true` is set but there is no visual indicator shown to the user (the busy indicator style may be hidden behind other content, or the `IsFree` grid disable occurs after the UI is already visible).

### Issue D — `finally` block in `OnAppearing` runs before `LoadDataSet` completes

As described in plan 05, the `finally` block that resets `IsPageBusy = false` and `IsBusy = false` runs before `LoadDataSet` is finished (because the `Dispatcher.Dispatch` call is not awaited). This means the loading indicator disappears too early.

---

## Fix

### Fix A — Tie splash end to animation completion event

The SkiaSharp Lottie `SKLottieView` control raises an `AnimationFinished` event (or exposes an `IsAnimating` property). Use this instead of a hardcoded delay:

```csharp
// SplashPage.xaml — add event handler
<skia:SKLottieView x:Name="LottieAnim"
                   AnimationFinished="LottieAnim_OnAnimationFinished"
                   ... />
```

```csharp
// SplashPage.xaml.cs
private void LottieAnim_OnAnimationFinished(object sender, EventArgs e)
{
    Application.Current.Windows[0].Page = _appShell;
}

protected override void OnAppearing()
{
    base.OnAppearing();
    // No Task.Delay needed — animation event drives the transition
}
```

If the event is unavailable in the current version of SkiaSharp.Extended.UI.Maui (3.0.0), keep a `Task.Delay` but compute it from the animation's known frame rate and duration exposed via `LottieAnim.Duration`.

### Fix B — Set `BindingContext` before data loads, populate after

Set `BindingContext = _viewModel` in the constructor (or at the start of `OnAppearing` before any async work), then let the ViewModel's observable properties update the UI as data arrives. This eliminates the blank-field flash.

```csharp
// LoanView constructor
public LoanView(...)
{
    InitializeComponent();
    _viewModel = viewModel;
    BindingContext = _viewModel;  // set immediately — bindings evaluate to current (empty/default) state
    _viewModel.IsBusy = true;
    _viewModel.IsPageBusy = true;
}

// Remove "BindingContext ??= _viewModel;" from LoadDataSet
```

The `isUpdating` and `HasInitialized` guards in property setters already prevent spurious saves/calculations before initialization completes, so this is safe.

### Fix C — Add a visible loading overlay

In each view's XAML, add a semi-transparent overlay that shows while `IsBusy = true`:

```xml
<!-- Overlay — appears on top of all content while loading -->
<Grid IsVisible="{Binding IsPageBusy}"
      BackgroundColor="{DynamicResource LoanAppDefaultBackgroundColor}"
      Opacity="0.85"
      ZIndex="100">
    <ActivityIndicator IsRunning="{Binding IsPageBusy}"
                       Color="{DynamicResource LoanAppPrimaryColor}"
                       HorizontalOptions="Center"
                       VerticalOptions="Center" />
    <Label Text="Loading..."
           Style="{DynamicResource LabelSubtitleStyle}"
           HorizontalOptions="Center"
           VerticalOptions="Center"
           Margin="0,60,0,0" />
</Grid>
```

`IsPageBusy` is already in `BaseViewModel` — no ViewModel changes needed.

### Fix D — Await `LoadDataSet` directly (see plan 05 for the full fix)

Moving `await LoadDataSet()` directly into the `try` block of `OnAppearing` ensures `IsPageBusy` is reset only after loading completes.

---

## Verification

1. Cold-launch the app — the splash animation plays fully, then the shell appears (no premature cut).
2. On a throttled network/slow device, data loads and the loading overlay is visible until data is ready.
3. Fast device — no brief flash of empty fields; the loading overlay covers the initial state.
4. All 4 tabs show a loading indicator on their first activation.
