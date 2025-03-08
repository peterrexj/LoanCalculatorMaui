using LoanCalculator.Core;
using LoanCalculator.Models;
using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculator.Models.Income.Summary;
using LoanCalculatorMaui.Services;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using LoanCalculatorMaui.Extensions;

namespace LoanCalculatorMaui.ViewModel;

public class ExpenseViewModel(IErrorHandlingService errorHandlingService, IAlertService alertService)
    : ExpenseEntryViewBaseModel
{
    [JsonIgnore]
    private readonly IErrorHandlingService _errorHandlingService = errorHandlingService;
    [JsonIgnore]
    private readonly IAlertService _alertService = alertService;

    public ExpenseViewModel() : this(ServiceLocator.GetService<IErrorHandlingService>(), ServiceLocator.GetService<IAlertService>())
    {
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
        TransactionRecords?.Add("Food and Groceries", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
        TransactionRecords?.Add("Utility bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
        TransactionRecords?.Add("Ongoing maintenance", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
        TransactionRecords?.Add("Residence expenses", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
        TransactionRecords?.Add("Household furnishings", 0, TimeFrequencyEnum.Monthly,
            isCheckForExistingRequired: false);
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
        TransactionRecords?.Add("Parents or Dependents", 0, TimeFrequencyEnum.Monthly,
            isCheckForExistingRequired: false);
        TransactionRecords?.Add("Holiday Travel", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
    }

    public void TriggerOneTimeUpdateOnPage()
    {
        IsBusy = true;
        OnPropertyChanged(nameof(CustomChartColors));
        IsBusy = false;
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

            if (isUpdating == false && PageHelper.IsFormLoading == false)
            {
                isUpdating = true;
                OnPropertyChanged(nameof(ShowIncomeAfterExpense)); //TODO: required to call this as the refreshincomeproperty is called anyways
                RefreshIncomePropertyChangedAsync();
                isUpdating = false;
            }
        }
    }

    private bool _showPropertyExpense;
    public bool ShowPropertyExpense
    {
        get => _showPropertyExpense;
        set
        {
            _showPropertyExpense = value;

            if (isUpdating == false && PageHelper.IsFormLoading == false)
            {
                isUpdating = true;
                OnPropertyChanged(nameof(ShowPropertyExpense));
                RefreshIncomePropertyChangedAsync();
                isUpdating = false;
            }
        }
    }

    private bool _includeIncomeInProjection;
    public bool IncludeIncomeInProjection
    {
        get => _includeIncomeInProjection;
        set
        {
            _includeIncomeInProjection = value;

            if (isUpdating == false && PageHelper.IsFormLoading == false)
            {
                isUpdating = true;
                OnPropertyChanged(nameof(IncludeIncomeInProjection));
                UpdateProjectionDataAsync();
                isUpdating = false;
            }
        }
    }

    private bool _includePropertyExpensesInProjection;
    public bool IncludePropertyExpenses
    {
        get => _includePropertyExpensesInProjection;
        set
        {
            _includePropertyExpensesInProjection = value;

            if (isUpdating == false && PageHelper.IsFormLoading == false)
            {
                isUpdating = true;
                OnPropertyChanged(nameof(IncludePropertyExpenses));
                UpdateProjectionDataAsync();
                isUpdating = false;
            }
        }
    }

    [JsonIgnore] private IncomeExpenseSummary? _incomeSummary;

    [JsonIgnore]
    public IncomeExpenseSummary? IncomeSummary
    {
        get => _incomeSummary;
        set
        {
            _incomeSummary = value;
            OnPropertyChanged(nameof(IncomeSummary));
        }
    }

    [JsonIgnore] private IncomeExpenseSummary? _propertyExpenseSummary;

    [JsonIgnore]
    public IncomeExpenseSummary? PropertyExpenseSummary
    {
        get => _propertyExpenseSummary;
        set
        {
            _propertyExpenseSummary = value;
            OnPropertyChanged(nameof(PropertyExpenseSummary));
        }
    }
    [JsonIgnore] private PaymentOutput? _propertyPayment;

    [JsonIgnore]
    public PaymentOutput? PropertyPayment
    {
        get => _propertyPayment;
        set
        {
            _propertyPayment = value;
            OnPropertyChanged(nameof(PropertyPayment));
        }
    }

    //[JsonIgnore]
    //public bool IsExpenseBreakdownVisible => ShowPropertyExpense ? true : false;
    //[JsonIgnore]
    //public int FontOnExpenseToxBox => ShowPropertyExpense ? 14 : 18;

    [JsonIgnore]
    public string StringMonthlyExpenseOnTopBox => ShowPropertyExpense ? "Monthly and Property loan & expense" : "Monthly expenses";

    [JsonIgnore]
    public string TotalMonthlySumExpenseWithComma
    {
        get
        {
            double expenses = TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;

            if (ShowPropertyExpense)
            {
                expenses += PropertyExpenseSummary?.TotalMonthly ?? 0;
                expenses += PropertyPayment?.TermPaymentMonthly ?? 0;
            }

            return $"{Math.Round(expenses, 0):N0}";
        }
    }
    [JsonIgnore]
    public string TotalMonthlyExpenseBreakdownWithComma
    {
        get
        {
            if (ShowPropertyExpense)
            {
                var expenses = System.Environment.NewLine;

                expenses += $"(${Math.Round(TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0, 0):N0}";

                expenses += $" + ${Math.Round(PropertyExpenseSummary?.TotalMonthly ?? 0, 0):N0}";
                expenses += $" + ${Math.Round(PropertyPayment?.TermPaymentMonthly ?? 0, 0):N0})";

                return expenses;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    [JsonIgnore]
    public double TotalMonthlyExpense
    {
        get
        {
            double expenses = 0;
            expenses = TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;

            if (ShowPropertyExpense)
            {
                expenses += PropertyExpenseSummary?.TotalMonthly ?? 0;
                expenses += PropertyPayment?.TermPaymentMonthly ?? 0;
            }

            return expenses;
        }
    }

    [JsonIgnore]
    public double TotalYearlyExpense
    {
        get
        {
            double expenses = 0;
            expenses = TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;

            if (ShowPropertyExpense)
            {
                expenses += PropertyExpenseSummary?.TotalYearly ?? 0;
                expenses += PropertyPayment?.TermPaymentYearly ?? 0;
            }

            return expenses;
        }
    }

    [JsonIgnore]
    public string StringIncomeTextOnTopBox => ShowIncomeAfterExpense ? "Monthly Income (after expenses)" : "Monthly Income";

    #endregion

    #region Total Details

    [JsonIgnore]
    public string TotalMonthlyIncomeWithComma => TransactionRecords.IncomeExpenseSummary.TotalMonthlyWithComma;

    [JsonIgnore]
    public string TotalYearlyIncomeWithComma => $"{TotalYearlyExpense:N0}";

    [JsonIgnore]
    public string TotalProjectedYearlyIncomeWithComma => TransactionRecords.IncomeExpenseSummary.ProjectTotalYearlyWithComma;

    [JsonIgnore]
    public string TotalIncomeMonthlyWithComma
    {
        get
        {
            double income = IncomeSummary?.TotalMonthly ?? 0;

            if (ShowIncomeAfterExpense)
            {
                income -= (TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0);

                if (ShowPropertyExpense)
                {
                    income -= PropertyExpenseSummary?.TotalMonthly ?? 0;
                    income -= PropertyPayment?.TermPaymentMonthly ?? 0;
                }
            }

            return $"{Math.Round(income, 0):N0}";
        }
    }


    #endregion

    #region Charts

    [JsonIgnore]
    public ObservableCollection<ChartDataModel> ChartProjectionTermStartAmountAxis
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
    public ObservableCollection<ChartDataModel> ChartProjectionIncomeExpenseAmountAxis
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
    public ObservableCollection<ChartDataModel> ChartProjectionDeductionAmountAxis
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
                    { Name = f.YearOfPayment.ToString(), Value = f.TermAdjustments }));
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

    private async void UpdateProjectionDataAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                UpdateProjectionData();
                TriggerPropertyChangedOnProjectionTab();
            });
        }
        catch (Exception e)
        {
            _errorHandlingService.HandleException(e);
        }
    }
    private async void RefreshIncomePropertyChangedAsync()
    {
        try
        {
            await Task.Run(RefreshIncomePropertyChanged);
        }
        catch (Exception e)
        {
            _errorHandlingService.HandleException(e);
        }
    }

    public void RefreshIncomePropertyChanged()
    {
        TransactionRecords?.SumUpData();

        OnPropertyChanged(nameof(StringMonthlyExpenseOnTopBox));
        OnPropertyChanged(nameof(TotalMonthlyExpenseBreakdownWithComma));
        OnPropertyChanged(nameof(TotalMonthlyExpense));
        OnPropertyChanged(nameof(TotalYearlyExpense));
        OnPropertyChanged(nameof(IncomeEntryName));
        OnPropertyChanged(nameof(HasErrorIncomeDescription));
        OnPropertyChanged(nameof(IncomeEntryAmount));
        OnPropertyChanged(nameof(HasErrorIncomeAmount));
        OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
        OnPropertyChanged(nameof(TotalMonthlyIncomeWithComma));
        OnPropertyChanged(nameof(TotalMonthlySumExpenseWithComma));
        OnPropertyChanged(nameof(TotalYearlyIncomeWithComma));
        OnPropertyChanged(nameof(TotalIncomeMonthlyWithComma));
        OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
        OnPropertyChanged(nameof(Transactions));
        OnPropertyChanged(nameof(ShowIncomeAfterExpense));
        OnPropertyChanged(nameof(StringIncomeTextOnTopBox));

        SharedServices.SaveData(this);
    }

    public void RefreshTransactionEntry()
    {
        OnPropertyChanged(nameof(IncomeEntryName));
        OnPropertyChanged(nameof(HasErrorIncomeDescription));
        OnPropertyChanged(nameof(IncomeEntryAmount));
        OnPropertyChanged(nameof(HasErrorIncomeAmount));
        OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
    }

    public void UpdateProjectionData()
    {
        double propertyExpenses = 0;

        if (IncludePropertyExpenses)
        {
            if (PropertyExpenseSummary != null)
            {
                propertyExpenses += PropertyExpenseSummary?.TotalYearly ?? 0;
            }
            if (PropertyPayment != null)
            {
                propertyExpenses += PropertyPayment?.TermPaymentYearly ?? 0;
            }
        }

        TransactionRecords.SumUpData();
        HomeLoanCalculator.UpdateExpenseProjectionDataByYear(TransactionRecords.IncomeExpenseSummary,
            additionalExpensesFromNewProperty: propertyExpenses
            );
    }

    public void TriggerPropertyChangedOnProjectionTab()
    {
        OnPropertyChanged(nameof(ChartProjectionTermStartAmountAxis));
        OnPropertyChanged(nameof(ChartProjectionIncomeExpenseAmountAxis));
        OnPropertyChanged(nameof(ChartProjectionDeductionAmountAxis));
        OnPropertyChanged(nameof(TotalYearsToProject));
        OnPropertyChanged(nameof(TotalProjectedYearlyIncomeWithComma));
        OnPropertyChanged(nameof(AnnualGrowthRatePercentage));
        OnPropertyChanged(nameof(AnnualGrowthRate));
        OnPropertyChanged(nameof(IncomeProjectList));

        SharedServices.SaveData(this);
    }

    #endregion
}
