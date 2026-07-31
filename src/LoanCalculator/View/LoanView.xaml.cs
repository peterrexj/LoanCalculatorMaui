using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Pdf;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Extensions;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.Charts;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.TabView;

namespace LoanCalculatorMaui.View;

public partial class LoanView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private LoanViewModel _viewModel;
    private readonly IThemeHandler _themeHandler;
    private readonly IncomeViewModel _incomeViewModel;
    private readonly ExpenseViewModel _expenseViewModel;

    public LoanView(
        IErrorHandlingService errorHandlingService,
        LoanViewModel viewModel,
        IncomeViewModel incomeViewModel,
        ExpenseViewModel expenseViewModel,
        IThemeHandler themeHandler)
    {
        InitializeComponent();

        _errorHandlingService = errorHandlingService;
        _themeHandler = themeHandler;
        _incomeViewModel = incomeViewModel;
        _expenseViewModel = expenseViewModel;

        _viewModel = viewModel;
        _viewModel.IsBusy = true;
        _viewModel.IsUpdating = true;
        _viewModel.IsActive = false;
        _viewModel.IsPageBusy = true;
        _viewModel.SetWizardPeerViewModels(_incomeViewModel, _expenseViewModel);

        BindingContext = _viewModel;

        // Subscribe once — auto-show wizard after the disclaimer is accepted on first launch
        SharedServiceCore.DisclaimerAccepted += OnDisclaimerAccepted;
    }

    private void OnDisclaimerAccepted(object? sender, EventArgs e)
    {
        SharedServiceCore.DisclaimerAccepted -= OnDisclaimerAccepted;
        if (!SharedServiceCore.ShouldShowWizard()) return;
        SharedServiceCore.SetWizardShown();
        // Small delay so the disclaimer popup fully closes before the wizard appears
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(600);
            if (_hasLoadedOnce) OnWizardFab_Clicked(this, EventArgs.Empty);
            else _viewModel.IsWizardStep1Visible = true;
        });
    }

    private bool _hasLoadedOnce;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
        SharedServiceCore.MarkLoanDirty();
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            await Task.Delay(100);

            if (!_hasLoadedOnce)
            {
                _hasLoadedOnce = true;
                await LoadDataSet();
            }
            else if (SharedServiceCore.IsIncomeDirty || SharedServiceCore.IsExpenseDirty)
            {
                await RefreshCrossTabSummaries();
            }
            else
            {
                // Re-load Australian mode in case it was changed in Settings
                _viewModel.LoadAustralianModeSetting();
                _viewModel.TriggerPropertyChangedOnPropertyTab();
            }

            // Re-fetch chart colors on every appearance so a theme change (applied on the
            // Settings tab) is reflected when returning to the Loan charts.
            if (_hasLoadedOnce)
                _viewModel.CustomChartColors = _themeHandler.GetChartColors();

            // Re-apply slider colors after a possible theme change (Syncfusion caches these).
            LoanCalculatorMaui.Extensions.SliderThemeRefresher.Refresh(this);

            if (SharedServiceCore.IsTrialUser)
            {
                await ServiceLocator.GetService<IInAppPurchaseService>().CheckPendingPurchasesAsync(isSilentMode: true);
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            _viewModel.IsUpdating = false;
            _viewModel.IsBusy = false;
            _viewModel.IsActive = true;
            _viewModel.IsPageBusy = false;
        }
    }

    private async Task RefreshCrossTabSummaries()
    {
        _viewModel.ExpenseSummary = _expenseViewModel.HasInitialized
            ? _expenseViewModel
            : await SharedServiceCore.GetExpenseSummaryAsync();

        _viewModel.IncomeSummary = _incomeViewModel.HasInitialized
            ? _incomeViewModel
            : await SharedServiceCore.GetIncomeSummaryAsync();

        _viewModel.HasIncomeExpensesRecorded =
            _viewModel.ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 &&
            _viewModel.IncomeSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;

        SharedServiceCore.ClearIncomeDirty();
        SharedServiceCore.ClearExpenseDirty();

        _viewModel.TriggerPropertyChangedOnPropertyTab();
        _viewModel.TriggerPropertyChangedOnPageLevel();
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

            // Use the in-memory singleton ViewModels (already populated from disk on their
            // own tab's first visit). Fall back to disk only if not yet initialized this session.
            Task<ExpenseViewModel> expenseSummaryTask = _expenseViewModel.HasInitialized
                ? Task.FromResult(_expenseViewModel)
                : SharedServiceCore.GetExpenseSummaryAsync();
            Task<IncomeViewModel> incomeSummaryTask = _incomeViewModel.HasInitialized
                ? Task.FromResult(_incomeViewModel)
                : SharedServiceCore.GetIncomeSummaryAsync();
            var lstSourceTask = Task.Run(() =>
            {
                lstEntry.DataSource?.SortDescriptors.Clear();
                lstEntry.DataSource?.SortDescriptors.Add(new SortDescriptor()
                { PropertyName = "Name", Direction = ListSortDirection.Ascending });
            });


            await Task.WhenAll(viewModelInitializeTask, expenseSummaryTask, incomeSummaryTask, chartColorsTask, lstSourceTask);

            _viewModel.PdfGenerator = new PdfInsightsGenerator(ServiceLocator.GetService<IFontUnicodeProvider>());

            _viewModel.CustomChartColors = chartColorsTask.Result;
            _viewModel.ExpenseSummary = expenseSummaryTask.Result;
            _viewModel.IncomeSummary = incomeSummaryTask.Result;

            // Ensure peer VM collections are populated for the wizard HasValue checks,
            // even if those tabs haven't been visited yet this session.
            if (!_incomeViewModel.HasInitialized && _viewModel.IncomeSummary?.TransactionRecords != null)
                _incomeViewModel.TransactionRecords = _viewModel.IncomeSummary.TransactionRecords;
            if (!_expenseViewModel.HasInitialized && _viewModel.ExpenseSummary?.TransactionRecords != null)
                _expenseViewModel.TransactionRecords = _viewModel.ExpenseSummary.TransactionRecords;
            _viewModel.HasIncomeExpensesRecorded =
                _viewModel.ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 &&
                _viewModel.IncomeSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;

            _viewModel.CurrencySymbol = Helper.CurrencySymbol;

            _viewModel.MarkInitializationComplete();

            // Load Australian mode setting — also cascades to stamp duty
            _viewModel.LoadAustralianModeSetting();

            _viewModel.IsUpdating = false;

            // Clear the loading flag BEFORE triggers so TriggerPropertyChanged* methods
            // don't no-op and ScheduleSave is allowed to fire.
            PageHelper.PageLoadingComplete();

            _viewModel.TriggerSegmentCollectionsRefresh();

            if (requiresDefault)
            {
                _viewModel.AddDefaultValues();
            }

            if (SharedServiceCore.IsTrialUser && await SharedServiceCore.IsCurrentDayAsync() == false)
            {
                // Trial users: reset expense entries on new day — but preserve loan inputs.
                _viewModel.AddDefaultToExpenses();
                try
                {
                    if (await SharedServiceCore.HasAlertedUserForDataWipeAsync() == false)
                    {
                        if (SharedServiceCore.ShouldShowAppLaunchDisclaimer() == false)
                        {
                            var showPlan = await SharedServiceCore.AlertUserForDataWipe();
                            if (showPlan)
                            {
                                PremiumWindow.ShowPremiumBuyWindow = true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _errorHandlingService.HandleException(e);
                }
            }

            // Trigger methods fire OnPropertyChanged — must run on the UI thread.
            _viewModel.TriggerOneTimeUpdateOnPage();
            _viewModel.TriggerPropertyChangedOnPropertyTab();
            _viewModel.RefreshExpenseTabPropertyChanged();
            _viewModel.TriggerPropertyChangedOnPageLevel();

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

    private void autoComplete_Completed(object sender, EventArgs e) { }

    private void autoComplete_SelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        try
        {
            var text = autoComplete.SelectedItem as string ?? autoComplete.Text ?? string.Empty;
            _viewModel.SearchExpenseIncomeName = text.Trim();
            if (string.IsNullOrWhiteSpace(text))
                autoComplete.IsDropDownOpen = false;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }



    private void AddNewIncome_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Surface inline validation errors only after a submit attempt.
            _viewModel.ShowValidationErrors = true;
            if (_viewModel.HasErrorIncomeDescription || _viewModel.HasErrorIncomeAmount) return;
            if (_viewModel.AddOrUpdateEntryFromView() == false) return;
            _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
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
            _viewModel.ShowValidationErrors = false;
            _viewModel.IncomeExpenseEntry = _viewModel.TransactionRecords.Get(Guid.Parse(button.AutomationId)).DeepClone();
            _viewModel.IncomeExpenseFrequencySelectedIndex = _viewModel.IncomeExpenseEntry.Frequency.ToString();
            _viewModel.RefreshExpenseTabPropertyChanged();
            _viewModel.IsAddFormVisible = true;
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
            _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
            _viewModel.RefreshExpenseTabPropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    // FAB drag state for Expense on Asset tab
    private double _assetFabY;

    private void OnAddAssetExpenseFab_Clicked(object sender, EventArgs e)
    {
        _viewModel.ResetTransactionEntryData();
        _viewModel.IsAddFormVisible = true;
    }

    // ── Quick Setup Wizard ────────────────────────────────────────────────────

    private bool _wizardSuppressTextChanged;

    private void OnWizardEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_wizardSuppressTextChanged || sender is not Entry entry || entry.IsReadOnly) return;

        var digits = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
        if (!double.TryParse(digits, out var val)) val = 0;
        var formatted = val > 0 ? $"{val:N0}" : string.Empty;

        _wizardSuppressTextChanged = true;
        entry.Text = formatted;
        entry.CursorPosition = formatted.Length;
        _wizardSuppressTextChanged = false;

        if (!_viewModel.HasInitialized) return;
        switch (entry.AutomationId)
        {
            case "WizardAsset":   _viewModel.WizardAssetText      = formatted; break;
            case "WizardDeposit": _viewModel.WizardDepositText     = formatted; break;
            case "WizardUpfront": _viewModel.WizardUpfrontText     = formatted; break;
            case "WizardRunning": _viewModel.WizardRunningCostText = formatted; break;
            case "WizardIncome":  _viewModel.WizardIncomeText      = formatted; break;
            case "WizardExpense": _viewModel.WizardExpenseText     = formatted; break;
        }
    }

    private void OnWizardFab_Clicked(object sender, EventArgs e)
    {
        try
        {
            _viewModel.WizardAssetText = _viewModel.PropertyAmount > 0
                ? $"{_viewModel.PropertyAmount:N0}" : string.Empty;
            _viewModel.WizardDepositText = _viewModel.DepositAmountDirectInput > 0
                ? $"{_viewModel.DepositAmountDirectInput:N0}" : string.Empty;
            _viewModel.WizardUpfrontText = (_viewModel.HomeLoanInfo?.OtherExpenseTotalAmount ?? 0) > 0
                ? $"{_viewModel.HomeLoanInfo.OtherExpenseTotalAmount:N0}" : string.Empty;

            _viewModel.TransactionRecords?.SumUpData();
            var runningTotal = _viewModel.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;
            _viewModel.WizardRunningCostText = runningTotal > 0
                ? $"{runningTotal:N0}" : string.Empty;

            PrepopulateWizardStep2();
            _viewModel.IsWizardStep1Visible = true;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(80);
                _viewModel.NotifyWizardPropertiesChanged();
            });
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private void PrepopulateWizardStep2()
    {
        _incomeViewModel.TransactionRecords?.SumUpData();
        _expenseViewModel.TransactionRecords?.SumUpData();

        var totalIncome = _incomeViewModel.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;
        _viewModel.WizardIncomeText = totalIncome > 0 ? $"{totalIncome:N0}" : string.Empty;

        var totalExpense = _expenseViewModel.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;
        _viewModel.WizardExpenseText = totalExpense > 0 ? $"{totalExpense:N0}" : string.Empty;
    }

    private void OnWizardCancel(object sender, EventArgs e)
    {
        _viewModel.IsWizardStep1Visible = false;
        _viewModel.IsWizardStep2Visible = false;
        _viewModel.IsWizardStep3Visible = false;
    }

    private void OnWizardStep1Next(object sender, EventArgs e)
    {
        try
        {
            ApplyWizardStep1();
            _viewModel.IsWizardStep1Visible = false;
            _viewModel.IsWizardStep2Visible = true;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(80);
                _viewModel.NotifyWizardPropertiesChanged();
            });
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private void OnWizardStep1Skip(object sender, EventArgs e)
    {
        _viewModel.IsWizardStep1Visible = false;
        _viewModel.IsWizardStep2Visible = true;
        MainThread.BeginInvokeOnMainThread(async () => { await Task.Delay(80); _viewModel.NotifyWizardPropertiesChanged(); });
    }

    private void OnWizardStep2Back(object sender, EventArgs e)
    {
        _viewModel.IsWizardStep2Visible = false;
        _viewModel.IsWizardStep1Visible = true;
        MainThread.BeginInvokeOnMainThread(async () => { await Task.Delay(80); _viewModel.NotifyWizardPropertiesChanged(); });
    }

    private void OnWizardStep2Next(object sender, EventArgs e)
    {
        try
        {
            _viewModel.IsWizardStep2Visible = false;
            _viewModel.IsWizardStep3Visible = true;
            PrepopulateWizardStep2();
            MainThread.BeginInvokeOnMainThread(async () => { await Task.Delay(80); _viewModel.NotifyWizardPropertiesChanged(); });
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private void OnWizardStep3Back(object sender, EventArgs e)
    {
        _viewModel.IsWizardStep3Visible = false;
        _viewModel.IsWizardStep2Visible = true;
        MainThread.BeginInvokeOnMainThread(async () => { await Task.Delay(80); _viewModel.NotifyWizardPropertiesChanged(); });
    }

    private void OnWizardCalculate(object sender, EventArgs e)
    {
        try
        {
            ApplyWizardStep1();
            ApplyWizardStep2();
            _viewModel.IsWizardStep3Visible = false;

            // The wizard added income/expense straight into the peer VMs. Re-point the loan
            // VM's summaries at them and recompute HasIncomeExpensesRecorded so the Affordability
            // box and IsAffordabilityAvailable flip on — they were evaluated once at load (empty).
            _viewModel.IncomeSummary = _incomeViewModel;
            _viewModel.ExpenseSummary = _expenseViewModel;
            _incomeViewModel.TransactionRecords?.SumUpData();
            _expenseViewModel.TransactionRecords?.SumUpData();
            _viewModel.HasIncomeExpensesRecorded =
                _expenseViewModel.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 &&
                _incomeViewModel.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;

            _viewModel.TriggerPropertyChangedOnPropertyTab();
            _viewModel.RefreshExpenseTabPropertyChanged();
            _viewModel.RefreshInsightsTabPropertyChanged();
            SharedServiceCore.MarkIncomeDirty();
            SharedServiceCore.MarkExpenseDirty();
            _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
            TabView.SelectedIndex = 0;
        }
        catch (Exception ex) { _errorHandlingService.HandleException(ex); }
    }

    private void ApplyWizardStep1()
    {
        if (!_viewModel.HasInitialized) return;

        if (double.TryParse(_viewModel.WizardAssetText?.Replace(",", ""), out var asset) && asset > 0)
            _viewModel.PropertyAmount = asset;

        if (double.TryParse(_viewModel.WizardDepositText?.Replace(",", ""), out var deposit) && deposit > 0)
            _viewModel.DepositAmountDirectInput = deposit;

        if (!_viewModel.WizardUpfrontHasValue)
        {
            if (double.TryParse(_viewModel.WizardUpfrontText?.Replace(",", ""), out var upfront) && upfront > 0)
                _viewModel.OtherExpenses = upfront;
        }

        if (!_viewModel.WizardRunningCostHasValue)
        {
            if (double.TryParse(_viewModel.WizardRunningCostText?.Replace(",", ""), out var running) && running > 0)
            {
                _viewModel.TransactionRecords ??= new Incomes { IncomeExpenseEntries = [] };
                _viewModel.TransactionRecords.Add("Running Costs", running, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
                _viewModel.RefreshExpenseTabPropertyChanged();
            }
        }
    }

    private void ApplyWizardStep2()
    {
        if (!_viewModel.WizardIncomeHasValue)
        {
            if (double.TryParse(_viewModel.WizardIncomeText?.Replace(",", ""), out var income) && income > 0)
            {
                _incomeViewModel.TransactionRecords ??= new Incomes { IncomeExpenseEntries = [] };
                _incomeViewModel.TransactionRecords.Add("Total Income", income, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
                _incomeViewModel.TransactionRecords.SumUpData();
                SharedServiceCore.SaveData(_incomeViewModel);
            }
        }

        if (!_viewModel.WizardExpenseHasValue)
        {
            if (double.TryParse(_viewModel.WizardExpenseText?.Replace(",", ""), out var expense) && expense > 0)
            {
                _expenseViewModel.TransactionRecords ??= new Incomes { IncomeExpenseEntries = [] };
                _expenseViewModel.TransactionRecords.Add("Total Expenses", expense, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
                _expenseViewModel.TransactionRecords.SumUpData();
                SharedServiceCore.SaveData(_expenseViewModel);
            }
        }
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

    // ── Upfront Costs popup ─────────────────────────────────────────────────
    private readonly Dictionary<string, Entry>  _upfrontEntries = new();
    private readonly Dictionary<string, Label>  _upfrontLabels  = new();

    private void OnUpfrontCostsTapped(object sender, TappedEventArgs e)
    {
        _viewModel.IsUpfrontInputVisible = true;
    }

    private void OnUpfrontDone(object sender, EventArgs e)
    {
        _viewModel.IsUpfrontInputVisible = false;
        _viewModel.TriggerPropertyChangedOnPropertyTab();
    }

    private void OnUpfrontEntryLoaded(object sender, EventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrEmpty(entry.AutomationId))
        {
            _upfrontEntries[entry.AutomationId] = entry;
            PopulateUpfrontEntry(entry.AutomationId);
        }
    }

    private void OnUpfrontLabelLoaded(object sender, EventArgs e)
    {
        if (sender is Label lbl && !string.IsNullOrEmpty(lbl.AutomationId))
            _upfrontLabels[lbl.AutomationId] = lbl;
    }

    private void OnUpfrontControlLoaded(object sender, EventArgs e) { /* segment wired via binding */ }

    private void OnUpfrontEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry entry) return;
        var id = entry.AutomationId;
        FormatEntry(entry, e.NewTextValue,
            formatted => { /* no formatted label for upfront */ },
            words     =>
            {
                var wordsId = id.Replace("Entry", "Words");
                if (_upfrontLabels.TryGetValue(wordsId, out var lbl)) lbl.Text = words;
            },
            val       => SetUpfrontValue(id, val));
    }

    private void SetUpfrontValue(string automationId, double val)
    {
        if (!_viewModel.HasInitialized) return;
        switch (automationId)
        {
            case "StampDutyEntry":     _viewModel.StampDuty         = val; break;
            case "MortgageEntry":      _viewModel.MortgageCharges   = val; break;
            case "ConveyanceEntry":    _viewModel.ConveyancerFee    = val; break;
            case "BankFeeEntry":       _viewModel.BankFee           = val; break;
            case "InspectionEntry":    _viewModel.InspectionFee     = val; break;
            case "OtherExpensesEntry": _viewModel.OtherExpenses     = val; break;
        }
    }

    private void PopulateUpfrontEntry(string automationId)
    {
        if (!_upfrontEntries.TryGetValue(automationId, out var entry)) return;
        var val = automationId switch
        {
            "StampDutyEntry"     => _viewModel.StampDuty,
            "MortgageEntry"      => _viewModel.MortgageCharges,
            "ConveyanceEntry"    => _viewModel.ConveyancerFee,
            "BankFeeEntry"       => _viewModel.BankFee,
            "InspectionEntry"    => _viewModel.InspectionFee,
            "OtherExpensesEntry" => _viewModel.OtherExpenses,
            _                    => 0.0
        };
        _suppressTextChanged = true;
        entry.Text = val > 0 ? $"{val:N0}" : string.Empty;
        _suppressTextChanged = false;

        var wordsId = automationId.Replace("Entry", "Words");
        if (_upfrontLabels.TryGetValue(wordsId, out var lbl))
            lbl.Text = val > 0 ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)Math.Round(val)) : string.Empty;
    }

    private void OnAssetValueTapped(object sender, TappedEventArgs e)
    {
        UpdateQuickInputLabels();
        _viewModel.PropertyChanged += OnViewModelPropertyChangedForPopup;
        _viewModel.IsQuickInputVisible = true;
    }

    // Quick Input live display — backing fields since labels are inside DataTemplate
    private Label? _lblAssetFormatted;
    private Label? _lblAssetWords;
    private Label? _lblDepositFormatted;
    private Label? _lblDepositWords;
    private Label? _lblLoanFormatted;
    private Label? _lblLoanWords;
    private Entry? _entryAssetValue;
    private Entry? _entryDepositAmount;
    private Entry? _entryLoanAmount;
    private bool _suppressTextChanged;

    private void OnDepositTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry entry) return;
        FormatEntry(entry, e.NewTextValue,
            formatted => { if (_lblDepositFormatted != null) _lblDepositFormatted.Text = formatted; },
            words     => { if (_lblDepositWords     != null) _lblDepositWords.Text     = words; },
            val       => { if (_viewModel.HasInitialized) _viewModel.DepositAmountDirectInput = val; });
    }

    private void OnAssetTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry entry) return;
        FormatEntry(entry, e.NewTextValue,
            formatted => { if (_lblAssetFormatted != null) _lblAssetFormatted.Text = formatted; },
            words     => { if (_lblAssetWords     != null) _lblAssetWords.Text     = words; },
            val       => { if (_viewModel.HasInitialized) _viewModel.PropertyAmount = val; });
    }

    private void OnLoanTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry entry) return;
        FormatEntry(entry, e.NewTextValue,
            formatted => { if (_lblLoanFormatted != null) _lblLoanFormatted.Text = formatted; },
            words     => { if (_lblLoanWords     != null) _lblLoanWords.Text     = words; },
            val       => { if (_viewModel.HasInitialized) _viewModel.LoanAmountDirectInput = val; });
    }

    private void FormatEntry(Entry entry, string rawText,
        Action<string> setFormatted, Action<string> setWords, Action<double> setViewModel)
    {
        // Strip everything except digits
        var digits = new string(rawText.Where(char.IsDigit).ToArray());
        if (!double.TryParse(digits, out var val)) val = 0;

        // Format with commas
        var formatted = val > 0 ? $"{val:N0}" : string.Empty;
        var words     = val > 0
            ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)val)
            : string.Empty;

        // Update text without re-triggering TextChanged
        _suppressTextChanged = true;
        var cursorPos = Math.Min(entry.CursorPosition, formatted.Length);
        entry.Text = formatted;
        entry.CursorPosition = Math.Max(0, formatted.Length); // keep cursor at end
        _suppressTextChanged = false;

        setFormatted($"{_viewModel.CurrencySymbol}{formatted}");
        setWords(words);
        setViewModel(val);
    }

    private void OnAssetValueChanged(object sender, Syncfusion.Maui.Inputs.NumericEntryValueChangedEventArgs e) { }
    private void OnLoanAmountChanged(object sender, Syncfusion.Maui.Inputs.NumericEntryValueChangedEventArgs e) { }

    private void OnQuickInputFab_Clicked(object sender, EventArgs e)
    {
        UpdateQuickInputLabels();
        _viewModel.PropertyChanged += OnViewModelPropertyChangedForPopup;
        _viewModel.IsQuickInputVisible = true;
    }

    private void UpdateQuickInputLabels()
    {
        var sym     = _viewModel.CurrencySymbol;
        var asset   = _viewModel.PropertyAmount;
        var deposit = _viewModel.DepositAmountDirectInput;
        var loan    = _viewModel.LoanAmountDirectInput;

        _suppressTextChanged = true;
        if (_entryAssetValue   != null) _entryAssetValue.Text   = asset   > 0 ? $"{asset:N0}"   : string.Empty;
        if (_entryDepositAmount != null) _entryDepositAmount.Text = deposit > 0 ? $"{deposit:N0}" : string.Empty;
        if (_entryLoanAmount   != null) _entryLoanAmount.Text   = loan    > 0 ? $"{loan:N0}"    : string.Empty;
        _suppressTextChanged = false;

        if (_lblAssetFormatted   != null) _lblAssetFormatted.Text   = asset   > 0 ? $"{sym}{asset:N0}"   : string.Empty;
        if (_lblAssetWords       != null) _lblAssetWords.Text       = asset   > 0 ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)Math.Round(asset))   : string.Empty;
        if (_lblDepositFormatted != null) _lblDepositFormatted.Text = deposit > 0 ? $"{sym}{deposit:N0}" : string.Empty;
        if (_lblDepositWords     != null) _lblDepositWords.Text     = deposit > 0 ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)Math.Round(deposit)) : string.Empty;
        if (_lblLoanFormatted    != null) _lblLoanFormatted.Text    = loan    > 0 ? $"{sym}{loan:N0}"    : string.Empty;
        if (_lblLoanWords        != null) _lblLoanWords.Text        = loan    > 0 ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)Math.Round(loan))    : string.Empty;
    }

    private void RefreshLoanDisplay()
    {
        var sym     = _viewModel.CurrencySymbol;
        var deposit = _viewModel.DepositAmountDirectInput;
        var loan    = _viewModel.LoanAmountDirectInput;

        _suppressTextChanged = true;
        if (_entryDepositAmount != null) _entryDepositAmount.Text = deposit > 0 ? $"{deposit:N0}" : string.Empty;
        if (_entryLoanAmount    != null) _entryLoanAmount.Text    = loan    > 0 ? $"{loan:N0}"    : string.Empty;
        _suppressTextChanged = false;

        if (_lblDepositFormatted != null) _lblDepositFormatted.Text = deposit > 0 ? $"{sym}{deposit:N0}" : string.Empty;
        if (_lblDepositWords     != null) _lblDepositWords.Text     = deposit > 0 ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)Math.Round(deposit)) : string.Empty;
        if (_lblLoanFormatted    != null) _lblLoanFormatted.Text    = loan    > 0 ? $"{sym}{loan:N0}"    : string.Empty;
        if (_lblLoanWords        != null) _lblLoanWords.Text        = loan    > 0 ? LoanCalculator.Core.Models.ViewModels.PrimaryModels.LoanViewModel.NumberToWordsPublic((long)Math.Round(loan))    : string.Empty;
    }

    private void OnViewModelPropertyChangedForPopup(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.LoanAmountDirectInput) ||
            e.PropertyName == nameof(_viewModel.LoanAmountStrFormatted) ||
            e.PropertyName == nameof(_viewModel.DepositAmountDirectInput))
        {
            MainThread.BeginInvokeOnMainThread(RefreshLoanDisplay);
        }
    }

    private void OnQuickInputEntryLoaded(object sender, EventArgs e)
    {
        if (sender is Entry entry)
        {
            if (entry.AutomationId == "AssetEntry")   _entryAssetValue   = entry;
            else if (entry.AutomationId == "DepositEntry") _entryDepositAmount = entry;
            else if (entry.AutomationId == "LoanEntry")    _entryLoanAmount   = entry;
            UpdateQuickInputLabels();
        }
    }

    private void OnQuickInputLabelLoaded(object sender, EventArgs e)
    {
        // Wire backing field references when labels render inside the DataTemplate
        if (sender is Label lbl)
        {
            if      (lbl.AutomationId == "AssetFormatted")   _lblAssetFormatted   = lbl;
            else if (lbl.AutomationId == "AssetWords")       _lblAssetWords       = lbl;
            else if (lbl.AutomationId == "DepositFormatted") _lblDepositFormatted = lbl;
            else if (lbl.AutomationId == "DepositWords")     _lblDepositWords     = lbl;
            else if (lbl.AutomationId == "LoanFormatted")    _lblLoanFormatted    = lbl;
            else if (lbl.AutomationId == "LoanWords")        _lblLoanWords        = lbl;
            UpdateQuickInputLabels();
        }
    }

    private void OnQuickInputDone(object sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChangedForPopup;
        _viewModel.IsQuickInputVisible = false;
        _viewModel.TriggerPropertyChangedOnPropertyTab();
    }

    private void OnInterestRateDecrease(object sender, EventArgs e)
    {
        if (_viewModel.InterestRate > 0)
            _viewModel.InterestRate = Math.Max(0, Math.Round(_viewModel.InterestRate - 0.05, 2));
    }

    private void OnInterestRateIncrease(object sender, EventArgs e)
    {
        if (_viewModel.InterestRate < 100)
            _viewModel.InterestRate = Math.Min(100, Math.Round(_viewModel.InterestRate + 0.05, 2));
    }

    private void OnInterestRateLabelTapped(object sender, TappedEventArgs e)
    {
        lblInterestRate.IsVisible = false;
        entryInterestRate.Text = _viewModel.InterestRate.ToString("0.##");
        entryInterestRate.IsVisible = true;
        entryInterestRate.Focus();
    }

    private void OnInterestRateEntryCompleted(object sender, EventArgs e) => CommitInterestRateEntry();
    private void OnInterestRateEntryUnfocused(object sender, FocusEventArgs e) => CommitInterestRateEntry();

    private void CommitInterestRateEntry()
    {
        if (!entryInterestRate.IsVisible) return;
        if (double.TryParse(entryInterestRate.Text, out var val))
            _viewModel.InterestRate = Math.Clamp(Math.Round(val, 2), 0, 100);
        entryInterestRate.IsVisible = false;
        lblInterestRate.IsVisible = true;
    }

    private void OnLoanTermDecrease(object sender, EventArgs e)
    {
        if (_viewModel.LoanTermInYears > 1)
            _viewModel.LoanTermInYears -= 1;
    }

    private void OnLoanTermIncrease(object sender, EventArgs e)
    {
        if (_viewModel.LoanTermInYears < 30)
            _viewModel.LoanTermInYears += 1;
    }

    private void OnAmortizationAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
    {
        if (!double.TryParse(e.Label, out var val)) return;
        var sym = _viewModel?.CurrencySymbol ?? "$";
        e.Label = Math.Abs(val) >= 1_000_000
            ? $"{sym}{val / 1_000_000:0.#}M"
            : Math.Abs(val) >= 1_000
                ? $"{sym}{val / 1_000:0.#}K"
                : $"{sym}{val:0}";
    }

    private void TabView_OnSelectionChanging(object? sender, SelectionChangingEventArgs e)
    {
        try
        {
            // Update tab-visibility flags so TriggerPropertyChangedOnPropertyTab
            // knows which chart updates to skip while other tabs are inactive.
            _viewModel.IsAmortizationTabActive = e.Index == 1;
            _viewModel.IsInsightsTabActive = e.Index == 3;

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
                    // Revert flag — tab switch was cancelled
                    _viewModel.IsAmortizationTabActive = false;
                    _viewModel.IsInsightsTabActive = false;
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
                    // Revert flag — tab switch was cancelled
                    _viewModel.IsAmortizationTabActive = false;
                    _viewModel.IsInsightsTabActive = false;
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