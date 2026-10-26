using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Extensions;
using Syncfusion.Maui.Charts;

namespace LoanCalculatorMaui.Controls.Scenarios;

public partial class ExtraRepaymentScenarioView : ContentView
{
    public ExtraRepaymentScenarioView() => InitializeComponent();

    /// <summary>
    /// The host page sets the WhatIfViewModel singleton as its BindingContext and this view
    /// inherits it. Typed here so the handlers below read the same as they did on WhatIfView.
    /// </summary>
    private WhatIfViewModel? Vm => BindingContext as WhatIfViewModel;

    private void OnExtraRepaymentDecrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetWhatIfMonthlyStep();
        if (Vm!.ExtraRepaymentMonthly > step)
            Vm!.ExtraRepaymentMonthly = Vm!.ExtraRepaymentMonthly - step;
    }
    private void OnExtraRepaymentIncrease(object sender, EventArgs e)
        => Vm!.ExtraRepaymentMonthly = Vm!.ExtraRepaymentMonthly + SharedServiceCore.GetWhatIfMonthlyStep();
    private void OnWhatIfAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
        => ChartAxisFormatter.Money(e, Vm?.CurrencySymbol);
}
