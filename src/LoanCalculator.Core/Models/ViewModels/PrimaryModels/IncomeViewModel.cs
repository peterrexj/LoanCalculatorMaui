using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Income.Summary;
using LoanCalculator.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class IncomeViewModel : ExpenseEntryViewBaseModel
    {
        [JsonIgnore] private readonly IErrorHandlingService _errorHandlingService;
        [JsonIgnore] private readonly IAlertService _alertService;

        public IncomeViewModel()
        {
            
        }

        public IncomeViewModel(IErrorHandlingService errorHandlingService, IAlertService alertService)
        {
            _errorHandlingService = errorHandlingService;
            _alertService = alertService;
        }

        public void CopyPropertiesFrom(IncomeViewModel source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Get all properties of the IncomeViewModel
            var properties = typeof(IncomeViewModel).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

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


        public void InitializeViewData()
        {
            CurrencySymbol = Helper.CurrencySymbol;

            IncomeFrequencyCollection =
                new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

            IncomeExpenseEntry = new IncomeExpense();
        }

        public void AddDefaultToExpenses()
        {
            TransactionRecords = new Incomes
            {
                IncomeExpenseEntries = []
            };
        }

        // Wizard — existing-value indicators for the Quick Setup wizard in LoanView
        [JsonIgnore] public bool WizardIncomeHasValue => (TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0) > 0;
        [JsonIgnore] public bool WizardIncomeEditable => !WizardIncomeHasValue;
        [JsonIgnore] public string WizardIncomeSummary =>
            $"Recorded: {CurrencySymbol}{TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0:N0}/yr";

        [JsonIgnore]
        public List<IncomeExpenseProjectionOutput> IncomeProjectList => TransactionRecords?.IncomeExpenseSummary?.ProjectionTerms ?? new List<IncomeExpenseProjectionOutput>();

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
                    _ = RefreshIncomePropertyChangedAsync();
                    isUpdating = false;
                }
            }
        }

        private bool _showIncomeAfterPropertyExpense;
        public bool ShowIncomeAfterPropertyExpense
        {
            get => _showIncomeAfterPropertyExpense;
            set
            {
                _showIncomeAfterPropertyExpense = value;

                if (isUpdating == false && PageHelper.IsFormLoading == false)
                {
                    isUpdating = true;
                    OnPropertyChanged(nameof(ShowIncomeAfterPropertyExpense));
                    _ = RefreshIncomePropertyChangedAsync();
                    isUpdating = false;
                }
            }
        }

        private bool _includeExpenses;

        public bool IncludeExpenses
        {
            get => _includeExpenses;
            set
            {
                _includeExpenses = value;

                if (isUpdating == false && PageHelper.IsFormLoading == false)
                {
                    isUpdating = true;
                    OnPropertyChanged(nameof(IncludeExpenses));
                    _ = UpdateProjectionDataAsync();
                    isUpdating = false;
                }
            }
        }

        private bool _includePropertyExpenses;

        public bool IncludePropertyExpenses
        {
            get => _includePropertyExpenses;
            set
            {
                _includePropertyExpenses = value;

                if (isUpdating == false && PageHelper.IsFormLoading == false)
                {
                    isUpdating = true;
                    OnPropertyChanged(nameof(IncludePropertyExpenses));
                    _ = UpdateProjectionDataAsync();
                    isUpdating = false;
                }
            }
        }

        [JsonIgnore] private ExpenseViewModel? _expenseSummary;

        [JsonIgnore]
        public ExpenseViewModel? ExpenseSummary
        {
            get => _expenseSummary;
            set
            {
                _expenseSummary = value;
                OnPropertyChanged(nameof(ExpenseSummary));
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

        [JsonIgnore]
        public string StringMonthlyExpenseOnTopBox => ShowIncomeAfterPropertyExpense ? "Monthly and Property loan & expense" : "Monthly expenses";

        [JsonIgnore]
        public string TotalMonthlySumExpenseWithComma
        {
            get
            {
                double expenses = ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;

                if (ShowIncomeAfterPropertyExpense)
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
                if (ShowIncomeAfterPropertyExpense)
                {
                    var expenses = System.Environment.NewLine;

                    expenses += $"${Math.Round(ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0, 0):N0}";

                    expenses += $" + ${Math.Round(PropertyExpenseSummary?.TotalMonthly ?? 0, 0):N0}";
                    expenses += $" + ${Math.Round(PropertyPayment?.TermPaymentMonthly ?? 0, 0):N0}";

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
                if (ShowIncomeAfterExpense)
                {
                    expenses = ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;
                }

                if (ShowIncomeAfterPropertyExpense)
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
                if (ShowIncomeAfterExpense)
                {
                    expenses = ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;
                }

                if (ShowIncomeAfterPropertyExpense)
                {
                    expenses += PropertyExpenseSummary?.TotalYearly ?? 0;
                    expenses += PropertyPayment?.TermPaymentYearly ?? 0;
                }

                return expenses;
            }
        }

        [JsonIgnore]
        public string StringMonthlyTextOnTopBox =>
            ShowIncomeAfterExpense ? "Monthly Income (after expenses)" : "Monthly Income";

        [JsonIgnore]
        public string StringYearlyTextOnTopBox =>
            ShowIncomeAfterExpense ? "Yearly Income (after expenses)" : "Yearly Income";

        [JsonIgnore]
        public string StringChartTitleText => ShowIncomeAfterExpense
            ? "Income Growth Projection (after expense)"
            : "Income Growth Projection";

        [JsonIgnore] public string StringProjectionInfoText => ShowIncomeAfterExpense ? " after expense" : "";

        #endregion

        #region Total Details

        [JsonIgnore]
        public string TotalMonthlyIncomeWithComma => TransactionRecords?.IncomeExpenseSummary?.TotalMonthlyWithComma ?? "";

        [JsonIgnore]
        public string TotalYearlyIncomeWithComma => TransactionRecords?.IncomeExpenseSummary?.TotalYearlyWithComma ?? "";

        [JsonIgnore]
        public string TotalProjectedYearlyIncomeWithComma => TransactionRecords?.IncomeExpenseSummary.ProjectTotalYearlyWithComma ?? "";

        #endregion

        #region Charts

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartProjectionTermStartAmountAxis
        {
            get
            {
                if (TransactionRecords?.IncomeExpenseSummary?.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(
                        TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f =>
                            new ChartDataModel(name: f.YearOfPayment.ToString(), value: f.TermStartAmount)));
                }
            }
        }

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartProjectionIncomeExpenseAmountAxis
        {
            get
            {
                if (TransactionRecords?.IncomeExpenseSummary?.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(
                        TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f =>
                            new ChartDataModel(name: f.YearOfPayment.ToString(), value: f.IncomeExpenseAmount)));
                }
            }
        }

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> ChartProjectionDeductionAmountAxis
        {
            get
            {
                if (TransactionRecords?.IncomeExpenseSummary?.ProjectionTerms?.ToList() == null)
                {
                    return new ObservableCollection<ChartDataModel>();
                }
                else
                {
                    return new ObservableCollection<ChartDataModel>(
                        TransactionRecords.IncomeExpenseSummary.ProjectionTerms.Select(f =>
                            new ChartDataModel(name: f.YearOfPayment.ToString(), value: f.TermAdjustments)));
                }
            }
        }

        #endregion

        #region Projection Details

        [JsonIgnore]
        public int TotalYearsToProject
        {
            get => TransactionRecords?.IncomeExpenseSummary?.NumberOfYearsProjection ?? 0;
            set
            {
                if (isUpdating == false && TransactionRecords != null)
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
            get => TransactionRecords?.IncomeExpenseSummary?.AnnualGrowthRate ?? 0;
            set
            {
                if (isUpdating == false && TransactionRecords != null)
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
        public double AnnualGrowthRatePercentage => TransactionRecords?.IncomeExpenseSummary?.AnnualGrowthRatePercentage ?? 0;

        #endregion

        #region Live Updates

        private async Task UpdateProjectionDataAsync()
        {
            try
            {
                // Do CPU work off the UI thread
                await Task.Run(() =>
                {
                    if (PageHelper.IsFormLoading || SharedServiceCore.LoadSafe) return;
                    UpdateProjectionData();
                }).ConfigureAwait(false);

                // Marshal UI notifications back to the main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (PageHelper.IsFormLoading || SharedServiceCore.LoadSafe) return;
                    TriggerPropertyChangedOnProjectionTab();
                });
            }
            catch (Exception e)
            {
                _errorHandlingService.HandleException(e);
            }
        }

        private async Task RefreshIncomePropertyChangedAsync()
        {
            try
            {
                if (PageHelper.IsFormLoading || SharedServiceCore.LoadSafe) return;

                // Do the data summing off the UI thread
                await Task.Run(() =>
                {
                    if (PageHelper.IsFormLoading || SharedServiceCore.LoadSafe) return;
                    TransactionRecords?.SumUpData(TotalMonthlyExpense, TotalYearlyExpense);
                }).ConfigureAwait(false);

                // Fire all notifications on the UI thread
                MainThread.BeginInvokeOnMainThread(() => RefreshIncomePropertyChanged());
            }
            catch (Exception e)
            {
                _errorHandlingService.HandleException(e);
            }
        }

        public void RefreshIncomePropertyChanged()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe || TransactionRecords == null) return;

            TransactionRecords?.SumUpData(TotalMonthlyExpense, TotalYearlyExpense);

            OnPropertyChanged(nameof(StringMonthlyExpenseOnTopBox));
            OnPropertyChanged(nameof(TotalMonthlyExpenseBreakdownWithComma));
            OnPropertyChanged(nameof(TotalMonthlySumExpenseWithComma));
            OnPropertyChanged(nameof(TotalMonthlyExpense));
            OnPropertyChanged(nameof(TotalYearlyExpense));
            OnPropertyChanged(nameof(IncomeExpenseEntry));
            OnPropertyChanged(nameof(IncomeEntryName));
            OnPropertyChanged(nameof(HasErrorIncomeAmount));
            OnPropertyChanged(nameof(HasErrorIncomeDescription));
            OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            OnPropertyChanged(nameof(IncomeEntryAmount));
            OnPropertyChanged(nameof(IncomeEntryAmountText));
            OnPropertyChanged(nameof(HasErrorIncomeAmount));
            OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            OnPropertyChanged(nameof(TotalMonthlyIncomeWithComma));
            OnPropertyChanged(nameof(TotalYearlyIncomeWithComma));
            OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
            OnPropertyChanged(nameof(Transactions));
            OnPropertyChanged(nameof(FilteredTransactions));
            OnPropertyChanged(nameof(AutocompleteNameList));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(ShowIncomeAfterExpense));
            OnPropertyChanged(nameof(StringMonthlyTextOnTopBox));
            OnPropertyChanged(nameof(StringYearlyTextOnTopBox));
            OnPropertyChanged(nameof(StringChartTitleText));
            OnPropertyChanged(nameof(StringProjectionInfoText));

            ScheduleSave(() => SharedServiceCore.SaveData(this));
        }

        public void UpdateProjectionData()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe || TransactionRecords == null) return;

            double expenses = 0;
            if (IncludeExpenses && ExpenseSummary != null)
            {
                expenses = ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;
            }

            if (IncludePropertyExpenses)
            {
                if (PropertyExpenseSummary != null)
                {
                    expenses += PropertyExpenseSummary?.TotalYearly ?? 0;
                }
                if (PropertyPayment != null)
                {
                    expenses += PropertyPayment?.TermPaymentYearly ?? 0;
                }

            }

            TransactionRecords.SumUpData();
            HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear(TransactionRecords.IncomeExpenseSummary,
                personalExpense: expenses);
        }

        public void TriggerPropertyChangedOnProjectionTab()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            OnPropertyChanged(nameof(ChartProjectionTermStartAmountAxis));
            OnPropertyChanged(nameof(ChartProjectionIncomeExpenseAmountAxis));
            OnPropertyChanged(nameof(ChartProjectionDeductionAmountAxis));
            OnPropertyChanged(nameof(TotalYearsToProject));
            OnPropertyChanged(nameof(TotalProjectedYearlyIncomeWithComma));
            OnPropertyChanged(nameof(AnnualGrowthRatePercentage));
            OnPropertyChanged(nameof(AnnualGrowthRate));
            OnPropertyChanged(nameof(IncomeProjectList));
            OnPropertyChanged(nameof(StringChartTitleText));

            ScheduleSave(() => SharedServiceCore.SaveData(this));
        }

        #endregion
    }
}
