using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using Syncfusion.Maui.Charts;

namespace LoanCalculatorMaui.View;

public partial class WhatIfView : ContentPage
{
    private readonly WhatIfViewModel _viewModel;
    private readonly LoanViewModel _loanViewModel;
    private readonly IncomeViewModel _incomeViewModel;
    private readonly ExpenseViewModel _expenseViewModel;
    private bool _hasLoadedOnce;

    public WhatIfView(
        WhatIfViewModel viewModel,
        LoanViewModel loanViewModel,
        IncomeViewModel incomeViewModel,
        ExpenseViewModel expenseViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _loanViewModel = loanViewModel;
        _incomeViewModel = incomeViewModel;
        _expenseViewModel = expenseViewModel;
        _viewModel.SetLoanViewModel(_loanViewModel);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // The stress test reads _loanViewModel.IsAffordabilityAvailable and MonthlySurplus, both
        // derived from the cached HasIncomeExpensesRecorded flag. Only the Loan tab used to refresh
        // that, so arriving straight from Budget computed the stress test from a stale cache.
        // The dirty flags stay set — LoanView still owns clearing them and its notification pass.
        if (SharedServiceCore.IsIncomeDirty || SharedServiceCore.IsExpenseDirty)
            await _loanViewModel.RefreshIncomeExpenseSummariesAsync(_incomeViewModel, _expenseViewModel);

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
                if (saved.OffsetRate > 0) _viewModel.OffsetRate = saved.OffsetRate;
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
        => _viewModel.RateChangeDelta = Math.Round(_viewModel.RateChangeDelta + SharedServiceCore.GetWhatIfRateStep(), 2);

    private void OnRateDeltaDecrease(object sender, EventArgs e)
        => _viewModel.RateChangeDelta = Math.Round(_viewModel.RateChangeDelta - SharedServiceCore.GetWhatIfRateStep(), 2);

    private void OnExtraRepaymentIncrease(object sender, EventArgs e)
        => _viewModel.ExtraRepaymentMonthly = _viewModel.ExtraRepaymentMonthly + SharedServiceCore.GetWhatIfMonthlyStep();

    private void OnExtraRepaymentDecrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetWhatIfMonthlyStep();
        if (_viewModel.ExtraRepaymentMonthly > step)
            _viewModel.ExtraRepaymentMonthly = _viewModel.ExtraRepaymentMonthly - step;
    }

    private void OnLumpSumIncrease(object sender, EventArgs e)
        => _viewModel.LumpSumAmount = _viewModel.LumpSumAmount + SharedServiceCore.GetWhatIfLumpSumStep();

