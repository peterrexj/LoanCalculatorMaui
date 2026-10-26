using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Extensions;
using Syncfusion.Maui.Charts;

namespace LoanCalculatorMaui.Controls.Scenarios;

public partial class CombinedScenarioView : ContentView
{
    public CombinedScenarioView() => InitializeComponent();

    /// <summary>
    /// The host page sets the WhatIfViewModel singleton as its BindingContext and this view
    /// inherits it. Typed here so the handlers below read the same as they did on WhatIfView.
    /// </summary>
    private WhatIfViewModel? Vm => BindingContext as WhatIfViewModel;

    private void OnCombinedExtraDecrease(object sender, EventArgs e)        { var s = SharedServiceCore.GetWhatIfMonthlyStep(); if (Vm!.CombinedExtraMonthly >= s) Vm!.CombinedExtraMonthly -= s; }
    private void OnCombinedExtraIncrease(object sender, EventArgs e)        => Vm!.CombinedExtraMonthly += SharedServiceCore.GetWhatIfMonthlyStep();
    private void OnCombinedFreqFortnightly(object sender, EventArgs e)      => Vm!.CombinedFrequencyIndex = 1;
    private void OnCombinedFreqMonthly(object sender, EventArgs e)          => Vm!.CombinedFrequencyIndex = 0;
    private void OnCombinedFreqWeekly(object sender, EventArgs e)           => Vm!.CombinedFrequencyIndex = 2;
    private void OnCombinedLumpDecrease(object sender, EventArgs e)         { var s = SharedServiceCore.GetWhatIfLumpSumStep(); if (Vm!.CombinedLumpSum >= s) Vm!.CombinedLumpSum -= s; }
    private void OnCombinedLumpIncrease(object sender, EventArgs e)         => Vm!.CombinedLumpSum += SharedServiceCore.GetWhatIfLumpSumStep();
    private void OnCombinedOffsetDecrease(object sender, EventArgs e)       { var s = SharedServiceCore.GetWhatIfOffsetStep(); if (Vm!.CombinedOffset >= s) Vm!.CombinedOffset -= s; }
    private void OnCombinedOffsetIncrease(object sender, EventArgs e)       => Vm!.CombinedOffset += SharedServiceCore.GetWhatIfOffsetStep();
    private void OnCombinedOffsetRateDecrease(object sender, EventArgs e)   => Vm!.CombinedOffsetRate = Math.Round(Vm!.CombinedOffsetRate - SharedServiceCore.GetWhatIfRateStep(), 2);
    private void OnCombinedOffsetRateIncrease(object sender, EventArgs e)   => Vm!.CombinedOffsetRate = Math.Round(Vm!.CombinedOffsetRate + SharedServiceCore.GetWhatIfRateStep(), 2);
    private void OnWhatIfAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
        => ChartAxisFormatter.Money(e, Vm?.CurrencySymbol);
}
