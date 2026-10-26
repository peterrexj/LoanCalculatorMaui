using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Extensions;
using Syncfusion.Maui.Charts;

namespace LoanCalculatorMaui.Controls.Scenarios;

public partial class LumpSumScenarioView : ContentView
{
    public LumpSumScenarioView() => InitializeComponent();

    /// <summary>
    /// The host page sets the WhatIfViewModel singleton as its BindingContext and this view
    /// inherits it. Typed here so the handlers below read the same as they did on WhatIfView.
    /// </summary>
    private WhatIfViewModel? Vm => BindingContext as WhatIfViewModel;

    private void OnLumpSumDecrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetWhatIfLumpSumStep();
        if (Vm!.LumpSumAmount > step)
            Vm!.LumpSumAmount = Vm!.LumpSumAmount - step;
    }
    private void OnLumpSumIncrease(object sender, EventArgs e)
        => Vm!.LumpSumAmount = Vm!.LumpSumAmount + SharedServiceCore.GetWhatIfLumpSumStep();
    private void OnWhatIfAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
        => ChartAxisFormatter.Money(e, Vm?.CurrencySymbol);
}