    private void OnLumpSumDecrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetWhatIfLumpSumStep();
        if (_viewModel.LumpSumAmount > step)
            _viewModel.LumpSumAmount = _viewModel.LumpSumAmount - step;
    }

    private void OnOffsetIncrease(object sender, EventArgs e)
        => _viewModel.OffsetBalance = _viewModel.OffsetBalance + SharedServiceCore.GetWhatIfOffsetStep();

    private void OnOffsetDecrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetWhatIfOffsetStep();
        if (_viewModel.OffsetBalance > step)
            _viewModel.OffsetBalance = _viewModel.OffsetBalance - step;
    }

    private void OnOffsetRateIncrease(object sender, EventArgs e)
        => _viewModel.OffsetRate = Math.Round(_viewModel.OffsetRate + SharedServiceCore.GetWhatIfRateStep(), 2);

    private void OnOffsetRateDecrease(object sender, EventArgs e)
        => _viewModel.OffsetRate = Math.Round(_viewModel.OffsetRate - SharedServiceCore.GetWhatIfRateStep(), 2);

    private void OnCombinedExtraIncrease(object sender, EventArgs e)        => _viewModel.CombinedExtraMonthly += SharedServiceCore.GetWhatIfMonthlyStep();
    private void OnCombinedExtraDecrease(object sender, EventArgs e)        { var s = SharedServiceCore.GetWhatIfMonthlyStep(); if (_viewModel.CombinedExtraMonthly >= s) _viewModel.CombinedExtraMonthly -= s; }
    private void OnCombinedLumpIncrease(object sender, EventArgs e)         => _viewModel.CombinedLumpSum += SharedServiceCore.GetWhatIfLumpSumStep();
    private void OnCombinedLumpDecrease(object sender, EventArgs e)         { var s = SharedServiceCore.GetWhatIfLumpSumStep(); if (_viewModel.CombinedLumpSum >= s) _viewModel.CombinedLumpSum -= s; }
    private void OnCombinedOffsetIncrease(object sender, EventArgs e)       => _viewModel.CombinedOffset += SharedServiceCore.GetWhatIfOffsetStep();
    private void OnCombinedOffsetDecrease(object sender, EventArgs e)       { var s = SharedServiceCore.GetWhatIfOffsetStep(); if (_viewModel.CombinedOffset >= s) _viewModel.CombinedOffset -= s; }
    private void OnCombinedOffsetRateIncrease(object sender, EventArgs e)   => _viewModel.CombinedOffsetRate = Math.Round(_viewModel.CombinedOffsetRate + SharedServiceCore.GetWhatIfRateStep(), 2);
    private void OnCombinedOffsetRateDecrease(object sender, EventArgs e)   => _viewModel.CombinedOffsetRate = Math.Round(_viewModel.CombinedOffsetRate - SharedServiceCore.GetWhatIfRateStep(), 2);
    private void OnCombinedFreqMonthly(object sender, EventArgs e)          => _viewModel.CombinedFrequencyIndex = 0;
    private void OnCombinedFreqFortnightly(object sender, EventArgs e)      => _viewModel.CombinedFrequencyIndex = 1;
    private void OnCombinedFreqWeekly(object sender, EventArgs e)           => _viewModel.CombinedFrequencyIndex = 2;

    private async void OnFrequencyInfoTapped(object sender, EventArgs e)
    {
        await DisplayAlert(
            "How does repayment frequency save time?",
            "Paying fortnightly (half the monthly amount, 26 times a year) quietly makes 13 monthly-equivalent payments per year instead of 12 — one extra payment annually.\n\n" +
            "That extra payment goes entirely to principal, reducing the balance faster. Compounded over a 25–30 year term, this could save several years and a significant amount of interest — actual results vary with your rate, balance and repayments.\n\n" +
            "Weekly works the same way: 52 × (monthly ÷ 4) = 13 monthly equivalents per year. The saving is almost identical to fortnightly.",
            "Got it");
    }

    private async void OnOffsetInfoTapped(object sender, EventArgs e)
    {
        await DisplayAlert(
            "How does an offset account work?",
            "An offset account is a savings or transaction account linked to your loan. The bank charges interest only on the difference between your loan balance and your offset balance.\n\n" +
            "Example: $600,000 loan with $20,000 offset → interest is charged on $580,000.\n\n" +
            "Your repayment amount stays the same, so more of each payment hits principal instead of interest. This is what shortens the loan term — the time saving is real, not just an interest reduction.\n\n" +
            "The Rate control lets you model scenarios where your lender applies the offset at a different effective rate than your loan rate.",
            "Got it");
    }

    private async void OnStressTestInfoTapped(object sender, EventArgs e)
    {
        await DisplayAlert(
            "How is this calculated?",
            "Breaks Even At — the estimated rate at which your repayment would equal the income minus expenses you entered (zero surplus left). Above this estimated rate, your repayment would exceed the income you entered. These are estimates, not advice.\n\n" +
            "Your Buffer — the gap between the break-even rate and your current rate. A larger buffer means you can absorb more rate rises.\n\n" +
            "Current Monthly Surplus — your income minus expenses minus the current repayment. This is your breathing room right now.",
            "Got it");
    }

    private void OnWhatIfAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
    {
        if (!double.TryParse(e.Label, out var val)) return;
        var sym = _viewModel?.CurrencySymbol ?? "$";
        e.Label = Math.Abs(val) >= 1_000_000
            ? $"{sym}{val / 1_000_000:0.#}M"
            : Math.Abs(val) >= 1_000
                ? $"{sym}{val / 1_000:0.#}K"
                : $"{sym}{val:0}";
    }

    private void OnWhatIfRateAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
    {
        if (double.TryParse(e.Label, out var val))
            e.Label = $"{val:0.##}%";
    }
}
