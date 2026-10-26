using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Extensions;
using Syncfusion.Maui.Charts;
using Syncfusion.Maui.TabView;

namespace LoanCalculatorMaui.View;

/// <summary>
/// The What If destination. Shows the scenario index; each row opens its scenario full-screen.
/// </summary>
/// <remarks>
/// The nine scenarios used to be nine cards stacked here in one scroll — one card was taller
/// than the screen, so the page ran to roughly nine screens with no index and no way to address
/// a single scenario. They now live as ContentViews under <c>Controls/Scenarios/</c>, hosted by
/// <see cref="ScenarioPage"/>. Every stepper and info-button handler moved with its card; what
/// stays here is the cross-tab refresh and the persistence, both of which belong to the
/// container rather than to any one scenario.
/// </remarks>
public partial class WhatIfView : ContentPage
{
    private readonly WhatIfViewModel _viewModel;
    private readonly LoanViewModel _loanViewModel;
    private readonly IncomeViewModel _incomeViewModel;
    private readonly ExpenseViewModel _expenseViewModel;
    private readonly BudgetViewModel _budgetViewModel;
    private readonly IThemeHandler _themeHandler;
    private readonly IErrorHandlingService _errorHandlingService;

    private bool _hasLoadedOnce;
    private bool _isScenarioOpen;

    /// <summary>
    /// Sub-tab to select on the next appearance, or null to keep whichever is selected.
    /// </summary>
    /// <remarks>
    /// Shell keeps a tab's page alive, so arriving here lands on the sub-tab the user left
    /// selected. A caller linking to a specific sub-tab therefore has to say so — the Loan page's
    /// verdict box links to Answer and would otherwise drop the user wherever they last were.
    /// Consumed once and cleared, so it cannot "stick" and override a later deliberate selection.
    /// </remarks>
    public static int? PendingTabIndex { get; set; }

    public WhatIfView(
        WhatIfViewModel viewModel,
        LoanViewModel loanViewModel,
        IncomeViewModel incomeViewModel,
        ExpenseViewModel expenseViewModel,
        BudgetViewModel budgetViewModel,
        IThemeHandler themeHandler,
        IErrorHandlingService errorHandlingService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _loanViewModel = loanViewModel;
        _incomeViewModel = incomeViewModel;
        _expenseViewModel = expenseViewModel;
        _budgetViewModel = budgetViewModel;
        _themeHandler = themeHandler;
        _errorHandlingService = errorHandlingService;
        _viewModel.SetLoanViewModel(_loanViewModel);
        BindingContext = _viewModel;

        // Two sub-trees bind to a different model than the page. Answer shows the loan's
        // affordability; Projection was lifted from Budget and still binds BudgetViewModel.
        // Overriding the sub-tree context keeps every moved binding path unchanged — the same
        // approach the Asset-costs tab on Budget uses.
        AnswerRoot.BindingContext = _loanViewModel;
        ProjectionRoot.BindingContext = _budgetViewModel;

        // Insights was lifted wholesale from LoanView, so every binding in it — all nine panels'
        // figures, the export button and its progress bar — is a LoanViewModel path. Without this
        // the sub-tree inherited the page's WhatIfViewModel and every one of those bindings
        // silently resolved to nothing.
        //
        // The visible symptom was not blank figures but a progress bar that never went away:
        // IsVisible="{Binding IsGeneratingPdf}" failed to resolve, and a failed binding leaves
        // IsVisible at its default of true. A missing BindingContext can therefore show MORE than
        // intended, not less — which is why it read as a styling bug rather than a binding one.
        InsightsRoot.BindingContext = _loanViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Everything below is slow enough to read as a hang: a cross-tab summary refresh, a
        // disk read, then a theme/chart pass over up to eleven charts. The page is already on
        // screen for all of it, showing stale or blank figures, so it gets a scrim.
        BusyOverlay.Show("Loading your scenarios…");
        try
        {
            await OnAppearingCoreAsync();
        }
        finally
        {
            BusyOverlay.Hide();
        }
    }

