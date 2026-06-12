using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using Pj.Library;
using Syncfusion.Maui.TabView;

namespace LoanCalculatorMaui.View;

public partial class BudgetView : ContentPage
{
    private readonly BudgetViewModel _viewModel;
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly IThemeHandler _themeHandler;

    private bool _hasInitialized;

    public BudgetView(
        BudgetViewModel viewModel,
        IncomeViewModel incomeViewModel,
        ExpenseViewModel expenseViewModel,
        LoanViewModel loanViewModel,
        IErrorHandlingService errorHandlingService,
        IAlertService alertService,
        IThemeHandler themeHandler)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _errorHandlingService = errorHandlingService;
        _themeHandler = themeHandler;

        // Wire the singleton VMs so Budget shows the same data as Income/Expense tabs
        _viewModel.SetPeerViewModels(incomeViewModel, expenseViewModel, loanViewModel);

        // Mirror LoanView pattern: guard all sub-VMs before BindingContext so TwoWay
        // SfSwitch bindings don't fire async side-effects during context propagation.
        _viewModel.IsBusy = true;
        _viewModel.IsUpdating = true;
        _viewModel.IsActive = false;
        _viewModel.IsPageBusy = true;
        _viewModel.Income.IsBusy = true;
        _viewModel.Income.IsUpdating = true;
        _viewModel.Income.IsActive = false;
        _viewModel.Income.IsPageBusy = true;
        _viewModel.Expense.IsBusy = true;
        _viewModel.Expense.IsUpdating = true;
        _viewModel.Expense.IsActive = false;
        _viewModel.Expense.IsPageBusy = true;

        BindingContext = _viewModel;

        TopExpensesList.ItemsSource = _viewModel.TopExpenses;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            // Yield one frame so the page skeleton renders before we do any work.
            await Task.Delay(100);

            if (!_hasInitialized)
            {
                _hasInitialized = true;
                PageHelper.PageIsLoading();

                // EnsureSubVmsLoadedAsync is a no-op if SplashPage already pre-warmed the data
                await _viewModel.EnsureSubVmsLoadedAsync();

                _viewModel.InitializeBudget();
                _viewModel.CustomChartColors = _themeHandler.GetChartColors();
                _viewModel.Income.CustomChartColors = _viewModel.CustomChartColors;
                _viewModel.Expense.CustomChartColors = _viewModel.CustomChartColors;

                PageHelper.PageLoadingComplete();
            }

            // Release the guards set in the constructor so bindings become live
            _viewModel.Income.IsUpdating = false;
            _viewModel.Income.IsBusy = false;
            _viewModel.Income.IsActive = true;
            _viewModel.Income.IsPageBusy = false;
            _viewModel.Expense.IsUpdating = false;
            _viewModel.Expense.IsBusy = false;
            _viewModel.Expense.IsActive = true;
            _viewModel.Expense.IsPageBusy = false;
            _viewModel.IsUpdating = false;
            _viewModel.IsBusy = false;
            _viewModel.IsActive = true;
            _viewModel.IsPageBusy = false;

            _viewModel.Income.RefreshIncomePropertyChanged();
            _viewModel.Expense.RefreshIncomePropertyChanged();

            _ = WirePropertyExpenseDataAsync();

