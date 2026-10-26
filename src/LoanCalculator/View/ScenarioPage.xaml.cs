using LoanCalculator.Core.Constants;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Controls;
using LoanCalculatorMaui.Controls.Scenarios;

namespace LoanCalculatorMaui.View;

/// <summary>
/// Hosts one What If scenario, chosen at open time by <see cref="Configure"/>.
/// </summary>
/// <remarks>
/// <para>Follows the modal pattern established by <see cref="LoanDetailsPage"/>: a
/// <c>Task&lt;bool&gt; Completion</c> the launcher awaits, every dismissal route handled, and an
/// <c>_isClosing</c> guard. Nothing here commits anything — a scenario is read-only exploration,
/// so <see cref="Completion"/> always reports <c>false</c>; it exists so the launcher can clear
/// its re-entrancy latch rather than to signal a result.</para>
/// <para><b>This page deliberately does not save.</b> <c>WhatIfView</c> owns persisting the
/// view model on its own <c>OnDisappearing</c>. Nine scenario pages each saving as they closed
/// would race on one JSON file — the same class of corruption <c>CLAUDE.md</c> documents for
/// <c>namevaluedata.json</c>, where two un-coordinated fire-and-forget writes interleaved and
/// produced a file that threw on every subsequent launch.</para>
/// </remarks>
public partial class ScenarioPage : ContentPage
{
    private readonly WhatIfViewModel _viewModel;
    private readonly IErrorHandlingService _errorHandlingService;

    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _isClosing;

    public ScenarioPage(WhatIfViewModel viewModel, IErrorHandlingService errorHandlingService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _errorHandlingService = errorHandlingService;

        // The scenario views bind against this and inherit it, so their markup resolves
        // exactly as it did when they were cards on WhatIfView.
        BindingContext = _viewModel;
    }

    /// <summary>Completes when the page is dismissed. Always <c>false</c> — see the remarks.</summary>
    public Task<bool> Completion => _completion.Task;

    /// <summary>
    /// Shows one scenario, followed by its collapsed explanation card.
    /// </summary>
    /// <remarks>
    /// The explanation is appended here rather than added to each of the nine scenario views: one
    /// insertion point, one consistent position, and it inherits this page's BindingContext so the
    /// personalised line resolves without any per-view wiring.
    ///
    /// The title now comes from the explanation record too. It used to have its own
    /// <c>TitleOf</c> switch, which was a second table of per-scenario metadata to keep in step
    /// with the first.
    /// </remarks>
    public void Configure(WhatIfScenario scenario)
    {
        var explanation = ScenarioExplanations.For(scenario);

        lblTitle.Text = explanation.Title;

        // Tells the view model which scenario's personalised line to serve. Set before the card is
        // added so the first binding evaluation already has the right one.
        _viewModel.ActiveScenario = scenario;

        ScenarioHost.Children.Clear();
        ScenarioHost.Children.Add(ViewFor(scenario));
        ScenarioHost.Children.Add(new ScenarioExplainerView { Explanation = explanation });
    }

    // ContentView rather than View: this file's own namespace is LoanCalculatorMaui.View, which
    // shadows Microsoft.Maui.Controls.View. All nine scenarios are ContentViews regardless.
    private static ContentView ViewFor(WhatIfScenario scenario) => scenario switch
    {
        WhatIfScenario.RateChange => new RateChangeScenarioView(),
        WhatIfScenario.ExtraRepayment => new ExtraRepaymentScenarioView(),
        WhatIfScenario.TermComparison => new TermComparisonScenarioView(),
        WhatIfScenario.DepositScenarios => new DepositScenariosScenarioView(),
        WhatIfScenario.LumpSum => new LumpSumScenarioView(),
        WhatIfScenario.RepaymentFrequency => new RepaymentFrequencyScenarioView(),
        WhatIfScenario.Offset => new OffsetScenarioView(),
        WhatIfScenario.StressTest => new StressTestScenarioView(),
        WhatIfScenario.Combined => new CombinedScenarioView(),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
    };

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if IOS || MACCATALYST
        ApplyExpanderTouchFix();
#endif
    }

#if IOS || MACCATALYST
    /// <summary>
    /// Makes the explanation card's expander header tappable on iOS.
    /// </summary>
    /// <remarks>
    /// Syncfusion adds a drawing layer as the last sibling of the expander's own views, which on
    /// iOS means topmost; it accepts the touch and handles nothing, so the header silently does
    /// nothing. <c>ApplyToExpanders</c> disables it. See "Syncfusion iOS drawing overlays swallow
    /// taps" in CLAUDE.md — the matcher there keys on structure, not on gesture-recognizer count,
    /// because this particular overlay reports five of them.
    ///
    /// Two passes, copying <see cref="AmortisationPage"/>. The card is added by
    /// <see cref="Configure"/> and has not laid out when OnAppearing first runs, and Syncfusion
    /// rebuilds the overlay on later layout passes — so a single pass fixes nothing here.
    /// </remarks>
    private void ApplyExpanderTouchFix()
    {
        Extensions.SyncfusionIosTouchFix.ApplyToExpanders(this);

        // Fire-and-forget: awaiting would hold up OnAppearing for no benefit.
        _ = Task.Run(async () =>
        {
            await Task.Delay(150);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    Extensions.SyncfusionIosTouchFix.ApplyToExpanders(this);
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

        // Fallback for dismissal routes this page does not drive — the iPad/MacCatalyst sheet
        // swipe, or an exception between push and pop. TrySetResult means a real outcome wins.
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
}
