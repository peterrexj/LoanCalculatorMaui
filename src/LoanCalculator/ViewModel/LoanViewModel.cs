using Calculator;
using LoanCalculator.Models;
using LoanCalculator.Models.Charts;
using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculator.Models.Income.Summary;
using Pj.Library;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using LoanCalculatorMaui.Services;
using Syncfusion.Maui.Buttons;
using AndroidX.Lifecycle;

namespace LoanCalculatorMaui.ViewModel
{
    public class LoanViewModel : ViewModelUiBase    
    {
        public bool HasInitialized { get; private set; } = false;

        public LoanViewModel(ISharedServices sharedServices) : base(sharedServices) { }

        public LoanViewModel() : base() { }

        public void InitializeViewData()
        {
            RepaymentFrequencyCollection =
            [
                new SfSegmentItem { Text = "Monthly" },
                new SfSegmentItem { Text = "Fortnightly" },
                new SfSegmentItem { Text = "Weekly" }
            ];

            AmortizationBreakdownFrequencyCollection =
            [
                new SfSegmentItem { Text = "Yearly" },
                new SfSegmentItem { Text = "Term" }
            ];

            AustraliaStateCollection = new ObservableCollection<SfSegmentItem>(StampDutyOutput.AustralianStates.Select(f => new SfSegmentItem { Text = f.ToString() }));

            HomeLoanInfo = new LoanCalculator.Models.HomeLoanInformation
            {
                HomeLoanRepaymentRequest = new HomeLoanRepaymentInput()
            };

            IncomeFrequencyCollection = new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

            HasInitialized = true;
        }


        [JsonIgnore]
        private HomeLoanInformation _homeLoanInfo;
        public HomeLoanInformation HomeLoanInfo
        {
            get
            {
                return _homeLoanInfo;
            }
            set
            {
                _homeLoanInfo = value;
                OnPropertyChanged("HomeLoanInfo");
            }
        }

        #region Insights

