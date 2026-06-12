using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class WhatIfViewModel : ViewModelUiBase
    {
        [JsonIgnore] private readonly IErrorHandlingService _errorHandlingService;
        [JsonIgnore] private LoanViewModel? _loanVm;

        public WhatIfViewModel(IErrorHandlingService errorHandlingService)
        {
            _errorHandlingService = errorHandlingService;
        }

        public void SetLoanViewModel(LoanViewModel loanVm)
        {
            _loanVm = loanVm;
            Recalculate();
        }

        // ── Shared base values ─────────────────────────────────────────────
        private double BasePropertyAmount => _loanVm?.PropertyAmount ?? 0;
        private double BaseLoanAmount => _loanVm?.HomeLoanInfo?.LoanAmountDirectInput ?? 0;
        private double BaseRate => _loanVm?.InterestRate ?? 0;
        private int BaseTerm => _loanVm?.LoanTermInYears ?? 30;
        private int BasePaymentsPerYear => _loanVm?.HomeLoanInfo?.HomeLoanRepaymentRequest?.TotalNumberPaymentPerYear ?? 12;
        private double BaseMonthlyRepayment => _loanVm?.HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0;
        private double BaseTotalInterest => _loanVm?.HomeLoanInfo?.PaymentSummary?.Payment?.TotalInterestPayment ?? 0;
        private double BaseDeposit => _loanVm?.DepositAmountDirectInput ?? 0;

        // ── Scenario 1: Rate Change ────────────────────────────────────────
        [JsonIgnore] private double _rateChangeDelta = 0.5;
        [JsonIgnore]
        public double RateChangeDelta
        {
            get => _rateChangeDelta;
            set { _rateChangeDelta = Math.Round(value, 2); OnPropertyChanged(nameof(RateChangeDelta)); RecalculateRateScenario(); }
        }

        [JsonIgnore] public string RateChangeNewRate => $"{Math.Round(BaseRate + RateChangeDelta, 2)}%";
        [JsonIgnore] public string RateChangeMonthlyRepayment { get; private set; } = "--";
        [JsonIgnore] public string RateChangeMonthlyDiff { get; private set; } = "--";
        [JsonIgnore] public string RateChangeTotalInterest { get; private set; } = "--";
        [JsonIgnore] public bool RateChangeDiffIsPositive { get; private set; }

        private void RecalculateRateScenario()
        {
            if (BaseLoanAmount <= 0) return;
            var newRate = BaseRate + RateChangeDelta;
            if (newRate < 0) newRate = 0;
            var newMonthly = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BaseLoanAmount, newRate, BaseTerm);
            var newTotal = newMonthly * BaseTerm * 12;
            var diff = newMonthly - BaseMonthlyRepayment;
            RateChangeDiffIsPositive = diff > 0;
            RateChangeMonthlyRepayment = $"{CurrencySymbol}{newMonthly:N0}/mo";
            RateChangeMonthlyDiff = $"{(diff >= 0 ? "+" : "")}{CurrencySymbol}{diff:N0}/mo";
            RateChangeTotalInterest = $"{CurrencySymbol}{(newTotal - BaseLoanAmount):N0}";
            OnPropertyChanged(nameof(RateChangeNewRate));
            OnPropertyChanged(nameof(RateChangeMonthlyRepayment));
            OnPropertyChanged(nameof(RateChangeMonthlyDiff));
            OnPropertyChanged(nameof(RateChangeTotalInterest));
            OnPropertyChanged(nameof(RateChangeDiffIsPositive));
        }

        // ── Scenario 2: Extra Repayment ───────────────────────────────────
        [JsonIgnore] private double _extraRepaymentMonthly = 500;
        [JsonIgnore]
        public double ExtraRepaymentMonthly
        {
            get => _extraRepaymentMonthly;
            set { _extraRepaymentMonthly = value; OnPropertyChanged(nameof(ExtraRepaymentMonthly)); RecalculateExtraRepayment(); }
        }

        [JsonIgnore] public string ExtraRepaymentTimeSaved { get; private set; } = "--";
        [JsonIgnore] public string ExtraRepaymentInterestSaved { get; private set; } = "--";
        [JsonIgnore] public string ExtraRepaymentNewPayoff { get; private set; } = "--";

        private void RecalculateExtraRepayment()
        {
            if (BaseLoanAmount <= 0 || BaseRate <= 0) return;
            var (monthsSaved, interestSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(
                BaseLoanAmount, BaseRate, BaseTerm, ExtraRepaymentMonthly);
            var years = monthsSaved / 12;
            var months = monthsSaved % 12;
            ExtraRepaymentTimeSaved = months > 0 ? $"{years}yr {months}mo" : $"{years}yr";
            ExtraRepaymentInterestSaved = $"{CurrencySymbol}{interestSaved:N0}";
            ExtraRepaymentNewPayoff = $"{BaseTerm - years}yr {(12 - months) % 12}mo remaining";
            OnPropertyChanged(nameof(ExtraRepaymentTimeSaved));
            OnPropertyChanged(nameof(ExtraRepaymentInterestSaved));
            OnPropertyChanged(nameof(ExtraRepaymentNewPayoff));
        }

        // ── Scenario 3: Loan Term Comparison ─────────────────────────────
        [JsonIgnore] public string TermComparison20Monthly { get; private set; } = "--";
        [JsonIgnore] public string TermComparison25Monthly { get; private set; } = "--";
        [JsonIgnore] public string TermComparison30Monthly { get; private set; } = "--";
        [JsonIgnore] public string TermComparison20Interest { get; private set; } = "--";
        [JsonIgnore] public string TermComparison25Interest { get; private set; } = "--";
        [JsonIgnore] public string TermComparison30Interest { get; private set; } = "--";

        private void RecalculateTermComparison()
        {
            if (BaseLoanAmount <= 0 || BaseRate <= 0) return;
            foreach (var (term, suffix) in new[] { (20, "20"), (25, "25"), (30, "30") })
            {
                var m = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BaseLoanAmount, BaseRate, term);
                var interest = m * term * 12 - BaseLoanAmount;
                switch (suffix)
                {
                    case "20": TermComparison20Monthly = $"{CurrencySymbol}{m:N0}"; TermComparison20Interest = $"{CurrencySymbol}{interest:N0}"; break;
                    case "25": TermComparison25Monthly = $"{CurrencySymbol}{m:N0}"; TermComparison25Interest = $"{CurrencySymbol}{interest:N0}"; break;
                    case "30": TermComparison30Monthly = $"{CurrencySymbol}{m:N0}"; TermComparison30Interest = $"{CurrencySymbol}{interest:N0}"; break;
                }
            }
            OnPropertyChanged(nameof(TermComparison20Monthly)); OnPropertyChanged(nameof(TermComparison20Interest));
            OnPropertyChanged(nameof(TermComparison25Monthly)); OnPropertyChanged(nameof(TermComparison25Interest));
            OnPropertyChanged(nameof(TermComparison30Monthly)); OnPropertyChanged(nameof(TermComparison30Interest));
        }

        // ── Scenario 4: Deposit Scenarios ────────────────────────────────
        [JsonIgnore] public string Deposit10PcMonthly { get; private set; } = "--";
        [JsonIgnore] public string Deposit20PcMonthly { get; private set; } = "--";
        [JsonIgnore] public string Deposit30PcMonthly { get; private set; } = "--";
        [JsonIgnore] public string Deposit10PcLoan { get; private set; } = "--";
        [JsonIgnore] public string Deposit20PcLoan { get; private set; } = "--";
        [JsonIgnore] public string Deposit30PcLoan { get; private set; } = "--";

        private void RecalculateDepositScenarios()
        {
            if (BasePropertyAmount <= 0 || BaseRate <= 0) return;
            foreach (var pct in new[] { 0.10, 0.20, 0.30 })
            {
                var loan = BasePropertyAmount * (1 - pct);
                var monthly = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(loan, BaseRate, BaseTerm);
                var label = pct == 0.10 ? "10Pc" : pct == 0.20 ? "20Pc" : "30Pc";
                switch (label)
                {
                    case "10Pc": Deposit10PcLoan = $"{CurrencySymbol}{loan:N0}"; Deposit10PcMonthly = $"{CurrencySymbol}{monthly:N0}/mo"; break;
                    case "20Pc": Deposit20PcLoan = $"{CurrencySymbol}{loan:N0}"; Deposit20PcMonthly = $"{CurrencySymbol}{monthly:N0}/mo"; break;
                    case "30Pc": Deposit30PcLoan = $"{CurrencySymbol}{loan:N0}"; Deposit30PcMonthly = $"{CurrencySymbol}{monthly:N0}/mo"; break;
                }
            }
            OnPropertyChanged(nameof(Deposit10PcLoan)); OnPropertyChanged(nameof(Deposit10PcMonthly));
            OnPropertyChanged(nameof(Deposit20PcLoan)); OnPropertyChanged(nameof(Deposit20PcMonthly));
            OnPropertyChanged(nameof(Deposit30PcLoan)); OnPropertyChanged(nameof(Deposit30PcMonthly));
        }

        public void Recalculate()
        {
            CurrencySymbol = _loanVm?.CurrencySymbol ?? "$";
            RecalculateRateScenario();
            RecalculateExtraRepayment();
            RecalculateTermComparison();
            RecalculateDepositScenarios();
            OnPropertyChanged(nameof(HasLoanData));
            OnPropertyChanged(nameof(HasNoLoanData));
        }

        // ── No data state ─────────────────────────────────────────────────
        [JsonIgnore] public bool HasLoanData => BaseLoanAmount > 0;
        [JsonIgnore] public bool HasNoLoanData => !HasLoanData;
    }

    // Calculation helpers — pure math, no Syncfusion dependency
    public static class HomeLoanCalculatorHelper
    {
        public static double CalculateMonthlyRepayment(double loan, double annualRatePct, int termYears)
        {
            if (annualRatePct <= 0) return loan / (termYears * 12.0);
            var r = annualRatePct / 100.0 / 12.0;
            var n = termYears * 12;
            return loan * r * Math.Pow(1 + r, n) / (Math.Pow(1 + r, n) - 1);
        }

        public static (int monthsSaved, double interestSaved) CalculateExtraRepaymentImpact(
            double loan, double annualRatePct, int termYears, double extraMonthly)
        {
            if (annualRatePct <= 0 || extraMonthly <= 0) return (0, 0);
            var r = annualRatePct / 100.0 / 12.0;
            var standardMonthly = CalculateMonthlyRepayment(loan, annualRatePct, termYears);
            var standardTotal = standardMonthly * termYears * 12;

            // Simulate accelerated payoff
            var balance = loan;
            var totalPaid = 0.0;
            var months = 0;
            var payment = standardMonthly + extraMonthly;
            while (balance > 0.01 && months < termYears * 12)
            {
                var interest = balance * r;
                balance = balance + interest - payment;
                if (balance < 0) balance = 0;
                totalPaid += payment;
                months++;
            }

            var monthsSaved = termYears * 12 - months;
            var interestSaved = (standardTotal - loan) - (totalPaid - loan);
            return (Math.Max(0, monthsSaved), Math.Max(0, interestSaved));
        }
    }
}