            _viewModel.RecalculateSummary();
            UpdateSummaryLabels();
            LblProjectionYears.Text = _viewModel.ProjectionYears.ToString();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private async Task WirePropertyExpenseDataAsync()
    {
        try
        {
            var loanData = await SharedServiceCore.GetLoanViewModelAsync();
            _viewModel.Income.PropertyExpenseSummary = loanData.Item1;
            _viewModel.Income.PropertyPayment = loanData.Item2;
            _viewModel.Expense.PropertyExpenseSummary = loanData.Item1;
            _viewModel.Expense.PropertyPayment = loanData.Item2;
            // IncomeSummary wire is done in InitializeBudget; re-affirm here in case data changed
            _viewModel.Expense.IncomeSummary = _viewModel.Income;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void OnBudgetIncomeCancel(object sender, EventArgs e)
    {
        _viewModel.Income.ResetTransactionEntryData();
        BudgetIncomePopup.IsOpen = false;
    }

    private void OnBudgetIncomeSave(object sender, EventArgs e)
    {
        try
        {
            _viewModel.Income.ShowValidationErrors = true;
            if (_viewModel.Income.HasErrorIncomeDescription || _viewModel.Income.HasErrorIncomeAmount) return;
            if (_viewModel.Income.AddOrUpdateEntryFromView() == false) return;
            BudgetIncomePopup.IsOpen = false;
            _viewModel.Income.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel.Income));
            SharedServiceCore.MarkIncomeDirty();
            _viewModel.Income.RefreshIncomePropertyChanged();
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private async void OnBudgetIncomeEdit(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Syncfusion.Maui.Buttons.SfButton btn || string.IsNullOrEmpty(btn.AutomationId)) return;
            var entry = _viewModel.Income.TransactionRecords?.Get(Guid.Parse(btn.AutomationId))?.DeepClone();
            if (entry == null) return;
            _viewModel.Income.ShowValidationErrors = false;
            _viewModel.Income.IncomeExpenseEntry = entry;
            _viewModel.Income.IncomeExpenseFrequencySelectedIndex = entry.Frequency.ToString();
            PopulateBudgetIncomeAmountEntry();
            BudgetIncomePopup.HeaderTitle = "Edit Income";
            BudgetIncomePopup.IsOpen = true;
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private void OnBudgetIncomeDelete(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Syncfusion.Maui.Buttons.SfButton btn || string.IsNullOrEmpty(btn.AutomationId)) return;
            _viewModel.Income.TransactionRecords?.Delete(Guid.Parse(btn.AutomationId));
            _viewModel.Income.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel.Income));
            SharedServiceCore.MarkIncomeDirty();
            _viewModel.Income.RefreshIncomePropertyChanged();
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    // ── Expense popup handlers ────────────────────────────────────────────
    private void OnBudgetExpenseCancel(object sender, EventArgs e)
    {
        _viewModel.Expense.ResetTransactionEntryData();
        BudgetExpensePopup.IsOpen = false;
    }

