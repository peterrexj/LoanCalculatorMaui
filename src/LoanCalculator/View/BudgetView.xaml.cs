using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using Pj.Library;
using LoanCalculatorMaui.Extensions;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.Charts;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.TabView;

namespace LoanCalculatorMaui.View;

public partial class BudgetView : ContentPage
{
    private readonly BudgetViewModel _viewModel;
    private readonly LoanViewModel _loanViewModel;
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
        _loanViewModel = loanViewModel;
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

        // The Asset-costs tab (moved from LoanView) was compiled against LoanViewModel and its
        // bindings are unchanged, so its content root needs the LoanViewModel as BindingContext
        // rather than inheriting BudgetViewModel from the page.
        AssetCostsRoot.BindingContext = _loanViewModel;

        TopExpensesList.ItemsSource = _viewModel.TopExpenses;
    }

    protected override async void OnAppearing()
    {
        try
        {
        base.OnAppearing();

            // First visit does the expensive part: load both sub-VMs from disk, build the budget,
            // sort the asset list, then refresh the cross-tab affordability cache. Later visits
            // still redo the theme/affordability pass. Scrim it rather than leaving the user on a
            // page of stale figures with no sign anything is happening.
            BusyOverlay.Show(_hasInitialized ? "Updating…" : "Loading your budget…");

            // Yield one frame so the page skeleton renders before we do any work.
            await Task.Delay(100);

            if (!_hasInitialized)
            {
                _hasInitialized = true;
                PageHelper.PageIsLoading();

                // EnsureSubVmsLoadedAsync is a no-op if SplashPage already pre-warmed the data
                await _viewModel.EnsureSubVmsLoadedAsync();

                _viewModel.InitializeBudget();

                // Moved from LoanView.LoadDataSet — the Asset-costs list (lstEntry) is unsorted
                // without this.
                lstEntry.DataSource?.SortDescriptors.Clear();
                lstEntry.DataSource?.SortDescriptors.Add(new SortDescriptor()
                { PropertyName = "Name", Direction = ListSortDirection.Ascending });

                PageHelper.PageLoadingComplete();
            }

            // Re-fetch chart colors on EVERY appearance so a theme change applied on the
            // Settings tab is reflected when returning here (GetChartColors reads the live
            // Application.Current.Resources for the current theme).
            _viewModel.CustomChartColors = _themeHandler.GetChartColors();
            _viewModel.Income.CustomChartColors = _viewModel.CustomChartColors;
            _viewModel.Expense.CustomChartColors = _viewModel.CustomChartColors;

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

            // Refresh the cross-tab affordability cache BEFORE the summary reads it. The
            // Summary tab's verdict card gates on IsAffordabilityAvailable, which delegates to
            // LoanViewModel's *cached* HasIncomeExpensesRecorded flag — and CLAUDE.md's rule is
            // that every page reading affordability must refresh it on appearing. Only Loan and
            // What If did, so this card could never appear: it starts IsVisible="False" and the
            // gate was always false, which fails silently rather than showing a wrong number.
            await _loanViewModel.RefreshIncomeExpenseSummariesAsync(_viewModel.Income, _viewModel.Expense);

            _viewModel.RecalculateSummary();
            UpdateSummaryLabels();

            // Re-apply slider track/thumb colors so a theme change is reflected (Syncfusion
            // caches these from DynamicResource and won't update them live).
            LoanCalculatorMaui.Extensions.SliderThemeRefresher.Refresh(this);

#if IOS || MACCATALYST
            // Let taps reach the tab header strip — see SyncfusionIosTouchFix.
            LoanCalculatorMaui.Extensions.SyncfusionIosTouchFix.ApplyToTabView(tabView);
#endif
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            BusyOverlay.Hide();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Clears the navigation scrim raised by OnSeeProjectionTapped. Left set, it would still
        // be up when the user came back to this tab — OnAppearing's finally would eventually
        // clear it, but not before a visible flash of a greyed-out page.
        BusyOverlay.Hide();
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

    private async void OnBudgetIncomeEdit(object sender, EventArgs e)
    {
        try
        {
            if (sender is not BindableObject source || source.BindingContext is not IncomeExpense row) return;
            var entry = _viewModel.Income.TransactionRecords?.Get(row.Id)?.DeepClone();
            if (entry == null) return;
            _viewModel.Income.ShowValidationErrors = false;
            _viewModel.Income.IncomeExpenseEntry = entry;
            _viewModel.Income.IncomeExpenseFrequencySelectedIndex = entry.Frequency.ToString();
            await LaunchBudgetIncomeEntryAsync(row.Id);
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }


    // ── Expense handlers ──────────────────────────────────────────────────
    private async void OnBudgetExpenseEdit(object sender, EventArgs e)
    {
        try
        {
            if (sender is not BindableObject source || source.BindingContext is not IncomeExpense row) return;
            var entry = _viewModel.Expense.TransactionRecords?.Get(row.Id)?.DeepClone();
            if (entry == null) return;
            _viewModel.Expense.ShowValidationErrors = false;
            _viewModel.Expense.IncomeExpenseEntry = entry;
            _viewModel.Expense.IncomeExpenseFrequencySelectedIndex = entry.Frequency.ToString();
            await LaunchBudgetExpenseEntryAsync(row.Id);
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }


    private async void OnAddIncomeFab_Clicked(object sender, EventArgs e)
    {
        try
        {
            _viewModel.Income.ResetTransactionEntryData();
            await LaunchBudgetIncomeEntryAsync();
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private async void OnAddExpenseFab_Clicked(object sender, EventArgs e)
    {
        try
        {
            _viewModel.Expense.ResetTransactionEntryData();
            await LaunchBudgetExpenseEntryAsync();
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private bool _isBudgetIncomeEntryOpen;

    private async Task LaunchBudgetIncomeEntryAsync(Guid? editingId = null)
    {
        if (_isBudgetIncomeEntryOpen) return;
        _isBudgetIncomeEntryOpen = true;

        try
        {
            var page = ServiceLocator.GetService<TransactionEntryPage>();
            if (page is null) return;

            page.Configure(new TransactionEntryPage.Config
            {
                Target = _viewModel.Income,
                TitleNoun = "Income",
                NameAutomationId = "BudgetIncomeNameEntry",
                AmountAutomationId = "BudgetIncomeAmountEntry",
                SaveAutomationId = "BudgetIncomeSaveButton",
                DeleteAutomationId = "BudgetIncomeDeleteButton",
                // Null when adding, which hides the button. This is the only delete path for
                // income entries since the lists stopped carrying a per-row delete.
                OnDelete = editingId is Guid id
                    ? () =>
                    {
                        _viewModel.Income.TransactionRecords?.Delete(id);
                        _viewModel.Income.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel.Income));
                        SharedServiceCore.MarkIncomeDirty();
                        _viewModel.Income.RefreshIncomePropertyChanged();
                    }
                    : null,
                OnSaved = () =>
                {
                    _viewModel.Income.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel.Income));
                    SharedServiceCore.MarkIncomeDirty();
                    _viewModel.Income.RefreshIncomePropertyChanged();
                }
            });

            await Navigation.PushModalAsync(page);
            var committed = await page.Completion;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            _isBudgetIncomeEntryOpen = false;
        }
    }

    private bool _isBudgetExpenseEntryOpen;

    private async Task LaunchBudgetExpenseEntryAsync(Guid? editingId = null)
    {
        if (_isBudgetExpenseEntryOpen) return;
        _isBudgetExpenseEntryOpen = true;

        try
        {
            var page = ServiceLocator.GetService<TransactionEntryPage>();
            if (page is null) return;

            page.Configure(new TransactionEntryPage.Config
            {
                Target = _viewModel.Expense,
                TitleNoun = "Expense",
                NameAutomationId = "BudgetExpenseNameEntry",
                AmountAutomationId = "BudgetExpenseAmountEntry",
                SaveAutomationId = "BudgetExpenseSaveButton",
                DeleteAutomationId = "BudgetExpenseDeleteButton",
                OnDelete = editingId is Guid id
                    ? () =>
                    {
                        _viewModel.Expense.TransactionRecords?.Delete(id);
                        _viewModel.Expense.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel.Expense));
                        SharedServiceCore.MarkExpenseDirty();
                        _viewModel.Expense.RefreshIncomePropertyChanged();
                    }
                    : null,
                OnSaved = () =>
                {
                    _viewModel.Expense.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel.Expense));
                    SharedServiceCore.MarkExpenseDirty();
                    _viewModel.Expense.RefreshIncomePropertyChanged();
                }
            });

            await Navigation.PushModalAsync(page);
            var committed = await page.Completion;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            _isBudgetExpenseEntryOpen = false;
        }
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

    // ── Asset costs (moved from LoanView — "Running Cost" tab) ─────────────

    private void autoComplete_Completed(object sender, EventArgs e) { }

    private void autoComplete_SelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        try
        {
            var text = autoComplete.SelectedItem as string ?? autoComplete.Text ?? string.Empty;
            _loanViewModel.SearchExpenseIncomeName = text.Trim();
            if (string.IsNullOrWhiteSpace(text))
                autoComplete.IsDropDownOpen = false;
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
            // Reads the row from BindingContext rather than parsing a Guid out of the button's
            // AutomationId: the tap now comes from the row Border, and an id-as-payload trick
            // would break the moment the automation id is used for what it is actually for.
            if (sender is not BindableObject source || source.BindingContext is not IncomeExpense row) return;
            _loanViewModel.ShowValidationErrors = false;
            _loanViewModel.IncomeExpenseEntry = _loanViewModel.TransactionRecords.Get(row.Id).DeepClone();
            _loanViewModel.IncomeExpenseFrequencySelectedIndex = _loanViewModel.IncomeExpenseEntry.Frequency.ToString();
            _loanViewModel.RefreshExpenseTabPropertyChanged();
            _ = LaunchAssetExpenseEntryAsync(row.Id);
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }


    // FAB drag state for Expense on the Asset-costs tab
    private double _assetFabY;

    private void OnAddAssetExpenseFab_Clicked(object sender, EventArgs e)
    {
        _loanViewModel.ResetTransactionEntryData();
        _ = LaunchAssetExpenseEntryAsync();
    }

    private void OnAssetFabPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        const double maxUp = -112;
        const double maxDown = 0;
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                FabAddAssetExpense.TranslationY = Math.Clamp(_assetFabY + e.TotalY, maxUp, maxDown);
                break;
            case GestureStatus.Completed:
                _assetFabY = FabAddAssetExpense.TranslationY;
                break;
        }
    }

    // ── Asset Expense Entry page ───────────────────────────────────────────
    private bool _isAssetExpenseOpen;

    private async Task LaunchAssetExpenseEntryAsync(Guid? editingId = null)
    {
        if (_isAssetExpenseOpen) return;
        _isAssetExpenseOpen = true;

        try
        {
            var page = ServiceLocator.GetService<TransactionEntryPage>();
            if (page is null) return;

            page.Configure(new TransactionEntryPage.Config
            {
                Target = _loanViewModel,
                TitleNoun = "Asset Expense",
                NameAutomationId = "AssetExpenseNameEntry",
                AmountAutomationId = "AssetExpenseAmountEntry",
                SaveAutomationId = "AssetExpenseSaveButton",
                DeleteAutomationId = "AssetExpenseDeleteButton",
                // Asset costs now matches Income and Expenses: the row opens this page and
                // delete lives here, so TransactionEntryPage really is the only delete path
                // across all three Budget lists rather than just two of them.
                OnDelete = editingId is Guid id
                    ? () =>
                    {
                        _loanViewModel.TransactionRecords.Delete(id);
                        _loanViewModel.ResetTransactionEntryData();
                        _loanViewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_loanViewModel));
                        _loanViewModel.RefreshExpenseTabPropertyChanged();
                    }
                    : null,
                OnSaved = () =>
                {
                    _loanViewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_loanViewModel));
                    _loanViewModel.RefreshExpenseTabPropertyChanged();
                }
            });

            await Navigation.PushModalAsync(page);
            var committed = await page.Completion;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            _isAssetExpenseOpen = false;
        }
    }

    /// <summary>
    /// Takes the user to What If ▸ Projection, which used to be a Budget tab.
    /// </summary>
    /// <remarks>
    /// Requests the Projection sub-tab explicitly: Shell keeps the What If page alive, so a bare
    /// navigation lands on whichever sub-tab was last selected.
    /// </remarks>
    private async void OnSeeProjectionTapped(object sender, TappedEventArgs e)
    {
        try
        {
            // Shown on THIS page: Shell swaps pages before What If's OnAppearing runs, so there
            // is a gap where the old page is still up and nothing says a tap registered. What If
            // raises its own scrim on arrival; this covers the hop itself. Hidden in OnDisappearing
            // rather than here, so it survives until the swap actually happens.
            BusyOverlay.Show("Opening projection…");
            WhatIfView.PendingTabIndex = 2; // Projection
            await Shell.Current.GoToAsync("//WhatIf");
        }
        catch (Exception ex)
        {
            BusyOverlay.Hide();
            _errorHandlingService.HandleException(ex);
        }
    }

    /// <summary>
    /// Refreshes the cross-tab affordability cache, then redraws the summary from it.
    /// </summary>
    /// <remarks>
    /// Guarded: this is reached from a fire-and-forget call in a sync event handler, so an
    /// escaping exception would have no caller to catch it.
    /// </remarks>
    private async Task RefreshAffordabilityThenSummaryAsync()
    {
        try
        {
            await _loanViewModel.RefreshIncomeExpenseSummariesAsync(_viewModel.Income, _viewModel.Expense);
            _viewModel.RecalculateSummary();
            UpdateSummaryLabels();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    /// <summary>
    /// Resolves a theme colour key, falling back to the theme foreground. Used where a colour has
    /// to be set from code and so cannot be a <c>DynamicResource</c>.
    /// </summary>
    private static Color ThemeColor(string key)
    {
        var resources = Application.Current?.Resources;

        if (resources is not null && resources.TryGetValue(key, out var found) && found is Color color)
            return color;

        return resources is not null
            && resources.TryGetValue("LoanAppDefaultForegroundColor", out var fallback)
            && fallback is Color fallbackColor
                ? fallbackColor
                : Colors.Grey;
    }

    private void UpdateSummaryLabels()
    {
        // Affordability
        if (_viewModel.IsAffordabilityAvailable)
        {
            AffordabilityCard.IsVisible = true;
            LblAffordability.Text = $"{_viewModel.AffordabilityCurrencySymbol}{_viewModel.Affordability}";

            // Theme colours, not Colors.Red / Colors.Green. The raw pair ignored the theme
            // entirely — pure #FF0000 against a dark card, and a green that no theme uses —
            // and red/green is the one pairing colour-blind users cannot separate. These two
            // keys are the app's negative and positive tokens.
            LblAffordability.TextColor = ThemeColor(
                _viewModel.AffordabilityCurrencySymbol.StartsWith("-")
                    ? "LoanAppNegativeValueColor"
                    : "LoanAppChartColor1");

            LblAffordabilityDesc.Text = _viewModel.AffordabilityTextDescription?.Trim();
        }
        else
        {
            AffordabilityCard.IsVisible = false;
        }

        // The "not counted" banner. Names the repayment at the frequency the user actually
        // selected rather than assuming monthly — TermPaymentRoundedWithComma is the payment for
        // the selected frequency, so hard-coding "/mo" would misreport a weekly or fortnightly
        // loan. Hidden until there is a loan to talk about.
        var sym = _viewModel.CurrencySymbol ?? _viewModel.Income.CurrencySymbol ?? "$";
        var repayment = _loanViewModel.TermPaymentRoundedWithComma;
        var hasLoan = !string.IsNullOrWhiteSpace(repayment) && repayment != "0";
        LoanNotCountedBanner.IsVisible = hasLoan;
        if (hasLoan)
        {
            var freq = _loanViewModel.RepaymentFrequencySelected?.ToLowerInvariant();
            LblLoanNotCounted.Text =
                $"Your loan repayment of {sym}{repayment}"
                + (string.IsNullOrWhiteSpace(freq) ? "" : $" {freq}")
                + " is not counted above. See What If for what's left once it is.";
        }

        // Show/hide no-data placeholder and content
        var hasData = _viewModel.HasData;
        SummaryNoData.IsVisible = !hasData;
        SummaryHeroCard.IsVisible = hasData;
        SummaryThreeBoxes.IsVisible = hasData;
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
                    // The Expenses summary bar's third box now shows NET for the month rather
                    // than raw income (it is an expenses screen). Net lives on BudgetViewModel
                    // and is only notified by RecalculateSummary, so without this the box would
                    // show whatever the net was when the page was last entered.
                    _viewModel.RecalculateSummary();
                    break;
                case 2: // Summary — premium gated
                    if (SharedServiceCore.IsTrialUser)
                    {
                        PremiumWindow.ShowPremiumBuyWindow = true;
                        e.Cancel = true;
                        return;
                    }
                    // Refresh the affordability cache here too, not just in OnAppearing. Adding
                    // income on the Income tab and switching straight to Summary never leaves the
                    // page, so OnAppearing does not run again and the verdict would reflect the
                    // state from whenever the page was last entered. Fire-and-forget because this
                    // is a sync event handler; the in-memory read is cheap and the continuation
                    // re-runs the labels.
                    _ = RefreshAffordabilityThenSummaryAsync();
                    break;
                // Projection moved to What If, so Asset costs shifted from index 4 to 3.
                case 3: // Asset costs — premium gated (was Running Cost on LoanView)
                    if (SharedServiceCore.IsTrialUser)
                    {
                        PremiumWindow.ShowPremiumBuyWindow = true;
                        e.Cancel = true;
                        return;
                    }
                    _loanViewModel.RefreshExpenseTabPropertyChanged();
                    break;
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }


}