    private async Task OnAppearingCoreAsync()
    {
        // The stress test reads _loanViewModel.IsAffordabilityAvailable and MonthlySurplus, both
        // derived from the cached HasIncomeExpensesRecorded flag. Only the Loan tab used to refresh
        // that, so arriving straight from Budget computed the stress test from a stale cache.
        // The dirty flags stay set — LoanView still owns clearing them and its notification pass.
        if (SharedServiceCore.IsIncomeDirty || SharedServiceCore.IsExpenseDirty)
            await _loanViewModel.RefreshIncomeExpenseSummariesAsync(_incomeViewModel, _expenseViewModel);

        _viewModel.SetLoanViewModel(_loanViewModel);

        // Honour a requested sub-tab, then clear it. Read before the heavy work below so the
        // right tab is showing as early as possible rather than after a visible switch.
        //
        // EnterTab must be called explicitly, and for the already-selected tab too: assigning
        // SelectedIndex does NOT raise SelectionChanging, and neither does simply arriving on the
        // page. Without the second case the default tab (Answer) would never get its arrival work
        // either — which is how its chart came to render empty on first visit.
        try
        {
            // SelectedIndex is a double on SfTabView, not an int.
            var requested = PendingTabIndex ?? (int)tabView.SelectedIndex;
            PendingTabIndex = null;

            if (EnterTab(requested))
                tabView.SelectedIndex = requested;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }

        if (!_hasLoadedOnce)
        {
            _hasLoadedOnce = true;
            var saved = await SharedServiceCore.LoadDataFile<WhatIfViewModel>();
            if (saved != null)
            {
                _viewModel.RateChangeDelta       = saved.RateChangeDelta;
                _viewModel.ExtraRepaymentMonthly = saved.ExtraRepaymentMonthly;
                _viewModel.LumpSumAmount         = saved.LumpSumAmount;
                _viewModel.OffsetBalance         = saved.OffsetBalance;
                if (saved.OffsetRate > 0) _viewModel.OffsetRate = saved.OffsetRate;
                // RepaymentFrequencyIndex is always synced from the loan, not restored
            }
        }

        // ── Obligations inherited from the tabs that moved here ──────────────
        //
        // Guarded. This is an `async void` override, so an escaping exception has no caller to
        // catch it and becomes a process-level crash rather than a reported error — the same way
        // the unguarded SyncAmortization call took the app to the iOS home screen when the
        // schedule page opened.
        try
        {
            //
            // This page had NONE of these before: it carried no charts it owned and no sliders, so
            // it never refreshed theme colours and never refreshed sliders. Projection brought both,
            // and Insights brings nine more charts. Every one of these fails SILENTLY — stale colours
            // after a theme change, not an error — which is why they are listed rather than implied.

            // Charts on Projection (and Insights) keep the previous theme's palette without this.
            // LoanViewModel owns the Insights charts; BudgetViewModel owns Projection's.
            var chartColors = _themeHandler.GetChartColors();
            _loanViewModel.CustomChartColors = chartColors;
            _budgetViewModel.CustomChartColors = chartColors;
            _budgetViewModel.Income.CustomChartColors = chartColors;
            _budgetViewModel.Expense.CustomChartColors = chartColors;

            // Projection's year/growth sliders: Syncfusion caches slider colours and will not pick
            // up a theme change live.
            SliderThemeRefresher.Refresh(this);

#if IOS || MACCATALYST
            // This page now has a tab view, and Insights brings expanders. Both need the touch fix:
            // the drawing overlay Syncfusion adds as the last sibling swallows taps on iOS.
            SyncfusionIosTouchFix.ApplyToTabView(tabView);
            SyncfusionIosTouchFix.ApplyToExpanders(this);
#endif
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    /// <summary>
    /// Takes over what LoanView's tab strip used to do for Insights, and BudgetView's for
    /// Projection. Answer 0, Scenarios 1, Projection 2 — Insights is appended in the next step.
    /// </summary>
    private void TabView_OnSelectionChanging(object? sender, SelectionChangingEventArgs e)
    {
        try
        {
            if (!EnterTab(e.Index)) e.Cancel = true;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    /// <summary>
    /// Everything a tab needs on arrival, in ONE place because there are two ways in and
    /// <see cref="SelectionChangingEventArgs"/> only describes one of them.
    ///
    /// Syncfusion raises <c>SelectionChanging</c> for a *user* tap on the tab strip; assigning
    /// <c>tabView.SelectedIndex</c> in code does not raise it. So when Budget's "See the
    /// projection ›" link set the index programmatically, the handler never ran and Projection
    /// rendered with every imperatively-populated element blank — no summary figures, no growth
    /// percentages, no chart, no grid — because <see cref="RefreshProjection"/> is the only thing
    /// that fills them. Tapping away and back "fixed" it, which is what made it look intermittent.
    ///
    /// The premium gate lives here for the same reason, and it matters more: taking that path
    /// programmatically handed trial users Projection for free.
    ///
    /// Returns false when the tab must not be entered.
    /// </summary>
    private bool EnterTab(int index)
    {
        // Projection (2) and Insights (3) were both premium-gated on their old pages. This page
        // declared PremiumWindow in XAML but never used it, so without this the two features
        // would silently become free when they moved here.
        //
        // Belt-and-braces: AppShell.OnNavigating already blocks the whole WhatIf route for trial
        // users, so this is not currently the first line of defence. It stays because it is the
        // only thing covering the PROGRAMMATIC path — PendingTabIndex, set by Budget's "See the
        // projection" link — which does not go through Shell navigation at all.
        if (index == 2 || index == 3)
        {
            if (SharedServiceCore.IsTrialUser)
            {
                PremiumWindow.ShowPremiumBuyWindow = true;
                return false;
            }
        }

        if (index == 0) RefreshAnswerChart();
        if (index == 2) RefreshProjection();
        if (index == 3) RefreshInsights();

        return true;
    }

    /// <summary>
    /// Builds the nine Insights panels.
    /// </summary>
    /// <remarks>
    /// This had NO owner at all after Insights moved off LoanView: nothing in the app set
    /// <c>IsInsightsTabActive</c> or called <c>RefreshInsightsTabPropertyChanged()</c> any more,
    /// so <c>BuildInsights()</c> never ran and <c>InsightsDetails</c> stayed empty. Every panel
    /// still rendered — its accent bar, its info icon, its expander chrome — with no label and no
    /// value, which looks like a styling fault rather than missing data.
    ///
    /// <c>IsInsightsTabActive</c> is separately load-bearing: it gates <c>UpdateInsightCharts()</c>
    /// inside the shared notification path, so the Insights charts never redraw while it is false.
    /// It is cleared in OnDisappearing — left set, nine charts recompute on every notification for
    /// a page that is not on screen.
    /// </remarks>
    private void RefreshInsights()
    {
        try
        {
            _loanViewModel.IsInsightsTabActive = true;
            _loanViewModel.RefreshInsightsTabPropertyChanged();
            _loanViewModel.ForceUpdateInsightCharts();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    /// <summary>
    /// Fills the Answer tab's "where the money goes" stacked column chart.
    /// </summary>
    /// <remarks>
    /// Two deliberate steps. <c>SyncAmortization</c> computes the amortisation terms, which may
    /// not exist yet — the schedule page is the only other thing that asks for them, and the user
    /// need never have opened it. <c>ForceUpdateAmortizationCharts</c> then builds the chart
    /// collections *bypassing the <c>IsAmortizationTabActive</c> guard*, which is what the plain
    /// notification path checks; left to that path the collections stay empty on this page,
    /// because this page is not the amortisation tab and never sets that flag.
    ///
    /// The series are assigned here rather than bound because the collections are refilled in
    /// place, and Syncfusion does not reliably redraw a stacked series on a Clear/Add storm.
    /// </remarks>
    private void RefreshAnswerChart()
    {
        try
        {
            _loanViewModel.SyncAmortization();
            _loanViewModel.ForceUpdateAmortizationCharts();

            var hasData = _loanViewModel.AnswerPrincipalByYearAxis.Count > 0;

            AnswerPrincipalSeries.ItemsSource = null;
            AnswerInterestSeries.ItemsSource = null;

            if (hasData)
            {
                AnswerPrincipalSeries.ItemsSource = _loanViewModel.AnswerPrincipalByYearAxis;
                AnswerInterestSeries.ItemsSource = _loanViewModel.AnswerInterestByYearAxis;
            }

            AnswerChartCard.IsVisible = hasData;
            AnswerChartHeader.IsVisible = hasData;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void OnAnswerChartValueLabelCreated(object sender, ChartAxisLabelEventArgs e)
        => ChartAxisFormatter.Money(e, _loanViewModel?.CurrencySymbol);

    /// <summary>
    /// The empty state's call to action. This panel used to say "enter your details on the Loan
    /// tab" without taking the user there, which is the gap whatif-empty.html exists to close.
    /// </summary>
    private async void OnGoToLoanTapped(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//Loan");
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    /// <summary>Jumps from Answer to the scenario index without hunting for the tab header.</summary>
    private void OnSeeScenariosTapped(object sender, TappedEventArgs e)
    {
        try
        {
            tabView.SelectedIndex = 1; // Scenarios
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    /// <summary>
    /// Opens the repayment schedule, which lives under Loan as a pushed page.
    /// </summary>
    /// <remarks>
    /// Pushes the page straight from here rather than switching to the Loan tab and making the
    /// user find the row: the point of a contextual link is to land on the thing it names.
    /// </remarks>
    private async void OnSeeScheduleTapped(object sender, TappedEventArgs e)
    {
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
    }

    private void OnExportTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (_loanViewModel.ExportInsightsReportCommand?.CanExecute(null) == true)
                _loanViewModel.ExportInsightsReportCommand.Execute(null);
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    /// <summary>
    /// Saving stays here, on the container. <see cref="ScenarioPage"/> deliberately does not
    /// save: nine scenario pages each writing as they closed would race on one JSON file, which
    /// is the corruption class CLAUDE.md records for namevaluedata.json.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SharedServiceCore.SaveData(_viewModel);

        // Clearing this matters, and it is the half that fails quietly in the other direction:
        // left set, nine Insights charts recompute on every notification for a page that is no
        // longer on screen. LoanView's tab strip used to clear it when Insights was deselected.
        _loanViewModel.IsInsightsTabActive = false;

        BusyOverlay.Hide();
    }

    // ══ Projection ══════════════════════════════════════════════════════════
    //
    // Moved verbatim from BudgetView, which owned this as its fourth tab. It sits with the
    // scenarios because both are forward-looking. The markup still binds to BudgetViewModel,
    // so the Projection tab's content root gets that as a sub-tree BindingContext (set in the
    // constructor) and every binding path in the moved XAML resolves unchanged.

    private void UpdateProjectionSummaryBar()
    {
        var sym = _budgetViewModel.CurrencySymbol ?? "$";
        var incomeTotal = _budgetViewModel.Income.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;
        var expenseTotal = _budgetViewModel.Expense.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;

        ProjectionIncomeSpan.Text = $"{sym}{incomeTotal:N0}";
        ProjectionExpenseSpan.Text = $"{sym}{expenseTotal:N0}";

        // The horizon is stated in the intro sentence rather than in a summary box of its own —
        // see the note on LblProjectionIntro in the XAML.
        var years = _budgetViewModel.ProjectionYears;
        LblProjectionIntro.Text =
            $"Where your surplus goes over {years} year{(years == 1 ? "" : "s")}, "
            + "assuming income and expenses change at the rates below.";

        LblProjectionYears.Text = years.ToString();

        // Sync slider switch states to VM values
        SwitchIncludeExpenses.IsOn = _budgetViewModel.Income.IncludeExpenses;
        SwitchIncludePropertyExpenses.IsOn = _budgetViewModel.Income.IncludePropertyExpenses;
        SwitchExpensePropertyExpense.IsOn = _budgetViewModel.Expense.ShowPropertyExpense;
        LblIncomeGrowthRate.Text = $"{_budgetViewModel.Income.AnnualGrowthRate:0.##}%";
        LblExpenseGrowthRate.Text = $"{_budgetViewModel.Expense.AnnualGrowthRate:0.##}%";
    }

    private void OnProjectionYearsSliderChanged(object sender, EventArgs e)
    {
        if (sender is not Syncfusion.Maui.Sliders.SfSlider slider) return;
        var years = (int)slider.Value;
        _budgetViewModel.ProjectionYears = years;
        _budgetViewModel.Income.TotalYearsToProject = years;
        _budgetViewModel.Expense.TotalYearsToProject = years;
        // The slider's own label updates immediately so dragging stays responsive; the intro
        // sentence is rewritten by UpdateProjectionSummaryBar at the end of the (debounced)
        // rebuild, along with the two totals it sits above.
        LblProjectionYears.Text = years.ToString();
        RefreshProjection();
    }

    private void OnProjectionYearsDecrease(object sender, EventArgs e)
    {
        if (_budgetViewModel.ProjectionYears > 1)
            _budgetViewModel.ProjectionYears -= 1;
    }

    private void OnProjectionYearsIncrease(object sender, EventArgs e)
    {
        if (_budgetViewModel.ProjectionYears < SharedServiceCore.GetMaxProjectionYears())
            _budgetViewModel.ProjectionYears += 1;
    }

    private void OnIncomeGrowthRateIncrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetGrowthRateStep();
        _budgetViewModel.Income.AnnualGrowthRate = Math.Round(Math.Min(_budgetViewModel.Income.AnnualGrowthRate + step, 20), 2);
        LblIncomeGrowthRate.Text = $"{_budgetViewModel.Income.AnnualGrowthRate:0.##}%";
        ScheduleProjectionRefresh();
    }

    private void OnIncomeGrowthRateDecrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetGrowthRateStep();
        _budgetViewModel.Income.AnnualGrowthRate = Math.Round(Math.Max(_budgetViewModel.Income.AnnualGrowthRate - step, 0), 2);
        LblIncomeGrowthRate.Text = $"{_budgetViewModel.Income.AnnualGrowthRate:0.##}%";
        ScheduleProjectionRefresh();
    }

    private void OnExpenseGrowthRateIncrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetGrowthRateStep();
        _budgetViewModel.Expense.AnnualGrowthRate = Math.Round(Math.Min(_budgetViewModel.Expense.AnnualGrowthRate + step, 20), 2);
        LblExpenseGrowthRate.Text = $"{_budgetViewModel.Expense.AnnualGrowthRate:0.##}%";
        ScheduleProjectionRefresh();
    }

    private void OnExpenseGrowthRateDecrease(object sender, EventArgs e)
    {
        var step = SharedServiceCore.GetGrowthRateStep();
        _budgetViewModel.Expense.AnnualGrowthRate = Math.Round(Math.Max(_budgetViewModel.Expense.AnnualGrowthRate - step, 0), 2);
        LblExpenseGrowthRate.Text = $"{_budgetViewModel.Expense.AnnualGrowthRate:0.##}%";
        ScheduleProjectionRefresh();
    }

    private void OnIncludeExpensesChanged(object sender, Syncfusion.Maui.Buttons.SwitchStateChangedEventArgs e)
    {
        _budgetViewModel.Income.IncludeExpenses = e.NewValue == true;
        ScheduleProjectionRefresh();
    }

    private void OnIncludePropertyExpensesChanged(object sender, Syncfusion.Maui.Buttons.SwitchStateChangedEventArgs e)
    {
        _budgetViewModel.Income.IncludePropertyExpenses = e.NewValue == true;
        ScheduleProjectionRefresh();
    }

    private void OnExpensePropertyExpenseChanged(object sender, Syncfusion.Maui.Buttons.SwitchStateChangedEventArgs e)
    {
        _budgetViewModel.Expense.ShowPropertyExpense = e.NewValue == true;
        ScheduleProjectionRefresh();
    }

    private CancellationTokenSource? _projectionRefreshCts;

    /// <summary>
    /// Coalesces rapid Projection-tab edits (growth-rate +/- taps, toggles) into a single
    /// rebuild. Each <see cref="RefreshProjection"/> tears down and re-attaches two chart
    /// series and two paged grids, so running it per tap made every press feel laggy and
    /// queued up more work behind it. The on-screen number is updated by the caller straight
    /// away; only the expensive chart/grid rebuild waits for the taps to stop.
    /// </summary>
    private void ScheduleProjectionRefresh(int debounceMs = 260)
    {
        _projectionRefreshCts?.Cancel();
        _projectionRefreshCts = new CancellationTokenSource();
        var token = _projectionRefreshCts.Token;

        _budgetViewModel.IsProjectionRefreshing = true;

        Task.Delay(debounceMs, token).ContinueWith(t =>
        {
            if (t.IsCanceled || token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (token.IsCancellationRequested) return;
                try
                {
                    RefreshProjection();
                }
                finally
                {
                    _budgetViewModel.IsProjectionRefreshing = false;
                }
            });
        }, TaskScheduler.Default);
    }

    private void RefreshProjection()
    {
        // The flag is set here rather than only in ScheduleProjectionRefresh so the tab's
        // "Updating…" indicator covers every entry point — including arriving on the tab, which
        // is the slowest one and previously showed no progress at all.
        _budgetViewModel.IsProjectionRefreshing = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Detach before mutating
                ProjIncomeSeries.ItemsSource = null;
                ProjExpenseSeries.ItemsSource = null;
                IncomeDataPager.Source = null;
                ExpenseDataPager.Source = null;

                // Sync the sub-VMs to the Budget's ProjectionYears every refresh. On the first
                // visit the slider shows 10 but the sub-VMs still hold their own (low) default,
                // so the chart/grid would otherwise show only 1-2 years until the slider moves.
                _budgetViewModel.Income.TotalYearsToProject = _budgetViewModel.ProjectionYears;
                _budgetViewModel.Expense.TotalYearsToProject = _budgetViewModel.ProjectionYears;

                _budgetViewModel.Income.UpdateProjectionData();
                _budgetViewModel.Expense.UpdateProjectionData();
                _budgetViewModel.Income.TriggerPropertyChangedOnProjectionTab();
                _budgetViewModel.Expense.TriggerPropertyChangedOnProjectionTab();
                _budgetViewModel.RecalculateProjection();
                UpdateProjectionSummaryBar();

                // Reattach with fresh data — ToList() forces a new reference so SfDataPager
                // recounts pages (it won't recount if the same list object is reassigned)
                ProjIncomeSeries.ItemsSource = _budgetViewModel.ProjectionIncomeAxis;
                ProjExpenseSeries.ItemsSource = _budgetViewModel.ProjectionExpenseAxis;
                IncomeDataPager.Source = _budgetViewModel.Income.IncomeProjectList.ToList();
                ExpenseDataPager.Source = _budgetViewModel.Expense.IncomeProjectList.ToList();
            }
            catch (Exception ex)
            {
                _errorHandlingService.HandleException(ex);
            }
            finally
            {
                _budgetViewModel.IsProjectionRefreshing = false;
            }
        });
    }

    private void OnProjectionAxisLabelCreated(object sender, ChartAxisLabelEventArgs e)
        => ChartAxisFormatter.Money(e, _viewModel?.CurrencySymbol);

    private void OnScenarioTapped(object sender, TappedEventArgs e)
    {
        // Read the scenario from the event args, not from `sender`. MAUI raises Tapped with the
        // ELEMENT the recogniser is attached to as sender — the Border — not the recogniser, so
        // matching sender against TapGestureRecognizer never succeeded and this handler returned
        // silently on every tap. The row highlighted and nothing opened.
        var key = e.Parameter as string
                  ?? (sender as Microsoft.Maui.Controls.View)?.GestureRecognizers
                         .OfType<TapGestureRecognizer>()
                         .Select(g => g.CommandParameter as string)
                         .FirstOrDefault(p => p is not null);

        if (key is null || !Enum.TryParse<WhatIfScenario>(key, out var scenario)) return;

        _ = LaunchScenarioAsync(scenario);
    }

    private async Task LaunchScenarioAsync(WhatIfScenario scenario)
    {
        if (_isScenarioOpen) return;
        _isScenarioOpen = true;

        try
        {
            var page = ServiceLocator.GetService<ScenarioPage>();
            page.Configure(scenario);

            await Navigation.PushModalAsync(page);
            await page.Completion;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            // Always clear the latch. If an exception escaped between push and pop, leaving it
            // set would make every scenario row permanently dead with no visible error.
            _isScenarioOpen = false;
        }
    }
}
