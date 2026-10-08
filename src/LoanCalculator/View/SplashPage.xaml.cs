using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.View;

public partial class SplashPage : ContentPage
{
    /// <summary>
    /// Upper bound on the splash. Only <see cref="PreWarmAsync"/> can get anywhere near it, so
    /// this fires only when the pre-warm has genuinely stalled.
    /// </summary>
    private static readonly TimeSpan SplashDeadline = TimeSpan.FromSeconds(6);

    /// <summary>
    /// A floor on how briefly the splash may appear — not a brand hold. The pre-warm is often
    /// quicker than this, and without a floor the splash becomes a flicker between the native
    /// splash and the shell.
    /// </summary>
    private static readonly TimeSpan MinimumVisible = TimeSpan.FromMilliseconds(700);

    /// <summary>
    /// Hard stop for the dots pulse, so it can never keep running after the splash is gone even
    /// if its cancellation is somehow missed. Comfortably longer than any sane pre-warm.
    /// </summary>
    private static readonly TimeSpan DotsMaxLifetime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Last-resort handoff, independent of <see cref="OnAppearing"/> ever firing.
    /// </summary>
    /// <remarks>
    /// <see cref="SplashDeadline"/> only helps once the sequence has <em>started</em>, and it is
    /// started from <c>OnAppearing</c>. A page whose <c>OnAppearing</c> never fires therefore has
    /// no escape at all — which is what a stuck-at-full-opacity splash with unlit, unpulsing dots
    /// looks like, and it was seen on roughly 7% of launches during a long UI-test run that cycles
    /// terminate/activate 41 times.
    /// </remarks>
    private static readonly TimeSpan HandoffFailsafe = TimeSpan.FromSeconds(12);

    private readonly AppShell _appShell;

    /// <summary>
    /// A field, not a local, so the deadline path can stop the dots loop. That loop exits only
    /// on cancellation, so abandoning it un-cancelled leaves it spinning for the life of the
    /// process.
    /// </summary>
    private readonly CancellationTokenSource _dotsCts = new();

    private bool _hasNavigated;
    private bool _hasStarted;

    public SplashPage(AppShell appShell)
    {
        _appShell = appShell;
        InitializeComponent();

        // Started from the CONSTRUCTOR, not OnAppearing, which is the whole point: it has to run
        // even if the page never appears.
        StartHandoffFailsafe();
    }

    private void StartHandoffFailsafe() =>
        _ = Task.Delay(HandoffFailsafe).ContinueWith(_ =>
        {
            if (_hasNavigated) return;

            Log($"failsafe firing after {HandoffFailsafe.TotalSeconds:0}s "
                + $"(OnAppearing started: {_hasStarted})");

            try
            {
                MainThread.BeginInvokeOnMainThread(NavigateToShell);
            }
            catch (NotImplementedException)
            {
                // No platform main thread (unit/host context) — nothing to marshal to.
                NavigateToShell();
            }
        });

    /// <summary>
    /// Startup tracing. Tagged so run-ios.sh's log filter passes it; use <c>--logs</c> on Android.
    /// </summary>
    private static void Log(string message) =>
        Console.WriteLine($"[splash] {message}");

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Guard: OnAppearing can fire more than once (e.g. window re-activation).
        if (_hasStarted) return;
        _hasStarted = true;

        Log("OnAppearing — starting splash sequence");

        try
        {
            // The handoff no longer waits on any animation, so this deadline exists purely to
            // bound PreWarmAsync's disk work. Keep it: an await that cannot be bounded is how the
            // splash used to strand users (see the note on RunSplashAsync).
            var splash = RunSplashAsync();

            // Observe a late failure so losing the race cannot raise an unobserved task exception.
            _ = splash.ContinueWith(
                static t => _ = t.Exception,
                TaskContinuationOptions.OnlyOnFaulted);

            await Task.WhenAny(splash, Task.Delay(SplashDeadline));

            // Covers the deadline path: RunSplashAsync cancels this itself on the happy path,
            // but if it never got that far the loop is still running.
            _dotsCts.Cancel();
        }
        catch (Exception ex)
        {
            // Never let the splash trap the user — fall through to the app.
            Log($"splash sequence failed, continuing anyway: {ex.GetType().Name}: {ex.Message}");
        }

