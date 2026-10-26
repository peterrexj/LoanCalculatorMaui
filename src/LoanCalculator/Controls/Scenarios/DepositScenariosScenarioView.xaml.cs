using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Extensions;

namespace LoanCalculatorMaui.Controls.Scenarios;

public partial class DepositScenariosScenarioView : ContentView
{
    public DepositScenariosScenarioView() => InitializeComponent();

    /// <summary>
    /// The host page sets the WhatIfViewModel singleton as its BindingContext and this view
    /// inherits it. Typed here so the handlers below read the same as they did on WhatIfView.
    /// </summary>
    private WhatIfViewModel? Vm => BindingContext as WhatIfViewModel;

    // Display only — no interaction.
}
