using LoanCalculator.Core.Exts;
using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Charts;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Pdf;
using LoanCalculator.Core.Pdf;
using LoanCalculator.Core.Services;
using Syncfusion.Maui.Buttons;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class LoanViewModel : ExpenseEntryViewBaseModel
    {
        [JsonIgnore] private readonly IErrorHandlingService _errorHandlingService;
        [JsonIgnore] private readonly IAlertService _alertService;

        public LoanViewModel()
        {
        }

        public LoanViewModel(IErrorHandlingService errorHandlingService, IAlertService alertService)
        {
            _errorHandlingService = errorHandlingService;
            _alertService = alertService;

            ExportInsightsReportCommand = new Command(async () =>
            {
                try
                {
                    await ExportInsights();
                }
                catch (Exception e)
                {
                    _errorHandlingService.HandleException(e);
                }
            });
        }

        public void CopyPropertiesFrom(LoanViewModel source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Assign the backing field directly — property setters guard on HasInitialized
            // which is false during load, so they would silently drop every value.
            _homeLoanInfo = source._homeLoanInfo;
            TransactionRecords = source.TransactionRecords;
        }

        protected override void OnCurrencyChanged()
        {
            // Re-notify all currency-formatted display properties
            OnPropertyChanged(nameof(LoanAmount));
            OnPropertyChanged(nameof(LoanAmountStrFormatted));
            OnPropertyChanged(nameof(DepositAmountStrFormatted));
            OnPropertyChanged(nameof(PropertyTotalAmount));
            OnPropertyChanged(nameof(DepositAmountStrFormatted));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityNegative));
            OnPropertyChanged(nameof(HomeLoanInfo));
        }

        // Controls the Upfront Costs popup on the Asset tab
        [JsonIgnore]
        private bool _isUpfrontInputVisible;
        [JsonIgnore]
        public bool IsUpfrontInputVisible
        {
            get => _isUpfrontInputVisible;
            set
            {
                _isUpfrontInputVisible = value;
                OnPropertyChanged(nameof(IsUpfrontInputVisible));
            }
        }

        [JsonIgnore] protected HomeLoanInformation _homeLoanInfo;
        public HomeLoanInformation HomeLoanInfo
        {
            get => _homeLoanInfo;
            set
            {
                _homeLoanInfo = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore] private ObservableCollection<DataModel> _chartPropertyValueWithInterestPayment;

        [JsonIgnore]
        public ObservableCollection<DataModel> ChartPropertyValueWithInterestPayment
        {
            get => _chartPropertyValueWithInterestPayment;
            set
            {
                _chartPropertyValueWithInterestPayment = value;
                OnPropertyChanged(nameof(ChartPropertyValueWithInterestPayment));
            }
        }

        [JsonIgnore] public ICommand ExportInsightsReportCommand { get; }
        private PdfInsightsGenerator _pdfGenerator;

        [JsonIgnore]
        public PdfInsightsGenerator PdfGenerator
        {
            get => _pdfGenerator;
            set
            {
                _pdfGenerator = value;
                OnPropertyChanged(nameof(PdfGenerator));
            }
        }
        [JsonIgnore]
        private bool _isGeneratingPdf;

        [JsonIgnore]
        public bool IsGeneratingPdf
        {
            get => _isGeneratingPdf;
            set
            {
                if (_isGeneratingPdf == value) return; // Avoid unnecessary updates
                _isGeneratingPdf = value;
                OnPropertyChanged(nameof(IsGeneratingPdf)); // Notify UI of the change
            }
        }

        // Cached, not computed: refreshed by RefreshIncomeExpenseSummariesAsync when a page that
        // reads affordability appears and SharedServiceCore reports income/expense as dirty.
        private bool _hasIncomeExpensesRecorded;
        [JsonIgnore]
        public bool HasIncomeExpensesRecorded
        {
            get => _hasIncomeExpensesRecorded;
            set
            {
                _hasIncomeExpensesRecorded = value;
                OnPropertyChanged(nameof(HasIncomeExpensesRecorded));
            }
        }

        /// <summary>
        /// Re-points <see cref="IncomeSummary"/>/<see cref="ExpenseSummary"/> at the live singleton
        /// view models — or, when a tab owns its own instance and so has never initialised these,
        /// at a fresh snapshot from disk — and recomputes <see cref="HasIncomeExpensesRecorded"/>.
        /// </summary>
        /// <remarks>
        /// <para><see cref="IsAffordabilityAvailable"/> gates on that cached flag and
        /// <see cref="MonthlySurplus"/> gates on it in turn, so <em>every</em> page that reads
        /// affordability has to refresh first — not just the Loan page. What If reads both for its
        /// stress test, and going Budget → What If without this left it computing from a stale
        /// cache: the unlock prompt after income/expenses were added, stale results after they were
        /// deleted.</para>
        /// <para>Deliberately does not clear SharedServiceCore's dirty flags. The caller owns that,
        /// so refreshing on one tab cannot rob another of the notification pass it still needs.</para>
        /// <para>Source order matters and is a correctness issue, not a preference. The Budget tab
        /// owns its <em>own</em> Income/Expense instances (they are not the DI singletons), so those
        /// are the authoritative copy of what the user just typed. Reading them avoids a disk
        /// round-trip — and since <see cref="SharedServiceCore.SaveData"/> is fire-and-forget and
        /// cannot be awaited by anyone, reading back from disk is a race by construction. Disk is
        /// the last resort, used only before any tab has loaded.</para>
        /// </remarks>
        public async Task RefreshIncomeExpenseSummariesAsync(
            IncomeViewModel liveIncome, ExpenseViewModel liveExpense)
        {
            ExpenseSummary = ResolveAuthoritativeExpense(liveExpense)
                ?? await SharedServiceCore.GetExpenseSummaryAsync();

            IncomeSummary = ResolveAuthoritativeIncome(liveIncome)
                ?? await SharedServiceCore.GetIncomeSummaryAsync();

            HasIncomeExpensesRecorded =
                ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 &&
                IncomeSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;
        }

        /// <summary>
        /// The one instance of income that everything must agree on — readers and writers alike.
        /// Returns <c>null</c> only when nothing has loaded yet, which is the sole case where a
        /// caller may fall back to disk.
        /// </summary>
        /// <remarks>
        /// A <em>writer</em> must never fall back to disk, and must never just write the DI
        /// singleton. The Budget tab owns its own Income/Expense instances, which SplashPage
        /// pre-warms from disk on every launch — so they report <c>HasInitialized</c> while the DI
        /// singletons do not. Anything that writes the singleton is therefore invisible to
        /// <see cref="RefreshIncomeExpenseSummariesAsync"/>, and the next page appearance silently
        /// overwrites that write with Budget's copy. That is exactly how the Quick Setup wizard's
        /// income/expense commit used to vanish, flipping
        /// <see cref="HasIncomeExpensesRecorded"/> back to <c>false</c> and hiding the affordability
        /// box the user had just unlocked.
        /// </remarks>
        internal IncomeViewModel? ResolveAuthoritativeIncome(IncomeViewModel? liveIncome)
        {
            var budget = TryGetBudgetViewModel();
            if (budget?.Income?.HasInitialized == true) return budget.Income;
            if (liveIncome?.HasInitialized == true) return liveIncome;
            return null;
        }

        /// <inheritdoc cref="ResolveAuthoritativeIncome"/>
        internal ExpenseViewModel? ResolveAuthoritativeExpense(ExpenseViewModel? liveExpense)
        {
            var budget = TryGetBudgetViewModel();
            if (budget?.Expense?.HasInitialized == true) return budget.Expense;
            if (liveExpense?.HasInitialized == true) return liveExpense;
            return null;
        }

        /// <summary>
        /// ServiceLocator dereferences a settable provider, so it throws if resolved before
        /// App.xaml.cs sets it. Callers here all run from page OnAppearing, which is well after
        /// that — but a null is a better failure than taking down a page load.
        /// </summary>
        private static BudgetViewModel? TryGetBudgetViewModel()
        {
            try
            {
                return ServiceLocator.ServiceProvider == null
                    ? null
                    : ServiceLocator.GetService<BudgetViewModel>();
            }
            catch
            {
                return null;
            }
        }


        private async Task ExportInsights()
        {
            try
            {
                if (IsGeneratingPdf) return; // Prevent multiple exports at the same time
                IsGeneratingPdf = true;
                IsBusy = true;

                await Task.Delay(500); // Simulate a delay for the loader

                if (HasIncomeExpensesRecorded == false)
                {
                    await _alertService.ShowAlertAsync("Warning", "Please enter the income and expense details.", "OK");
                    return;
                }
                else
                {
                    try
                    {
                        IsBusy = true; // Show loader
                        IsActive = false;
                        await Task.Delay(500); // Simulate a delay for the loader
                        await PdfGenerator.GeneratePdf(SharedServiceCore.AppInformation.ApplicationTitle, taskDelay: 400);
                    }
                    catch (Exception ex)
                    {
                        _errorHandlingService.HandleException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors for the entire method
                _errorHandlingService.HandleException(ex, "An unexpected error occurred while exporting insights.");
            }
            finally
            {
                IsGeneratingPdf = false; // Ensure this is reset even if an error occurs
                IsBusy = false; // Hide loader
                IsActive = true;
            }
        }

        public void InitializeViewData()
        {
            if (RepaymentFrequencyCollection == null || RepaymentFrequencyCollection.Count == 0)
            {
                RepaymentFrequencyCollection =
                [
                    new SfSegmentItem { Text = "Monthly" },
                    new SfSegmentItem { Text = "Fortnightly" },
                    new SfSegmentItem { Text = "Weekly" }
                ];
            }


            if (AustraliaStateCollection == null || AustraliaStateCollection.Count == 0)
            {
                AustraliaStateCollection =
                    new ObservableCollection<SfSegmentItem>(
                        StampDutyOutput.AustralianStates.Select(f => new SfSegmentItem { Text = f.ToString() }));
            }

            HomeLoanInfo ??= new HomeLoanInformation
            {
                HomeLoanRepaymentRequest = new HomeLoanRepaymentInput()
            };

            // A fresh HomeLoanRepaymentInput defaults TotalNumberPaymentPerYear to 0, which makes
            // the payment calculation return 0 ("$0 monthly"). Default to Monthly (12) so any path
            // that builds a loan without touching the frequency segment still computes a payment.
            if (HomeLoanInfo.HomeLoanRepaymentRequest != null &&
                HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear == 0)
            {
                HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
            }

            IncomeFrequencyCollection ??=
                new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

            IncomeExpenseEntry ??= new IncomeExpense();
            TransactionRecords ??= new Incomes
            {
                IncomeExpenseEntries = []
            };

            IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
            IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
        }

        public void AddDefaultValues()
        {
            PropertyAmount = 1000000;
            InterestRate = SharedServiceCore.GetDefaultInterestRate();
            LoanTermInYears = SharedServiceCore.GetDefaultLoanTermYears();
            DepositPercentage = 10;
            //AustraliaStateSelectedIndex = null;
        }

        public void AddDefaultToExpenses()
        {
            TransactionRecords ??= new Incomes
            {
                IncomeExpenseEntries = []
            };
            TransactionRecords?.Add("Maintenance cost", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            TransactionRecords?.Add("Water bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            TransactionRecords?.Add("Electricity bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            TransactionRecords?.Add("Gas bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            TransactionRecords?.Add("Council bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
        }

        #region Loan Primary Values

        public double PropertyAmount
        {
            get => HomeLoanInfo?.PropertyAmount ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.PropertyAmount == value) return;

                isUpdating = true;
                HomeLoanInfo.PropertyAmount = value;
                EventsTriggerStampDutyUpdate();
                HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above
                OnPropertyChanged(nameof(PropertyAmountWords));
                OnPropertyChanged(nameof(PropertyAmountFormatted));
                OnPropertyChanged(nameof(LoanAmountWords));

                TriggerPropertyChangedOnPropertyTab();
                isUpdating = false;
            }
        }

        public int LoanTermInYears
        {
            get => HomeLoanInfo?.HomeLoanRepaymentRequest?.LoanTermInYears ?? SharedServiceCore.GetDefaultLoanTermYears();
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears == value) return;

                isUpdating = true;

                HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears = value;

                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        public double InterestRate
        {
            get => HomeLoanInfo?.HomeLoanRepaymentRequest?.InterestRate ?? SharedServiceCore.GetDefaultInterestRate();
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate == value) return;

                isUpdating = true;

                HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate = value;

                TriggerPropertyChangedOnPropertyTab();
                EventsTriggerPriceUpdate();

                isUpdating = false;
            }
        }

        public string LoanAmount => $"{CurrencySymbol}{HomeLoanInfo?.LoanAmount ?? 0:N0}";

        [JsonIgnore] public string PropertyTotalAmount => $"{CurrencySymbol}{HomeLoanInfo?.PropertyTotalAmount ?? 0:N0}";

        [JsonIgnore]
        private bool _isDepositPercentageSliderEnabled;
        public bool IsDepositPercentageSliderEnabled
        {
            get => _isDepositPercentageSliderEnabled;
            set
            {
                _isDepositPercentageSliderEnabled = value;
                OnPropertyChanged(nameof(IsDepositPercentageSliderEnabled));
            }
        }

        #endregion

        #region Loan Calculations

        [JsonIgnore]
        public string DepositAmountStrFormatted => $"{CurrencySymbol}{HomeLoanInfo?.DepositAmountDirectInput ?? 0:N0}";

        [JsonIgnore]
        public string InterestRateFormatted =>
            InterestRate % 1 == 0
                ? $"{InterestRate:N0}%"
                : $"{InterestRate:N2}%";

        public double DepositPercentage
        {
            get => HomeLoanInfo?.DepositPercentage ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.DepositPercentage == value) return;

                isUpdating = true;

                HomeLoanInfo.DepositPercentage = value;
                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        public double DepositAmountDirectInput
        {
            get => HomeLoanInfo?.DepositAmountDirectInput ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;
                    HomeLoanInfo.DepositAmountDirectInput = value;
                    TriggerPropertyChangedOnPropertyTab();
                    isUpdating = false;
                }
            }
        }

        [JsonIgnore] public string LoanAmountStrFormatted => $"{CurrencySymbol}{HomeLoanInfo?.LoanAmountDirectInput ?? 0:N0}";

        // Flat wrappers over the nested PaymentSummary path. EventsTriggerPriceUpdate replaces
        // HomeLoanInfo.PaymentSummary with a NEW object but HomeLoanInformation does not raise
        // PropertyChanged, so XAML bindings to the deep path (HomeLoanInfo.PaymentSummary.Payment.X)
        // never refresh. Bind the Asset-tab repayment box and chart center to these instead and
        // notify them from TriggerPropertyChangedOnPropertyTab.
        [JsonIgnore] public string TermPaymentRoundedWithComma =>
            HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentRoundedWithComma ?? "0";

        [JsonIgnore] public string TotalPaymentRoundedWithComma =>
            HomeLoanInfo?.PaymentSummary?.Payment?.TotalPaymentRoundedWithComma ?? "0";

        [JsonIgnore] public string ChartInterestCategoryLabel =>
            $"Interest over {LoanTermInYears} yr{(LoanTermInYears == 1 ? "" : "s")}";

        [JsonIgnore] public string ChartTotalCostSubtitle =>
            $"Total cost over {LoanTermInYears} yr{(LoanTermInYears == 1 ? "" : "s")}";

        [JsonIgnore] public string ChartInsightSubtitle =>
            $"Deposit · loan · lifetime interest over {LoanTermInYears} yr{(LoanTermInYears == 1 ? "" : "s")}";

        public double LoanAmountPercentage
        {
            get => HomeLoanInfo?.LoanAmountPercentage ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.LoanAmountPercentage == value) return;

                isUpdating = true;

                HomeLoanInfo.LoanAmountPercentage = value;
                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        public double LoanAmountDirectInput
        {
            get => HomeLoanInfo?.LoanAmountDirectInput ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.LoanAmountDirectInput == value) return;

                isUpdating = true;
                HomeLoanInfo.LoanAmountDirectInput = value;
                OnPropertyChanged(nameof(LoanAmountWords));
                TriggerPropertyChangedOnPropertyTab();
                isUpdating = false;
            }
        }

        // Live word representations for Quick Input popup
        [JsonIgnore]
        public string PropertyAmountWords => NumberToWords((long)Math.Round(HomeLoanInfo?.PropertyAmount ?? 0));
        [JsonIgnore]
        public string PropertyAmountFormatted => $"{CurrencySymbol}{HomeLoanInfo?.PropertyAmount ?? 0:N0}";
        [JsonIgnore]
        public string LoanAmountWords => NumberToWords((long)Math.Round(HomeLoanInfo?.LoanAmountDirectInput ?? 0));

        private static string NumberToWords(long n) => NumberToWordsPublic(n);

        public static string NumberToWordsPublic(long n)
        {
            if (n <= 0) return string.Empty;
            if (n < 0) return "Minus " + NumberToWords(-n);
            var parts = new System.Collections.Generic.List<string>();
            if (n >= 1_000_000_000) { parts.Add(NumberToWords(n / 1_000_000_000) + " Billion"); n %= 1_000_000_000; }
            if (n >= 1_000_000)     { parts.Add(NumberToWords(n / 1_000_000) + " Million"); n %= 1_000_000; }
            if (n >= 1_000)         { parts.Add(NumberToWords(n / 1_000) + " Thousand"); n %= 1_000; }
            if (n >= 100)           { parts.Add(Ones[n / 100] + " Hundred"); n %= 100; }
            if (n >= 20)            { parts.Add(Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "")); }
            else if (n > 0)         { parts.Add(Ones[n]); }
            return string.Join(", ", parts);
        }
        private static readonly string[] Ones = ["", "One","Two","Three","Four","Five","Six","Seven","Eight","Nine","Ten","Eleven","Twelve","Thirteen","Fourteen","Fifteen","Sixteen","Seventeen","Eighteen","Nineteen"];
        private static readonly string[] Tens  = ["","","Twenty","Thirty","Forty","Fifty","Sixty","Seventy","Eighty","Ninety"];


        #endregion

        #region Australian States

        [JsonIgnore] public ObservableCollection<SfSegmentItem> AustraliaStateCollection { get; set; }

        public int? AustraliaStateSelectedIndex
        {
            get => HomeLoanInfo?.StampDuty?.AustraliaStateIndex;
            set
            {
                if (HomeLoanInfo == null || value == null)
                    return;

                var newState = StampDutyOutput.AustraliaStateFromIndex(value.Value);
                if (HomeLoanInfo.StampDuty.AustraliaStateSelected != newState)
                {
                    HomeLoanInfo.StampDuty.AustraliaStateSelected = newState;
                    EventsTriggerStampDutyUpdate();
                    LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;
                }
            }
        }

        #endregion

        #region Country specific functionality

        private const string AustralianModeKey = "IsAustralianModeEnabled";

        // Loaded from Preferences on page appearance and updated when Settings changes
        [JsonIgnore]
        private bool _isAustralianModeEnabled = false;

        [JsonIgnore]
        public bool IsAustralianModeEnabled
        {
            get => _isAustralianModeEnabled;
            set
            {
                _isAustralianModeEnabled = value;
                OnPropertyChanged(nameof(IsAustralianModeEnabled));
                OnPropertyChanged(nameof(ShowAustralianStateSelectorOnStampDuty));
                OnPropertyChanged(nameof(HeightOfGridRowToggledByCountryOnStampDuty));
                // Australian mode always forces stamp duty on
                OnPropertyChanged(nameof(ShowStampDutyInput));
                OnPropertyChanged(nameof(IsStampDutyToggleEnabled));
            }
        }

        public void LoadAustralianModeSetting()
        {
            IsAustralianModeEnabled = Preferences.Get(AustralianModeKey, false);
            LoadStampDutySetting(); // re-evaluate stamp duty after Australian mode changes
        }

        // Visibility driven purely by the user's toggle
        [JsonIgnore]
        public bool ShowAustralianStateSelectorOnStampDuty => IsAustralianModeEnabled;

        // ── Stamp Duty toggle ──────────────────────────────────────────────────
        private const string StampDutyKey = "IsStampDutyEnabled";
        [JsonIgnore] private bool _isStampDutyEnabled;

        [JsonIgnore]
        public bool IsStampDutyEnabled
        {
            get => _isStampDutyEnabled;
            set
            {
                _isStampDutyEnabled = value;
                OnPropertyChanged(nameof(IsStampDutyEnabled));
                OnPropertyChanged(nameof(ShowStampDutyInput));
            }
        }

        public void LoadStampDutySetting()
        {
            // Australian mode always forces stamp duty on regardless of saved preference
            _isStampDutyEnabled = IsAustralianModeEnabled || Preferences.Get(StampDutyKey, false);
            OnPropertyChanged(nameof(IsStampDutyEnabled));
            OnPropertyChanged(nameof(ShowStampDutyInput));
            OnPropertyChanged(nameof(IsStampDutyToggleEnabled));
        }

        // True when stamp duty field should be visible in the popup
        [JsonIgnore]
        public bool ShowStampDutyInput => IsAustralianModeEnabled || IsStampDutyEnabled;

        // User cannot turn OFF stamp duty when Australian mode is active
        [JsonIgnore]
        public bool IsStampDutyToggleEnabled => !IsAustralianModeEnabled;

        [JsonIgnore]
        public int HeightOfGridRowToggledByCountryOnStampDuty
        {
            get
            {
                if (!ShowAustralianStateSelectorOnStampDuty)
                    return 0;

                if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                    return 60;
                else if (DeviceInfo.Idiom == DeviceIdiom.Tablet || DeviceInfo.Idiom == DeviceIdiom.Desktop || DeviceInfo.Idiom == DeviceIdiom.TV)
                    return 110;

                return 40; // Default value
            }
        }

        #endregion

        #region Insights

        // Set by the view's tab selection event to avoid firing expensive
        // notifications for tabs the user isn't currently looking at.
        [JsonIgnore] public bool IsAmortizationTabActive { get; set; }
        [JsonIgnore] public bool IsInsightsTabActive { get; set; }

        [JsonIgnore] private ObservableCollection<ChartDataModel> _insightChartLoanAmountAxis = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _insightChartLoanInterestAxis = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _insightChartLoanDepositAxis = new();

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanAmountAxis => _insightChartLoanAmountAxis;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanInterestAxis => _insightChartLoanInterestAxis;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanDepositAxis => _insightChartLoanDepositAxis;

        private void UpdateInsightCharts()
        {
            var year = DateTime.Now.Year.ToString();
            var loanAmount = HomeLoanInfo?.LoanAmountDirectInput ?? 0;
            var interest = HomeLoanInfo?.PaymentSummary?.Payment?.TotalInterestPayment ?? 0;
            var deposit = HomeLoanInfo?.DepositAmountDirectInput ?? 0;

            ReplaceChartPoint(_insightChartLoanAmountAxis, year, loanAmount);
            ReplaceChartPoint(_insightChartLoanInterestAxis, year, interest);
            ReplaceChartPoint(_insightChartLoanDepositAxis, year, deposit);
        }

        private static void ReplaceChartPoint(ObservableCollection<ChartDataModel> col, string name, double value)
        {
            if (col.Count == 0)
                col.Add(new ChartDataModel(name, value));
            else
            {
                col.Clear();
                col.Add(new ChartDataModel(name, value));
            }
        }

        [JsonIgnore]
        public string AffordabilityCurrencySymbol
        {
            get
            {
                if (!IsAffordabilityAvailable) return string.Empty;

                // The amount is rendered in a separate span (see Affordability), so when the
                // affordability is negative the minus must live with the symbol here to read
                // "-$6,328" rather than "$-6,328".
                var amount = AffordabilityRawValue;
                return amount < 0 ? $"-{CurrencySymbol}" : CurrencySymbol;
            }
        }

        // Exposed for WhatIfViewModel — monthly surplus after all income/expenses/loan repayment.
        // Returns 0 when income/expense data is not available.
        [JsonIgnore]
        public double MonthlySurplus => IsAffordabilityAvailable ? AffordabilityRawValue : 0;

        // Raw affordability value (monthly, after all expenses incl. property/loan) used to
        // decide sign placement and to render the absolute amount.
        [JsonIgnore]
        private double AffordabilityRawValue
        {
            get
            {
                if (!IsAffordabilityAvailable) return 0;
                // No defensive resets here any more: SumUpData no longer takes a deduction, so
                // nothing can leave these records holding a net figure (TECH-DEBT D1).
                var pdf = new PdfDataInsightsModel(this, IncomeSummary, ExpenseSummary);
                pdf.InitializeLocalDataSet();
                return pdf?.Income?.TotalAfterExpenseIncludingPropertyMonthly ?? 0;
            }
        }
        [JsonIgnore]
        public bool IsAffordabilityAvailable
        {
            get
            {
                if (SharedServiceCore.IsTrialUser) return false;
                if (HasIncomeExpensesRecorded == false) return false;

                return true;
            }
        }
        [JsonIgnore]
        public string Affordability
        {
            get
            {
                if (IsAffordabilityAvailable == false) return "Affordability";

                // Absolute value — the sign (and symbol) are rendered by AffordabilityCurrencySymbol.
                return $"{Math.Abs(AffordabilityRawValue):N0}";
            }
        }

        [JsonIgnore]
        public bool IsAffordabilityNegative => IsAffordabilityAvailable && AffordabilityRawValue < 0;

        [JsonIgnore]
        public string AffordabilityTextDescription
        {
            get
            {
                if (SharedServiceCore.IsTrialUser) return " try premium";
                if (HasIncomeExpensesRecorded == false) return " record your income & expenses";

                return " estimated monthly position (estimate only)";
            }
        }

        [JsonIgnore] public InsightsDetailsViewModel InsightsDetails { get; set; }

        private void BuildInsights()
        {
            if (PageHelper.IsFormLoading) return;

            PdfDataInsightsModel pdfDataInsights = new PdfDataInsightsModel(this, IncomeSummary, ExpenseSummary);
            pdfDataInsights.InitializeLocalDataSet();

            InsightsDetails = new InsightsDetailsViewModel();

            InsightsDetails.AffordabilityMonthly.Value = pdfDataInsights.Income
                .TotalAfterExpenseIncludingPropertyMonthly.ToCustomCurrencyRounded();
            InsightsDetails.AffordabilityYearly.Value = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyYearly
                .ToCustomCurrencyRounded();

            #region Property

            InsightsDetails.PropertyAmount.Value = PropertyAmount.ToCustomCurrencyRounded();
            InsightsDetails.PropertyEstimatedUpfront.Value = (HomeLoanInfo?.OtherExpenseTotalAmount ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.PropertyTotalAmount.Value =
                (HomeLoanInfo?.PropertyTotalAmount ?? 0).ToCustomCurrencyRounded();

            #endregion

            #region Loan

            InsightsDetails.LoanAmount.Value = (HomeLoanInfo?.LoanAmountDirectInput ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.DepositAmount.Value = (HomeLoanInfo?.DepositAmountDirectInput ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.TotalRepaymentToBank.Value =
                (HomeLoanInfo?.PaymentSummary?.Payment?.TotalPaymentRounded ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.TotalInterestToBank.Value =
                (HomeLoanInfo?.PaymentSummary?.Payment?.TotalInterestPaymentRounded ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.LoanTerm.Value = $"{LoanTermInYears} years";
            InsightsDetails.InterestRate.Value = $"{InterestRate}%";
            InsightsDetails.RepaymentDetailSelectedFrequency.Value =
                $"{HomeLoanInfo?.PaymentSummary?.Payment?.TermPayment.ToCustomCurrencyRounded()} {RepaymentFrequencySelected.Trim()}";
            InsightsDetails.RepaymentFrequency.Value = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(RepaymentFrequencySelected.Trim());
            InsightsDetails.RepaymentDetailMonthly.Value =
                (HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.RepaymentDetailYearly.Value =
                (HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentYearly ?? 0).ToCustomCurrencyRounded();

            #endregion

            #region Income

            InsightsDetails.IncomeTotalMonthly.Value = pdfDataInsights.Income.TotalMonthly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeTotalYearly.Value = pdfDataInsights.Income.TotalYearly.ToCustomCurrencyRounded();

            InsightsDetails.IncomeAfterExpenseMonthly.Value = pdfDataInsights.Income.TotalAfterExpenseMonthly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseMonthly.Description =
                $"The net earnings remaining after deducting {pdfDataInsights.Expense.TotalMonthly.ToCustomCurrencyRounded()} in monthly expenses from the total monthly income of {pdfDataInsights.Income.TotalMonthly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseYearly.Value = pdfDataInsights.Income.TotalAfterExpenseYearly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseYearly.Description =
                $"The total annual income after subtracting {pdfDataInsights.Expense.TotalYearly.ToCustomCurrencyRounded()} in yearly expenses from the total annual income of {pdfDataInsights.Income.TotalYearly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseWithLoanMonthly.Value = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyMonthly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseWithLoanMonthly.Description =
                $"The remaining income after deducting monthly expenses, loan repayments, and investment expenses from the total monthly income of {pdfDataInsights.Income.TotalMonthly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseWithLoanYearly.Value = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyYearly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseWithLoanYearly.Description =
                $"The total annual income after subtracting yearly expenses, loan repayments, and investment expenses from the total annual income of {pdfDataInsights.Income.TotalYearly.ToCustomCurrencyRounded()}.";
            #endregion

            #region Expense

            InsightsDetails.ExpenseOverallTotalMonthly.Value = pdfDataInsights.Income.TotalExpenseIncludingPropertyMonthly.ToCustomCurrencyRounded();
            InsightsDetails.ExpenseOverallTotalYearly.Value = pdfDataInsights.Income.TotalExpenseIncludingPropertyYearly.ToCustomCurrencyRounded();

            InsightsDetails.ExpenseCostOfNewPropertyOwnershipMonthly.Value =
                pdfDataInsights.Loan.TotalMonthlyRunningExpense.ToCustomCurrencyRounded();
            InsightsDetails.ExpenseCostOfNewPropertyOwnershipYearly.Value = pdfDataInsights.Loan.TotalYearlyRunningExpense.ToCustomCurrencyRounded();

            InsightsDetails.ExpenseLoanFinancialCommitmentsMonthly.Value = pdfDataInsights.Loan.MonthlyRepaymentWithExpenses.ToCustomCurrencyRounded();
            InsightsDetails.ExpenseLoanFinancialCommitmentsYearly.Value = pdfDataInsights.Loan.YearlyRepaymentWithExpenses.ToCustomCurrencyRounded();

            InsightsDetails.ExpenseCurrentFinancialOutflowsMonthly.Value =
                pdfDataInsights.Expense.TotalMonthly.ToCustomCurrencyRounded();
            InsightsDetails.ExpenseCurrentFinancialOutflowsYearly.Value = pdfDataInsights.Expense.TotalYearly.ToCustomCurrencyRounded();

            #endregion


            InsightsDetails.ExpenseOverallTotalMonthly.Description =
                $"Your overall financial outflows within a given month, covering loan repayments and all other expenses. {Environment.NewLine}(Calculation: {InsightsDetails.RepaymentDetailMonthly.Value} repayment + {InsightsDetails.ExpenseCostOfNewPropertyOwnershipMonthly.Value} cost of ownership + {InsightsDetails.ExpenseCurrentFinancialOutflowsMonthly.Value} current monthly expenses)";


            InsightsDetails.ExpenseOverallTotalYearly.Description =
                $"Your overall financial outflows within a given year, covering loan repayments and all other expenses. {Environment.NewLine}(Calculation: {InsightsDetails.RepaymentDetailYearly.Value} repayment + {InsightsDetails.ExpenseCostOfNewPropertyOwnershipYearly.Value} cost of ownership + {InsightsDetails.ExpenseCurrentFinancialOutflowsYearly.Value} current yearly expenses)";


            UpdateInsightCharts();
        }

        #endregion

        #region Repayment Frequency

        [JsonIgnore] public ObservableCollection<SfSegmentItem> RepaymentFrequencyCollection { get; set; }

        public int RepaymentFrequencySelectedIndex
        {
            get
            {
                if (HomeLoanInfo?.HomeLoanRepaymentRequest?.TotalNumberPaymentPerYear == 12)
                {
                    return 0;
                }
                else if (HomeLoanInfo?.HomeLoanRepaymentRequest?.TotalNumberPaymentPerYear == 24)
                {
                    return 1;
                }
                else if (HomeLoanInfo?.HomeLoanRepaymentRequest?.TotalNumberPaymentPerYear == 52)
                {
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (HomeLoanInfo?.HomeLoanRepaymentRequest == null) return;

                if (value == 0)
                {
                    HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
                }
                else if (value == 1)
                {
                    HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 24;
                }
                else if (value == 2)
                {
                    HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 52;
                }
                else
                {
                    HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
                }

                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore]
        public string RepaymentFrequencySelected => RepaymentFrequencySelectedIndex == 0 ? " monthly" :
            RepaymentFrequencySelectedIndex == 1 ? " fortnightly" :
            RepaymentFrequencySelectedIndex == 2 ? " weekly" : " monthly";

        #endregion

        #region Amortisation


        [JsonIgnore]
        public List<PaymentAmortisationOutput>? PaymentAmortization => HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms ?? new List<PaymentAmortisationOutput>();

        [JsonIgnore] private ObservableCollection<ChartDataModel> _amortizationChartPrincipal = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _amortizationChartInterest = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _amortizationChartBalance = new();

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartPrincipalAmountAxis => _amortizationChartPrincipal;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartInterestAmountAxis => _amortizationChartInterest;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartBalanceAxis => _amortizationChartBalance;

        [JsonIgnore] public string AmortizationBalanceSubtitle =>
            $"Remaining loan balance over {LoanTermInYears} yr{(LoanTermInYears == 1 ? "" : "s")}";

        private void UpdateAmortizationCharts()
        {
            var terms = HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms;
            if (terms == null)
            {
                _amortizationChartPrincipal.Clear();
                _amortizationChartInterest.Clear();
                _amortizationChartBalance.Clear();
                return;
            }

            var currentYear = DateTime.Now.Year;
            var filtered = terms.Where(f => f.YearOfPayment != currentYear).ToList();

            RefillCollection(_amortizationChartPrincipal,
                filtered.Select(f => new ChartDataModel(f.YearOfPayment.ToString(), f.PrincipalAmount)));

            RefillCollection(_amortizationChartInterest,
                filtered.Select(f => new ChartDataModel(f.YearOfPayment.ToString(), f.InterestAmount)));

            // Balance chart: one point per year using the last payment of that year
            var balanceByYear = filtered
                .GroupBy(f => f.YearOfPayment)
                .Select(g => new ChartDataModel(g.Key.ToString(), g.Last().BalanceAmount))
                .ToList();
            RefillCollection(_amortizationChartBalance, balanceByYear);
        }

        // Called when currency changes — rebuilds chart data and notifies the grid
        // unconditionally (bypasses the IsAmortizationTabActive guard).
        public void ForceUpdateAmortizationCharts()
        {
            UpdateAmortizationCharts();
            OnPropertyChanged(nameof(PaymentAmortization));
            OnPropertyChanged(nameof(CurrencySymbol));
        }

        private static void RefillCollection(ObservableCollection<ChartDataModel> col, IEnumerable<ChartDataModel> source)
        {
            col.Clear();
            foreach (var item in source)
                col.Add(item);
        }

        #endregion

        #region Other expenses

        [JsonIgnore]
        public double StampDuty
        {
            get => HomeLoanInfo?.StampDuty?.StampDuty ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.StampDuty.StampDuty == value) return;

                isUpdating = true;

                HomeLoanInfo.StampDuty.StampDuty = value;
                HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        [JsonIgnore]
        public double MortgageCharges
        {
            get => HomeLoanInfo?.StampDuty?.MortgageCharges ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.StampDuty.MortgageCharges == value) return;

                isUpdating = true;

                HomeLoanInfo.StampDuty.MortgageCharges = value;
                HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        [JsonIgnore]
        public double ConveyancerFee
        {
            get => HomeLoanInfo?.ConveyanceExpense?.ConveyancerFee ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    HomeLoanInfo.ConveyanceExpense.ConveyancerFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore]
        public double BankFee
        {
            get => HomeLoanInfo?.BankExpense?.BankSettlementFee ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    HomeLoanInfo.BankExpense.BankSettlementFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore]
        public double InspectionFee
        {
            get => HomeLoanInfo?.OtherExpense?.InspectionFee ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    HomeLoanInfo.OtherExpense.InspectionFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore]
        public double OtherExpenses
        {
            get => HomeLoanInfo?.OtherExpense?.OtherExpenses ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    HomeLoanInfo.OtherExpense.OtherExpenses = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore] public string OtherExpenseTotalAmount => $"{CurrencySymbol}{HomeLoanInfo?.OtherExpenseTotalAmount ?? 0:N0}";

        #endregion

        #region Expense

        #region Total Details

        [JsonIgnore]
        public string TotalMonthlyExpenseWithComma => TransactionRecords?.IncomeExpenseSummary?.TotalMonthlyWithComma;

        [JsonIgnore]
        public string TotalYearlyExpenseWithComma => TransactionRecords?.IncomeExpenseSummary?.TotalYearlyWithComma;


        [JsonIgnore] private IncomeViewModel _incomeSummary;

        [JsonIgnore]
        public IncomeViewModel IncomeSummary
        {
            get => _incomeSummary;
            set
            {
                _incomeSummary = value;
                OnPropertyChanged(nameof(IncomeSummary));
            }
        }

        [JsonIgnore] private ExpenseViewModel _expenseSummary;

        [JsonIgnore]
        public ExpenseViewModel ExpenseSummary
        {
            get => _expenseSummary;
            set
            {
                _expenseSummary = value;
                OnPropertyChanged(nameof(ExpenseSummary));
            }
        }

        [JsonIgnore] public string TotalMonthlyExistingExpense => $"{Math.Round(ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0, 0):N0}";

        [JsonIgnore]
        public string TotalMonthlyOverallExpense =>
            $"{Math.Round(TransactionRecords?.IncomeExpenseSummary?.TotalMonthly + HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0, 0):N0}";

        [JsonIgnore]
        public string TotalMonthlyOverallExpenseBreakdownWithComma
        {
            get
            {
                string expenses = System.Environment.NewLine;

                expenses += $"(${TransactionRecords?.IncomeExpenseSummary?.TotalMonthly.ToString("N0") ?? "0"}";

                if (HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly != null)
                {
                    expenses += $" + ${HomeLoanInfo.PaymentSummary.Payment.TermPaymentMonthly.ToString("N0")}";
                }

                expenses += ")";

                return expenses;
            }
        }


        #endregion

        #endregion

        public void TriggerPropertyChangedOnPropertyTab()
        {
            if (PageHelper.IsFormLoading) return;
            if (SharedServiceCore.LoadSafe) return;

            EventsTriggerPriceUpdate();

            // Always notify — these drive the Asset tab which is always visible
            OnPropertyChanged(nameof(PropertyAmount));
            OnPropertyChanged(nameof(PropertyAmountFormatted));
            OnPropertyChanged(nameof(LoanTermInYears));
            OnPropertyChanged(nameof(InterestRate));
            OnPropertyChanged(nameof(StampDuty));
            OnPropertyChanged(nameof(DepositAmountDirectInput));
            OnPropertyChanged(nameof(LoanAmountDirectInput));
            OnPropertyChanged(nameof(LoanAmountPercentage));
            OnPropertyChanged(nameof(LoanAmountStrFormatted));
            OnPropertyChanged(nameof(DepositPercentage));
            OnPropertyChanged(nameof(LoanAmount));
            OnPropertyChanged(nameof(PropertyTotalAmount));
            OnPropertyChanged(nameof(InterestRateFormatted));
            OnPropertyChanged(nameof(DepositAmountStrFormatted));
            OnPropertyChanged(nameof(OtherExpenseTotalAmount));
            OnPropertyChanged(nameof(MortgageCharges));
            OnPropertyChanged(nameof(ConveyancerFee));
            OnPropertyChanged(nameof(BankFee));
            OnPropertyChanged(nameof(InspectionFee));
            OnPropertyChanged(nameof(OtherExpenses));
            OnPropertyChanged(nameof(RepaymentFrequencySelected));
            OnPropertyChanged(nameof(HomeLoanInfo));
            OnPropertyChanged(nameof(TermPaymentRoundedWithComma));
            OnPropertyChanged(nameof(TotalPaymentRoundedWithComma));
            OnPropertyChanged(nameof(ChartInterestCategoryLabel));
            OnPropertyChanged(nameof(ChartTotalCostSubtitle));
            OnPropertyChanged(nameof(ChartInsightSubtitle));
            OnPropertyChanged(nameof(AmortizationBalanceSubtitle));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityNegative));
            OnPropertyChanged(nameof(AffordabilityTextDescription));

            if (IsAmortizationTabActive)
            {
                UpdateAmortizationCharts();
            }

            // Only update insight charts if the Insights tab is currently visible
            if (IsInsightsTabActive)
            {
                UpdateInsightCharts();
            }

            ScheduleSave(() => SharedServiceCore.SaveData(this));
            IsBusy = false;
        }
        public virtual void TriggerPropertyChangedOnAmortizationTab()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            UpdateAmortizationCharts();

            OnPropertyChanged(nameof(PaymentAmortization));
            OnPropertyChanged(nameof(AmortizationBalanceSubtitle));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityNegative));
            OnPropertyChanged(nameof(AffordabilityTextDescription));
            ScheduleSave(() => SharedServiceCore.SaveData(this));
        }

        public void RefreshExpenseTabPropertyChanged()
        {
            if (PageHelper.IsFormLoading || TransactionRecords == null) return;

            if (SharedServiceCore.LoadSafe) return;

            TransactionRecords.SumUpData();

            OnPropertyChanged(nameof(IncomeEntryName));
            OnPropertyChanged(nameof(HasErrorIncomeDescription));
            OnPropertyChanged(nameof(IncomeEntryAmount));
            OnPropertyChanged(nameof(IncomeEntryAmountText));
            OnPropertyChanged(nameof(HasErrorIncomeAmount));
            OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            OnPropertyChanged(nameof(TotalMonthlyExpenseWithComma));
            OnPropertyChanged(nameof(TotalYearlyExpenseWithComma));
            OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
            OnPropertyChanged(nameof(Transactions));
            OnPropertyChanged(nameof(FilteredTransactions));
            OnPropertyChanged(nameof(AutocompleteNameList));
            OnPropertyChanged(nameof(AutocompleteList));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(TotalMonthlyOverallExpense));
            OnPropertyChanged(nameof(TotalMonthlyExistingExpense));
            OnPropertyChanged(nameof(TotalMonthlyOverallExpenseBreakdownWithComma));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityNegative));
            OnPropertyChanged(nameof(AffordabilityTextDescription));

            ScheduleSave(() => SharedServiceCore.SaveData(this));
        }
        public void RefreshInsightsTabPropertyChanged()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            BuildInsights();

            OnPropertyChanged(nameof(InsightsDetails));
        }

        public void UpdateAmortizationData()
        {
            if (PageHelper.IsFormLoading) return;

            HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByYear(HomeLoanInfo.PaymentSummary);
        }

        public void SyncAmortization()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            UpdateAmortizationData();
            TriggerPropertyChangedOnAmortizationTab();
        }

        public void EventsTriggerStampDutyUpdate()
        {
            if (PageHelper.IsFormLoading || HomeLoanInfo == null) return;

            if (SharedServiceCore.LoadSafe) return;

            if (!ShowAustralianStateSelectorOnStampDuty) return;
            if (HomeLoanInfo.StampDuty.AustraliaStateSelected.HasValue)
            {
                HomeLoanInfo.StampDuty =
                    HomeLoanCalculator.StampDutyCalculator.CalculateStampDutyAustralia(
                        HomeLoanInfo.StampDuty.AustraliaStateSelected.Value, PropertyAmount);
            }
            else
            {
                // Optionally, handle the case where no state is selected.
                // For example, you could reset StampDuty or leave it unchanged.
                // HomeLoanInfo.StampDuty = new StampDutyOutput();
            }
            HomeLoanInfo.StampDuty.AutoUpdateMortgageCharges();
            OnPropertyChanged(nameof(StampDuty));
        }
        public void EventsTriggerPriceUpdate()
        {
            if (PageHelper.IsFormLoading || HomeLoanInfo == null) return;

            if (SharedServiceCore.LoadSafe) return;

            // Guard against a 0 payments-per-year (e.g. data saved before the frequency segment
            // was ever touched, or a wizard-only flow). Without this the payment calc returns 0
            // and the Asset tab shows "$0 monthly" even though a loan amount exists.
            if (HomeLoanInfo.HomeLoanRepaymentRequest != null &&
                HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear == 0)
            {
                HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
            }

            HomeLoanInfo.PaymentSummary =
                HomeLoanCalculator.CalculateHomeLoanPayments(HomeLoanInfo.LoanAmount,
                    HomeLoanInfo.HomeLoanRepaymentRequest);

            LiveEventsUpdate();
        }
        public void LiveEventsUpdate()
        {
            if (PageHelper.IsFormLoading || HomeLoanInfo == null) return;

            if (SharedServiceCore.LoadSafe) return;

            if (HomeLoanInfo.PropertyTotalAmount > 0 && HomeLoanInfo.PaymentSummary.Payment.TotalPaymentRounded > 0)
            {
                ChartPropertyValueWithInterestPayment = new ObservableCollection<DataModel>
                {
                    new DataModel
                    {
                        Category = "Loan", Value = HomeLoanInfo.LoanAmount,
                        ValueWithComma = $"{CurrencySymbol}{HomeLoanInfo.LoanAmount:N0}"
                    },
                    new DataModel
                    {
                        Category = ChartInterestCategoryLabel, Value = HomeLoanInfo.PaymentSummary.Payment.TotalInterestPayment,
                        ValueWithComma =
                            $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalInterestPaymentRoundedWithComma:N0}"
                    },
                    //new DataModel { Category = "Total", Value = HomeLoanInfo.PaymentSummary.Payment.TotalPayment, ValueWithComma = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalPaymentRounded:N2}" },
                };
            }
        }

        // Called once after data load to ensure SfSegmentedControl picks up its ItemsSource and SelectedIndex
        public void TriggerSegmentCollectionsRefresh()
        {
            OnPropertyChanged(nameof(RepaymentFrequencyCollection));
            OnPropertyChanged(nameof(RepaymentFrequencySelectedIndex));
            OnPropertyChanged(nameof(AustraliaStateCollection));
            OnPropertyChanged(nameof(AustraliaStateSelectedIndex));
        }

        public void TriggerPropertyChangedOnPageLevel()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;
            OnPropertyChanged(nameof(PdfGenerator));
        }
    }
}
