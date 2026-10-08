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

        BindingContext = _viewModel;

        // Subscribe once — auto-show wizard after the disclaimer is accepted on first launch
        SharedServiceCore.DisclaimerAccepted += OnDisclaimerAccepted;
    }

    private void OnDisclaimerAccepted(object? sender, EventArgs e)
    {
        SharedServiceCore.DisclaimerAccepted -= OnDisclaimerAccepted;
        if (!SharedServiceCore.ShouldShowWizard()) return;
        SharedServiceCore.SetWizardShown();
        // Small delay so the disclaimer popup fully closes before the wizard appears — pushing a
        // modal while it is still animating out leaves them fighting over the screen on iOS.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(600);
            await LaunchLoanDetailsAsync();
        });
    }

    private bool _hasLoadedOnce;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
        SharedServiceCore.MarkLoanDirty();
    }

    protected override async void OnAppearing()
    {
        // Idempotent: -= on an unsubscribed handler is a no-op, so repeated appearances cannot
        // stack duplicate subscriptions.
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

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

            ReapplyIosTouchFixes();
        }
    }

    /// <summary>
    /// Re-disables the Syncfusion drawing overlays that swallow taps on iOS.
    /// </summary>
    /// <remarks>
    /// <para>This has to be repeatable, not one-shot. The overlays come back with
    /// <c>UserInteractionEnabled = true</c> whenever Syncfusion rebuilds them, which a later layout
    /// pass can trigger long after <see cref="OnAppearing"/> has run. The symptom is sharp: show the
    /// "enter the income and expense details" warning from Export Insights, dismiss it, and every
    /// expander on the page is dead until you switch tabs and come back — because coming back is
    /// what re-runs <c>OnAppearing</c>.</para>
    /// <para>So it is also called whenever the page's busy state clears — see
    /// <see cref="OnViewModelPropertyChanged"/> — which covers the alert and loader paths.</para>
    /// </remarks>
    private void ReapplyIosTouchFixes()
    {
#if IOS || MACCATALYST
        SyncfusionIosTouchFix.ApplyToTabView(TabView);
        SyncfusionIosTouchFix.ApplyToSegmentedControl(SegmentedRepaymentFrequency);
        // None of this page's expanders have an x:Name, so the fix walks the tree for them.
        SyncfusionIosTouchFix.ApplyToExpanders(this);
#endif
    }

    /// <summary>
    /// Watches for the busy/PDF flags falling back to false — the point at which a modal alert or
    /// the loader has gone and Syncfusion may have rebuilt its overlays.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(LoanViewModel.IsBusy) or nameof(LoanViewModel.IsGeneratingPdf)))
            return;

        if (_viewModel.IsBusy || _viewModel.IsGeneratingPdf) return;

        // Queued rather than immediate: the alert is still tearing down when the flag flips, and
        // the overlay we need to disable may not exist yet at this instant.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(150);
            ReapplyIosTouchFixes();
        });
    }

    private async Task RefreshCrossTabSummaries()
    {
        await _viewModel.RefreshIncomeExpenseSummariesAsync(_incomeViewModel, _expenseViewModel);

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

            // The peer-VM seeding that used to sit here existed only so the wizard's HasValue
            // checks were right before those tabs had been visited. WizardViewModel now resolves
            // the authoritative instance itself (LoanViewModel.ResolveAuthoritativeIncome), which
            // SplashPage pre-warms from disk on every launch, so the seeding is redundant.
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
    //
    // ── Loan Details page ────────────────────────────────────────────────────
    //
    // One modal page for all loan data entry — asset, deposit and loan amount always, plus
    // upfront/running/income/expenses while they are still empty. It replaced the Quick Setup
    // wizard (three SfPopup steps and ~280 lines of state machine that used to live here) and the
    // separate Quick Input popup, which overlapped it on asset and deposit.
    //
    // Reached three ways: the ⚡ FAB, tapping the asset/deposit/loan figures on the Asset tab, and
    // automatically on first run once the disclaimer is accepted.

    private bool _isLoanDetailsOpen;

    private void OnWizardFab_Clicked(object sender, EventArgs e) => _ = LaunchLoanDetailsAsync();

    private void OnAssetValueTapped(object sender, TappedEventArgs e) => _ = LaunchLoanDetailsAsync();

    private async Task LaunchLoanDetailsAsync()
    {
        if (_isLoanDetailsOpen) return;
        _isLoanDetailsOpen = true;

        try
        {
            var page = ServiceLocator.GetService<LoanDetailsPage>();
            if (page is null) return;

            await Navigation.PushModalAsync(page);
            var committed = await page.Completion;

            // Selecting this tab is the page's only dependency on LoanView's markup, and it is
            // satisfied here — after the modal has gone — so the page stays independent of it.
            if (committed) TabView.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            // Always clear the latch. If an exception escaped between push and pop, leaving this
            // set would make the FAB and the tappable figures permanently dead with no error.
            _isLoanDetailsOpen = false;
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


    // Shared with the Upfront Costs popup's entries — see OnUpfrontEntryTextChanged.
    private bool _suppressTextChanged;


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



    private void OnInterestRateDecrease(object sender, EventArgs e)
    {
        if (_viewModel.InterestRate > 0)
        {
            var step = SharedServiceCore.GetLoanRateStep();
            _viewModel.InterestRate = Math.Max(0, Math.Round(_viewModel.InterestRate - step, 2));
        }
    }

    private void OnInterestRateIncrease(object sender, EventArgs e)
    {
        if (_viewModel.InterestRate < 100)
        {
            var step = SharedServiceCore.GetLoanRateStep();
            _viewModel.InterestRate = Math.Min(100, Math.Round(_viewModel.InterestRate + step, 2));
        }
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
        if (_viewModel.LoanTermInYears < SharedServiceCore.GetMaxLoanTermYears())
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