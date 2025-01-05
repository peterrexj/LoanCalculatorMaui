using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.ViewModel;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.ListView;
using SelectionChangedEventArgs = Syncfusion.Maui.Buttons.SelectionChangedEventArgs;

namespace LoanCalculatorMaui.View;

public partial class LoanView : ContentPage
{
    private LoanViewModel viewModel;
    public LoanView()
    {
        InitializeComponent();
        viewModel = new LoanViewModel();
    }

    protected override async void OnAppearing()
    {
        PageHelper.PageIsLoading();

        await LoadDataSet();

        PageHelper.PageLoadingComplete();

        BindingContext ??= viewModel;

        lstEntry.DataSource.SortDescriptors.Add(new SortDescriptor() { PropertyName = "Name", Direction = ListSortDirection.Ascending });

        base.OnAppearing();

        viewModel.TriggerOneTimeUpdateOnPage();
        viewModel.TriggerPropertyChangedOnPropertyTab();


        //viewModel.ShowAppLaunchDisclaimer(disclaimerAppLevelPopupLayout);
    }

    private async Task LoadDataSet()
    {
        LoanViewModel temp = null;
        bool shouldAddDefaultValues = false;
        try
        {
            SharedServices.LocalStorage.Initialize();

            if (SharedServices.LocalStorage != null)
            {
                //await SharedServices.LocalStorage.ClearData<LoanViewModel>().ConfigureAwait(false);
                temp = await SharedServices.LocalStorage.GetData<LoanViewModel>().ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            // Log or handle the exception as needed
            // ExceptionHandler.CaptureException(e);
        }

        if (!viewModel.HasInitialized)
        {
            if (temp == null)
            {
                //viewModel.InitializeViewData();
                shouldAddDefaultValues = true;
                viewModel.AddDefaultToExpenses();
            }
            else
            {
                viewModel = temp.DeepClone();
            }
        }
        else if (temp == null)
        {
            viewModel.Expenses.DeleteAll();
            shouldAddDefaultValues = true;
            viewModel.AddDefaultToExpenses();
        }

        viewModel.InitializeViewData();
        viewModel.InitializeBrushes();
       
        viewModel.MarkInitializationComplete();

        if (shouldAddDefaultValues)
        {
            viewModel.AddDefaultValues();
        }

        lstEntry.DataSource.SortDescriptors.Clear();

        viewModel.ExpenseSummary = SharedServices.ExpenseSummary;
        viewModel.IncomeSummary = SharedServices.IncomeSummary;

        SegmentedRepaymentFrequency.SelectionChanged += SegmentedRepaymentFrequency_SelectionChanged;
        AmortizationBreadDownFrequencySegmentCtrl.SelectionChanged += AmortizationBreadDownFrequencySegmentCtrlOnSelectionChanged;
        SegmentedAustraliaStates.SelectionChanged += SegmentedAustraliaStatesOnSelectionChanged;
    }

    private void SegmentedAustraliaStatesOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.NewIndex != null) viewModel.AustraliaStateSelectedIndex = e.NewIndex.Value;
    }


    private void AmortizationBreadDownFrequencySegmentCtrlOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.NewIndex != null) viewModel.AmortizationBreakdownFrequencySelectedIndex = e.NewIndex.Value;
    }

    private void SegmentedRepaymentFrequency_SelectionChanged(object? sender, Syncfusion.Maui.Buttons.SelectionChangedEventArgs e)
    {
        if (e.NewIndex != null) viewModel.RepaymentFrequencySelectedIndex = e.NewIndex.Value;
    }



    private void Estimate_TabItem_Clicked(object sender, EventArgs e)
    {
        //tabView.SelectedIndex = 0;
    }

    private void OnTabSelectionChanged(object sender, Syncfusion.Maui.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            viewModel.SyncAmortization();
        }
        else if (e.NewIndex == 2)
        {
            viewModel.RefreshExpenseTabPropertyChanged();
        }
        else if (e.NewIndex == 3)
        {
            viewModel.RefreshInsightsTabPropertyChanged();
        }
    }



    private void autoComplete_Completed(object sender, EventArgs e)
    {

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
        viewModel.RefreshExpenseTabPropertyChanged();
    }

    private void ResetButton_Clicked(object sender, EventArgs e)
    {
        viewModel.IncomeExpenseEntry.Name = string.Empty;
        viewModel.IncomeExpenseEntry.Amount = 0;
        viewModel.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
        viewModel.IncomeExpenseEntry.Id = Guid.Empty;
        viewModel.RefreshExpenseTabPropertyChanged();
    }

    private void btnEditEntry_Clicked(object sender, EventArgs e)
    {
        if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

        viewModel.IncomeExpenseEntry = viewModel.Expenses.Get(Guid.Parse(button.AutomationId)).DeepClone();
        viewModel.IncomeExpenseFrequencySelectedIndex = viewModel.IncomeExpenseEntry.Frequency.ToString();

        viewModel.RefreshExpenseTabPropertyChanged();
    }

    private void btnDeleteEntry_Clicked(object sender, EventArgs e)
    {
        if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

        viewModel.Expenses.Delete(Guid.Parse(button.AutomationId));
        viewModel.RefreshExpenseTabPropertyChanged();
    }

    private void LstEntry_OnSelectionChanged(object? sender, ItemSelectionChangedEventArgs e)
    {

    }
}