        [JsonIgnore]
        public double SavingsMonthly
        {
            get
            {
                return ((IncomeSummary?.TotalMonthly ?? 0) -
                    (
                        (ExpenseSummary?.TotalMonthly ?? 0) +
                        (Expenses?.IncomeExpenseSummary?.TotalMonthly ?? 0) +
                        (HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0)
                    )).Round0();
            }
        }
        [JsonIgnore]
        public string SavingsMonthlyWithComma => SavingsMonthly.WithComma();
        [JsonIgnore]
        public double SavingsYearly => ModelHelper.ConvertAmountToYearlyFrequency(SavingsMonthly, TimeFrequencyEnum.Monthly).Round0();
        [JsonIgnore]
        public string SavingsYearlyWithComma => SavingsYearly.WithComma();

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanAmountAxis
        {
            get
            {
                return new ObservableCollection<ChartDataModel>(new List<ChartDataModel> { new() { Name = "2025", Value = HomeLoanInfo.LoanAmountDirectInput } }.AsEnumerable());
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanInterestAxis
        {
            get
            {
                return new ObservableCollection<ChartDataModel>(new List<ChartDataModel> { new() { Name = "2025", Value = HomeLoanInfo.PaymentSummary.Payment.TotalInterestPayment } }.AsEnumerable());
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanDepositAxis
        {
            get
            {
                return new ObservableCollection<ChartDataModel>(new List<ChartDataModel> { new() { Name = "2025", Value = HomeLoanInfo.DepositAmountDirectInput } }.AsEnumerable());
            }
        }

        [JsonIgnore]
        public InsightsDetailsViewModel InsightsDetails { get; set; }
        private void BuildInsights()
        {
            InsightsDetails = new InsightsDetailsViewModel();

            #region Property
            InsightsDetails.PropertyAmount.Value = $"{CurrencySymbol}{PropertyAmount:N0}";
            InsightsDetails.PropertyEstimatedUpfront.Value = $"{CurrencySymbol}{HomeLoanInfo.OtherExpenseTotalAmount:N0}";
            InsightsDetails.PropertyTotalAmount.Value = $"{CurrencySymbol}{HomeLoanInfo.PropertyTotalAmount:N0}";
            #endregion

            #region Loan
            InsightsDetails.LoanAmount.Value = $"{CurrencySymbol}{HomeLoanInfo.LoanAmountDirectInput:N0}";
            InsightsDetails.DepositAmount.Value = $"{CurrencySymbol}{HomeLoanInfo.DepositAmountDirectInput:N0}";
            InsightsDetails.TotalRepaymentToBank.Value = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalPaymentRounded:N0}";
            InsightsDetails.TotalInterestToBank.Value = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalInterestPaymentRoundedWithComma:N0}";
            InsightsDetails.LoanTerm.Value = $"{LoanTermInYears} years";
            InsightsDetails.InterestRate.Value = $"{InterestRate}%";
            InsightsDetails.RepaymentDetailSelectedFrequency.Value = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TermPaymentRoundedWithComma} {RepaymentFrequencySelected}";
            InsightsDetails.RepaymentFrequency.Value = RepaymentFrequencySelected.Trim();
            InsightsDetails.RepaymentDetailYearly.Value = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TermPaymentYearlyWithComma} yearly";
            #endregion

            #region Expense Income
            InsightsDetails.ExpenseExistingMonthly.Value = $"{CurrencySymbol}{Math.Round(ExpenseSummary?.TotalMonthly ?? 0, 0):N0}";
            InsightsDetails.ExpenseExistingYearly.Value = $"{CurrencySymbol}{Math.Round(ExpenseSummary?.TotalYearly ?? 0, 0):N0}";
            InsightsDetails.ExpenseThisPropertyMonthly.Value = $"{CurrencySymbol}{TotalMonthlyExpenseWithComma}";
            InsightsDetails.ExpenseThisPropertyYearly.Value = $"{CurrencySymbol}{TotalYearlyExpenseWithComma}";
            InsightsDetails.ExpenseTotalMonthly.Value = $"{CurrencySymbol}{Math.Round((ExpenseSummary?.TotalMonthly ?? 0) + (Expenses?.IncomeExpenseSummary?.TotalMonthly ?? 0), 0)}";
            InsightsDetails.ExpenseTotalYearly.Value = $"{CurrencySymbol}{Math.Round((ExpenseSummary?.TotalYearly ?? 0) + (Expenses?.IncomeExpenseSummary?.TotalYearly ?? 0), 0):N0}";
            InsightsDetails.IncomeTotalMonthly.Value = $"{CurrencySymbol}{Math.Round(IncomeSummary?.TotalMonthly ?? 0, 0):N0}";
            InsightsDetails.IncomeTotalYearly.Value = $"{CurrencySymbol}{Math.Round(IncomeSummary?.TotalYearly ?? 0, 0):N0}";
            InsightsDetails.SavingMonthly.Value = $"{CurrencySymbol}{SavingsMonthlyWithComma}";
            InsightsDetails.SavingYearly.Value = $"{CurrencySymbol}{SavingsYearlyWithComma}";
            #endregion

            OnPropertyChanged("InsightChartLoanDepositAxis");
            OnPropertyChanged("InsightChartLoanAmountAxis");
            OnPropertyChanged("InsightChartLoanInterestAxis");
        }
        #endregion

        #region Loan Primary Values

        public double PropertyAmount
        {
            get => HomeLoanInfo.PropertyAmount;
            set
            {
                if (isUpdating == false)
                {
                    if (HomeLoanInfo.PropertyAmount != value)
                    {
                        isUpdating = true;
                        HomeLoanInfo.PropertyAmount = value;
                        HomeLoanInfo.LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;

                        EventsTriggerStampDutyUpdate();
                        TriggerPropertyChangedOnPropertyTab();
                        isUpdating = false;
                    }
                }
            }
        }
        public int LoanTermInYears
        {
            get => HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears = value;
                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        public double InterestRate
        {
            get => HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate = value;
                    TriggerPropertyChangedOnPropertyTab();
                    EventsTriggerPriceUpdate();

                    isUpdating = false;
                }
            }
        }

