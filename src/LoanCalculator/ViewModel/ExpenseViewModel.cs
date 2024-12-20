using System.Collections.ObjectModel;
using Calculator;
using LoanCalculator.Models;
using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculator.Models.Income.Summary;
using LoanCalculatorMaui.Services;
using Newtonsoft.Json;
using Pj.Library;

namespace LoanCalculatorMaui.ViewModel
{
    public class ExpenseViewModel : ViewModelUiBase
    {
        public ExpenseViewModel(ISharedServices sharedServices) : base(sharedServices)
        {
            CurrencySymbol = Helper.CurrencySymbol;

            IncomeFrequencyCollection = new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

            IncomeExpenseEntry = new IncomeExpense();
        }

        public ExpenseViewModel() : base()
        {
            CurrencySymbol = Helper.CurrencySymbol;

            IncomeFrequencyCollection = new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

            IncomeExpenseEntry = new IncomeExpense();
        }
       

        public Incomes Expenses { get; set; }

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
        [JsonIgnore]
        public ObservableCollection<Color> CustomPaletteColors => DefaultStyle?.CustomPaletteColors;

        #endregion

        [JsonIgnore]
        public ObservableCollection<string> IncomeFrequencyCollection { get; set; }

        [JsonIgnore]
        public ObservableCollection<IncomeExpense> ExpensesList => Expenses.IncomeExpenseEntries;

        [JsonIgnore]
        public List<IncomeExpenseProjectionOutput> IncomeProjectList => Expenses.IncomeExpenseSummary.ProjectionTerms;

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
                OnPropertyChanged("ExpenseSummary");
            }
        }
        public string StringIncomeTextOnTopBox => ShowIncomeAfterExpense ? "Monthly Income (after expense)" : "Monthly Income";

        #endregion

        #region Expense Entry
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

        [JsonIgnore]
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

        #region AutoCompleteSearch
        [JsonIgnore]
        public IEnumerable<SearchAutoCompleteViewModel> AutocompleteList
            => ExpensesList.Select(f => new SearchAutoCompleteViewModel { Id = 0, Name = f.Name });
        [JsonIgnore]
        public string SearchExpenseIncomeName { get; set; }

        #endregion

        #endregion

        #region Total Details
        [JsonIgnore]
        public string TotalMonthlyIncomeWithComma => Expenses.IncomeExpenseSummary.TotalMonthlyWithComma;
        [JsonIgnore]
        public string TotalYearlyIncomeWithComma => Expenses.IncomeExpenseSummary.TotalYearlyWithComma;
        [JsonIgnore]
        public string TotalProjectedYearlyIncomeWithComma => Expenses.IncomeExpenseSummary.ProjectTotalYearlyWithComma;
        [JsonIgnore]
        public string TotalIncomeMonthlyWithComma => 
            $"{Math.Round(ShowIncomeAfterExpense ? (ExpenseSummary?.TotalMonthly ?? 0) - (Expenses?.IncomeExpenseSummary?.TotalMonthly ?? 0) : (ExpenseSummary?.TotalMonthly ?? 0), 0):N0}";
        #endregion

        #region Charts
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionAmountAxis
        {
            get
            {
                if (Expenses.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(Expenses.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.IncomeExpenseAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionTermStartAxis
        {
            get
            {
                if (Expenses.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(Expenses.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.TermStartAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionGrowthAmountAxis
        {
            get
            {
                if (Expenses.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(Expenses.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.TermGrowthAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionAccumulatedAmountAxis
        {
            get
            {
                if (Expenses.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(Expenses.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.AccumulatedAmount }));
                }
            }
        }
        #endregion

        #region Projection Details
        [JsonIgnore]
        public int TotalYearsToProject
        {
            get => Expenses.IncomeExpenseSummary.NumberOfYearsProjection;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    Expenses.IncomeExpenseSummary.NumberOfYearsProjection = value;
                    UpdateProjectionData();
                    TriggerPropertyChangedOnProjectionTab();
                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double AnnualGrowthRate
        {
            get => Expenses.IncomeExpenseSummary.AnnualGrowthRate;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    Expenses.IncomeExpenseSummary.AnnualGrowthRate = value;
                    UpdateProjectionData();
                    TriggerPropertyChangedOnProjectionTab();
                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double AnnualGrowthRatePercentage => Expenses.IncomeExpenseSummary.AnnualGrowthRatePercentage;
        #endregion

        #region Live Updates
        public void EventsTriggerStyleUpdate()
        {
            OnPropertyChanged("DefaultStyle");
            OnPropertyChanged("CustomPaletteColors");
        }
        public void RefreshIncomePropertyChanged()
        {
            Expenses?.SumUpData();
            OnPropertyChanged("IncomeEntryName");
            OnPropertyChanged("HasErrorIncomeDescription");

            OnPropertyChanged("IncomeEntryAmount");
            OnPropertyChanged("HasErrorIncomeAmount");

            OnPropertyChanged("IsExpenseDataFormReadyToSubmit");

            OnPropertyChanged("TotalMonthlyIncomeWithComma");
            OnPropertyChanged("TotalYearlyIncomeWithComma");
            OnPropertyChanged("TotalIncomeMonthlyWithComma");
            OnPropertyChanged("IncomeExpenseFrequencySelectedIndex");

            OnPropertyChanged("ExpensesList");
            OnPropertyChanged("ShowIncomeAfterExpense");
            OnPropertyChanged("StringIncomeTextOnTopBox");

            base.SaveData(this);
        }
        public void UpdateProjectionData()
        {
            Expenses.SumUpData();
            HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear(Expenses.IncomeExpenseSummary);
        }
        public void TriggerPropertyChangedOnProjectionTab()
        {
            OnPropertyChanged("ChartIncomeExpenseProjectionAmountAxis");
            OnPropertyChanged("ChartIncomeExpenseProjectionTermStartAxis");
            OnPropertyChanged("ChartIncomeExpenseProjectionGrowthAmountAxis");
            OnPropertyChanged("ChartIncomeExpenseProjectionAccumulatedAmountAxis");

            OnPropertyChanged("TotalYearsToProject");
            OnPropertyChanged("TotalProjectedYearlyIncomeWithComma");
            OnPropertyChanged("AnnualGrowthRatePercentage");
            OnPropertyChanged("AnnualGrowthRate");
            OnPropertyChanged("IncomeProjectList");

            base.SaveData(this);
        }
        #endregion
    }
}
