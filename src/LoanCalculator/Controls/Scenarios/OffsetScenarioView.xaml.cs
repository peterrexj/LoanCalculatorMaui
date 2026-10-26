using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Extensions;
using Syncfusion.Maui.Charts;

namespace LoanCalculatorMaui.Controls.Scenarios;

public partial class OffsetScenarioView : ContentView
{
    public OffsetScenarioView() => InitializeComponent();

    /// <summary>
    /// The host page sets the WhatIfViewModel singleton as its BindingContext and this view
    /// inherits it. Typed here so the handlers below read the same as they did on WhatIfView.
    /// </summary>
    private WhatIfViewModel? Vm => BindingContext as WhatIfViewModel;

    private void OnOffsetDecrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetWhatIfOffsetStep();
        if (Vm!.OffsetBalance > step)
            Vm!.OffsetBalance = Vm!.OffsetBalance - step;
    }
    private void OnOffsetIncrease(object sender, EventArgs e)
        => Vm!.OffsetBalance = Vm!.OffsetBalance + SharedServiceCore.GetWhatIfOffsetStep();
    private void OnOffsetRateDecrease(object sender, EventArgs e)
        => Vm!.OffsetRate = Math.Round(Vm!.OffsetRate - SharedServiceCore.GetWhatIfRateStep(), 2);
    private void OnOffsetRateIncrease(object sender, EventArgs e)
        => Vm!.OffsetRate = Math.Round(Vm!.OffsetRate + SharedServiceCore.GetWhatIfRateStep(), 2);
    private void OnWhatIfAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
        => ChartAxisFormatter.Money(e, Vm?.CurrencySymbol);
}
