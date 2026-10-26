using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Extensions;
using Pj.Library;
using Syncfusion.Maui.Charts;

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

        // The affordability box links through to the full answer. Assigned here rather than bound
        // in XAML because switching Shell tabs is a MAUI concern and this page's BindingContext is
        // LoanViewModel, which lives in Core and must stay ignorant of Shell.
        LoanSummaryBox.Box3Command = new Command(() => _ = GoToWhatIfAnswerAsync());

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

    /// <summary>
    /// Takes the user from the affordability verdict to the full answer on What If.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="WhatIfView.PendingTabIndex"/> before navigating, because arriving at What
    /// If lands on whichever sub-tab was last selected — so a link promising "the answer" could
    /// legitimately drop you on Projection. That is not hypothetical: the screenshot sweep
    /// captured What If showing Projection for exactly this reason.
    /// </remarks>
    private async Task GoToWhatIfAnswerAsync()
    {
        try
        {
            WhatIfView.PendingTabIndex = 0; // Answer
            await Shell.Current.GoToAsync("//WhatIf");
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
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
        // No ApplyToTabView: this page no longer has a tab strip, and passing a removed
        // element would null-ref rather than fail visibly.
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

            await Task.WhenAll(viewModelInitializeTask, expenseSummaryTask, incomeSummaryTask, chartColorsTask);

            // PdfGenerator is now built lazily, in LoanViewModel.ExportInsights, on first actual
            // export — not here, on every page load. See the comment there for why: constructing
            // it eagerly meant loading three font files via a blocking FileSystem call on the main
            // thread as soon as this method resumed, which was slow enough on iOS Simulator to trip
            // an AppHang and, on at least one run, throw outright.

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
            // The modal used to need the Asset tab reselected on commit; with the tab strip
            // gone there is nothing to select, so its last dependency on this page's markup
            // has disappeared along with it.
            await page.Completion;
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

    // ── Upfront Costs page ─────────────────────────────────────────────────
    private bool _isUpfrontCostsOpen;

    private void OnUpfrontCostsTapped(object sender, TappedEventArgs e) => _ = LaunchUpfrontCostsAsync();

    // ── Repayment schedule page ──────────────────────────────────────────────
    private bool _isSchedulePageOpen;

    private void OnScheduleTapped(object sender, TappedEventArgs e) => _ = LaunchSchedulePageAsync();

    private async Task LaunchSchedulePageAsync()
    {
        if (_isSchedulePageOpen) return;
        _isSchedulePageOpen = true;

        try
        {
            var page = ServiceLocator.GetService<AmortisationPage>();
            await Navigation.PushModalAsync(page);
            await page.Completion;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            // Always clear the latch, or the row goes permanently dead with no visible error.
            _isSchedulePageOpen = false;
        }
    }

    private async Task LaunchUpfrontCostsAsync()
    {
        if (_isUpfrontCostsOpen) return;
        _isUpfrontCostsOpen = true;

        try
        {
            var page = ServiceLocator.GetService<UpfrontCostsPage>();
            if (page is null) return;
            await Navigation.PushModalAsync(page);
            var committed = await page.Completion;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            _isUpfrontCostsOpen = false;
        }
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
        => ChartAxisFormatter.Money(e, _viewModel?.CurrencySymbol);

}