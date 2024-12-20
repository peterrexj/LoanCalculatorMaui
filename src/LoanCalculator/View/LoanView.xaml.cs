using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.ViewModel;
using Pj.Library;
using SelectionChangedEventArgs = Syncfusion.Maui.Buttons.SelectionChangedEventArgs;

namespace LoanCalculatorMaui.View;

public partial class LoanView : ContentPage
{
    private LoanViewModel viewModel;
    private ISharedServices _sharedServices;


    public LoanView(LoanViewModel vm, ISharedServices sharedServices)
	{
		InitializeComponent();
        _sharedServices = sharedServices;
        viewModel = vm;
	}

    protected override void OnAppearing()
    {
        //Task.Run(async () => await LoadDataSet());
        LoadDataSet();

        BindingContext ??= viewModel;

        base.OnAppearing();

    }

    private void LoadDataSet()
    {
        LoanViewModel temp = null;
        //Task.Run(async () => temp = await _sharedServices.LocalStorage.GetData<LoanViewModel>).Wait();
        Task.Run(async () => temp = await _sharedServices.LocalStorage.GetData<LoanViewModel>());



        //if (viewModel.HasInitialized == false)
        //{
        //    viewModel.InitializeViewData();
        //}
        if (!viewModel.HasInitialized && temp == null)
        {
            viewModel.InitializeViewData();
            viewModel.IncomeExpenseEntry = new IncomeExpense();
            viewModel.Expenses = new Incomes
            {
                IncomeExpenseEntries = new System.Collections.ObjectModel.ObservableCollection<IncomeExpense>()
            };
            AddDefaultValues();
            AddDefaultToExpenses();
        }
        else if (!viewModel.HasInitialized && temp != null)
        {
            viewModel = temp.DeepClone();
        }
        else if (temp == null)
        {
            viewModel.Expenses.DeleteAll();
            AddDefaultValues();
            AddDefaultToExpenses();
        }

        if (viewModel.IncomeExpenseEntry == null)
        {
            viewModel.IncomeExpenseEntry = new IncomeExpense();
        }
        if (viewModel.Expenses == null)
        {
            viewModel.Expenses = new Incomes
            {
                IncomeExpenseEntries = new System.Collections.ObjectModel.ObservableCollection<IncomeExpense>()
            };
            AddDefaultToExpenses();
        }

        viewModel.IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
        viewModel.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
        //lstEntry.DataSource.SortDescriptors.Clear();

        viewModel.ExpenseSummary = _sharedServices.ExpenseSummary;
        viewModel.IncomeSummary = _sharedServices.IncomeSummary;

        segmentedRepaymentFrequency.SelectionChanged += SegmentedRepaymentFrequency_SelectionChanged;
        AmortizationBreadDownFrequencySegmentCtrl.SelectionChanged += AmortizationBreadDownFrequencySegmentCtrlOnSelectionChanged;
    }

    private void AmortizationBreadDownFrequencySegmentCtrlOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        viewModel.AmortizationBreakdownFrequencySelectedIndex = e.NewIndex.Value;
    }

    private void SegmentedRepaymentFrequency_SelectionChanged(object? sender, Syncfusion.Maui.Buttons.SelectionChangedEventArgs e)
    {
        viewModel.RepaymentFrequencySelectedIndex = e.NewIndex.Value;
    }

    private void AddDefaultValues()
    {
        viewModel.PropertyAmount = 1000000;
        viewModel.InterestRate = 5.0;
        viewModel.LoanTermInYears = 30;
        viewModel.DepositPercentage = 10;
    }
    private void AddDefaultToExpenses()
    {
        viewModel?.Expenses?.Add("Maintenance cost", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
        viewModel?.Expenses?.Add("Water bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
        viewModel?.Expenses?.Add("Electricity bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
        viewModel?.Expenses?.Add("Gas bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
        viewModel?.Expenses?.Add("Council bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
    }

    private void Estimate_TabItem_Clicked(object sender, EventArgs e)
    {

    }

    private void OnTabSelectionChanged(object sender, Syncfusion.Maui.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            viewModel.SyncAmortization();
        }
    }
}