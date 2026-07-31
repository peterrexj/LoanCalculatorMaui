using LoanCalculator.Core.Exts;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class WhatIfViewModel : ViewModelUiBase
    {
        [JsonIgnore] private readonly IErrorHandlingService _errorHandlingService;
        [JsonIgnore] private LoanViewModel? _loanVm;

        [JsonConstructor]
        public WhatIfViewModel() { }

        public WhatIfViewModel(IErrorHandlingService errorHandlingService)
        {
            _errorHandlingService = errorHandlingService;
        }

        public void SetLoanViewModel(LoanViewModel loanVm)
        {
            _loanVm = loanVm;
            // Always reflect the loan's actual payment frequency so the card highlights
            // the user's real position as the baseline for comparison.
            _currentFreqIndex = BasePaymentsPerYear switch { 24 => 1, 52 => 2, _ => 0 };
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
        private double BaseMonthlySurplus => _loanVm?.MonthlySurplus ?? 0;
        private bool HasAffordabilityData => _loanVm?.IsAffordabilityAvailable ?? false;

        // ── Scenario 1: Rate Change ────────────────────────────────────────
        [JsonIgnore] private double _rateChangeDelta = 0.5;
        public double RateChangeDelta
        {
            get => _rateChangeDelta;
            set { _rateChangeDelta = Math.Round(value, 2); OnPropertyChanged(nameof(RateChangeDelta)); RecalculateRateScenario(); }
        }

        [JsonIgnore] public string RateChangeNewRate          => $"{Math.Round(BaseRate + RateChangeDelta, 2)}%";
        [JsonIgnore] public string RateChangeMonthlyRepayment  { get; private set; } = "--";
        [JsonIgnore] public string RateChangeMonthlyDiff       { get; private set; } = "--";
        [JsonIgnore] public string RateChangeTotalInterest     { get; private set; } = "--";
        [JsonIgnore] public bool   RateChangeDiffIsPositive    { get; private set; }
        // Headroom indicator — only shown when income/expense data is present
        [JsonIgnore] public string RateChangeHeadroom          { get; private set; } = "";
        [JsonIgnore] public bool   RateChangeShowHeadroom      { get; private set; }
        // -1 = comfortable, 0 = tight (≤25% of original surplus), 1 = unaffordable
        [JsonIgnore] public int    RateChangeHeadroomStatus    { get; private set; }
        [JsonIgnore] public ObservableCollection<ChartDataModel> RateChangePrincipalData { get; } = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> RateChangeInterestData { get; } = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> RateChangeBalanceData { get; } = new();

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
            RateChangeMonthlyDiff = $"{diff.ToSignedCurrencyRounded()}/mo";
            RateChangeTotalInterest = $"{CurrencySymbol}{(newTotal - BaseLoanAmount):N0}";

            if (HasAffordabilityData && BaseMonthlySurplus != 0)
            {
                var newSurplus = BaseMonthlySurplus - diff;
                RateChangeShowHeadroom = true;
                if (newSurplus < 0)
                {
                    RateChangeHeadroom = $"Unaffordable — {CurrencySymbol}{Math.Abs(newSurplus):N0}/mo over budget";
                    RateChangeHeadroomStatus = 1;
                }
                else if (newSurplus < BaseMonthlySurplus * 0.25)
                {
                    RateChangeHeadroom = $"Tight — only {CurrencySymbol}{newSurplus:N0}/mo headroom";
                    RateChangeHeadroomStatus = 0;
                }
                else
                {
                    RateChangeHeadroom = $"{CurrencySymbol}{newSurplus:N0}/mo headroom remaining";
                    RateChangeHeadroomStatus = -1;
                }
            }
            else
            {
                RateChangeShowHeadroom = false;
                RateChangeHeadroom = "";
            }

            var amortization = HomeLoanCalculatorHelper.SimulateAnnualAmortization(BaseLoanAmount, newRate, BaseTerm);
            var balances = HomeLoanCalculatorHelper.SimulateYearlyBalances(BaseLoanAmount, newRate, BaseTerm, 0);
            RateChangePrincipalData.Clear();
            RateChangeInterestData.Clear();
            RateChangeBalanceData.Clear();
            foreach (var (year, principal, interest) in amortization)
            {
                RateChangePrincipalData.Add(new ChartDataModel(year.ToString(), principal));
                RateChangeInterestData.Add(new ChartDataModel(year.ToString(), interest));
            }
            foreach (var (year, balance) in balances)
                RateChangeBalanceData.Add(new ChartDataModel(year.ToString(), balance));

            OnPropertyChanged(nameof(RateChangeNewRate));
            OnPropertyChanged(nameof(RateChangeMonthlyRepayment));
            OnPropertyChanged(nameof(RateChangeMonthlyDiff));
            OnPropertyChanged(nameof(RateChangeTotalInterest));
            OnPropertyChanged(nameof(RateChangeDiffIsPositive));
            OnPropertyChanged(nameof(RateChangeHeadroom));
            OnPropertyChanged(nameof(RateChangeShowHeadroom));
            OnPropertyChanged(nameof(RateChangeHeadroomStatus));
        }

        // ── Scenario 2: Extra Repayment ───────────────────────────────────
        [JsonIgnore] private double _extraRepaymentMonthly = 500;
        public double ExtraRepaymentMonthly
        {
            get => _extraRepaymentMonthly;
            set { _extraRepaymentMonthly = value; OnPropertyChanged(nameof(ExtraRepaymentMonthly)); RecalculateExtraRepayment(); }
        }

        [JsonIgnore] public string ExtraRepaymentTimeSaved { get; private set; } = "--";
        [JsonIgnore] public string ExtraRepaymentInterestSaved { get; private set; } = "--";
        [JsonIgnore] public string ExtraRepaymentNewPayoff { get; private set; } = "--";
        [JsonIgnore] public ObservableCollection<ChartDataModel> ExtraRepaymentOriginalBalanceData { get; } = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> ExtraRepaymentAcceleratedBalanceData { get; } = new();

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

            var originalBalances = HomeLoanCalculatorHelper.SimulateYearlyBalances(BaseLoanAmount, BaseRate, BaseTerm, 0);
            var acceleratedBalances = HomeLoanCalculatorHelper.SimulateYearlyBalances(BaseLoanAmount, BaseRate, BaseTerm, ExtraRepaymentMonthly);
            ExtraRepaymentOriginalBalanceData.Clear();
            ExtraRepaymentAcceleratedBalanceData.Clear();
            foreach (var (year, balance) in originalBalances)
                ExtraRepaymentOriginalBalanceData.Add(new ChartDataModel(year.ToString(), balance));
            foreach (var (year, balance) in acceleratedBalances)
                ExtraRepaymentAcceleratedBalanceData.Add(new ChartDataModel(year.ToString(), balance));

            OnPropertyChanged(nameof(ExtraRepaymentTimeSaved));
            OnPropertyChanged(nameof(ExtraRepaymentInterestSaved));
            OnPropertyChanged(nameof(ExtraRepaymentNewPayoff));
        }

        // ── Scenario 3: Loan Term Comparison ─────────────────────────────
        // Three columns always show distinct terms in ascending order.
        // The user's actual term is always one of them; TermCurrentColIndex (0/1/2) marks which.
        [JsonIgnore] public string TermColALabel    { get; private set; } = "--";
        [JsonIgnore] public string TermColBLabel    { get; private set; } = "--";
        [JsonIgnore] public string TermColCLabel    { get; private set; } = "--";
        [JsonIgnore] public string TermColAMonthly  { get; private set; } = "--";
        [JsonIgnore] public string TermColBMonthly  { get; private set; } = "--";
        [JsonIgnore] public string TermColCMonthly  { get; private set; } = "--";
        [JsonIgnore] public string TermColAInterest { get; private set; } = "--";
        [JsonIgnore] public string TermColBInterest { get; private set; } = "--";
        [JsonIgnore] public string TermColCInterest { get; private set; } = "--";
        [JsonIgnore] public bool TermIsCurrentColA  { get; private set; }
        [JsonIgnore] public bool TermIsCurrentColB  { get; private set; }
        [JsonIgnore] public bool TermIsCurrentColC  { get; private set; }
        [JsonIgnore] public ObservableCollection<ChartDataModel> TermComparisonChartData { get; } = new();

        private void RecalculateTermComparison()
        {
            if (BaseLoanAmount <= 0 || BaseRate <= 0) return;

            const int MaxTerm = 30;
            var step = BaseTerm >= 15 ? 5 : 2;
            var clamped = Math.Min(BaseTerm, MaxTerm);

            // Build three distinct ascending terms; user's term is always included.
            int t1, t2, t3;
            if (clamped >= MaxTerm)
            {
                // At ceiling — user is in col C, show two steps below
                t3 = MaxTerm;
                t2 = Math.Max(1, MaxTerm - step);
                t1 = Math.Max(1, MaxTerm - 2 * step);
                TermIsCurrentColA = false; TermIsCurrentColB = false; TermIsCurrentColC = true;
            }
            else if (clamped <= step)
            {
                // At or near floor — user is in col A, show two steps above
                t1 = clamped;
                t2 = Math.Min(MaxTerm, clamped + step);
                t3 = Math.Min(MaxTerm, clamped + 2 * step);
                TermIsCurrentColA = true; TermIsCurrentColB = false; TermIsCurrentColC = false;
            }
            else
            {
                // Normal case — user in centre col B
                t1 = clamped - step;
                t2 = clamped;
                t3 = Math.Min(MaxTerm, clamped + step);
                TermIsCurrentColA = false; TermIsCurrentColB = true; TermIsCurrentColC = false;
            }

            (TermColALabel, TermColAMonthly, TermColAInterest) = BuildTermColumn(t1);
            (TermColBLabel, TermColBMonthly, TermColBInterest) = BuildTermColumn(t2);
            (TermColCLabel, TermColCMonthly, TermColCInterest) = BuildTermColumn(t3);

            TermComparisonChartData.Clear();
            TermComparisonChartData.Add(new ChartDataModel($"{t1} YRS", HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BaseLoanAmount, BaseRate, t1)));
            TermComparisonChartData.Add(new ChartDataModel($"{t2} YRS", HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BaseLoanAmount, BaseRate, t2)));
            TermComparisonChartData.Add(new ChartDataModel($"{t3} YRS", HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BaseLoanAmount, BaseRate, t3)));

            foreach (var n in new[]
            {
                nameof(TermColALabel), nameof(TermColAMonthly), nameof(TermColAInterest),
                nameof(TermColBLabel), nameof(TermColBMonthly), nameof(TermColBInterest),
                nameof(TermColCLabel), nameof(TermColCMonthly), nameof(TermColCInterest),
                nameof(TermIsCurrentColA), nameof(TermIsCurrentColB), nameof(TermIsCurrentColC),
            })
                OnPropertyChanged(n);
        }

        private (string label, string monthly, string interest) BuildTermColumn(int term)
        {
            var m = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BaseLoanAmount, BaseRate, term);
            var interest = m * term * 12 - BaseLoanAmount;
            return ($"{term} YRS", $"{CurrencySymbol}{m:N0}", $"{CurrencySymbol}{interest:N0}");
        }

        // ── Scenario 4: Deposit Scenarios ────────────────────────────────
        // Columns are always: lower deposit | YOUR DEPOSIT | higher deposit
        [JsonIgnore] public string DepositColALabel      { get; private set; } = "--";
        [JsonIgnore] public string DepositColBLabel      { get; private set; } = "--";
        [JsonIgnore] public string DepositColCLabel      { get; private set; } = "--";
        [JsonIgnore] public string DepositColALoan       { get; private set; } = "--";
        [JsonIgnore] public string DepositColBLoan       { get; private set; } = "--";
        [JsonIgnore] public string DepositColCLoan       { get; private set; } = "--";
        [JsonIgnore] public string DepositColAMonthly    { get; private set; } = "--";
        [JsonIgnore] public string DepositColBMonthly    { get; private set; } = "--";
        [JsonIgnore] public string DepositColCMonthly    { get; private set; } = "--";
        // False when the deposit is already at/near the ceiling — col C shows a "Max" placeholder
        [JsonIgnore] public bool   DepositColCAvailable  { get; private set; } = true;
        [JsonIgnore] public ObservableCollection<ChartDataModel> DepositScenariosChartData { get; } = new();

        private void RecalculateDepositScenarios()
        {
            if (BasePropertyAmount <= 0 || BaseRate <= 0) return;

            // Col B = exact actual deposit. Cols A and C = nearest 5% brackets below/above.
            var exactPct = BasePropertyAmount > 0 ? BaseDeposit / BasePropertyAmount * 100 : 20;
            exactPct = Math.Clamp(exactPct, 1, 99);

            // Floor/ceil to the nearest 5% step that is strictly below/above exact
            var floorPct = Math.Floor(exactPct / 5.0) * 5;
            var ceilPct  = Math.Ceiling(exactPct / 5.0) * 5;

            // If exact already lands on a 5% boundary, nudge neighbours by one step
            var lowPct    = floorPct < exactPct ? floorPct : Math.Max(5, exactPct - 5);
            var rawHighPct = ceilPct > exactPct ? ceilPct  : exactPct + 5;

            lowPct = Math.Max(5, lowPct);

            // Col C is available only when a strictly-higher deposit (≤99%) makes sense
            DepositColCAvailable = rawHighPct <= 99.0 && rawHighPct > exactPct + 0.1;

            if (!DepositColCAvailable)
            {
                // At or near the ceiling — widen col A gap so it stays distinct from col B
                lowPct = Math.Max(5, Math.Floor((exactPct - 10) / 5.0) * 5);
            }

            var highPct = DepositColCAvailable ? Math.Min(99, rawHighPct) : 0;

            (DepositColALabel, DepositColALoan, DepositColAMonthly) = BuildDepositColumn(lowPct / 100.0);
            (DepositColBLabel, DepositColBLoan, DepositColBMonthly) = BuildDepositColumnExact(exactPct / 100.0);

            if (DepositColCAvailable)
                (DepositColCLabel, DepositColCLoan, DepositColCMonthly) = BuildDepositColumn(highPct / 100.0);
            else
                (DepositColCLabel, DepositColCLoan, DepositColCMonthly) = ("Max", "—", "—");

            DepositScenariosChartData.Clear();
            var pctDisplay = exactPct % 1 == 0 ? $"{exactPct:0}%" : $"{exactPct:0.#}%";
            DepositScenariosChartData.Add(new ChartDataModel($"{lowPct:0}%", HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BasePropertyAmount * (1 - lowPct / 100.0), BaseRate, BaseTerm)));
            DepositScenariosChartData.Add(new ChartDataModel(pctDisplay, HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BasePropertyAmount * (1 - exactPct / 100.0), BaseRate, BaseTerm)));
            if (DepositColCAvailable)
                DepositScenariosChartData.Add(new ChartDataModel($"{highPct:0}%", HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BasePropertyAmount * (1 - highPct / 100.0), BaseRate, BaseTerm)));

            foreach (var n in new[]
            {
                nameof(DepositColALabel), nameof(DepositColALoan), nameof(DepositColAMonthly),
                nameof(DepositColBLabel), nameof(DepositColBLoan), nameof(DepositColBMonthly),
                nameof(DepositColCLabel), nameof(DepositColCLoan), nameof(DepositColCMonthly),
                nameof(DepositColCAvailable),
            })
                OnPropertyChanged(n);
        }

        private (string label, string loan, string monthly) BuildDepositColumn(double pct)
        {
            var loan    = BasePropertyAmount * (1 - pct);
            var monthly = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(loan, BaseRate, BaseTerm);
            return ($"{pct * 100:0}%", $"{CurrencySymbol}{loan:N0}", $"{CurrencySymbol}{monthly:N0}/mo");
        }

        private (string label, string loan, string monthly) BuildDepositColumnExact(double pct)
        {
            var loan    = BasePropertyAmount * (1 - pct);
            var monthly = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(loan, BaseRate, BaseTerm);
            var pctDisplay = pct * 100;
            var label = pctDisplay % 1 == 0 ? $"{pctDisplay:0}%" : $"{pctDisplay:0.#}%";
            return (label, $"{CurrencySymbol}{loan:N0}", $"{CurrencySymbol}{monthly:N0}/mo");
        }

        // ── Scenario 5: Lump Sum Payment ─────────────────────────────────
        [JsonIgnore] private double _lumpSumAmount = 10000;
        public double LumpSumAmount
        {
            get => _lumpSumAmount;
            set { _lumpSumAmount = Math.Max(0, value); OnPropertyChanged(nameof(LumpSumAmount)); RecalculateLumpSum(); }
        }

        [JsonIgnore] public string LumpSumTimeSaved     { get; private set; } = "--";
        [JsonIgnore] public string LumpSumInterestSaved { get; private set; } = "--";
        [JsonIgnore] public string LumpSumNewBalance    { get; private set; } = "--";
        [JsonIgnore] public ObservableCollection<ChartDataModel> LumpSumOriginalBalanceData { get; } = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> LumpSumReducedBalanceData { get; } = new();

        private void RecalculateLumpSum()
        {
            if (BaseLoanAmount <= 0 || BaseRate <= 0) return;
            var reducedLoan = Math.Max(0, BaseLoanAmount - LumpSumAmount);
            LumpSumNewBalance = $"{CurrencySymbol}{reducedLoan:N0}";

            var (monthsSaved, interestSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(
                BaseLoanAmount, BaseRate, BaseTerm, 0, LumpSumAmount);
            var years = monthsSaved / 12;
            var months = monthsSaved % 12;
            LumpSumTimeSaved     = months > 0 ? $"{years}yr {months}mo" : $"{years}yr";
            LumpSumInterestSaved = $"{CurrencySymbol}{interestSaved:N0}";

            var originalBalances = HomeLoanCalculatorHelper.SimulateYearlyBalances(BaseLoanAmount, BaseRate, BaseTerm, 0);
            var reducedBalances = HomeLoanCalculatorHelper.SimulateYearlyBalances(BaseLoanAmount, BaseRate, BaseTerm, 0, LumpSumAmount);
            LumpSumOriginalBalanceData.Clear();
            LumpSumReducedBalanceData.Clear();
            foreach (var (year, balance) in originalBalances)
                LumpSumOriginalBalanceData.Add(new ChartDataModel(year.ToString(), balance));
            foreach (var (year, balance) in reducedBalances)
                LumpSumReducedBalanceData.Add(new ChartDataModel(year.ToString(), balance));

            OnPropertyChanged(nameof(LumpSumTimeSaved));
            OnPropertyChanged(nameof(LumpSumInterestSaved));
            OnPropertyChanged(nameof(LumpSumNewBalance));
        }

        // ── Scenario 6: Repayment Frequency ──────────────────────────────
        // Three columns always visible: Monthly | Fortnightly | Weekly
        // The user's actual loan frequency is highlighted.
        // 0 = Monthly, 1 = Fortnightly, 2 = Weekly
        [JsonIgnore] private int _currentFreqIndex = 0;
        [JsonIgnore] public bool FreqIsMonthly     => _currentFreqIndex == 0;
        [JsonIgnore] public bool FreqIsFortnightly => _currentFreqIndex == 1;
        [JsonIgnore] public bool FreqIsWeekly      => _currentFreqIndex == 2;

        [JsonIgnore] public string FreqMonthlyPayment    { get; private set; } = "--";
        [JsonIgnore] public string FreqMonthlyTimeSaved  { get; private set; } = "--";
        [JsonIgnore] public string FreqMonthlyIntSaved   { get; private set; } = "--";

        [JsonIgnore] public string FreqFortPayment       { get; private set; } = "--";
        [JsonIgnore] public string FreqFortTimeSaved     { get; private set; } = "--";
        [JsonIgnore] public string FreqFortIntSaved      { get; private set; } = "--";

        [JsonIgnore] public string FreqWeeklyPayment     { get; private set; } = "--";
        [JsonIgnore] public string FreqWeeklyTimeSaved   { get; private set; } = "--";
        [JsonIgnore] public string FreqWeeklyIntSaved    { get; private set; } = "--";
        [JsonIgnore] public ObservableCollection<ChartDataModel> FreqMonthlyBalanceData { get; } = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> FreqFortBalanceData { get; } = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> FreqWeeklyBalanceData { get; } = new();

        private void RecalculateRepaymentFrequency()
        {
            if (BaseLoanAmount <= 0 || BaseRate <= 0) return;
            var monthly = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BaseLoanAmount, BaseRate, BaseTerm);

            // Monthly baseline — always 0 savings (it is the reference)
            FreqMonthlyPayment   = $"{CurrencySymbol}{monthly:N0}/mo";
            FreqMonthlyTimeSaved = "0yr";
            FreqMonthlyIntSaved  = $"{CurrencySymbol}0";

            // Fortnightly: half monthly × 26 = 13 months/yr (real-world fortnightly saving)
            var fort = monthly / 2.0;
            var (fortMonths, fortInt) = HomeLoanCalculatorHelper.SimulateFrequency(BaseLoanAmount, BaseRate, BaseTerm, fort, 26);
            FreqFortPayment   = $"{CurrencySymbol}{fort:N0}/fn";
            FreqFortTimeSaved = fortMonths % 12 > 0 ? $"{fortMonths / 12}yr {fortMonths % 12}mo" : $"{fortMonths / 12}yr";
            FreqFortIntSaved  = $"{CurrencySymbol}{fortInt:N0}";

            // Weekly: quarter monthly × 52 = 13 months/yr
            var week = monthly / 4.0;
            var (weekMonths, weekInt) = HomeLoanCalculatorHelper.SimulateFrequency(BaseLoanAmount, BaseRate, BaseTerm, week, 52);
            FreqWeeklyPayment   = $"{CurrencySymbol}{week:N0}/wk";
            FreqWeeklyTimeSaved = weekMonths % 12 > 0 ? $"{weekMonths / 12}yr {weekMonths % 12}mo" : $"{weekMonths / 12}yr";
            FreqWeeklyIntSaved  = $"{CurrencySymbol}{weekInt:N0}";

            var monthlyBalances = HomeLoanCalculatorHelper.SimulateYearlyBalances(BaseLoanAmount, BaseRate, BaseTerm, 0);
            var fortBalances = HomeLoanCalculatorHelper.SimulateYearlyBalancesForFrequency(BaseLoanAmount, BaseRate, BaseTerm, fort, 26);
            var weekBalances = HomeLoanCalculatorHelper.SimulateYearlyBalancesForFrequency(BaseLoanAmount, BaseRate, BaseTerm, week, 52);
            FreqMonthlyBalanceData.Clear();
            FreqFortBalanceData.Clear();
            FreqWeeklyBalanceData.Clear();
            foreach (var (year, balance) in monthlyBalances)
                FreqMonthlyBalanceData.Add(new ChartDataModel(year.ToString(), balance));
            foreach (var (year, balance) in fortBalances)
                FreqFortBalanceData.Add(new ChartDataModel(year.ToString(), balance));
            foreach (var (year, balance) in weekBalances)
                FreqWeeklyBalanceData.Add(new ChartDataModel(year.ToString(), balance));

            foreach (var n in new[]
            {
                nameof(FreqIsMonthly), nameof(FreqIsFortnightly), nameof(FreqIsWeekly),
                nameof(FreqMonthlyPayment), nameof(FreqMonthlyTimeSaved), nameof(FreqMonthlyIntSaved),
                nameof(FreqFortPayment), nameof(FreqFortTimeSaved), nameof(FreqFortIntSaved),
                nameof(FreqWeeklyPayment), nameof(FreqWeeklyTimeSaved), nameof(FreqWeeklyIntSaved),
            })
                OnPropertyChanged(n);
        }

        // ── Scenario 7: Offset Account ────────────────────────────────────
        [JsonIgnore] private double _offsetBalance = 20000;
        public double OffsetBalance
        {
            get => _offsetBalance;
            set { _offsetBalance = Math.Max(0, value); OnPropertyChanged(nameof(OffsetBalance)); RecalculateOffset(); }
        }

        // 0 = auto-track BaseRate; any other value = user-set absolute rate
        [JsonIgnore] private double _offsetRate = 0.0;
        public double OffsetRate
        {
            get => _offsetRate > 0 ? _offsetRate : BaseRate;
            set { _offsetRate = Math.Round(Math.Max(0.01, value), 2); OnPropertyChanged(nameof(OffsetRate)); RecalculateOffset(); }
        }

        [JsonIgnore] public string OffsetMonthlySaving  { get; private set; } = "--";
        [JsonIgnore] public string OffsetInterestSaved  { get; private set; } = "--";
        [JsonIgnore] public string OffsetTimeSaved      { get; private set; } = "--";
        [JsonIgnore] public string OffsetRateNote       { get; private set; } = "";
        [JsonIgnore] public ObservableCollection<ChartDataModel> OffsetWithoutBalanceData { get; } = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> OffsetWithBalanceData { get; } = new();

        private void RecalculateOffset()
        {
            if (BaseLoanAmount <= 0 || BaseRate <= 0) return;
            var effectiveBalance = Math.Min(_offsetBalance, BaseLoanAmount);
            var effectiveRate = OffsetRate;
            var r = effectiveRate / 100.0 / 12.0;

            OnPropertyChanged(nameof(OffsetRate));

            // Monthly interest saving = offset × monthly rate
            var monthlySaving = effectiveBalance * r;
            OffsetMonthlySaving = $"{CurrencySymbol}{monthlySaving:N0}/mo";

            // Simulate payoff: apply normal payment to reduced effective balance
            var (monthsSaved, interestSaved) = HomeLoanCalculatorHelper.SimulateOffset(BaseLoanAmount, effectiveRate, BaseTerm, effectiveBalance);
            var years = monthsSaved / 12; var months = monthsSaved % 12;
            OffsetTimeSaved     = months > 0 ? $"{years}yr {months}mo" : $"{years}yr";
            OffsetInterestSaved = $"{CurrencySymbol}{interestSaved:N0}";
            OffsetRateNote      = _offsetRate == 0.0
                ? $"calculated at your loan rate ({BaseRate:0.##}%)"
                : $"calculated at {effectiveRate:0.##}% (loan rate: {BaseRate:0.##}%)";

            var withoutOffsetBalances = HomeLoanCalculatorHelper.SimulateYearlyBalances(BaseLoanAmount, effectiveRate, BaseTerm, 0);
            var withOffsetBalances = HomeLoanCalculatorHelper.SimulateYearlyBalancesWithOffset(BaseLoanAmount, effectiveRate, BaseTerm, effectiveBalance);
            OffsetWithoutBalanceData.Clear();
            OffsetWithBalanceData.Clear();
            foreach (var (year, balance) in withoutOffsetBalances)
                OffsetWithoutBalanceData.Add(new ChartDataModel(year.ToString(), balance));
            foreach (var (year, balance) in withOffsetBalances)
                OffsetWithBalanceData.Add(new ChartDataModel(year.ToString(), balance));

            OnPropertyChanged(nameof(OffsetMonthlySaving));
            OnPropertyChanged(nameof(OffsetInterestSaved));
            OnPropertyChanged(nameof(OffsetTimeSaved));
            OnPropertyChanged(nameof(OffsetRateNote));
        }

        // ── Scenario 8: Affordability Stress Test ────────────────────────
        [JsonIgnore] public bool   StressTestAvailable        { get; private set; }
        [JsonIgnore] public bool   StressTestUnavailable      => !StressTestAvailable;
        [JsonIgnore] public string StressTestBreakEvenRate    { get; private set; } = "--";
        [JsonIgnore] public string StressTestRateBuffer       { get; private set; } = "--";
        [JsonIgnore] public string StressTestCurrentSurplus   { get; private set; } = "--";
        // -1 = safe (buffer > 2%), 0 = moderate (1–2%), 1 = at risk (< 1%)
        [JsonIgnore] public int    StressTestRisk             { get; private set; }
        [JsonIgnore] public ObservableCollection<ChartDataModel> StressTestChartData { get; } = new();

        private void RecalculateStressTest()
        {
            if (BaseLoanAmount <= 0 || BaseRate <= 0 || !HasAffordabilityData || BaseMonthlySurplus == 0)
            {
                StressTestAvailable = false;
                OnPropertyChanged(nameof(StressTestAvailable));
                OnPropertyChanged(nameof(StressTestUnavailable));
                return;
            }

            StressTestAvailable = true;

            // Maximum affordable monthly = current repayment + current surplus
            var maxAffordableMonthly = BaseMonthlyRepayment + BaseMonthlySurplus;

            // Binary search: find the rate where repayment = maxAffordableMonthly
            var breakEvenRate = HomeLoanCalculatorHelper.FindBreakEvenRate(
                BaseLoanAmount, BaseTerm, maxAffordableMonthly);

            var rateBuffer = breakEvenRate - BaseRate;
            StressTestBreakEvenRate = breakEvenRate > 0
                ? $"{breakEvenRate:0.##}%"
                : "Already unaffordable";
            StressTestRateBuffer = rateBuffer > 0
                ? $"+{rateBuffer:0.##}% buffer"
                : $"{rateBuffer:0.##}% over limit";
            StressTestCurrentSurplus = $"{CurrencySymbol}{BaseMonthlySurplus:N0}/mo";

            StressTestRisk = rateBuffer >= 2.0 ? -1 : rateBuffer >= 1.0 ? 0 : 1;

            StressTestChartData.Clear();
            StressTestChartData.Add(new ChartDataModel("Current Rate", BaseRate));
            StressTestChartData.Add(new ChartDataModel("Break-even Rate", breakEvenRate > 0 ? breakEvenRate : BaseRate));

            OnPropertyChanged(nameof(StressTestAvailable));
            OnPropertyChanged(nameof(StressTestUnavailable));
            OnPropertyChanged(nameof(StressTestBreakEvenRate));
            OnPropertyChanged(nameof(StressTestRateBuffer));
            OnPropertyChanged(nameof(StressTestCurrentSurplus));
            OnPropertyChanged(nameof(StressTestRisk));
        }

        // ── Scenario 9: Combined Strategy ────────────────────────────────
        [JsonIgnore] private double _combinedExtraMonthly = 500;
        public double CombinedExtraMonthly
        {
            get => _combinedExtraMonthly;
            set { _combinedExtraMonthly = Math.Max(0, value); OnPropertyChanged(nameof(CombinedExtraMonthly)); RecalculateCombined(); }
        }

        [JsonIgnore] private double _combinedLumpSum = 10000;
        public double CombinedLumpSum
        {
            get => _combinedLumpSum;
            set { _combinedLumpSum = Math.Max(0, value); OnPropertyChanged(nameof(CombinedLumpSum)); RecalculateCombined(); }
        }

        [JsonIgnore] private double _combinedOffset = 20000;
        public double CombinedOffset
        {
            get => _combinedOffset;
            set { _combinedOffset = Math.Max(0, value); OnPropertyChanged(nameof(CombinedOffset)); RecalculateCombined(); }
        }

        // 0 = auto-track BaseRate; any other value = absolute rate
        [JsonIgnore] private double _combinedOffsetRate = 0.0;
        public double CombinedOffsetRate
        {
            get => _combinedOffsetRate > 0 ? _combinedOffsetRate : BaseRate;
            set { _combinedOffsetRate = Math.Round(Math.Max(0.01, value), 2); OnPropertyChanged(nameof(CombinedOffsetRate)); RecalculateCombined(); }
        }

        [JsonIgnore] private int _combinedFrequencyIndex = 1;
        public int CombinedFrequencyIndex
        {
            get => _combinedFrequencyIndex;
            set { _combinedFrequencyIndex = value; OnPropertyChanged(nameof(CombinedFrequencyIndex)); RecalculateCombined(); }
        }

        [JsonIgnore] private bool _combinedUseFrequency = true;
        public bool CombinedUseFrequency
        {
            get => _combinedUseFrequency;
            set { _combinedUseFrequency = value; OnPropertyChanged(nameof(CombinedUseFrequency)); RecalculateCombined(); }
        }

        [JsonIgnore] private bool _combinedUseExtra = true;
        public bool CombinedUseExtra
        {
            get => _combinedUseExtra;
            set { _combinedUseExtra = value; OnPropertyChanged(nameof(CombinedUseExtra)); RecalculateCombined(); }
        }

        [JsonIgnore] private bool _combinedUseLumpSum = true;
        public bool CombinedUseLumpSum
        {
            get => _combinedUseLumpSum;
            set { _combinedUseLumpSum = value; OnPropertyChanged(nameof(CombinedUseLumpSum)); RecalculateCombined(); }
        }

        [JsonIgnore] private bool _combinedUseOffset = true;
        public bool CombinedUseOffset
        {
            get => _combinedUseOffset;
            set { _combinedUseOffset = value; OnPropertyChanged(nameof(CombinedUseOffset)); RecalculateCombined(); }
        }

        [JsonIgnore] public string CombinedTimeSaved        { get; private set; } = "--";
        [JsonIgnore] public string CombinedInterestSaved    { get; private set; } = "--";
        [JsonIgnore] public string CombinedNewTerm          { get; private set; } = "--";
        [JsonIgnore] public string CombinedFrequencyPayment { get; private set; } = "--";
        [JsonIgnore] public string CombinedExtraDisplay     { get; private set; } = "--";
        [JsonIgnore] public string CombinedLumpDisplay      { get; private set; } = "--";
        [JsonIgnore] public string CombinedOffsetDisplay    { get; private set; } = "--";
        [JsonIgnore] public ObservableCollection<ChartDataModel> CombinedBaselineData { get; } = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> CombinedStrategyData { get; } = new();

        private void RecalculateCombined()
        {
            if (BaseLoanAmount <= 0 || BaseRate <= 0) return;

            var paymentsPerYear = !_combinedUseFrequency ? 12
                : _combinedFrequencyIndex == 2 ? 52
                : _combinedFrequencyIndex == 1 ? 26 : 12;
            var extra       = _combinedUseExtra   ? _combinedExtraMonthly : 0;
            var lump        = _combinedUseLumpSum ? _combinedLumpSum      : 0;
            var offset      = _combinedUseOffset  ? _combinedOffset       : 0;
            var offsetRate  = CombinedOffsetRate;

            // Frequency payment display — must match the formula in SimulateCombined.
            var standardMonthly = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(BaseLoanAmount, BaseRate, BaseTerm);
            double periodicPayment;
            if (paymentsPerYear == 52)
                periodicPayment = (standardMonthly + extra) / 4.0;
            else if (paymentsPerYear == 26)
                periodicPayment = (standardMonthly + extra) / 2.0;
            else
                periodicPayment = standardMonthly + extra;
            var freqLabel = paymentsPerYear == 52 ? "wk" : paymentsPerYear == 26 ? "fn" : "mo";
            CombinedFrequencyPayment = $"{CurrencySymbol}{periodicPayment:N0}/{freqLabel}";

            // Formatted display values with currency symbol
            CombinedExtraDisplay  = $"{CurrencySymbol}{_combinedExtraMonthly:N0}/mo";
            CombinedLumpDisplay   = $"{CurrencySymbol}{_combinedLumpSum:N0}";
            CombinedOffsetDisplay = $"{CurrencySymbol}{_combinedOffset:N0}";

            var (monthsSaved, interestSaved, strategyBalances) =
                HomeLoanCalculatorHelper.SimulateCombined(
                    BaseLoanAmount, BaseRate, BaseTerm, extra, lump, offset, paymentsPerYear, offsetRate);

            var yrs = monthsSaved / 12; var mos = monthsSaved % 12;
            CombinedTimeSaved     = mos > 0 ? $"{yrs}yr {mos}mo" : $"{yrs}yr";
            CombinedInterestSaved = $"{CurrencySymbol}{interestSaved:N0}";
            var payoffMonths = Math.Max(0, BaseTerm * 12 - monthsSaved);
            CombinedNewTerm = payoffMonths % 12 > 0
                ? $"{payoffMonths / 12}yr {payoffMonths % 12}mo"
                : $"{payoffMonths / 12}yr";

            var baselineBalances = HomeLoanCalculatorHelper.SimulateYearlyBalances(BaseLoanAmount, BaseRate, BaseTerm, 0);
            CombinedBaselineData.Clear();
            CombinedStrategyData.Clear();
            foreach (var (year, bal) in baselineBalances)
                CombinedBaselineData.Add(new ChartDataModel(year.ToString(), bal));
            foreach (var (year, bal) in strategyBalances)
                CombinedStrategyData.Add(new ChartDataModel(year.ToString(), bal));

            OnPropertyChanged(nameof(CombinedTimeSaved));
            OnPropertyChanged(nameof(CombinedInterestSaved));
            OnPropertyChanged(nameof(CombinedNewTerm));
            OnPropertyChanged(nameof(CombinedFrequencyPayment));
            OnPropertyChanged(nameof(CombinedExtraDisplay));
            OnPropertyChanged(nameof(CombinedLumpDisplay));
            OnPropertyChanged(nameof(CombinedOffsetDisplay));
            OnPropertyChanged(nameof(CombinedOffsetRate));
        }

        public void Recalculate()
        {
            CurrencySymbol = _loanVm?.CurrencySymbol ?? "$";
            RecalculateRateScenario();
            RecalculateExtraRepayment();
            RecalculateTermComparison();
            RecalculateDepositScenarios();
            RecalculateLumpSum();
            RecalculateRepaymentFrequency();
            RecalculateOffset();
            RecalculateStressTest();
            RecalculateCombined();
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
            double loan, double annualRatePct, int termYears, double extraMonthly, double upfrontLumpSum = 0)
        {
            if (annualRatePct <= 0) return (0, 0);
            if (extraMonthly <= 0 && upfrontLumpSum <= 0) return (0, 0);
            var r = annualRatePct / 100.0 / 12.0;
            var standardMonthly = CalculateMonthlyRepayment(loan, annualRatePct, termYears);
            var standardTotal = standardMonthly * termYears * 12;

            // Apply lump sum immediately, then simulate accelerated payoff
            var balance = Math.Max(0, loan - upfrontLumpSum);
            var totalPaid = upfrontLumpSum;
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

        // Simulate payoff at a given periodic payment and payments-per-year (26=fortnightly, 52=weekly)
        public static (int monthsSaved, double interestSaved) SimulateFrequency(
            double loan, double annualRatePct, int termYears, double periodicPayment, int paymentsPerYear)
        {
            if (annualRatePct <= 0 || periodicPayment <= 0) return (0, 0);
            var rPeriod = annualRatePct / 100.0 / paymentsPerYear;
            var standardMonthly = CalculateMonthlyRepayment(loan, annualRatePct, termYears);
            var standardTotal = standardMonthly * termYears * 12;

            var balance = loan;
            var totalPaid = 0.0;
            var periods = 0;
            var maxPeriods = (termYears + 10) * paymentsPerYear;
            while (balance > 0.01 && periods < maxPeriods)
            {
                var interest = balance * rPeriod;
                balance = balance + interest - periodicPayment;
                if (balance < 0) balance = 0;
                totalPaid += periodicPayment;
                periods++;
            }

            var monthsTaken = (int)Math.Round(periods * 12.0 / paymentsPerYear);
            var monthsSaved = Math.Max(0, termYears * 12 - monthsTaken);
            var interestSaved = Math.Max(0, (standardTotal - loan) - (totalPaid - loan));
            return (monthsSaved, interestSaved);
        }

        // Binary search for the rate at which monthly repayment equals maxMonthly
        public static double FindBreakEvenRate(double loan, int termYears, double maxMonthly)
        {
            if (loan <= 0 || maxMonthly <= 0) return 0;
            double lo = 0, hi = 30.0;
            for (int i = 0; i < 60; i++)
            {
                var mid = (lo + hi) / 2.0;
                var payment = CalculateMonthlyRepayment(loan, mid, termYears);
                if (payment < maxMonthly) lo = mid; else hi = mid;
            }
            return Math.Round((lo + hi) / 2.0, 2);
        }

        // Year-by-year balance snapshots for balance curve charts
        public static List<(int year, double balance)> SimulateYearlyBalances(
            double loan, double annualRatePct, int termYears, double extraMonthly, double upfrontLumpSum = 0)
        {
            var result = new List<(int, double)> { (0, Math.Max(0, loan - upfrontLumpSum)) };
            if (annualRatePct <= 0)
            {
                for (int y = 1; y <= termYears; y++)
                    result.Add((y, 0));
                return result;
            }
            var r = annualRatePct / 100.0 / 12.0;
            var standardMonthly = CalculateMonthlyRepayment(loan, annualRatePct, termYears);
            var payment = standardMonthly + extraMonthly;
            var balance = Math.Max(0, loan - upfrontLumpSum);
            for (int y = 1; y <= termYears; y++)
            {
                for (int m = 0; m < 12 && balance > 0.01; m++)
                {
                    balance = balance + balance * r - payment;
                    if (balance < 0) balance = 0;
                }
                result.Add((y, balance));
                if (balance <= 0) break;
            }
            return result;
        }

        // Year-by-year balance snapshots using a sub-monthly periodic frequency
        public static List<(int year, double balance)> SimulateYearlyBalancesForFrequency(
            double loan, double annualRatePct, int termYears, double periodicPayment, int paymentsPerYear)
        {
            var result = new List<(int, double)> { (0, loan) };
            if (annualRatePct <= 0 || periodicPayment <= 0) return result;
            var rPeriod = annualRatePct / 100.0 / paymentsPerYear;
            var balance = loan;
            int periodsPerYear = paymentsPerYear;
            int maxPeriods = (termYears + 5) * periodsPerYear;
            int periodCount = 0;
            for (int y = 1; y <= termYears && balance > 0.01; y++)
            {
                for (int p = 0; p < periodsPerYear && balance > 0.01 && periodCount < maxPeriods; p++, periodCount++)
                {
                    balance = balance + balance * rPeriod - periodicPayment;
                    if (balance < 0) balance = 0;
                }
                result.Add((y, balance));
                if (balance <= 0) break;
            }
            return result;
        }

        // Annual principal and interest amounts for stacked column chart
        public static List<(int year, double principal, double interest)> SimulateAnnualAmortization(
            double loan, double annualRatePct, int termYears, double extraMonthly = 0, double upfrontLumpSum = 0)
        {
            var result = new List<(int, double, double)>();
            if (annualRatePct <= 0 || loan <= 0) return result;
            var r = annualRatePct / 100.0 / 12.0;
            var standardMonthly = CalculateMonthlyRepayment(loan, annualRatePct, termYears);
            var payment = standardMonthly + extraMonthly;
            var balance = Math.Max(0, loan - upfrontLumpSum);
            for (int y = 1; y <= termYears && balance > 0.01; y++)
            {
                double yearPrincipal = 0, yearInterest = 0;
                for (int m = 0; m < 12 && balance > 0.01; m++)
                {
                    var interest = balance * r;
                    var principal = Math.Min(payment - interest, balance);
                    yearInterest += interest;
                    yearPrincipal += principal;
                    balance -= principal;
                    if (balance < 0) balance = 0;
                }
                result.Add((y, yearPrincipal, yearInterest));
            }
            return result;
        }

        // Year-by-year balance snapshots with a constant offset applied each month
        public static List<(int year, double balance)> SimulateYearlyBalancesWithOffset(
            double loan, double annualRatePct, int termYears, double offsetBalance)
        {
            var result = new List<(int, double)> { (0, loan) };
            if (annualRatePct <= 0) return result;
            var r = annualRatePct / 100.0 / 12.0;
            var payment = CalculateMonthlyRepayment(loan, annualRatePct, termYears);
            var balance = loan;
            for (int y = 1; y <= termYears && balance > 0.01; y++)
            {
                for (int m = 0; m < 12 && balance > 0.01; m++)
                {
                    var effective = Math.Max(0, balance - offsetBalance);
                    var interest = effective * r;
                    balance = balance + interest - payment;
                    if (balance < 0) balance = 0;
                }
                result.Add((y, balance));
                if (balance <= 0) break;
            }
            return result;
        }

        public static (int monthsSaved, double interestSaved, List<(int year, double balance)> yearlyBalances)
            SimulateCombined(
                double loan,
                double annualRatePct,
                int termYears,
                double extraMonthly,
                double upfrontLumpSum,
                double offsetBalance,
                int paymentsPerYear,
                double offsetRatePct = 0)
        {
            if (annualRatePct <= 0)
                return (0, 0, new List<(int, double)> { (0, loan) });

            // The offset rate only determines the interest credit on the offset portion.
            // The loan itself always accrues interest at the loan rate (annualRatePct).
            var effectiveOffsetRate = offsetRatePct > 0 ? offsetRatePct : annualRatePct;
            var standardMonthly = CalculateMonthlyRepayment(loan, annualRatePct, termYears);
            var balance = Math.Max(0, loan - upfrontLumpSum);
            var effectiveOffset = Math.Min(offsetBalance, balance);

            // Periodic payment: real-world fortnightly/weekly convention.
            // Fortnightly = (monthly + extra) / 2, paid 26×/yr → 13 monthly-equivalents/yr.
            // Weekly      = (monthly + extra) / 4, paid 52×/yr → 13 monthly-equivalents/yr.
            // Monthly     = monthly + extra, paid 12×/yr.
            double periodicPayment;
            if (paymentsPerYear == 52)
                periodicPayment = (standardMonthly + extraMonthly) / 4.0;
            else if (paymentsPerYear == 26)
                periodicPayment = (standardMonthly + extraMonthly) / 2.0;
            else
                periodicPayment = standardMonthly + extraMonthly;

            var loanRatePeriod   = annualRatePct / 100.0 / paymentsPerYear;
            var offsetRatePeriod = effectiveOffsetRate / 100.0 / paymentsPerYear;

            var periods = 0;
            var totalInterestPaid = 0.0;
            var yearlyBalances = new List<(int, double)>();
            yearlyBalances.Add((0, balance));
            var maxPeriods = (termYears + 10) * paymentsPerYear;

            for (int p = 0; p < maxPeriods && balance > 0.001; p++)
            {
                // Full loan balance accrues interest at the loan rate.
                // The offset balance provides an interest credit at the offset rate.
                var grossInterest  = balance * loanRatePeriod;
                var offsetCredit   = Math.Min(effectiveOffset, balance) * offsetRatePeriod;
                var netInterest    = Math.Max(0, grossInterest - offsetCredit);
                totalInterestPaid += netInterest;
                balance = Math.Max(0, balance + netInterest - periodicPayment);
                periods++;
                if ((p + 1) % paymentsPerYear == 0)
                    yearlyBalances.Add((yearlyBalances.Count, balance));
            }

            // Baseline: standard monthly payment, no levers, loan rate — simulate to actual payoff
            var baselineR = annualRatePct / 100.0 / 12.0;
            var baselineBal = loan;
            var baselineInterest = 0.0;
            var baselineMonths = 0;
            for (int m = 0; m < (termYears + 10) * 12 && baselineBal > 0.001; m++)
            {
                var i = baselineBal * baselineR;
                baselineInterest += i;
                baselineBal = Math.Max(0, baselineBal + i - standardMonthly);
                baselineMonths++;
            }

            var actualMonths = (int)Math.Round(periods * 12.0 / paymentsPerYear);
            var monthsSaved   = Math.Max(0, baselineMonths - actualMonths);
            var interestSaved = Math.Max(0, baselineInterest - totalInterestPaid);
            return (monthsSaved, interestSaved, yearlyBalances);
        }

        // Simulate offset: reduce effective balance each month by offset amount
        public static (int monthsSaved, double interestSaved) SimulateOffset(
            double loan, double annualRatePct, int termYears, double offsetBalance)
        {
            if (annualRatePct <= 0) return (0, 0);
            var r = annualRatePct / 100.0 / 12.0;
            var payment = CalculateMonthlyRepayment(loan, annualRatePct, termYears);
            var standardTotal = payment * termYears * 12;

            var balance = loan;
            var totalPaid = 0.0;
            var months = 0;
            while (balance > 0.01 && months < termYears * 12)
            {
                // Interest only charged on (balance - offset); offset can't exceed balance
                var effective = Math.Max(0, balance - offsetBalance);
                var interest = effective * r;
                // Payment stays the same — more goes to principal
                balance = balance + interest - payment;
                if (balance < 0) balance = 0;
                totalPaid += payment;
                months++;
            }

            var monthsSaved = Math.Max(0, termYears * 12 - months);
            var interestSaved = Math.Max(0, (standardTotal - loan) - (totalPaid - loan));
            return (monthsSaved, interestSaved);
        }
    }
}