        public string LoanAmount
        {
            get => $"{CurrencySymbol}{HomeLoanInfo.LoanAmount:N0}";
        }
        [JsonIgnore]
        public string PropertyTotalAmount => $"{CurrencySymbol}{HomeLoanInfo.PropertyTotalAmount:N0}";

        #endregion

        [JsonIgnore]
        private ObservableCollection<DataModel> _chartPropertyValueWithInterestPayment;
        [JsonIgnore]
        public ObservableCollection<DataModel> ChartPropertyValueWithInterestPayment
        {
            get => _chartPropertyValueWithInterestPayment;
            set
            {
                _chartPropertyValueWithInterestPayment = value;
                OnPropertyChanged("ChartPropertyValueWithInterestPayment");
            }
        }

        #region Repayment Frequency
        [JsonIgnore]
        public ObservableCollection<SfSegmentItem> RepaymentFrequencyCollection { get; set; }
        public int RepaymentFrequencySelectedIndex
        {
            get
            {
                if (HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear == 12)
                {
                    return 0;
                }
                else if (HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear == 24)
                {
                    return 1;
                }
                else if (HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear == 52)
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
                if (isUpdating == false)
                {
                    isUpdating = true;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public string RepaymentFrequencySelected => RepaymentFrequencySelectedIndex == 0 ?
                    " monthly" :
                    RepaymentFrequencySelectedIndex == 1 ?
                    " fortnightly" :
                    RepaymentFrequencySelectedIndex == 2 ?
                    " weekly" : " monthly";

        #endregion

        #region Styles
        [JsonIgnore]
        private StyleModelDefault styleModelDefault;
        [JsonIgnore]
        public StyleModelDefault DefaultStyle
        {
            get => styleModelDefault;
            set
            {
                styleModelDefault = value;
                OnPropertyChanged("DefaultStyle");
            }
        }
        //[JsonIgnore]
        public ObservableCollection<Color> CustomPaletteColors => DefaultStyle?.CustomPaletteColors;

        #endregion

        #region Loan Calculations

        [JsonIgnore]
        public string DepositAmountStrFormatted => $"{CurrencySymbol}{HomeLoanInfo.DepositAmountDirectInput:N0}";
        public double DepositPercentage
        {
            get => HomeLoanInfo.DepositPercentage;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.DepositPercentage = value;
                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        public double DepositAmountDirectInput
        {
            get => HomeLoanInfo.DepositAmountDirectInput;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    HomeLoanInfo.DepositAmountDirectInput = value;
                    TriggerPropertyChangedOnPropertyTab();
                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public string LoanAmountStrFormatted => $"{CurrencySymbol}{HomeLoanInfo.LoanAmountDirectInput:N0}";
        public double LoanAmountPercentage
        {
            get => HomeLoanInfo.LoanAmountPercentage;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.LoanAmountPercentage = value;
                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        public double LoanAmountDirectInput
        {
            get => HomeLoanInfo.LoanAmountDirectInput;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    HomeLoanInfo.LoanAmountDirectInput = value;
                    TriggerPropertyChangedOnPropertyTab();
                    isUpdating = false;
                }
            }
        }


        #endregion

        #region Australian States

        [JsonIgnore]
        public ObservableCollection<SfSegmentItem> AustraliaStateCollection { get; set; }

        public int AustraliaStateSelectedIndex
        {
            get { return HomeLoanInfo.StampDuty.AustraliaStateIndex; }
            set
            {
                if (HomeLoanInfo.StampDuty.AustraliaStateSelected != StampDutyOutput.AustraliaStateFromIndex(value))
                {
                    HomeLoanInfo.StampDuty.AustraliaStateSelected = StampDutyOutput.AustraliaStateFromIndex(value);
                    EventsTriggerStampDutyUpdate();
                    LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;
                }
            }
        }

        #endregion

        #region Other expenses

        [JsonIgnore]
        public double StampDuty
        {
            get => HomeLoanInfo.StampDuty.StampDuty;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.StampDuty.StampDuty = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double MortgageCharges
        {
            get => HomeLoanInfo.StampDuty.MortgageCharges;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.StampDuty.MortgageCharges = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double ConveyancerFee
        {
            get => HomeLoanInfo.ConveyanceExpense.ConveyancerFee;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.ConveyanceExpense.ConveyancerFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double BankFee
        {
            get => HomeLoanInfo.BankExpense.BankSettlementFee;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.BankExpense.BankSettlementFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double InspectionFee
        {
            get => HomeLoanInfo.OtherExpense.InspectionFee;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.OtherExpense.InspectionFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double OtherExpenses
        {
            get => HomeLoanInfo.OtherExpense.OtherExpenses;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;

                    HomeLoanInfo.OtherExpense.OtherExpenses = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public string OtherExpenseTotalAmount => $"{CurrencySymbol}{HomeLoanInfo.OtherExpenseTotalAmount:N0}";

        #endregion

        #region Amortisation
        [JsonIgnore]
        public ObservableCollection<SfSegmentItem> AmortizationBreakdownFrequencyCollection { get; set; }

        private int _amortizationBreakdownFrequencySelectedIndex;
        public int AmortizationBreakdownFrequencySelectedIndex
        {
            get => _amortizationBreakdownFrequencySelectedIndex;
            set
            {
                _amortizationBreakdownFrequencySelectedIndex = value;
                if (isUpdating == false)
                {
                    isUpdating = true;

                    UpdateAmortizationData();
                    TriggerPropertyChangedOnAmortizationTab();

                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public bool IsAmortizationTermBased => AmortizationBreakdownFrequencySelectedIndex != 0;
        [JsonIgnore]
        public bool IsAmortizationYearBased => AmortizationBreakdownFrequencySelectedIndex == 0;

        [JsonIgnore]
        public List<PaymentAmortisationOutput> PaymentAmortization
        {
            get
            {
                return HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms;
            }
        }

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartPrincipalAmountAxis
        {
            get
            {
                if (HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Where(f => f.YearOfPayment != DateTime.Now.Year).Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.PrincipalAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartInterestAmountAxis
        {
            get
            {
                if (HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Where(f => f.YearOfPayment != DateTime.Now.Year).Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.InterestAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartAreaPrincipalAmountAxis
        {
            get
            {
                if (HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    var breakdownCount = RepaymentFrequencySelected.Trim() == "monthly" ? 2 : RepaymentFrequencySelected.Trim() == "fortnightly" ? 4 : RepaymentFrequencySelected.Trim() == "weekly" ? 8 : 2;
                    return new ObservableCollection<ChartDataModel>(
                        HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms
                            .Where(f => f.YearOfPayment != DateTime.Now.Year)
                            .Skip(2)
                            .Where((num, index) => index % breakdownCount == 0)
                            //.Union(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Skip(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Count - 1))
                            .Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.PrincipalAmount })
                        );
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartAreaInterestAmountAxis
        {
            get
            {
                if (HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    var breakdownCount = RepaymentFrequencySelected.Trim() == "monthly" ? 2 : RepaymentFrequencySelected.Trim() == "fortnightly" ? 4 : RepaymentFrequencySelected.Trim() == "weekly" ? 8 : 2;
                    return new ObservableCollection<ChartDataModel>(
                        HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms
                            .Where(f => f.YearOfPayment != DateTime.Now.Year)
                            .Skip(2)
                            .Where((num, index) => index % breakdownCount == 0)
                            //.Union(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Skip(HomeLoanInfo.PaymentSummary.PaymentAmortizationTerms.Count - 1))
                            .Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.InterestAmount })
                        );
                }
            }
        }

        #endregion

        #region Expense
        [JsonIgnore]
        public ObservableCollection<string> IncomeFrequencyCollection { get; set; }

        [JsonIgnore]
        public ObservableCollection<IncomeExpense> ExpensesList => Expenses.IncomeExpenseEntries;

        #region AutoCompleteSearch
        [JsonIgnore]
        public IEnumerable<SearchAutoCompleteViewModel> AutocompleteList
            => ExpensesList.Select(f => new SearchAutoCompleteViewModel { Id = 0, Name = f.Name });
        [JsonIgnore]
        public string SearchExpenseIncomeName { get; set; }

        #endregion

        #region Total Details
        [JsonIgnore]
        public string TotalMonthlyExpenseWithComma => Expenses?.IncomeExpenseSummary?.TotalMonthlyWithComma;
        [JsonIgnore]
        public string TotalYearlyExpenseWithComma => Expenses?.IncomeExpenseSummary?.TotalYearlyWithComma;


        [JsonIgnore]
        private IncomeExpenseSummary _incomeSummary;
        [JsonIgnore]
        public IncomeExpenseSummary IncomeSummary
        {
            get => _incomeSummary;
            set
            {
                _incomeSummary = value;
                OnPropertyChanged("IncomeSummary");
            }
        }
        [JsonIgnore]
        private IncomeExpenseSummary _expenseSummary;
        [JsonIgnore]
        public IncomeExpenseSummary ExpenseSummary
        {
            get => _expenseSummary;
            set
            {
                _expenseSummary = value;
                OnPropertyChanged("ExpenseSummary");
            }
        }
        [JsonIgnore]
        public string TotalMonthlyExistingExpense => $"{Math.Round(ExpenseSummary?.TotalMonthly ?? 0, 0):N0}";
        [JsonIgnore]
        public string TotalMonthlyOverallExpense => $"{Math.Round(ExpenseSummary?.TotalMonthly + Expenses?.IncomeExpenseSummary?.TotalMonthly ?? 0, 0):N0}";

        #endregion

        #region Expense Entry
        public Incomes Expenses { get; set; }

        [JsonIgnore]
        private IncomeExpense _incomeExpenseEntry;
        [JsonIgnore]
        public IncomeExpense IncomeExpenseEntry
        {
            get => _incomeExpenseEntry;
            set
            {
                _incomeExpenseEntry = value;
                OnPropertyChanged(nameof(IncomeExpenseEntry));
            }
        }

        [JsonIgnore]
        public bool HasErrorIncomeDescription
        {
            get => IncomeExpenseEntry == null || IncomeExpenseEntry.Name.IsEmpty();
        }
        [JsonIgnore]
        public string IncomeEntryName
        {
            get => IncomeExpenseEntry?.Name;
            set
            {
                if (value == null || IncomeExpenseEntry == null) return;
                IncomeExpenseEntry.Name = value;
                OnPropertyChanged("IncomeEntryName");
                OnPropertyChanged("HasErrorIncomeDescription");
                OnPropertyChanged("IsExpenseDataFormReadyToSubmit");
            }
        }

        [JsonIgnore]
        public bool HasErrorIncomeAmount
        {
            get => IncomeExpenseEntry == null || IncomeExpenseEntry.Amount <= 0;
        }

        [JsonIgnore]
        public double IncomeEntryAmount
        {
            get => IncomeExpenseEntry?.Amount ?? 0;
            set
            {
                if (IncomeExpenseEntry == null) return;
                IncomeExpenseEntry.Amount = value;
                OnPropertyChanged("IncomeEntryAmount");
                OnPropertyChanged("HasErrorIncomeAmount");
                OnPropertyChanged("IsExpenseDataFormReadyToSubmit");
            }
        }

        private string _IncomeExpenseFrequencySelectedIndex;
        [JsonIgnore]
        public string IncomeExpenseFrequencySelectedIndex
        {
            get => _IncomeExpenseFrequencySelectedIndex;
            set
            {
                if (value == null) return;
                _IncomeExpenseFrequencySelectedIndex = value;
                IncomeExpenseEntry.Frequency = IncomeExpenseHelper.TimeFrequencyFromString(value);
                OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
            }
        }

        [JsonIgnore]
        public bool IsExpenseDataFormReadyToSubmit => HasErrorIncomeDescription == false && HasErrorIncomeAmount == false;

        public bool AddOrUpdateEntryFromView()
        {
            if (IncomeExpenseEntry.Id != Guid.Empty && Expenses.Exists(IncomeExpenseEntry.Id))
            {
                Expenses.Delete(IncomeExpenseEntry.Id);
            }
            else if (Expenses.Exists(IncomeExpenseEntry.Name))
            {
                Expenses.Delete(Expenses.Get(IncomeExpenseEntry.Name).Id);
            }

            Expenses.Add(IncomeExpenseEntry.Name,
                IncomeExpenseEntry.Amount,
                IncomeExpenseEntry.Frequency, isCheckForExistingRequired: false);

            IncomeExpenseEntry.Name = string.Empty;
            IncomeExpenseEntry.Amount = 0;
            IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();

            return true;
        }

        #endregion

        #endregion

        #region Country specific functionality

        [JsonIgnore]
        public bool ShowAustralianStateSelectorOnStampDuty => _sharedServices.AppInformation.IsAustralia;
        [JsonIgnore]
        public int HeightOfGridRowToggledByCountryOnStampDuty => ShowAustralianStateSelectorOnStampDuty ? 59 : 0;

        #endregion

        public void TriggerPropertyChangedOnPropertyTab()
        {
            IsBusy = false;
            EventsTriggerPriceUpdate();
            OnPropertyChanged("PropertyAmount");
            OnPropertyChanged("LoanTermInYears");
            OnPropertyChanged("InterestRate");
            OnPropertyChanged("StampDuty");
            OnPropertyChanged("DepositAmountDirectInput");
            OnPropertyChanged("LoanAmountDirectInput");
            OnPropertyChanged("LoanAmountPercentage");
            OnPropertyChanged("LoanAmountStrFormatted");
            OnPropertyChanged("DepositPercentage");
            OnPropertyChanged("LoanAmount");
            OnPropertyChanged("PropertyTotalAmount");
            OnPropertyChanged("DepositAmountStrFormatted");
            OnPropertyChanged("OtherExpenseTotalAmount");
            OnPropertyChanged("MortgageCharges");
            OnPropertyChanged("ConveyancerFee");
            OnPropertyChanged("BankFee");
            OnPropertyChanged("InspectionFee");
            OnPropertyChanged("OtherExpenses");
            OnPropertyChanged("RepaymentFrequencySelected");
            base.SaveData(this);
            IsBusy = true;
        }

        public void TriggerPropertyChangedOnAmortizationTab()
        {
            IsBusy = false;

            OnPropertyChanged("PaymentAmortization");
            OnPropertyChanged("AmortizationBreakdownFrequencyCollection");
            OnPropertyChanged("AmortizationBreakdownFrequencySelectedIndex");
            OnPropertyChanged("AmortizationChartPrincipalAmountAxis");
            OnPropertyChanged("AmortizationChartAreaPrincipalAmountAxis");
            OnPropertyChanged("AmortizationChartInterestAmountAxis");
            OnPropertyChanged("AmortizationChartAreaInterestAmountAxis");
            OnPropertyChanged("IsAmortizationTermBased");
            OnPropertyChanged("IsAmortizationYearBased");

            base.SaveData(this);
            IsBusy = true;
        }

        public void RefreshExpenseTabPropertyChanged()
        {
            Expenses.SumUpData();
            OnPropertyChanged("IncomeEntryName");
            OnPropertyChanged("HasErrorIncomeDescription");

            OnPropertyChanged("IncomeEntryAmount");
            OnPropertyChanged("HasErrorIncomeAmount");

            OnPropertyChanged("IsExpenseDataFormReadyToSubmit");

            OnPropertyChanged("TotalMonthlyExpenseWithComma");
            OnPropertyChanged("TotalYearlyExpenseWithComma");
            OnPropertyChanged("TotalIncomeMonthlyWithComma");
            OnPropertyChanged("IncomeExpenseFrequencySelectedIndex");

            OnPropertyChanged("ExpensesList");
            OnPropertyChanged("AutocompleteList");
            OnPropertyChanged("ShowIncomeAfterExpense");
            OnPropertyChanged("TotalMonthlyOverallExpense");
            OnPropertyChanged("TotalMonthlyExistingExpense");

            base.SaveData(this);
        }

        public void RefreshInsightsTabPropertyChanged()
        {
            BuildInsights();

            OnPropertyChanged("InsightsCollection");
            OnPropertyChanged("InsightsDetails");
        }

        public void SyncAmortization()
        {
            UpdateAmortizationData();
            UpdateAmortizationFrequencyText();
            TriggerPropertyChangedOnAmortizationTab();
        }

        public void UpdateAmortizationData()
        {
            if (AmortizationBreakdownFrequencySelectedIndex == 0)
            {
                HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByYear(HomeLoanInfo.PaymentSummary);
            }
            else
            {
                HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByTerm(HomeLoanInfo.PaymentSummary);
            }
        }
        public void UpdateAmortizationFrequencyText()
        {
            var frequency = RepaymentFrequencySelected.Trim();
            frequency = char.ToUpper(frequency[0]) + frequency.Substring(1);
            AmortizationBreakdownFrequencyCollection[1].Text = frequency;
        }

        public void EventsTriggerStampDutyUpdate()
        {
            HomeLoanInfo.StampDuty = HomeLoanCalculator.StampDutyCalculator.CalculateStampDutyAustralia(HomeLoanInfo.StampDuty.AustraliaStateSelected, PropertyAmount);
            HomeLoanInfo.StampDuty.AutoUpdateMortgageCharges();
        }
        public void EventsTriggerPriceUpdate()
        {
            HomeLoanInfo.PaymentSummary = HomeLoanCalculator.CalculateHomeLoanPayments(HomeLoanInfo.LoanAmount, HomeLoanInfo.HomeLoanRepaymentRequest);

            LiveEventsUpdate();
        }
        public void EventsTriggerStyleUpdate()
        {
            OnPropertyChanged("DefaultStyle");
            OnPropertyChanged("CustomPaletteColors");
        }
        public void LiveEventsUpdate()
        {
            if (HomeLoanInfo.PropertyTotalAmount > 0 && HomeLoanInfo.PaymentSummary.Payment.TotalPaymentRounded > 0)
            {
                ChartPropertyValueWithInterestPayment = new ObservableCollection<DataModel>
                {
                    new DataModel { Category = "Loan", Value = HomeLoanInfo.LoanAmount, ValueWithComma = $"{CurrencySymbol}{HomeLoanInfo.LoanAmount:N0}" },
                    new DataModel { Category = "Interest", Value = HomeLoanInfo.PaymentSummary.Payment.TotalInterestPayment, ValueWithComma = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalInterestPaymentRoundedWithComma:N0}" },
                    //new DataModel { Category = "Total", Value = HomeLoanInfo.PaymentSummary.Payment.TotalPayment, ValueWithComma = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalPaymentRounded:N2}" },
                };
            }
        }
    }
}
