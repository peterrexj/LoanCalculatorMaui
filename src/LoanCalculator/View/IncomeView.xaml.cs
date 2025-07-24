using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Services;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.TabView;

namespace LoanCalculatorMaui.View;

public partial class IncomeView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly IAlertService _alertService;
    private IncomeViewModel _viewModel;
    private readonly IThemeHandler _themeHandler;

    public IncomeView(
        IErrorHandlingService errorHandlingService, 
        IAlertService alertService, 
        IncomeViewModel viewModel,
        IThemeHandler themeHandler)
    {
        InitializeComponent();

        _errorHandlingService = errorHandlingService;
        _alertService = alertService;
        _themeHandler = themeHandler;
        _viewModel = viewModel;
        _viewModel.IsUpdating = true;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            
            await Task.Delay(100); // Delay to allow UI to load

            await Task.Yield();

            Dispatcher.Dispatch(async () =>
            {
                await LoadDataSet();
            });
        }
        catch (Exception ex)
        {
            base.OnAppearing();
            _errorHandlingService.HandleException(ex);
        }
    }

    private async Task LoadDataSet()
    {
        try
        {
            PageHelper.PageIsLoading();

            var viewModelInitializeTask = Task.Run(async () =>
            {
                //if (!_viewModel.HasInitialized)
                //{
                    var data = await SharedServiceCore.LoadDataFile<IncomeViewModel>();

                    if (data == null)
                    {
                        _viewModel.AddDefaultToExpenses();
                    }
                    else if (data is { TransactionRecords: null })
                    {
                        _viewModel.AddDefaultToExpenses();
                    }
                    else
                    {
                        //_viewModel = data;
                        _viewModel.CopyPropertiesFrom(data);
                    }
                //}
                if (_viewModel?.TransactionRecords == null)
                {
                    _viewModel.AddDefaultToExpenses();
                }
                _viewModel.InitializeViewData();

                _viewModel.IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
                _viewModel.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
            });

            var expenseSummaryTask = Task.Run(() => SharedServiceCore.ExpenseSummary);
            var loanDataTask = Task.Run(() => SharedServiceCore.GetLoanViewModel());
            var chartColorsTask = Task.Run(() => _themeHandler.GetChartColors());

            var lstSourceTask = Task.Run(() =>
            {
                lstEntry.DataSource?.SortDescriptors.Clear();
                lstEntry.DataSource?.SortDescriptors.Add(new SortDescriptor() { PropertyName = "Name", Direction = ListSortDirection.Ascending });
            });

            await Task.WhenAll(viewModelInitializeTask, expenseSummaryTask, loanDataTask, chartColorsTask, lstSourceTask);

            _viewModel.CustomChartColors = chartColorsTask.Result;
            _viewModel.ExpenseSummary = expenseSummaryTask.Result;

            var loanData = loanDataTask.Result;
            _viewModel.PropertyExpenseSummary = loanData.Item1;
            _viewModel.PropertyPayment = loanData.Item2;

            if (SharedServiceCore.IsTrialUser && SharedServiceCore.IsCurrentDay() == false)
            {
                _viewModel.AddDefaultToExpenses();
            }

            _viewModel.CurrencySymbol = Helper.CurrencySymbol;

            _viewModel.MarkInitializationComplete();

            BindingContext ??= _viewModel;

            _viewModel.IsUpdating = false;
            PageHelper.PageLoadingComplete();

            _viewModel.TriggerOneTimeUpdateOnPage();
            _viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            _viewModel.IsUpdating = false;
            PageHelper.PageLoadingComplete();

            _errorHandlingService.HandleException(ex);
        }
    }

    private async Task RefreshListOfIncomeExpense()
    {
        try
        {
            await Task.Run(() =>
            {
                ViewHelper.RunOnAppDispatcherAsync(() =>
                {
                    if (lstEntry.DataSource != null)
                    {
                        lstEntry.DataSource.Filter = FilterExpenseIncome;
                        lstEntry.DataSource.RefreshFilter();
                    }
                });
            });
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
    private bool FilterExpenseIncome(object obj)
    {
        try
        {
            var persona = obj as IncomeExpense;
            var filterText = string.Empty;
            if (_viewModel.SearchExpenseIncomeName.HasValue())
            {
                filterText = _viewModel.SearchExpenseIncomeName;
            }
            else if (autoComplete.SelectedItem != null)
            {
                filterText = (autoComplete.SelectedItem as SearchAutoCompleteViewModel).Name;
            }
            else if (autoComplete.Text.HasValue())
            {
                filterText = autoComplete.Text;
            }

            if (filterText.IsEmpty())
            {
                return true;
            }

            return persona.Name.ContainsIgnoreCase(filterText);
        }
        catch (OperationCanceledException)
        {
            return true;
        }
        catch (Exception ex)
        {
            //ExceptionHandler.CaptureException(ex);
            return true;
        }
    }


    private void autoComplete_Completed(object sender, EventArgs e)
    {

    }
    private async void autoComplete_SelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        try
        {
            if (autoComplete.Text.IsEmpty())
            {
                await ViewHelper.RunOnAppDispatcherAsync(() =>
                {
                    autoComplete.SelectedItem = null;
                });
                _viewModel.SearchExpenseIncomeName = "";
                await RefreshListOfIncomeExpense();
                autoComplete.IsDropDownOpen = false;
            }
            else
            {
                if (lstEntry.DataSource == null) return;

                lstEntry.DataSource.Filter = FilterExpenseIncome;
                lstEntry.DataSource.RefreshFilter();
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }


    #region Transaction Entry
    private void AddNewIncome_Clicked(object sender, EventArgs e)
    {
        try
        {
            txtIncomeDescription.Unfocus();
            txtInputAmount.Unfocus();

            if (_viewModel.HasErrorIncomeDescription)
            {
                txtIncomeDescription.Focus();
                return;
            }
            if (_viewModel.HasErrorIncomeAmount)
            {
                txtInputAmount.Focus();
                return;
            }

            if (_viewModel.AddOrUpdateEntryFromView() == false) return;

            lstEntry.DataSource?.SortDescriptors.Clear();
            lstEntry.DataSource?.SortDescriptors.Add(new SortDescriptor() { PropertyName = "Name", Direction = ListSortDirection.Ascending });

            lstEntry.RefreshItem(canReload: true);
            _viewModel.RefreshIncomePropertyChanged();
            _viewModel.UpdateProjectionData();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
    private void ResetButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            txtIncomeDescription.Unfocus();
            txtInputAmount.Unfocus();

            _viewModel.ResetTransactionEntryData();
            _viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
    private void btnEditEntry_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

            _viewModel.IncomeExpenseEntry = _viewModel.TransactionRecords.Get(Guid.Parse(button.AutomationId)).DeepClone();
            _viewModel.IncomeExpenseFrequencySelectedIndex = _viewModel.IncomeExpenseEntry.Frequency.ToString();

            _viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
    private void btnDeleteEntry_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

            _viewModel.TransactionRecords.Delete(Guid.Parse(button.AutomationId));
            _viewModel.ResetTransactionEntryData();
            _viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
    #endregion

    private void TabView_OnSelectionChanging(object? sender, SelectionChangingEventArgs e)
    {
        try
        {
            if (e.Index == 1)
            {
                if (SharedServiceCore.IsTrialUser)
                {
                    PremiumWindow.ShowPremiumBuyWindow = true;
                    e.Cancel = true;
                }
                else
                {
                    _viewModel.UpdateProjectionData();
                    _viewModel.TriggerPropertyChangedOnProjectionTab();
                }
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
}