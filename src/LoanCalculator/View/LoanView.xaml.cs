using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Extensions;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.TabView;
using SelectionChangedEventArgs = Syncfusion.Maui.Buttons.SelectionChangedEventArgs;

namespace LoanCalculatorMaui.View;

public partial class LoanView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private LoanViewModel _viewModel;
    private readonly IThemeHandler _themeHandler;

    public LoanView(
        IErrorHandlingService errorHandlingService,
        LoanViewModel viewModel,
        IThemeHandler themeHandler)
    {
        InitializeComponent();

        _errorHandlingService = errorHandlingService;
        _themeHandler = themeHandler;

        _viewModel = viewModel;
        _viewModel.IsBusy = true;
        _viewModel.IsUpdating = true;
        _viewModel.IsActive = false;
        _viewModel.IsPageBusy = true;
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
        finally
        {
            PageHelper.PageLoadingComplete();
            _viewModel.IsUpdating = false;
            _viewModel.IsBusy = false;
            _viewModel.IsActive = true;
            _viewModel.IsPageBusy = false;
        }
    }

    private async Task LoadDataSet()
    {
        try
        {
            PageHelper.PageIsLoading();

            bool requiresDefault = false;

            var viewModelInitializeTask = Task.Run(async () =>
            {
                _viewModel.InitializeViewData();

                var data = await SharedServiceCore.LoadDataFile<LoanViewModel>();
                if (!_viewModel.HasInitialized || data == null || _viewModel.TransactionRecords == null)
                {
                    if (data == null)
                    {
                        requiresDefault = true;
                        //The reason for not calling AddDefaultValues here is that it will not go into the SET method as there are few checks
                    }
                    else
                    {
                        _viewModel.CopyPropertiesFrom(data);
                    }

                    if (_viewModel.TransactionRecords == null)
                    {
                        _viewModel.AddDefaultToExpenses();
                    }
                }
            });

            var chartColorsTask = Task.Run(() => _themeHandler.GetChartColors());
            var expenseSummaryTask = Task.Run(() => SharedServiceCore.ExpenseSummary);
            var incomeSummaryTask = Task.Run(() => SharedServiceCore.IncomeSummary);
            var lstSourceTask = Task.Run(() =>
            {
                lstEntry.DataSource?.SortDescriptors.Clear();
                lstEntry.DataSource?.SortDescriptors.Add(new SortDescriptor()
                { PropertyName = "Name", Direction = ListSortDirection.Ascending });
            });


            await Task.WhenAll(viewModelInitializeTask, expenseSummaryTask, chartColorsTask, lstSourceTask);

            _viewModel.CustomChartColors = chartColorsTask.Result;
            _viewModel.ExpenseSummary = expenseSummaryTask.Result;
            _viewModel.IncomeSummary = incomeSummaryTask.Result;
            _viewModel.HasIncomeExpensesRecorded = _viewModel.ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 && 
                _viewModel.IncomeSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;
           
                SegmentedRepaymentFrequency.SelectionChanged += SegmentedRepaymentFrequency_SelectionChanged;
            AmortizationBreadDownFrequencySegmentCtrl.SelectionChanged +=
                AmortizationBreadDownFrequencySegmentCtrlOnSelectionChanged;
            SegmentedAustraliaStates.SelectionChanged += SegmentedAustraliaStatesOnSelectionChanged;

            _viewModel.MarkInitializationComplete();

            _viewModel.IsUpdating = false;

            BindingContext ??= _viewModel;

            PageHelper.PageLoadingComplete();

            if (requiresDefault)
            {
                _viewModel.AddDefaultValues();
            }

            //var syncAmortizationTask = Task.Run(() => _viewModel.SyncAmortization());
            var triggerOneTimeUpdateTask = Task.Run(() => _viewModel.TriggerOneTimeUpdateOnPage());
            var triggerPropertyChangedTask = Task.Run(() => _viewModel.TriggerPropertyChangedOnPropertyTab());
            var refreshExpenseTabTask = Task.Run(() => _viewModel.RefreshExpenseTabPropertyChanged());

            await Task.WhenAll(triggerOneTimeUpdateTask, triggerPropertyChangedTask, refreshExpenseTabTask);

            _viewModel.SyncAmortization(); //has to be done later as the amortization requires the property data which cannot refreshed in parallel
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            PageHelper.PageLoadingComplete();
        }
    }

    private void SegmentedAustraliaStatesOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.NewIndex != null) _viewModel.AustraliaStateSelectedIndex = e.NewIndex.Value;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void AmortizationBreadDownFrequencySegmentCtrlOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.NewIndex != null) _viewModel.AmortizationBreakdownFrequencySelectedIndex = e.NewIndex.Value;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void SegmentedRepaymentFrequency_SelectionChanged(object? sender, Syncfusion.Maui.Buttons.SelectionChangedEventArgs e)
    {
        try
        {
            if (e.NewIndex != null) _viewModel.RepaymentFrequencySelectedIndex = e.NewIndex.Value;
        }
        catch (Exception ex)
        {
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
                lstEntry.DataSource.Filter = FilterExpenseIncome;
                lstEntry.DataSource.RefreshFilter();
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
    private void autoComplete_Completed(object sender, EventArgs e)
    {

    }

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
            _viewModel.RefreshExpenseTabPropertyChanged();
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
            _viewModel.RefreshExpenseTabPropertyChanged();
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

            _viewModel.RefreshExpenseTabPropertyChanged();
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
            _viewModel.RefreshExpenseTabPropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void OnLabelTapped(object sender, TappedEventArgs e)
    {
        EnableSliderCheckBox.IsChecked = !EnableSliderCheckBox.IsChecked;
    }

    private void TabView_OnSelectionChanging(object? sender, SelectionChangingEventArgs e)
    {
        try
        {
            if (e.Index == 1)
            {
                _viewModel.SyncAmortization();
            }
            else if (e.Index == 2)
            {
                if (SharedServiceCore.IsTrialUser)
                {
                    PremiumWindow.ShowPremiumBuyWindow = true;
                    e.Cancel = true;
                }
                else
                {
                    _viewModel.RefreshExpenseTabPropertyChanged();
                }
            }
            else if (e.Index == 3)
            {
                if (SharedServiceCore.IsTrialUser)
                {
                    PremiumWindow.ShowPremiumBuyWindow = true;
                    e.Cancel = true;
                }
                else
                {
                    _viewModel.RefreshInsightsTabPropertyChanged();
                }
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
}