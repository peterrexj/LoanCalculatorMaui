using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.View;

public partial class WhatIfView : ContentPage
{
    private readonly WhatIfViewModel _viewModel;
    private readonly LoanViewModel _loanViewModel;
    private bool _hasLoadedOnce;

    public WhatIfView(WhatIfViewModel viewModel, LoanViewModel loanViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _loanViewModel = loanViewModel;
        _viewModel.SetLoanViewModel(_loanViewModel);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _viewModel.SetLoanViewModel(_loanViewModel);

        if (!_hasLoadedOnce)
        {
            _hasLoadedOnce = true;
            var saved = await SharedServiceCore.LoadDataFile<WhatIfViewModel>();
            if (saved != null)
            {
                _viewModel.RateChangeDelta       = saved.RateChangeDelta;
                _viewModel.ExtraRepaymentMonthly = saved.ExtraRepaymentMonthly;
                _viewModel.LumpSumAmount         = saved.LumpSumAmount;
                _viewModel.OffsetBalance         = saved.OffsetBalance;
                // RepaymentFrequencyIndex is always synced from the loan, not restored
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SharedServiceCore.SaveData(_viewModel);
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

    private void OnLumpSumIncrease(object sender, EventArgs e)
        => _viewModel.LumpSumAmount = _viewModel.LumpSumAmount + 5000;

    private void OnLumpSumDecrease(object sender, EventArgs e)
    {
        if (_viewModel.LumpSumAmount > 5000)
            _viewModel.LumpSumAmount = _viewModel.LumpSumAmount - 5000;
    }

    private void OnOffsetIncrease(object sender, EventArgs e)
        => _viewModel.OffsetBalance = _viewModel.OffsetBalance + 5000;

    private void OnOffsetDecrease(object sender, EventArgs e)
    {
        if (_viewModel.OffsetBalance > 5000)
            _viewModel.OffsetBalance = _viewModel.OffsetBalance - 5000;
    }
}