        NavigateToShell();
    }

    /// <summary>
    /// Shows the splash for as long as the pre-warm needs, and not a moment longer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is deliberately no entrance animation and no exit fade. Both were removed on
    /// 2026-10-06 because they cost ~1.8s of startup and bought nothing:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// The entrance ran every element in from <c>Opacity="0"</c>, so ~1.55s elapsed before the
    /// pre-warm was even started. The page is now composed at its resting state in XAML and is
    /// simply visible when it renders.
    /// </item>
    /// <item>
    /// The exit <c>FadeTo(0)</c> could not cross-fade into anything: NavigateToShell swaps the
    /// window's page instantly, and it runs AFTER the fade. So those 280ms were spent fading the
    /// splash to blank before a hard cut — the user watched the screen empty out for no reason.
    /// </item>
    /// </list>
    /// <para>
    /// Removing them also removed a hang: MAUI animations are driven by the platform animator,
    /// which may render an animation and never signal its task, so awaiting one can block
    /// forever. That stranded the app on a fully-faded splash on iOS, and is the same mechanism
    /// as the documented Android <c>animator_duration_scale=0</c> hang. The only animation left
    /// is the dots pulse, which is fire-and-forget and never gates the handoff.
    /// </para>
    /// </remarks>
    private async Task RunSplashAsync()
    {
        // The only animation left: a progress hint for a slow pre-warm. Fire-and-forget by
        // design — nothing below waits on it.
        _ = AnimateDotsAsync(_dotsCts.Token);

        var prewarm = Task.Run(PreWarmAsync);

        // WhenAll, so the splash lasts for whichever is slower: the real work, or the floor that
        // stops a fast pre-warm turning it into a flicker.
        await Task.WhenAll(prewarm, Task.Delay(MinimumVisible));

        _dotsCts.Cancel();
    }

    /// <summary>
    /// Pulses the loading dots while the pre-warm runs. Cosmetic, fire-and-forget.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These awaits must stay uncapped.</b> Capping them looks like an improvement — a lost
    /// animation completion would otherwise freeze the dots mid-pulse — but it caused a far worse
    /// failure, so do not "fix" it again:
    /// </para>
    /// <para>
    /// Each iteration was raced against a timeout. When completions never arrive (a wedged
    /// simulator, or any device where the animator is not ticking) the loop abandoned the
    /// in-flight animation and started a new one every ~340ms, three dots, without end. Every
    /// abandoned MAUI animation stays registered with the animation ticker, so the UI thread
    /// accumulated thousands of them — about 9,000 over an 18-minute run.
    /// </para>
    /// <para>
    /// That starves the UI thread, and <see cref="SplashDeadline"/> is awaited with the UI
    /// SynchronizationContext captured: its continuation can only run on that thread, so a
    /// flooded thread means <c>NavigateToShell</c> never runs. The cap defeated the very deadline
    /// meant to rescue the hang. Uncapped, a lost completion simply parks this loop on one pending
    /// animation, which costs nothing and cannot affect the handoff.
    /// </para>
    /// <para>
    /// The hard lifetime below is belt and braces: the loop stops on its own even if the token is
    /// never cancelled, so it can never outlive the splash.
    /// </para>
    /// </remarks>
    private async Task AnimateDotsAsync(CancellationToken token)
    {
        var dots = new[] { Dot1, Dot2, Dot3 };
        var deadline = DateTime.UtcNow + DotsMaxLifetime;

        try
        {
            while (!token.IsCancellationRequested && DateTime.UtcNow < deadline)
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

        // 2. Pre-inflating BudgetView here was measured at 8.6s of *blocking main-thread* work
        //    (Android Debug/emulator), which is paid on every single launch to make one later tab
        //    tap feel instant. That is the wrong side of the trade: it is the single largest
        //    contributor to a 23s cold start, and the user waits for it before seeing anything.
        //
        //    BudgetView is a DI singleton, so the first Budget tap builds it once and every tab
        //    switch after that is free — measured at 0.0ms in OnAppearing.
        //
        //    The data load above (step 1) is kept: it is genuinely async, off the UI thread, and
        //    is what BudgetView needs in order to render without a second pass.
        //
        //    NOTE: LoanView was deliberately never pre-built here — it is the landing page and
        //    hosts the full-screen disclaimer SfPopup, which flashes if built off-screen.
    }

    private void NavigateToShell()
    {
        if (_hasNavigated) return;
        _hasNavigated = true;

        // Wrapped: this used to sit outside any try, so a failure here vanished into an async void
        // and left the user looking at the splash with nothing logged.
        try
        {
            Application.Current!.Windows[0].Page = _appShell;
            Log("handed off to AppShell");
        }
        catch (Exception ex)
        {
            _hasNavigated = false;
            Log($"HANDOFF FAILED, splash will remain: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
