using Calculator;
using LoanCalculator.Models;
using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculator.Models.Income.Summary;
using Newtonsoft.Json;
using Pj.Library;
using System.Collections.ObjectModel;

namespace LoanCalculatorMaui.ViewModel
{
    public class IncomeViewModel : ViewModelUiBase
    {
        public IncomeViewModel()
        {
            CurrencySymbol = Helper.CurrencySymbol;

            IncomeFrequencyCollection = new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

            IncomeExpenseEntry = new IncomeExpense();
        }

        public Incomes Incomes { get; set; }

        [JsonIgnore]
        public ObservableCollection<string> IncomeFrequencyCollection { get; set; }

        [JsonIgnore]
        public ObservableCollection<IncomeExpense> ExpensesList => Incomes.IncomeExpenseEntries;

        [JsonIgnore]
        public List<IncomeExpenseProjectionOutput> IncomeProjectList => Incomes.IncomeExpenseSummary.ProjectionTerms;

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
        [JsonIgnore]
        private double TotalMonthlyExpense => ShowIncomeAfterExpense ? ExpenseSummary?.TotalMonthly ?? 0 : 0;
        [JsonIgnore]
        private double TotalYearlyExpense => ShowIncomeAfterExpense ? ExpenseSummary?.TotalYearly ?? 0 : 0;
        [JsonIgnore]
        public string StringMonthlyTextOnTopBox => ShowIncomeAfterExpense ? "Monthly Income (after expense)" : "Monthly Income";
        [JsonIgnore]
        public string StringYearlyTextOnTopBox => ShowIncomeAfterExpense ? "Yearly Income (after expense)" : "Yearly Income";
        [JsonIgnore]
        public string StringChartTitleText => ShowIncomeAfterExpense ? "Income Growth Projection (after expense)" : "Income Growth Projection";
        [JsonIgnore]
        public string StringProjectionInfoText => ShowIncomeAfterExpense ? " after expense" : "";

        #endregion

        #region Income Entry

        [JsonIgnore]
        private IncomeExpense _incomeExpenseEntry;
        [JsonIgnore]
        public IncomeExpense IncomeExpenseEntry
        {
            get => _incomeExpenseEntry;
            set
            {
                _incomeExpenseEntry = value;
                OnPropertyChanged("IncomeExpenseEntry");
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
            if (IncomeExpenseEntry.Id != Guid.Empty && Incomes.Exists(IncomeExpenseEntry.Id))
            {
                Incomes.Delete(IncomeExpenseEntry.Id);
            }
            else if (Incomes.Exists(IncomeExpenseEntry.Name))
            {
                Incomes.Delete(Incomes.Get(IncomeExpenseEntry.Name).Id);
            }

            Incomes.Add(IncomeExpenseEntry.Name,
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
        public string TotalMonthlyIncomeWithComma => Incomes.IncomeExpenseSummary.TotalMonthlyWithComma;
        [JsonIgnore]
        public string TotalYearlyIncomeWithComma => Incomes.IncomeExpenseSummary.TotalYearlyWithComma;
        [JsonIgnore]
        public string TotalProjectedYearlyIncomeWithComma => Incomes.IncomeExpenseSummary.ProjectTotalYearlyWithComma;

        #endregion

        #region Charts
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionAmountAxis
        {
            get
            {
                if (Incomes.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(Incomes.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.IncomeExpenseAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionTermStartAxis
        {
            get
            {
                if (Incomes.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(Incomes.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.TermStartAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionGrowthAmountAxis
        {
            get
            {
                if (Incomes.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(Incomes.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.TermGrowthAmount }));
                }
            }
        }
        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartIncomeExpenseProjectionAccumulatedAmountAxis
        {
            get
            {
                if (Incomes.IncomeExpenseSummary.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(Incomes.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel { Name = f.YearOfPayment.ToString(), Value = f.AccumulatedAmount }));
                }
            }
        }
        #endregion

        #region Projection Details
        [JsonIgnore]
        public int TotalYearsToProject
        {
            get => Incomes.IncomeExpenseSummary.NumberOfYearsProjection;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    Incomes.IncomeExpenseSummary.NumberOfYearsProjection = value;
                    UpdateProjectionData();
                    TriggerPropertyChangedOnProjectionTab();
                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double AnnualGrowthRate
        {
            get => Incomes.IncomeExpenseSummary.AnnualGrowthRate;
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    Incomes.IncomeExpenseSummary.AnnualGrowthRate = value;
                    UpdateProjectionData();
                    TriggerPropertyChangedOnProjectionTab();
                    isUpdating = false;
                }
            }
        }
        [JsonIgnore]
        public double AnnualGrowthRatePercentage => Incomes.IncomeExpenseSummary.AnnualGrowthRatePercentage;
        #endregion

        #region Live Updates
        public void EventsTriggerStyleUpdate()
        {
            OnPropertyChanged("DefaultStyle");
            OnPropertyChanged("CustomPaletteColors");
        }
        public void RefreshIncomePropertyChanged()
        {
            Incomes?.SumUpData(TotalMonthlyExpense, TotalYearlyExpense);

            OnPropertyChanged("IncomeExpenseEntry");
            OnPropertyChanged("IncomeEntryName");
            OnPropertyChanged("HasErrorIncomeAmount");
            OnPropertyChanged("HasErrorIncomeDescription");
            OnPropertyChanged("IsExpenseDataFormReadyToSubmit");

            OnPropertyChanged("IncomeEntryAmount");
            OnPropertyChanged("HasErrorIncomeAmount");

            OnPropertyChanged("IsExpenseDataFormReadyToSubmit");

            OnPropertyChanged("TotalMonthlyIncomeWithComma");
            OnPropertyChanged("TotalYearlyIncomeWithComma");
            OnPropertyChanged("IncomeExpenseFrequencySelectedIndex");

            OnPropertyChanged("ExpensesList");
            OnPropertyChanged("ShowIncomeAfterExpense");
            OnPropertyChanged("StringMonthlyTextOnTopBox");
            OnPropertyChanged("StringYearlyTextOnTopBox");
            OnPropertyChanged("StringChartTitleText");
            OnPropertyChanged("StringProjectionInfoText");

            base.SaveData(this);
        }
        public void UpdateProjectionData()
        {
            Incomes.SumUpData();
            HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear(Incomes.IncomeExpenseSummary, ShowIncomeAfterExpense ? ExpenseSummary : null);
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
            OnPropertyChanged("StringChartTitleText");

            base.SaveData(this);
        }
        #endregion
    }
}
