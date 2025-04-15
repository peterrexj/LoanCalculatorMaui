using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.Themes;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.DataSource;
using SelectionChangedEventArgs = Syncfusion.Maui.Buttons.SelectionChangedEventArgs;

namespace LoanCalculatorMaui.View;

public partial class LoanView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private LoanViewModel _viewModel;

    public LoanView(IErrorHandlingService errorHandlingService)
    {
        _errorHandlingService = errorHandlingService;

        InitializeComponent();
        _viewModel = new LoanViewModel
        {
            IsBusy = true,
            IsUpdating = true,
            IsActive = false
        };
    }

    protected override async void OnAppearing()
    {
        try
        {
            await LoadDataSet();

            base.OnAppearing();

            _viewModel.TriggerOneTimeUpdateOnPage();
            _viewModel.TriggerPropertyChangedOnPropertyTab();
            _viewModel.RefreshExpenseTabPropertyChanged();
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
        }
    }

    private async Task LoadDataSet()
    {
        try
        {
            PageHelper.PageIsLoading();

            var data = await SharedServiceCore.LoadDataFile<LoanViewModel>();

            var shouldAddDefaultValues = false;

            if (!_viewModel.HasInitialized)
            {
                if (data == null)
                {
                    //viewModel.InitializeViewData();
                    shouldAddDefaultValues = true;
                    _viewModel.AddDefaultToExpenses();
                }
                else
                {
                    _viewModel = data;
                    _viewModel.IsUpdating = true;
                    _viewModel.IsBusy = true;
                }
            }
            else if (data == null)
            {
                _viewModel.TransactionRecords.DeleteAll();
                shouldAddDefaultValues = true;
                _viewModel.AddDefaultToExpenses();
            }

            _viewModel.InitializeViewData();
            _viewModel.CustomChartColors = StyleProvider.GetChartColors();

            _viewModel.MarkInitializationComplete();

            if (shouldAddDefaultValues)
            {
                _viewModel.IsUpdating = false;

                _viewModel.AddDefaultValues();
            }

            lstEntry.DataSource?.SortDescriptors.Clear();

            _viewModel.ExpenseSummary = SharedServices.ExpenseSummary;
            _viewModel.IncomeSummary = SharedServices.IncomeSummary;

            SegmentedRepaymentFrequency.SelectionChanged += SegmentedRepaymentFrequency_SelectionChanged;
            AmortizationBreadDownFrequencySegmentCtrl.SelectionChanged += AmortizationBreadDownFrequencySegmentCtrlOnSelectionChanged;
            SegmentedAustraliaStates.SelectionChanged += SegmentedAustraliaStatesOnSelectionChanged;

            _viewModel.SyncAmortization();

            PageHelper.PageLoadingComplete();

            BindingContext ??= _viewModel;

            lstEntry.DataSource?.SortDescriptors.Add(new SortDescriptor() { PropertyName = "Name", Direction = ListSortDirection.Ascending });

            _viewModel.IsUpdating = false;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
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

    private void OnTabSelectionChanged(object sender, Syncfusion.Maui.TabView.TabSelectionChangedEventArgs e)
    {
        try
        {
            if (e.NewIndex == 1)
            {
                _viewModel.SyncAmortization();
            }
            else if (e.NewIndex == 2)
            {
                _viewModel.RefreshExpenseTabPropertyChanged();
            }
            else if (e.NewIndex == 3)
            {
                _viewModel.RefreshInsightsTabPropertyChanged();
            }
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
}