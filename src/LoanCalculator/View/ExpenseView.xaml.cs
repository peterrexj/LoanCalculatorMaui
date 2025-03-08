using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.ViewModel;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.DataSource;

namespace LoanCalculatorMaui.View;

public partial class ExpenseView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly IAlertService _alertService;
    ExpenseViewModel viewModel;

    public ExpenseView(IErrorHandlingService errorHandlingService, IAlertService alertService)
    {
        _errorHandlingService = errorHandlingService;
        _alertService = alertService;

        InitializeComponent();

        viewModel = new ExpenseViewModel(_errorHandlingService, _alertService);
    }

    protected override async void OnAppearing()
    {
        try
        {
            await LoadDataSet();

            base.OnAppearing();

            viewModel.TriggerOneTimeUpdateOnPage();
            viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            base.OnAppearing();
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            PageHelper.PageLoadingComplete();
            viewModel.IsUpdating = false;
        }
    }

    private async Task LoadDataSet()
    {
        try
        {
            PageHelper.PageIsLoading();

            if (!viewModel.HasInitialized)
            {
                var data = await SharedServices.LoadDataFile<ExpenseViewModel>();

                if (data == null)
                {
                    viewModel.AddDefaultToExpenses();
                }
                else if (data is { TransactionRecords: null })
                {
                    viewModel.AddDefaultToExpenses();
                }
                else
                {
                    viewModel = data;
                }
            }
            if (viewModel?.TransactionRecords == null)
            {
                viewModel!.AddDefaultToExpenses();
            }
            
            viewModel.InitializeViewData();
            viewModel.InitializeBrushes();
            viewModel.MarkInitializationComplete();

            viewModel.IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
            viewModel.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
            viewModel.IncomeSummary = SharedServices.IncomeSummary;

            var loanData = SharedServices.GetLoanViewModel();
            viewModel.PropertyExpenseSummary = loanData.Item1;
            viewModel.PropertyPayment = loanData.Item2;

            lstEntry.DataSource?.SortDescriptors.Clear();

            PageHelper.PageLoadingComplete();

            BindingContext ??= viewModel;

            lstEntry.DataSource?.SortDescriptors.Add(new SortDescriptor() { PropertyName = "Name", Direction = ListSortDirection.Ascending });

            viewModel.IsUpdating = false;
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
            if (viewModel.SearchExpenseIncomeName.HasValue())
            {
                filterText = viewModel.SearchExpenseIncomeName;
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
                viewModel.SearchExpenseIncomeName = "";
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

            if (viewModel.HasErrorIncomeDescription)
            {
                txtIncomeDescription.Focus();
                return;
            }
            if (viewModel.HasErrorIncomeAmount)
            {
                txtInputAmount.Focus();
                return;
            }

            if (viewModel.AddOrUpdateEntryFromView() == false) return;

            lstEntry.DataSource?.SortDescriptors.Clear();
            lstEntry.DataSource?.SortDescriptors.Add(new SortDescriptor() { PropertyName = "Name", Direction = ListSortDirection.Ascending });

            lstEntry.RefreshItem(canReload: true);
            viewModel.RefreshIncomePropertyChanged();
            viewModel.UpdateProjectionData();
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

            viewModel.ResetTransactionEntryData();
            viewModel.RefreshTransactionEntry();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
    private async void btnEditEntry_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

            var entryData = viewModel.TransactionRecords?.Get(Guid.Parse(button.AutomationId))?.DeepClone();
            if (entryData == null)
            {
                await _alertService.ShowAlertAsync("Error", "Unable to find the entry to edit.", "OK");
                return;
            }

            viewModel.IncomeExpenseEntry = entryData;

            viewModel.IncomeExpenseFrequencySelectedIndex = entryData.Frequency.ToString();

            viewModel.RefreshTransactionEntry();
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

            viewModel.TransactionRecords.Delete(Guid.Parse(button.AutomationId));
            viewModel.ResetTransactionEntryData();
            viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
    #endregion

    private void OnTabSelectionChanged(object sender, Syncfusion.Maui.TabView.TabSelectionChangedEventArgs e)
    {
        try
        {
            if (e.NewIndex == 1)
            {
                viewModel.UpdateProjectionData();
                viewModel.TriggerPropertyChangedOnProjectionTab();
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
}