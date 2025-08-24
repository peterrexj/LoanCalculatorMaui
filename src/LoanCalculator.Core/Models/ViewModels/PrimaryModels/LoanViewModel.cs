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

            // Get all properties of the IncomeViewModel
            var properties = typeof(LoanViewModel).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var property in properties)
            {
                // Skip properties with [JsonIgnore] attribute
                if (property.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Any())
                {
                    continue;
                }

                // Check if the property can be written to
                if (property.CanWrite)
                {
                    // Copy the value from the source to the current instance
                    var value = property.GetValue(source);
                    property.SetValue(this, value);
                }
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

        // this should be updated on the page load only as the income or expense will not change unless the user has navigated to the page and added new income or expense
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

            if (AmortizationBreakdownFrequencyCollection == null || AmortizationBreakdownFrequencyCollection.Count == 0)
            {
                AmortizationBreakdownFrequencyCollection =
                [
                    new SfSegmentItem { Text = "Yearly" },
                    new SfSegmentItem { Text = "Term" }
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
            InterestRate = 5.0;
            LoanTermInYears = 30;
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
                HomeLoanInfo.LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;

                TriggerPropertyChangedOnPropertyTab();
                isUpdating = false;
            }
        }

        public int LoanTermInYears
        {
            get => HomeLoanInfo?.HomeLoanRepaymentRequest?.LoanTermInYears ?? 30;
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
            get => HomeLoanInfo?.HomeLoanRepaymentRequest?.InterestRate ?? 5.0;
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
                //if (HomeLoanInfo.DepositPercentage == value) return;

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

        public double LoanAmountPercentage
        {
            get => HomeLoanInfo?.LoanAmountPercentage ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                //if (HomeLoanInfo.LoanAmountPercentage == value) return;

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
                //if (HomeLoanInfo.LoanAmountDirectInput == value) return;

                isUpdating = true;
                HomeLoanInfo.LoanAmountDirectInput = value;
                TriggerPropertyChangedOnPropertyTab();
                isUpdating = false;
            }
        }


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

        [JsonIgnore] public bool ShowAustralianStateSelectorOnStampDuty => SharedServiceCore.AppInformation != null && SharedServiceCore.AppInformation.IsAustralia;

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

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanAmountAxis =>
            new(new List<ChartDataModel>
                {
                    new(name: DateTime.Now.Year.ToString(), value: HomeLoanInfo?.LoanAmountDirectInput ?? 0)
                }.AsEnumerable());

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanInterestAxis
        {
            get
            {
                return new ObservableCollection<ChartDataModel>(new List<ChartDataModel>
                    {
                        new(name: "2025", value: HomeLoanInfo?.PaymentSummary?.Payment?.TotalInterestPayment ?? 0)
                    }
                    .AsEnumerable());
            }
        }

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanDepositAxis
        {
            get
            {
                return new ObservableCollection<ChartDataModel>(new List<ChartDataModel>
                {
                    new(name: "2025", value: HomeLoanInfo?.DepositAmountDirectInput ?? 0)
                }.AsEnumerable());
            }
        }

        [JsonIgnore]
        public string AffordabilityCurrencySymbol
        {
            get
            {
                if (IsAffordabilityAvailable) return CurrencySymbol;
                else return string.Empty;
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

                PdfDataInsightsModel pdfDataInsights = new PdfDataInsightsModel(this, IncomeSummary, ExpenseSummary);
                pdfDataInsights.InitializeLocalDataSet();

                var t01 = pdfDataInsights?.Income?.TotalAfterExpenseIncludingPropertyMonthly ?? 0;
                //var t02 = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyYearly
                //    .ToCustomCurrencyRounded();

                return $"{t01:N0}";
            }
        }

        [JsonIgnore]
        public string AffordabilityTextDescription
        {
            get
            {
                if (SharedServiceCore.IsTrialUser) return " try premium";
                if (HasIncomeExpensesRecorded == false) return " record your income & expenses";

                return " your monthly affordability status";
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
                $"{pdfDataInsights.Income.TotalAfterExpenseMonthly.ToCustomCurrencyRounded()} This represents the net earnings remaining after deducting {pdfDataInsights.Expense.TotalMonthly.ToCustomCurrencyRounded()} in monthly expenses from the total monthly income of {pdfDataInsights.Income.TotalMonthly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseYearly.Value = pdfDataInsights.Income.TotalAfterExpenseYearly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseYearly.Description =
                $"{pdfDataInsights.Income.TotalAfterExpenseYearly.ToCustomCurrencyRounded()} This reflects the total annual income after subtracting {pdfDataInsights.Expense.TotalYearly.ToCustomCurrencyRounded()} in yearly expenses from the total annual income of {pdfDataInsights.Income.TotalYearly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseWithLoanMonthly.Value = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyMonthly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseWithLoanMonthly.Description =
                $"{pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyMonthly.ToCustomCurrencyRounded()} represents the remaining income after deducting monthly expenses, loan repayments, and investment expenses from the total monthly income of {pdfDataInsights.Income.TotalMonthly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseWithLoanYearly.Value = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyYearly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseWithLoanYearly.Description =
                $"{pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyYearly.ToCustomCurrencyRounded()} reflects the total annual income after subtracting yearly expenses, loan repayments, and investment expenses from the total annual income of {pdfDataInsights.Income.TotalYearly.ToCustomCurrencyRounded()}.";
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
                $"is your overall financial outflows within a given month, covering loan repayments and all other expenses. {Environment.NewLine}(Calculation: {InsightsDetails.RepaymentDetailMonthly.Value} repayment + {InsightsDetails.ExpenseCostOfNewPropertyOwnershipMonthly.Value} cost of ownership + {InsightsDetails.ExpenseCurrentFinancialOutflowsMonthly.Value} current monthly expenses)";


            InsightsDetails.ExpenseOverallTotalYearly.Description =
                $"is your overall financial outflows within a given year, covering loan repayments and all other expenses. {Environment.NewLine}(Calculation: {InsightsDetails.RepaymentDetailYearly.Value} repayment + {InsightsDetails.ExpenseCostOfNewPropertyOwnershipYearly.Value} cost of ownership + {InsightsDetails.ExpenseCurrentFinancialOutflowsYearly.Value} current monthly expenses)";


            OnPropertyChanged(nameof(InsightChartLoanDepositAxis));
            OnPropertyChanged(nameof(InsightChartLoanAmountAxis));
            OnPropertyChanged(nameof(InsightChartLoanInterestAxis));

            IncomeSummary.TransactionRecords?.SumUpData();
            ExpenseSummary.TransactionRecords?.SumUpData();
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

        [JsonIgnore] public ObservableCollection<SfSegmentItem> AmortizationBreakdownFrequencyCollection { get; set; }

        private int _amortizationBreakdownFrequencySelectedIndex;

        public int AmortizationBreakdownFrequencySelectedIndex
        {
            get => _amortizationBreakdownFrequencySelectedIndex;
            set
            {
                _amortizationBreakdownFrequencySelectedIndex = value;
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    UpdateAmortizationData();
                    TriggerPropertyChangedOnAmortizationTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore] public bool IsAmortizationTermBased => AmortizationBreakdownFrequencySelectedIndex != 0;
        [JsonIgnore] public bool IsAmortizationYearBased => AmortizationBreakdownFrequencySelectedIndex == 0;

        [JsonIgnore]
        public List<PaymentAmortisationOutput>? PaymentAmortization => HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms ?? new List<PaymentAmortisationOutput>();

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartPrincipalAmountAxis
        {
            get
            {
                if (HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms
                        .Where(f => f.YearOfPayment != DateTime.Now.Year).Select(f =>
                            new ChartDataModel(name: f.YearOfPayment.ToString(), value: f.PrincipalAmount)));
                }
            }
        }

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartInterestAmountAxis
        {
            get
            {
                if (HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms
                        .Where(f => f.YearOfPayment != DateTime.Now.Year).Select(f =>
                            new ChartDataModel(name: f.YearOfPayment.ToString(), value: f.InterestAmount)));
                }
            }
        }

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartAreaPrincipalAmountAxis
        {
            get
            {
                if (HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    var breakdownCount = RepaymentFrequencySelected.Trim() == "monthly" ? 2 :
                        RepaymentFrequencySelected.Trim() == "fortnightly" ? 4 :
                        RepaymentFrequencySelected.Trim() == "weekly" ? 8 : 2;
                    return new ObservableCollection<ChartDataModel>(
                        HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms
                            .Where(f => f.YearOfPayment != DateTime.Now.Year)
                            .Skip(2)
                            .Where((num, index) => index % breakdownCount == 0)
                            //.Union(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Skip(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Count - 1))
                            .Select(f => new ChartDataModel(name: f.YearOfPayment.ToString(), value: f.PrincipalAmount))
                    );
                }
            }
        }

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartAreaInterestAmountAxis
        {
            get
            {
                if (HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    var breakdownCount = RepaymentFrequencySelected.Trim() == "monthly" ? 2 :
                        RepaymentFrequencySelected.Trim() == "fortnightly" ? 4 :
                        RepaymentFrequencySelected.Trim() == "weekly" ? 8 : 2;
                    return new ObservableCollection<ChartDataModel>(
                        HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms
                            .Where(f => f.YearOfPayment != DateTime.Now.Year)
                            .Skip(2)
                            .Where((num, index) => index % breakdownCount == 0)
                            //.Union(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Skip(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Count - 1))
                            .Select(f => new ChartDataModel(name: f.YearOfPayment.ToString(), value: f.InterestAmount))
                    );
                }
            }
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
                //if (HomeLoanInfo.StampDuty.StampDuty == value) return;

                isUpdating = true;

                HomeLoanInfo.StampDuty.StampDuty = value;
                HomeLoanInfo.LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;

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
                //if (HomeLoanInfo.StampDuty.MortgageCharges == value) return;

                isUpdating = true;

                HomeLoanInfo.StampDuty.MortgageCharges = value;
                HomeLoanInfo.LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;

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
                    HomeLoanInfo.LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;

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
                    HomeLoanInfo.LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;

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
                    HomeLoanInfo.LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;

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
                    HomeLoanInfo.LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;

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
            OnPropertyChanged(nameof(PropertyAmount));
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
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(AffordabilityTextDescription));

            SharedServiceCore.SaveData(this);
            IsBusy = false;
        }
        public virtual void TriggerPropertyChangedOnAmortizationTab()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            OnPropertyChanged(nameof(PaymentAmortization));
            OnPropertyChanged(nameof(AmortizationBreakdownFrequencyCollection));
            OnPropertyChanged(nameof(AmortizationBreakdownFrequencySelectedIndex));
            OnPropertyChanged(nameof(AmortizationChartPrincipalAmountAxis));
            OnPropertyChanged(nameof(AmortizationChartAreaPrincipalAmountAxis));
            OnPropertyChanged(nameof(AmortizationChartInterestAmountAxis));
            OnPropertyChanged(nameof(AmortizationChartAreaInterestAmountAxis));
            OnPropertyChanged(nameof(IsAmortizationTermBased));
            OnPropertyChanged(nameof(IsAmortizationYearBased));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(AffordabilityTextDescription));
            SharedServiceCore.SaveData(this);
        }

        public void RefreshExpenseTabPropertyChanged()
        {
            if (PageHelper.IsFormLoading || TransactionRecords == null) return;

            if (SharedServiceCore.LoadSafe) return;

            TransactionRecords.SumUpData();

            OnPropertyChanged(nameof(IncomeEntryName));
            OnPropertyChanged(nameof(HasErrorIncomeDescription));
            OnPropertyChanged(nameof(IncomeEntryAmount));
            OnPropertyChanged(nameof(HasErrorIncomeAmount));
            OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            OnPropertyChanged(nameof(TotalMonthlyExpenseWithComma));
            OnPropertyChanged(nameof(TotalYearlyExpenseWithComma));
            OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
            OnPropertyChanged(nameof(Transactions));
            OnPropertyChanged(nameof(AutocompleteList));
            OnPropertyChanged(nameof(TotalMonthlyOverallExpense));
            OnPropertyChanged(nameof(TotalMonthlyExistingExpense));
            OnPropertyChanged(nameof(TotalMonthlyOverallExpenseBreakdownWithComma));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(AffordabilityTextDescription));

            SharedServiceCore.SaveData(this);
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

            if (AmortizationBreakdownFrequencySelectedIndex == 0)
            {
                HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByYear(HomeLoanInfo.PaymentSummary);
            }
            else
            {
                HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByTerm(HomeLoanInfo.PaymentSummary);
            }
        }
        public void SyncAmortization()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            UpdateAmortizationData();
            UpdateAmortizationFrequencyText();
            TriggerPropertyChangedOnAmortizationTab();
        }
        public void UpdateAmortizationFrequencyText()
        {
            if (PageHelper.IsFormLoading) return;

            var frequency = RepaymentFrequencySelected.Trim();
            frequency = char.ToUpper(frequency[0]) + frequency.Substring(1);
            AmortizationBreakdownFrequencyCollection[1].Text = frequency;
        }

        public void EventsTriggerStampDutyUpdate()
        {
            if (PageHelper.IsFormLoading || HomeLoanInfo == null) return;

            if (SharedServiceCore.LoadSafe) return;

            if (SharedServiceCore.AppInformation != null && SharedServiceCore.AppInformation.IsAustralia == false) return;
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
                        Category = "Interest", Value = HomeLoanInfo.PaymentSummary.Payment.TotalInterestPayment,
                        ValueWithComma =
                            $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalInterestPaymentRoundedWithComma:N0}"
                    },
                    //new DataModel { Category = "Total", Value = HomeLoanInfo.PaymentSummary.Payment.TotalPayment, ValueWithComma = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalPaymentRounded:N2}" },
                };
            }
        }

        public void TriggerPropertyChangedOnPageLevel()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            OnPropertyChanged(nameof(PdfGenerator));
        }
    }
}
