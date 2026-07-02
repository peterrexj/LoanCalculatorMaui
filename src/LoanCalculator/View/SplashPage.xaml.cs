using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.View;

public partial class SplashPage : ContentPage
{
    private readonly AppShell _appShell;
    private bool _hasNavigated;
    private bool _hasStarted;

    public SplashPage(AppShell appShell)
    {
        _appShell = appShell;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Guard: OnAppearing can fire more than once (e.g. window re-activation).
        if (_hasStarted) return;
        _hasStarted = true;

        try
        {
            await RunSplashAsync();
        }
        catch
        {
            // Never let the splash trap the user — fall through to the app.
        }

        NavigateToShell();
    }

    private async Task RunSplashAsync()
    {
        // ── Phase 1: entrance animation (uninterrupted on the UI thread) ──────
        // Kick off the pulsing dots loop (fires later once dots are visible).
        var dotsCts = new CancellationTokenSource();

        // Glow fades up gently behind the logo (soft halo — capped low so the solid
        // teal ellipse reads as a glow, not a bright disc).
        _ = GlowEllipse.FadeTo(0.22, 600, Easing.CubicOut);

        // Logo: pop-in with a subtle overshoot (scale + fade together).
        var logoFade = LogoCard.FadeTo(1, 450, Easing.CubicOut);
        var logoScale = LogoCard.ScaleTo(1.0, 600, Easing.SpringOut);
        await Task.WhenAll(logoFade, logoScale);

        // Title slides up and fades in.
        var titleFade = TitleLabel.FadeTo(1, 350, Easing.CubicOut);
        var titleMove = TitleLabel.TranslateTo(0, 0, 400, Easing.CubicOut);
        await Task.WhenAll(titleFade, titleMove);

        // Tagline follows just behind.
        var tagFade = TaglineLabel.FadeTo(1, 300, Easing.CubicOut);
        var tagMove = TaglineLabel.TranslateTo(0, 0, 350, Easing.CubicOut);
        await Task.WhenAll(tagFade, tagMove);

        // Reveal the loading dots and start the pulse loop.
        await DotsLayout.FadeTo(1, 200, Easing.CubicOut);
        _ = AnimateDotsAsync(dotsCts.Token);

        // ── Phase 2: background pre-warm (UI thread now free for the dots) ────
        var prewarm = Task.Run(PreWarmAsync);

        // Hold the brand for a beat, but bail out as soon as pre-warm is done
        // (with a minimum + maximum so it always feels intentional, never stuck).
        var minHold = Task.Delay(1100);
        await Task.WhenAll(prewarm, minHold);

        dotsCts.Cancel();

        // ── Phase 3: graceful exit — fade the whole splash out ────────────────
        await this.FadeTo(0, 280, Easing.CubicIn);
    }

    private async Task AnimateDotsAsync(CancellationToken token)
    {
        var dots = new[] { Dot1, Dot2, Dot3 };
        try
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var dot in dots)
                {
                    if (token.IsCancellationRequested) return;
                    await dot.FadeTo(0.3, 220, Easing.CubicInOut);
                    await dot.FadeTo(1.0, 220, Easing.CubicInOut);
                }
            }
        }
        catch
        {
            // animation cancelled / view torn down — ignore
        }
    }

    // Heavy work, all off the UI thread so the animation stays buttery smooth.
    private async Task PreWarmAsync()
    {
        // 0. Warm the disclaimer / metadata state so the LoanView popup doesn't flash.
        //    PopupDisclaimerViewModel.IsPopupRequired returns a `true` fallback until
        //    LocalStorage is initialised AND the value has been read once (the getter caches
        //    it as a side-effect). The popup binds IsOpen to this with no change-notification,
        //    so it reads exactly once at bind time — if that read hits the fallback, the popup
        //    opens then snaps shut when the real value (already-accepted → false) resolves.
        //    Initialising storage and reading the value here, on the SAME singleton VM the
        //    popup uses, caches the correct value before LoanView ever binds.
        try
        {
            SharedServiceCore.LocalStorage.Initialize();
            // Force NameValueDataModel to load from disk (lazy getter).
            _ = SharedServiceCore.NameValueDataService?.NameValueDataModel;
            // Read IsPopupRequired once so the singleton caches the resolved value.
            _ = ServiceLocator.GetService<PopupDisclaimerViewModel>()?.IsPopupRequired;
        }
        catch { /* best-effort */ }

        // 1. Load Budget's Income/Expense data from disk.
        try
        {
            var budgetVm = ServiceLocator.GetService<BudgetViewModel>();
            if (budgetVm != null)
                await budgetVm.EnsureSubVmsLoadedAsync();
        }
        catch { /* best-effort; BudgetView.OnAppearing will retry */ }

        // 2. Pre-inflate the heavy NON-landing tab pages so the first tab tap is instant.
        //    NOTE: we deliberately do NOT pre-build LoanView here. LoanView is the landing
        //    page (built by Shell navigation anyway) and it hosts the full-screen disclaimer
        //    SfPopup. Pre-building it off-screen opens that popup in a detached state, which
        //    then resets when the page actually appears — causing a visible popup flash.
        await PreBuildOnMainThread<BudgetView>();
    }

    private static Task PreBuildOnMainThread<T>() where T : class
    {
        var tcs = new TaskCompletionSource();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try { ServiceLocator.GetService<T>(); }
            catch { /* page will build lazily on first tap */ }
            finally { tcs.TrySetResult(); }
        });
        return tcs.Task;
    }

    private void NavigateToShell()
    {
        if (_hasNavigated) return;
        _hasNavigated = true;
        Application.Current!.Windows[0].Page = _appShell;
    }
}
