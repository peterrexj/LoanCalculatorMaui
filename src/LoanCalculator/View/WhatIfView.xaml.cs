using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using Syncfusion.Maui.Charts;

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

    private void OnOffsetRateIncrease(object sender, EventArgs e)
        => _viewModel.OffsetRate = Math.Round(_viewModel.OffsetRate + 0.25, 2);

    private void OnOffsetRateDecrease(object sender, EventArgs e)
        => _viewModel.OffsetRate = Math.Round(_viewModel.OffsetRate - 0.25, 2);

    private void OnCombinedExtraIncrease(object sender, EventArgs e)        => _viewModel.CombinedExtraMonthly += 100;
    private void OnCombinedExtraDecrease(object sender, EventArgs e)        { if (_viewModel.CombinedExtraMonthly >= 100) _viewModel.CombinedExtraMonthly -= 100; }
    private void OnCombinedLumpIncrease(object sender, EventArgs e)         => _viewModel.CombinedLumpSum += 5000;
    private void OnCombinedLumpDecrease(object sender, EventArgs e)         { if (_viewModel.CombinedLumpSum >= 5000) _viewModel.CombinedLumpSum -= 5000; }
    private void OnCombinedOffsetIncrease(object sender, EventArgs e)       => _viewModel.CombinedOffset += 5000;
    private void OnCombinedOffsetDecrease(object sender, EventArgs e)       { if (_viewModel.CombinedOffset >= 5000) _viewModel.CombinedOffset -= 5000; }
    private void OnCombinedOffsetRateIncrease(object sender, EventArgs e)   => _viewModel.CombinedOffsetRate = Math.Round(_viewModel.CombinedOffsetRate + 0.25, 2);
    private void OnCombinedOffsetRateDecrease(object sender, EventArgs e)   => _viewModel.CombinedOffsetRate = Math.Round(_viewModel.CombinedOffsetRate - 0.25, 2);
    private void OnCombinedFreqMonthly(object sender, EventArgs e)          => _viewModel.CombinedFrequencyIndex = 0;
    private void OnCombinedFreqFortnightly(object sender, EventArgs e)      => _viewModel.CombinedFrequencyIndex = 1;
    private void OnCombinedFreqWeekly(object sender, EventArgs e)           => _viewModel.CombinedFrequencyIndex = 2;

    private async void OnFrequencyInfoTapped(object sender, EventArgs e)
    {
        await DisplayAlert(
            "How does repayment frequency save time?",
            "Paying fortnightly (half the monthly amount, 26 times a year) quietly makes 13 monthly-equivalent payments per year instead of 12 — one extra payment annually.\n\n" +
            "That extra payment goes entirely to principal, reducing the balance faster. Compounded over a 25–30 year term, this typically saves 4–6 years and tens of thousands in interest.\n\n" +
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
            "Breaks Even At — the rate at which your repayment exactly equals your income minus all expenses (zero surplus left). Above this rate the loan becomes unaffordable.\n\n" +
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
