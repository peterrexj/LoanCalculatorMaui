using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Extensions;
using Syncfusion.Maui.Charts;

namespace LoanCalculatorMaui.View;

/// <summary>
/// The repayment schedule, previously LoanView's Amortisation tab.
/// </summary>
/// <remarks>
/// <para>This page inherits three obligations that <c>LoanView.TabView_OnSelectionChanging</c>
/// used to discharge when its index-1 tab was selected. Every one of them fails <b>silently</b>
/// if dropped, which is why they are spelled out here rather than left to be inferred:</para>
/// <list type="bullet">
///   <item><c>SyncAmortization()</c> — without it the table shows the previous loan's schedule.</item>
///   <item><c>IsAmortizationTabActive</c> — this flag gates chart updates inside
///   <c>TriggerPropertyChangedOnPropertyTab</c>. Left false the chart never redraws; left true it
///   recomputes on every notification for a page nobody is looking at. Hence set on appearing and
///   cleared on disappearing.</item>
///   <item><c>CustomChartColors</c> — LoanView refreshed these on every appearance so a theme
///   change made in Settings showed up here. A pushed page has to do its own.</item>
/// </list>
/// <para>It does not save: the loan is owned and persisted by LoanView.</para>
/// </remarks>
public partial class AmortisationPage : ContentPage
{
    private readonly LoanViewModel _viewModel;
    private readonly IThemeHandler _themeHandler;
    private readonly IErrorHandlingService _errorHandlingService;

    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _isClosing;

    public AmortisationPage(
        LoanViewModel viewModel,
        IThemeHandler themeHandler,
        IErrorHandlingService errorHandlingService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _themeHandler = themeHandler;
        _errorHandlingService = errorHandlingService;
        BindingContext = _viewModel;
    }

    /// <summary>Completes on dismissal. Always false — nothing here commits.</summary>
    public Task<bool> Completion => _completion.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Guarded, and that guard is load-bearing. On LoanView this same work ran inside
        // TabView_OnSelectionChanging's try/catch. Moved here unguarded, a throw out of
        // SyncAmortization propagates through MAUI to the native marshal boundary and takes
        // the whole app down rather than surfacing — which is exactly what it did: opening the
        // schedule dropped the app to the iOS home screen with no error anywhere.
        try
        {
            _viewModel.IsAmortizationTabActive = true;
            _viewModel.CustomChartColors = _themeHandler.GetChartColors();
            _viewModel.SyncAmortization();

            // The decade list is a plain getter off PaymentAmortization, so it has to be asked
            // for again after SyncAmortization recomputes the terms or the page keeps whatever
            // the previous loan produced.
            _viewModel.RaiseAmortisationDecadesChanged();

#if IOS || MACCATALYST
            // This page had no expanders until the schedule became decade disclosures. On iOS a
            // Syncfusion expander ships a drawing overlay as its last sibling that swallows the
            // header tap — so without this, every decade here looks inert. CLAUDE.md records three
            // separate occurrences of exactly this.
            //
            // Called twice for the reason LoanView calls it twice: Syncfusion rebuilds the overlay
            // on later layout passes, so a single pass at OnAppearing fixes views that do not exist
            // yet. The delayed pass is deliberately not awaited.
            ApplyExpanderTouchFix();
#endif
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

#if IOS || MACCATALYST
    private void ApplyExpanderTouchFix()
    {
        SyncfusionIosTouchFix.ApplyToExpanders(this);

        // Second pass once the decade rows have actually laid out. Fire-and-forget: awaiting it
        // would hold up OnAppearing for no benefit, and the first pass already covers what exists.
        _ = Task.Run(async () =>
        {
            await Task.Delay(150);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    SyncfusionIosTouchFix.ApplyToExpanders(this);
                }
                catch (Exception ex)
                {
                    _errorHandlingService.HandleException(ex);
                }
            });
        });
    }
#endif

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Clearing this matters: left set, the amortisation chart recomputes on every
        // notification for a page that is no longer on screen.
        try
        {
            _viewModel.IsAmortizationTabActive = false;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }

        // Fallback for dismissal routes this page does not drive — the iPad sheet swipe, or an
        // exception between push and pop. TrySetResult means a real outcome wins.
        _completion.TrySetResult(false);
    }

    /// <summary>Android's hardware BACK would otherwise pop without releasing the launcher's latch.</summary>
    protected override bool OnBackButtonPressed()
    {
        Dismiss();
        return true;
    }

    private void OnBack(object sender, EventArgs e) => Dismiss();

    private async void Dismiss()
    {
        if (_isClosing) return;
        _isClosing = true;

        try
        {
            _completion.TrySetResult(false);

            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void OnAmortizationAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
        => ChartAxisFormatter.Money(e, _viewModel?.CurrencySymbol);
}
