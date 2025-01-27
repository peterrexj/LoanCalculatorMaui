using Calculator;
using LoanCalculator.Models;
using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculator.Models.Income.Summary;
using Pj.Library;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace LoanCalculatorMaui.ViewModel;

public class IncomeViewModel : ExpenseEntryViewBaseModel
{
    public void TriggerOneTimeUpdateOnPage()
    {
        IsBusy = true;
        OnPropertyChanged(nameof(CustomChartColors));
        IsBusy = false;
    }

    public void InitializeViewData()
    {
        CurrencySymbol = Helper.CurrencySymbol;

        IncomeFrequencyCollection =
            new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

        IncomeExpenseEntry = new IncomeExpense();
    }

    public void AddDefaultToExpenses()
    {
        TransactionRecords ??= new Incomes
        {
            IncomeExpenseEntries = []
        };
    }


    [JsonIgnore]
    public List<IncomeExpenseProjectionOutput> IncomeProjectList =>
        TransactionRecords.IncomeExpenseSummary.ProjectionTerms;

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

    [JsonIgnore] private IncomeExpenseSummary _expenseSummary;

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

    [JsonIgnore] private double TotalMonthlyExpense => ShowIncomeAfterExpense ? ExpenseSummary?.TotalMonthly ?? 0 : 0;
    [JsonIgnore] private double TotalYearlyExpense => ShowIncomeAfterExpense ? ExpenseSummary?.TotalYearly ?? 0 : 0;

    [JsonIgnore]
    public string StringMonthlyTextOnTopBox =>
        ShowIncomeAfterExpense ? "Monthly Income (after expense)" : "Monthly Income";

    [JsonIgnore]
    public string StringYearlyTextOnTopBox =>
        ShowIncomeAfterExpense ? "Yearly Income (after expense)" : "Yearly Income";

    [JsonIgnore]
    public string StringChartTitleText => ShowIncomeAfterExpense
        ? "Income Growth Projection (after expense)"
        : "Income Growth Projection";

    [JsonIgnore] public string StringProjectionInfoText => ShowIncomeAfterExpense ? " after expense" : "";

    #endregion

    #region Total Details

    [JsonIgnore]
    public string TotalMonthlyIncomeWithComma => TransactionRecords.IncomeExpenseSummary.TotalMonthlyWithComma;

    [JsonIgnore]
    public string TotalYearlyIncomeWithComma => TransactionRecords.IncomeExpenseSummary.TotalYearlyWithComma;

    [JsonIgnore]
    public string TotalProjectedYearlyIncomeWithComma =>
        TransactionRecords.IncomeExpenseSummary.ProjectTotalYearlyWithComma;

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
                return new ObservableCollection<ChartDataModel>(
                    TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel
                        { Name = f.YearOfPayment.ToString(), Value = f.IncomeExpenseAmount }));
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
                return new ObservableCollection<ChartDataModel>(
                    TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel
                        { Name = f.YearOfPayment.ToString(), Value = f.TermStartAmount }));
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
                return new ObservableCollection<ChartDataModel>(
                    TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel
                        { Name = f.YearOfPayment.ToString(), Value = f.TermGrowthAmount }));
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
                return new ObservableCollection<ChartDataModel>(
                    TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f => new ChartDataModel
                        { Name = f.YearOfPayment.ToString(), Value = f.AccumulatedAmount }));
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
        TransactionRecords?.SumUpData(TotalMonthlyExpense, TotalYearlyExpense);

        OnPropertyChanged(nameof(IncomeExpenseEntry));
        OnPropertyChanged(nameof(IncomeEntryName));
        OnPropertyChanged(nameof(HasErrorIncomeAmount));
        OnPropertyChanged(nameof(HasErrorIncomeDescription));
        OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
        OnPropertyChanged(nameof(IncomeEntryAmount));
        OnPropertyChanged(nameof(HasErrorIncomeAmount));
        OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
        OnPropertyChanged(nameof(TotalMonthlyIncomeWithComma));
        OnPropertyChanged(nameof(TotalYearlyIncomeWithComma));
        OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
        OnPropertyChanged(nameof(Transactions));
        OnPropertyChanged(nameof(ShowIncomeAfterExpense));
        OnPropertyChanged(nameof(StringMonthlyTextOnTopBox));
        OnPropertyChanged(nameof(StringYearlyTextOnTopBox));
        OnPropertyChanged(nameof(StringChartTitleText));
        OnPropertyChanged(nameof(StringProjectionInfoText));

        base.SaveData(this);
    }

    public void UpdateProjectionData()
    {
        TransactionRecords.SumUpData();
        HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear(TransactionRecords.IncomeExpenseSummary,
            ShowIncomeAfterExpense ? ExpenseSummary : null);
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
        OnPropertyChanged(nameof(StringChartTitleText));

        base.SaveData(this);
    }

    #endregion
}
