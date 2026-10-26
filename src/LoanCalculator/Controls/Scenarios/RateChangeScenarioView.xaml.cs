using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Extensions;
using Syncfusion.Maui.Charts;

namespace LoanCalculatorMaui.Controls.Scenarios;

public partial class RateChangeScenarioView : ContentView
{
    public RateChangeScenarioView() => InitializeComponent();

    /// <summary>
    /// The host page sets the WhatIfViewModel singleton as its BindingContext and this view
    /// inherits it. Typed here so the handlers below read the same as they did on WhatIfView.
    /// </summary>
    private WhatIfViewModel? Vm => BindingContext as WhatIfViewModel;

    private void OnRateDeltaDecrease(object sender, EventArgs e)
        => Vm!.RateChangeDelta = Math.Round(Vm!.RateChangeDelta - SharedServiceCore.GetWhatIfRateStep(), 2);
    private void OnRateDeltaIncrease(object sender, EventArgs e)
        => Vm!.RateChangeDelta = Math.Round(Vm!.RateChangeDelta + SharedServiceCore.GetWhatIfRateStep(), 2);
    private void OnWhatIfAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
        => ChartAxisFormatter.Money(e, Vm?.CurrencySymbol);
}
