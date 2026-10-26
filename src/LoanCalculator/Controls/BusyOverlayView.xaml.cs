namespace LoanCalculatorMaui.Controls;

/// <summary>
/// Full-bleed busy scrim for pages whose arrival work is slow enough to look like a hang —
/// What If and Budget both load data, recompute cross-tab summaries and rebuild charts in
/// OnAppearing, during which the page is already on screen but showing nothing useful.
///
/// Drop it in as the LAST child of a page's root <c>Grid</c> so it covers the content, and
/// drive it with <see cref="Show"/>/<see cref="Hide"/> from a try/finally.
/// </summary>
public partial class BusyOverlayView : ContentView
{
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(
            nameof(Message),
            typeof(string),
            typeof(BusyOverlayView),
            "Working…");

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public BusyOverlayView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Hard ceiling on how long the scrim may stay up, whatever the caller does.
    /// </summary>
    /// <remarks>
    /// This blocks input, so a scrim that outlives its work does not merely look wrong — it makes
    /// the page unusable, and the user has no way to dismiss it. The callers all hide it from a
    /// <c>finally</c>, but a <c>finally</c> only runs if the awaited work completes: anything that
    /// stalls (a disk read, a cross-tab refresh, a Syncfusion layout pass) leaves it up for good.
    ///
    /// Same lesson as the splash, recorded in CLAUDE.md: cosmetic or blocking UI must never be
    /// able to outlast the thing it reports on. Better to drop the scrim early and show a
    /// half-built page than to lock the user out of a working one.
    /// </remarks>
    private static readonly TimeSpan MaximumVisible = TimeSpan.FromSeconds(8);

    private CancellationTokenSource? _failsafeCts;

    /// <summary>
    /// Shows the overlay. Safe to call from any thread.
    /// </summary>
    public void Show(string? message = null)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!string.IsNullOrWhiteSpace(message)) Message = message;
            IsVisible = true;
            StartFailsafe();
        });
    }

    /// <summary>
    /// Hides the overlay. Safe to call from any thread, and safe to call when already hidden —
    /// callers put this in a <c>finally</c>, so it has to tolerate the path where Show never ran.
    /// </summary>
    public void Hide()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CancelFailsafe();
            IsVisible = false;
        });
    }

    private void StartFailsafe()
    {
        CancelFailsafe();

        var cts = new CancellationTokenSource();
        _failsafeCts = cts;
        var token = cts.Token;

        // Deliberately not awaited, and deliberately a bare Delay: racing it against the caller's
        // work would need a handle on that work, which the control does not have and should not.
        _ = Task.Delay(MaximumVisible, token).ContinueWith(t =>
        {
            if (t.IsCanceled || token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (token.IsCancellationRequested) return;
                IsVisible = false;
            });
        }, TaskScheduler.Default);
    }

    private void CancelFailsafe()
    {
        _failsafeCts?.Cancel();
        _failsafeCts?.Dispose();
        _failsafeCts = null;
    }
}
