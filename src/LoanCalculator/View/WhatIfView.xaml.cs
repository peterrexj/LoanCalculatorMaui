using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.View;

public partial class WhatIfView : ContentPage
{
    private readonly WhatIfViewModel _viewModel;
    private readonly LoanViewModel _loanViewModel;

    public WhatIfView(WhatIfViewModel viewModel, LoanViewModel loanViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _loanViewModel = loanViewModel;
        _viewModel.SetLoanViewModel(_loanViewModel);
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.SetLoanViewModel(_loanViewModel);
    }

    private void OnRateDeltaIncrease(object sender, EventArgs e)
        => _viewModel.RateChangeDelta = Math.Round(_viewModel.RateChangeDelta + 0.25, 2);

    private void OnRateDeltaDecrease(object sender, EventArgs e)
        => _viewModel.RateChangeDelta = Math.Round(_viewModel.RateChangeDelta - 0.25, 2);

    private void OnExtraRepaymentIncrease(object sender, EventArgs e)
        => _viewModel.ExtraRepaymentMonthly = _viewModel.ExtraRepaymentMonthly + 100;

    private void OnExtraRepaymentDecrease(object sender, EventArgs e)
    {
        if (_viewModel.ExtraRepaymentMonthly > 100)
            _viewModel.ExtraRepaymentMonthly = _viewModel.ExtraRepaymentMonthly - 100;
    }
}
