using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.ViewModel;
using Microsoft.Extensions.Logging.Abstractions;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.Inputs;

namespace LoanCalculatorMaui.View;

public partial class IncomeView : ContentPage
{
    private IncomeViewModel viewModel;

    public IncomeView()
    {
        InitializeComponent();
        viewModel = new IncomeViewModel();
    }

    protected override async void OnAppearing()
    {
        PageHelper.PageIsLoading();
        await LoadDataSet();
        PageHelper.PageLoadingComplete();

        BindingContext ??= viewModel;
        lstEntry.DataSource.SortDescriptors.Add(new SortDescriptor() { PropertyName = "Name", Direction = ListSortDirection.Ascending });

        base.OnAppearing();

        viewModel.IsUpdating = false;
        viewModel.TriggerOneTimeUpdateOnPage();
        viewModel.RefreshIncomePropertyChanged();
    }

    private async Task LoadDataSet()
    {
        IncomeViewModel? data = await viewModel.LoadDataFile<IncomeViewModel>();

        if (!viewModel.HasInitialized)
        {
            if (data == null)
            {
                //viewModel.InitializeViewData();
                viewModel.AddDefaultToExpenses();
            }
            else
            {
                viewModel = data;
            }
        }
        else if (data == null)
        {
            viewModel.TransactionRecords.DeleteAll();
        }

        viewModel.InitializeViewData();
        viewModel.InitializeBrushes();
        viewModel.MarkInitializationComplete();

        viewModel.IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
        viewModel.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
        viewModel.ExpenseSummary = SharedServices.ExpenseSummary;

        lstEntry.DataSource.SortDescriptors.Clear();
    }

    private async Task RefreshListOfIncomeExpense()
    {
        await Task.Run(() =>
        {
            try
            {
                ViewHelper.RunOnAppDispatcherAsync(() =>
                {
                    if (lstEntry.DataSource != null)
                    {
                        lstEntry.DataSource.Filter = FilterExpenseIncome;
                        lstEntry.DataSource.RefreshFilter();
                    }
                });
            }
            catch (Exception ex)
            {
                //ExceptionHandler.CaptureException(ex);
            }
        });
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
                lstEntry.DataSource.Filter = FilterExpenseIncome;
                lstEntry.DataSource.RefreshFilter();
            }
        }
        catch (Exception ex)
        {
            //ExceptionHandler.CaptureException(ex);
        }
    }

    private void AddNewIncome_Clicked(object sender, EventArgs e)
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

        lstEntry.DataSource.SortDescriptors.Clear();
        lstEntry.DataSource.SortDescriptors.Add(new SortDescriptor() { PropertyName = "Name", Direction = ListSortDirection.Ascending });

        lstEntry.RefreshItem(canReload: true);
        viewModel.RefreshIncomePropertyChanged();
    }
    private void ResetButton_Clicked(object sender, EventArgs e)
    {
        txtIncomeDescription.Unfocus();
        txtInputAmount.Unfocus();

        viewModel.ResetTransactionEntryData();
        viewModel.RefreshIncomePropertyChanged();
    }

    private void btnEditEntry_Clicked(object sender, EventArgs e)
    {
        if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

        viewModel.IncomeExpenseEntry = viewModel.TransactionRecords.Get(Guid.Parse(button.AutomationId)).DeepClone();
        viewModel.IncomeExpenseFrequencySelectedIndex = viewModel.IncomeExpenseEntry.Frequency.ToString();

        viewModel.RefreshIncomePropertyChanged();
    }

    private void btnDeleteEntry_Clicked(object sender, EventArgs e)
    {
        if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

        viewModel.TransactionRecords.Delete(Guid.Parse(button.AutomationId));
        viewModel.ResetTransactionEntryData();
        viewModel.RefreshIncomePropertyChanged();
    }
}