    private void OnBudgetExpenseSave(object sender, EventArgs e)
    {
        try
        {
            _viewModel.Expense.ShowValidationErrors = true;
            if (_viewModel.Expense.HasErrorIncomeDescription || _viewModel.Expense.HasErrorIncomeAmount) return;
            if (_viewModel.Expense.AddOrUpdateEntryFromView() == false) return;
            BudgetExpensePopup.IsOpen = false;
            _viewModel.Expense.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel.Expense));
            SharedServiceCore.MarkExpenseDirty();
            _viewModel.Expense.RefreshIncomePropertyChanged();
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private async void OnBudgetExpenseEdit(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Syncfusion.Maui.Buttons.SfButton btn || string.IsNullOrEmpty(btn.AutomationId)) return;
            var entry = _viewModel.Expense.TransactionRecords?.Get(Guid.Parse(btn.AutomationId))?.DeepClone();
            if (entry == null) return;
            _viewModel.Expense.ShowValidationErrors = false;
            _viewModel.Expense.IncomeExpenseEntry = entry;
            _viewModel.Expense.IncomeExpenseFrequencySelectedIndex = entry.Frequency.ToString();
            PopulateBudgetExpenseAmountEntry();
            BudgetExpensePopup.HeaderTitle = "Edit Expense";
            BudgetExpensePopup.IsOpen = true;
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private void OnBudgetExpenseDelete(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Syncfusion.Maui.Buttons.SfButton btn || string.IsNullOrEmpty(btn.AutomationId)) return;
            _viewModel.Expense.TransactionRecords?.Delete(Guid.Parse(btn.AutomationId));
            _viewModel.Expense.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel.Expense));
            SharedServiceCore.MarkExpenseDirty();
            _viewModel.Expense.RefreshIncomePropertyChanged();
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private void OnAddIncomeFab_Clicked(object sender, EventArgs e)
    {
        try
        {
            _viewModel.Income.ResetTransactionEntryData();
            PopulateBudgetIncomeAmountEntry();
            BudgetIncomePopup.HeaderTitle = "Add Income";
            BudgetIncomePopup.IsOpen = true;
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private void OnAddExpenseFab_Clicked(object sender, EventArgs e)
    {
        try
        {
            _viewModel.Expense.ResetTransactionEntryData();
            PopulateBudgetExpenseAmountEntry();
            BudgetExpensePopup.HeaderTitle = "Add Expense";
            BudgetExpensePopup.IsOpen = true;
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    // ── Amount formatting for income popup ────────────────────────────────
    private Entry? _budgetIncomeAmountEntry;
    private Label? _budgetIncomeFormattedLabel;
    private Label? _budgetIncomeWordsLabel;
    private bool _suppressBudgetIncomeTextChanged;

    private void OnBudgetIncomeAmountLoaded(object sender, EventArgs e)
    {
        if (sender is Entry entry)
        {
            _budgetIncomeAmountEntry = entry;
            PopulateBudgetIncomeAmountEntry();
        }
    }

    private void OnBudgetIncomeFormattedLoaded(object sender, EventArgs e)
    {
        if (sender is Label lbl)
        {
            if (lbl.AutomationId == "BudgetIncomeFormatted") _budgetIncomeFormattedLabel = lbl;
            else if (lbl.AutomationId == "BudgetIncomeWords") _budgetIncomeWordsLabel = lbl;
            PopulateBudgetIncomeAmountEntry();
        }
    }

    private void OnBudgetIncomeAmountTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressBudgetIncomeTextChanged || sender is not Entry entry) return;
        FormatBudgetAmountEntry(entry, e.NewTextValue,
            sym => { if (_budgetIncomeFormattedLabel != null) _budgetIncomeFormattedLabel.Text = sym; },
            words => { if (_budgetIncomeWordsLabel != null) _budgetIncomeWordsLabel.Text = words; },
            val => _viewModel.Income.IncomeEntryAmount = val,
            ref _suppressBudgetIncomeTextChanged);
    }

    private void PopulateBudgetIncomeAmountEntry()
    {
        if (_budgetIncomeAmountEntry == null) return;
        var val = _viewModel.Income.IncomeEntryAmount;
        _suppressBudgetIncomeTextChanged = true;
        _budgetIncomeAmountEntry.Text = val > 0 ? $"{val:N0}" : string.Empty;
        _suppressBudgetIncomeTextChanged = false;
        var sym = _viewModel.Income.CurrencySymbol ?? "$";
        var formatted = val > 0 ? $"{sym}{val:N0}" : string.Empty;
        var words = val > 0 ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)Math.Round(val)) : string.Empty;
        if (_budgetIncomeFormattedLabel != null) _budgetIncomeFormattedLabel.Text = formatted;
        if (_budgetIncomeWordsLabel != null) _budgetIncomeWordsLabel.Text = words;
    }

    // ── Amount formatting for expense popup ───────────────────────────────
    private Entry? _budgetExpenseAmountEntry;
    private Label? _budgetExpenseFormattedLabel;
    private Label? _budgetExpenseWordsLabel;
    private bool _suppressBudgetExpenseTextChanged;

    private void OnBudgetExpenseAmountLoaded(object sender, EventArgs e)
    {
        if (sender is Entry entry)
        {
            _budgetExpenseAmountEntry = entry;
            PopulateBudgetExpenseAmountEntry();
        }
    }

    private void OnBudgetExpenseFormattedLoaded(object sender, EventArgs e)
    {
        if (sender is Label lbl)
        {
            if (lbl.AutomationId == "BudgetExpenseFormatted") _budgetExpenseFormattedLabel = lbl;
            else if (lbl.AutomationId == "BudgetExpenseWords") _budgetExpenseWordsLabel = lbl;
            PopulateBudgetExpenseAmountEntry();
        }
    }

    private void OnBudgetExpenseAmountTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressBudgetExpenseTextChanged || sender is not Entry entry) return;
        FormatBudgetAmountEntry(entry, e.NewTextValue,
            sym => { if (_budgetExpenseFormattedLabel != null) _budgetExpenseFormattedLabel.Text = sym; },
            words => { if (_budgetExpenseWordsLabel != null) _budgetExpenseWordsLabel.Text = words; },
            val => _viewModel.Expense.IncomeEntryAmount = val,
            ref _suppressBudgetExpenseTextChanged);
    }

    private void PopulateBudgetExpenseAmountEntry()
    {
        if (_budgetExpenseAmountEntry == null) return;
        var val = _viewModel.Expense.IncomeEntryAmount;
        _suppressBudgetExpenseTextChanged = true;
        _budgetExpenseAmountEntry.Text = val > 0 ? $"{val:N0}" : string.Empty;
        _suppressBudgetExpenseTextChanged = false;
        var sym = _viewModel.Expense.CurrencySymbol ?? "$";
        var formatted = val > 0 ? $"{sym}{val:N0}" : string.Empty;
        var words = val > 0 ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)Math.Round(val)) : string.Empty;
        if (_budgetExpenseFormattedLabel != null) _budgetExpenseFormattedLabel.Text = formatted;
        if (_budgetExpenseWordsLabel != null) _budgetExpenseWordsLabel.Text = words;
    }

    private static void FormatBudgetAmountEntry(Entry entry, string rawText,
        Action<string> setFormatted, Action<string> setWords, Action<double> setViewModel,
        ref bool suppress)
    {
        var digits = new string(rawText.Where(char.IsDigit).ToArray());
        if (!double.TryParse(digits, out var val)) val = 0;
        var formatted = val > 0 ? $"{val:N0}" : string.Empty;
        var words = val > 0
            ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)val)
            : string.Empty;
        suppress = true;
        entry.Text = formatted;
        entry.CursorPosition = Math.Max(0, formatted.Length);
        suppress = false;
        setFormatted(formatted);
        setWords(words);
        setViewModel(val);
    }

    private void OnIncomeAutoCompleteSelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        var text = IncomeAutoComplete.SelectedItem as string ?? IncomeAutoComplete.Text ?? string.Empty;
        _viewModel.Income.SearchExpenseIncomeName = text.Trim();
        if (string.IsNullOrWhiteSpace(text)) IncomeAutoComplete.IsDropDownOpen = false;
    }

    private void OnIncomeAutoCompleteCompleted(object sender, EventArgs e)
    {
        var text = IncomeAutoComplete.Text ?? string.Empty;
        _viewModel.Income.SearchExpenseIncomeName = text.Trim();
    }

    private void OnExpenseAutoCompleteSelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        var text = ExpenseAutoComplete.SelectedItem as string ?? ExpenseAutoComplete.Text ?? string.Empty;
        _viewModel.Expense.SearchExpenseIncomeName = text.Trim();
        if (string.IsNullOrWhiteSpace(text)) ExpenseAutoComplete.IsDropDownOpen = false;
    }

    private void OnExpenseAutoCompleteCompleted(object sender, EventArgs e)
    {
        var text = ExpenseAutoComplete.Text ?? string.Empty;
        _viewModel.Expense.SearchExpenseIncomeName = text.Trim();
    }

    private void UpdateSummaryLabels()
    {
        var sym = _viewModel.CurrencySymbol ?? _viewModel.Income.CurrencySymbol ?? "$";

        LblIncomeMonthly.Text = $"{sym}{_viewModel.TotalIncomeMonthly:N0}";
        LblExpenseMonthly.Text = $"{sym}{_viewModel.TotalExpenseMonthly:N0}";

        var net = _viewModel.NetMonthly;
        LblNetMonthly.Text = $"{(net >= 0 ? "+" : "-")}{sym}{Math.Abs(net):N0}";
        LblNetMonthly.TextColor = net >= 0
            ? Microsoft.Maui.Graphics.Colors.Green
            : Microsoft.Maui.Graphics.Colors.Red;

        LblNetYearly.Text = $"Yearly net: {_viewModel.NetYearlyFormatted}";

        // Affordability
        if (_viewModel.IsAffordabilityAvailable)
        {
            AffordabilityCard.IsVisible = true;
            LblAffordability.Text = _viewModel.Affordability;
            LblAffordabilityDesc.Text = _viewModel.AffordabilityTextDescription?.Trim();
        }

        // Show/hide no-data placeholder
        var hasData = _viewModel.HasData;
        SummaryNoData.IsVisible = !hasData;
        SummaryNetCard.IsVisible = hasData;
        SummaryChartCard.IsVisible = hasData;
        TopExpensesCard.IsVisible = hasData && _viewModel.TopExpenses.Count > 0;

        // Refresh top expenses list
        TopExpensesList.ItemsSource = null;
        TopExpensesList.ItemsSource = _viewModel.TopExpenses;
    }

    private void TabView_OnSelectionChanging(object? sender, SelectionChangingEventArgs e)
    {
        try
        {
            switch (e.Index)
            {
                case 0: // Income
                    _viewModel.Income.RefreshIncomePropertyChanged();
                    break;
                case 1: // Expenses
                    _viewModel.Expense.RefreshIncomePropertyChanged();
                    break;
                case 2: // Summary
                    _viewModel.RecalculateSummary();
                    UpdateSummaryLabels();
                    break;
                case 3: // Projection — premium gated
                    if (SharedServiceCore.IsTrialUser)
                    {
                        PremiumWindow.ShowPremiumBuyWindow = true;
                        e.Cancel = true;
                        return;
                    }
                    RefreshProjection();
                    break;
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void UpdateProjectionSummaryBar()
    {
        var sym = _viewModel.CurrencySymbol ?? "$";
        var incomeTotal = _viewModel.Income.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;
        var expenseTotal = _viewModel.Expense.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;

        ProjectionIncomeSpan.Text = $"{sym}{incomeTotal:N0}";
        ProjectionExpenseSpan.Text = $"{sym}{expenseTotal:N0}";
        ProjectionYearsSpan.Text = _viewModel.ProjectionYears.ToString();
        LblProjectionYears.Text = _viewModel.ProjectionYears.ToString();

        // Sync slider switch states to VM values
        SwitchIncludeExpenses.IsOn = _viewModel.Income.IncludeExpenses;
        SwitchIncludePropertyExpenses.IsOn = _viewModel.Income.IncludePropertyExpenses;
        SwitchExpensePropertyExpense.IsOn = _viewModel.Expense.ShowPropertyExpense;
        LblIncomeGrowthRate.Text = $"{_viewModel.Income.AnnualGrowthRatePercentage}%";
        LblExpenseGrowthRate.Text = $"{_viewModel.Expense.AnnualGrowthRatePercentage}%";
    }

    private void OnProjectionYearsSliderChanged(object sender, EventArgs e)
    {
        if (sender is not Syncfusion.Maui.Sliders.SfSlider slider) return;
        var years = (int)slider.Value;
        _viewModel.ProjectionYears = years;
        _viewModel.Income.TotalYearsToProject = years;
        _viewModel.Expense.TotalYearsToProject = years;
        LblProjectionYears.Text = years.ToString();
        ProjectionYearsSpan.Text = years.ToString();
        RefreshProjection();
    }

    private void OnProjectionYearsDecrease(object sender, EventArgs e)
    {
        if (_viewModel.ProjectionYears > 1)
            _viewModel.ProjectionYears -= 1;
    }

    private void OnProjectionYearsIncrease(object sender, EventArgs e)
    {
        if (_viewModel.ProjectionYears < 25)
            _viewModel.ProjectionYears += 1;
    }

    private void OnIncomeGrowthRateIncrease(object sender, EventArgs e)
    {
        _viewModel.Income.AnnualGrowthRate = Math.Min(_viewModel.Income.AnnualGrowthRate + 0.5, 20);
        LblIncomeGrowthRate.Text = $"{_viewModel.Income.AnnualGrowthRatePercentage}%";
        RefreshProjection();
    }

    private void OnIncomeGrowthRateDecrease(object sender, EventArgs e)
    {
        _viewModel.Income.AnnualGrowthRate = Math.Max(_viewModel.Income.AnnualGrowthRate - 0.5, 0);
        LblIncomeGrowthRate.Text = $"{_viewModel.Income.AnnualGrowthRatePercentage}%";
        RefreshProjection();
    }

    private void OnExpenseGrowthRateIncrease(object sender, EventArgs e)
    {
        _viewModel.Expense.AnnualGrowthRate = Math.Min(_viewModel.Expense.AnnualGrowthRate + 0.5, 20);
        LblExpenseGrowthRate.Text = $"{_viewModel.Expense.AnnualGrowthRatePercentage}%";
        RefreshProjection();
    }

    private void OnExpenseGrowthRateDecrease(object sender, EventArgs e)
    {
        _viewModel.Expense.AnnualGrowthRate = Math.Max(_viewModel.Expense.AnnualGrowthRate - 0.5, 0);
        LblExpenseGrowthRate.Text = $"{_viewModel.Expense.AnnualGrowthRatePercentage}%";
        RefreshProjection();
    }

    private void OnIncludeExpensesChanged(object sender, Syncfusion.Maui.Buttons.SwitchStateChangedEventArgs e)
    {
        _viewModel.Income.IncludeExpenses = e.NewValue == true;
        RefreshProjection();
    }

    private void OnIncludePropertyExpensesChanged(object sender, Syncfusion.Maui.Buttons.SwitchStateChangedEventArgs e)
    {
        _viewModel.Income.IncludePropertyExpenses = e.NewValue == true;
        RefreshProjection();
    }

    private void OnExpensePropertyExpenseChanged(object sender, Syncfusion.Maui.Buttons.SwitchStateChangedEventArgs e)
    {
        _viewModel.Expense.ShowPropertyExpense = e.NewValue == true;
        RefreshProjection();
    }

    private void RefreshProjection()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Detach before mutating
                ProjIncomeSeries.ItemsSource = null;
                ProjExpenseSeries.ItemsSource = null;
                IncomeDataPager.Source = null;
                ExpenseDataPager.Source = null;

                _viewModel.Income.UpdateProjectionData();
                _viewModel.Expense.UpdateProjectionData();
                _viewModel.Income.TriggerPropertyChangedOnProjectionTab();
                _viewModel.Expense.TriggerPropertyChangedOnProjectionTab();
                _viewModel.RecalculateProjection();
                UpdateProjectionSummaryBar();

                // Reattach with fresh data — ToList() forces a new reference so SfDataPager
                // recounts pages (it won't recount if the same list object is reassigned)
                ProjIncomeSeries.ItemsSource = _viewModel.ProjectionIncomeAxis;
                ProjExpenseSeries.ItemsSource = _viewModel.ProjectionExpenseAxis;
                IncomeDataPager.Source = _viewModel.Income.IncomeProjectList.ToList();
                ExpenseDataPager.Source = _viewModel.Expense.IncomeProjectList.ToList();
            }
            catch (Exception ex)
            {
                _errorHandlingService.HandleException(ex);
            }
        });
    }

}
