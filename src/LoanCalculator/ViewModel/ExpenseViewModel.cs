using AndroidX.Lifecycle;
using Calculator;
using LoanCalculator.Models;
using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculator.Models.Income.Summary;
using Pj.Library;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using LoanCalculatorMaui.Services;

namespace LoanCalculatorMaui.ViewModel
{
    public class ExpenseViewModel : ExpenseEntryViewBaseModel
    {
        public void InitializeViewData()
        {
            CurrencySymbol = Helper.CurrencySymbol;

            IncomeFrequencyCollection = new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

            IncomeExpenseEntry = new IncomeExpense();
        }
        public void AddDefaultToExpenses()
        {
            TransactionRecords ??= new Incomes
            {
                IncomeExpenseEntries = []
            };
            TransactionRecords?.Add("Food and Groceries", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Utility bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Ongoing maintenance", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Residence expenses", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Household furnishings", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Household goods", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Communication", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Clothing", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Personal Care", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Education", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Transport", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Vehicle maintenance", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Medical", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Fitness", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Insurance", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Recreation", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Travel", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Entertainment", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Children", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Pets", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Parents or Dependents", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            TransactionRecords?.Add("Holiday Travel", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
        }
        public void TriggerOneTimeUpdateOnPage()
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CustomChartColors));
            IsBusy = true;
        }


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
                OnPropertyChanged(nameof(DefaultStyle));
            }
        }

        #endregion

        [JsonIgnore]
        public List<IncomeExpenseProjectionOutput> IncomeProjectList => TransactionRecords.IncomeExpenseSummary.ProjectionTerms;

        #region Income after Expense
        private bool _showIncomeAfterExpense;
        public bool ShowIncomeAfterExpense
        {
            get => _showIncomeAfterExpense;
            set
            {
                _showIncomeAfterExpense = value;

                if (isUpdating == false)
                {
                    isUpdating = true;
                    RefreshIncomePropertyChanged();
                    isUpdating = false;
                }

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
                OnPropertyChanged(nameof(ExpenseSummary));
            }
        }
        public string StringIncomeTextOnTopBox => ShowIncomeAfterExpense ? "Monthly Income (after expense)" : "Monthly Income";

        #endregion

        #region Total Details
        [JsonIgnore]
        public string TotalMonthlyIncomeWithComma => TransactionRecords.IncomeExpenseSummary.TotalMonthlyWithComma;
        [JsonIgnore]
        public string TotalYearlyIncomeWithComma => TransactionRecords.IncomeExpenseSummary.TotalYearlyWithComma;
        [JsonIgnore]
        public string TotalProjectedYearlyIncomeWithComma => TransactionRecords.IncomeExpenseSummary.ProjectTotalYearlyWithComma;
        [JsonIgnore]
        public string TotalIncomeMonthlyWithComma => 
            $"{Math.Round(ShowIncomeAfterExpense ? (ExpenseSummary?.TotalMonthly ?? 0) - (TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0) : (ExpenseSummary?.TotalMonthly ?? 0), 0):N0}";
        #endregion

        #region Charts
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionAmountAxis
        {
            get
            {
                if (TransactionRecords.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.IncomeExpenseAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionTermStartAxis
        {
            get
            {
                if (TransactionRecords.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.TermStartAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionGrowthAmountAxis
        {
            get
            {
                if (TransactionRecords.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.TermGrowthAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionAccumulatedAmountAxis
        {
            get
            {
                if (TransactionRecords.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.AccumulatedAmount }));
                }
            }
        }
        #endregion

        #region Projection Details
        [JsonIgnore]
        public int TotalYearsToProject
        {
            get => TransactionRecords.IncomeExpenseSummary.NumberOfYearsProjection;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    TransactionRecords.IncomeExpenseSummary.NumberOfYearsProjection = value;
                    UpdateProjectionData();
                    TriggerPropertyChangedOnProjectionTab();
                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double AnnualGrowthRate
        {
            get => TransactionRecords.IncomeExpenseSummary.AnnualGrowthRate;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    TransactionRecords.IncomeExpenseSummary.AnnualGrowthRate = value;
                    UpdateProjectionData();
                    TriggerPropertyChangedOnProjectionTab();
                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double AnnualGrowthRatePercentage => TransactionRecords.IncomeExpenseSummary.AnnualGrowthRatePercentage;
        #endregion

        #region Live Updates
        
        public void RefreshIncomePropertyChanged()
        {
            TransactionRecords?.SumUpData();
            OnPropertyChanged(nameof(IncomeEntryName));
            OnPropertyChanged(nameof(HasErrorIncomeDescription));
            OnPropertyChanged(nameof(IncomeEntryAmount));
            OnPropertyChanged(nameof(HasErrorIncomeAmount));
            OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            OnPropertyChanged(nameof(TotalMonthlyIncomeWithComma));
            OnPropertyChanged(nameof(TotalYearlyIncomeWithComma));
            OnPropertyChanged(nameof(TotalIncomeMonthlyWithComma));
            OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
            OnPropertyChanged(nameof(Transactions));
            OnPropertyChanged(nameof(ShowIncomeAfterExpense));
            OnPropertyChanged(nameof(StringIncomeTextOnTopBox));

            base.SaveData(this);
        }
        public void UpdateProjectionData()
        {
            TransactionRecords.SumUpData();
            HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear(TransactionRecords.IncomeExpenseSummary);
        }
        public void TriggerPropertyChangedOnProjectionTab()
        {
            OnPropertyChanged(nameof(ChartIncomeExpenseProjectionAmountAxis));
            OnPropertyChanged(nameof(ChartIncomeExpenseProjectionTermStartAxis));
            OnPropertyChanged(nameof(ChartIncomeExpenseProjectionGrowthAmountAxis));
            OnPropertyChanged(nameof(ChartIncomeExpenseProjectionAccumulatedAmountAxis));
            OnPropertyChanged(nameof(TotalYearsToProject));
            OnPropertyChanged(nameof(TotalProjectedYearlyIncomeWithComma));
            OnPropertyChanged(nameof(AnnualGrowthRatePercentage));
            OnPropertyChanged(nameof(AnnualGrowthRate));
            OnPropertyChanged(nameof(IncomeProjectList));

            base.SaveData(this);
        }
        #endregion
    }
}
