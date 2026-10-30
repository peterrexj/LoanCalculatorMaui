using System.ComponentModel;
using System.Globalization;
using LoanCalculator.Core.Constants;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Pdf;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Controls;
using LoanCalculatorMaui.Converters;
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

        // The pinned totals row sits OUTSIDE ProjectionRoot now, so it no longer inherits the
        // sub-tree BindingContext set above — without this it would fall back to the page's
        // WhatIfViewModel and any binding in it would quietly resolve to nothing.
        ProjectionPinnedHeader.BindingContext = _budgetViewModel;

        // Insights is NOT given its BindingContext here any more, because it does not exist here
        // any more: its content is inflated into InsightsHost on first entry. RefreshInsights()
        // assigns the same _loanViewModel at that point. Do not be tempted to reinstate a line
        // here — the host is an empty ContentView, so setting its BindingContext would be
        // harmless but pointless, and it would quietly read as if the tab were eager again.
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // -= before += : OnAppearing can run again without an intervening OnDisappearing, and a
        // second subscription would raise the scrim twice and, worse, leave one subscription
        // behind when OnDisappearing removes "the" handler.
        _loanViewModel.PropertyChanged -= OnLoanViewModelPropertyChanged;
        _loanViewModel.PropertyChanged += OnLoanViewModelPropertyChanged;

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

        // Before EnterTab, which is what runs RefreshProjection. The Projection tab's two "take
        // off the loan and its running costs" switches deduct PropertyExpenseSummary and
        // PropertyPayment, and those are null until something assigns them — previously only the
        // Budget tab did, so arriving here without having visited Budget made both switches
        // deduct zero. They redrew everything and changed nothing. See WireLoanFiguresAsync.
        try
        {
            // Load income/expense first if nothing has yet. This page used to depend on the Budget
            // tab having been visited: Budget's OnAppearing is the only other caller, so arriving
            // here first left both sub-view-models empty and every projection figure derived from
            // zero. A no-op once they are loaded, and SplashPage pre-warms it anyway.
            await _budgetViewModel.EnsureSubVmsLoadedAsync();

            await _budgetViewModel.WireLoanFiguresAsync();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }

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
            EnsureInsightsContentBuilt();

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
    /// Inflates the Insights tab's content the first time that tab is entered.
    /// </summary>
    /// <remarks>
    /// Insights is 27 SfExpanders and 6 charts — 34 of this page's 59 heavy Syncfusion controls,
    /// for a tab that may never be opened. SfTabItem.Content is a plain property, not a
    /// DataTemplate, so leaving the markup inline meant all of it was constructed as part of
    /// constructing the page; and AppShell.xaml declares this page as
    /// ContentTemplate="{DataTemplate localview:WhatIfView}", so Shell does that construction on
    /// the first tap of the What If tab. That is what made the first tap slow, and no amount of
    /// scrimming in OnAppearing could cover it, because OnAppearing does not run until the
    /// construction it would be hiding has already finished.
    ///
    /// Idempotent, and it has to be: RefreshInsights runs on every entry to the tab, via both the
    /// user path and the programmatic one.
    /// </remarks>
    private void EnsureInsightsContentBuilt()
    {
        if (InsightsHost.Content is not null) return;

        var insights = new InsightsTabView
        {
            // Every binding in that markup is a LoanViewModel path — all nine panels' figures, the
            // export button and its progress bar. This assignment replaces the one the constructor
            // used to make onto InsightsRoot, and it is just as load-bearing: without it the
            // content inherits the page's WhatIfViewModel and every one of those bindings resolves
            // to nothing. The symptom is not blank figures but a progress bar that never goes away
            // — IsVisible="{Binding IsGeneratingPdf}" fails to resolve and IsVisible defaults to
            // true — so it reads as a styling bug rather than a binding one.
            BindingContext = _loanViewModel,
        };

        InsightsHost.Content = insights;

#if IOS || MACCATALYST
        // Mandatory, and it fails silently if skipped. OnAppearing already ran
        // ApplyToExpanders(this), but it ran before these 27 expanders existed, so they would
        // never be covered — Syncfusion 33.2.6 lays a touch-enabled drawing layer over an expander
        // header as the LAST sibling, which on iOS is topmost, and it accepts the tap and handles
        // nothing. Every Insights panel would simply refuse to open, with no error anywhere. See
        // CLAUDE.md: this defect class cost three failed diagnoses, and gesture-recognizer count is
        // not a reliable test for it.
        //
        // Two passes, matching AmortisationPage: the content has not laid out yet on this tick, so
        // the first pass only covers what already measured and the second catches the rest.
        ApplyExpanderTouchFix();
#endif
    }

#if IOS || MACCATALYST
    private void ApplyExpanderTouchFix()
    {
        SyncfusionIosTouchFix.ApplyToExpanders(this);

        // Second pass once the panels have actually laid out. Fire-and-forget: awaiting it would
        // hold up entry to the tab for no benefit.
        _ = Task.Run(async () =>
        {
            await Task.Delay(150);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    SyncfusionIosTouchFix.ApplyToExpanders(this);
                }
                catch (Exception ex)
                {
                    _errorHandlingService.HandleException(ex);
                }
            });
        });
    }
#endif

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
            // The scrim covers construction and the push, and stops there. page.Completion waits
            // for the USER to dismiss the modal, which is not loading — scrimming that would put
            // an input-blocking overlay behind the modal for as long as it is open, and leave it
            // up on return.
            AmortisationPage? page = null;

            await BusyOverlay.RunWithScrimAsync("Opening your repayment schedule…", async () =>
            {
                page = ServiceLocator.GetService<AmortisationPage>();
                await Navigation.PushModalAsync(page);
            });

            if (page is not null) await page.Completion;
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

        _loanViewModel.PropertyChanged -= OnLoanViewModelPropertyChanged;
        WatchPdfGenerator(null);

        BusyOverlay.Hide();
    }

    // ══ PDF export feedback ═════════════════════════════════════════════════

    /// <summary>
    /// The generator currently being watched for progress, so the handler can be detached.
    /// </summary>
    /// <remarks>
    /// Held separately from <c>_loanViewModel.PdfGenerator</c> because that property is created
    /// lazily on first export and could in principle be replaced; detaching from whatever we
    /// actually attached to is what keeps this from leaking a handler onto a dead generator.
    /// </remarks>
    private PdfInsightsGenerator? _watchedPdfGenerator;

    /// <summary>
    /// Raises the busy scrim over the whole page while the insights PDF is being built.
    /// </summary>
    /// <remarks>
    /// The export used to report itself with nothing but a 10px SfLinearProgressBar sitting under
    /// the button, which is easy to miss entirely — the export then appears to do nothing until
    /// SaveAndView hands off to the system PDF viewer, so it reads as "it just opened a PDF" with
    /// no indication that several seconds of work happened in between.
    ///
    /// The progress bar stays — it is the fine-grained read — but the scrim is what makes it
    /// obvious that the app is working, and it blocks input, which also stops a second tap
    /// landing while the first export runs (the command guards that too, but silently).
    /// </remarks>
    private void OnLoanViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The generator is assigned lazily, inside the export, AFTER IsGeneratingPdf goes true.
        // Watching for it separately is what lets the percentage appear at all.
        if (e.PropertyName == nameof(LoanViewModel.PdfGenerator))
        {
            WatchPdfGenerator(_loanViewModel.PdfGenerator);
            return;
        }

        if (e.PropertyName != nameof(LoanViewModel.IsGeneratingPdf)) return;

        if (_loanViewModel.IsGeneratingPdf)
        {
            WatchPdfGenerator(_loanViewModel.PdfGenerator);
            BusyOverlay.Show(ExportScrimMessage());
        }
        else
        {
            WatchPdfGenerator(null);
            BusyOverlay.Hide();
        }
    }

    private void WatchPdfGenerator(PdfInsightsGenerator? generator)
    {
        if (ReferenceEquals(_watchedPdfGenerator, generator)) return;

        if (_watchedPdfGenerator is not null)
            _watchedPdfGenerator.PropertyChanged -= OnPdfGeneratorPropertyChanged;

        _watchedPdfGenerator = generator;

        if (_watchedPdfGenerator is not null)
            _watchedPdfGenerator.PropertyChanged += OnPdfGeneratorPropertyChanged;
    }

    /// <remarks>
    /// Re-calling Show rather than only setting Message is deliberate: Show restarts the
    /// overlay's own 8-second failsafe. Generation paces itself with a 400ms delay between seven
    /// progress steps and does real work on top, so on a slow device the whole export can
    /// outlast that ceiling — and the scrim would then vanish mid-export, which looks like a
    /// failure. Each progress tick pushing the ceiling out means the scrim lasts exactly as long
    /// as progress keeps arriving, and still disappears if the export genuinely wedges.
    /// </remarks>
    private void OnPdfGeneratorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PdfInsightsGenerator.Progress)) return;
        if (!_loanViewModel.IsGeneratingPdf) return;

        BusyOverlay.Show(ExportScrimMessage());
    }

    private string ExportScrimMessage()
    {
        var progress = _watchedPdfGenerator?.Progress ?? 0;

        // 0 covers both "not started" and the reset the generator does on the way out; a bare
        // "0%" reads as stuck.
        return progress <= 0
            ? "Building your insights report…"
            : $"Building your insights report… {progress}%";
    }

    // ══ Projection ══════════════════════════════════════════════════════════
    //
    // Moved verbatim from BudgetView, which owned this as its fourth tab. It sits with the
    // scenarios because both are forward-looking. The markup still binds to BudgetViewModel,
    // so the Projection tab's content root gets that as a sub-tree BindingContext (set in the
    // constructor) and every binding path in the moved XAML resolves unchanged.


    /// <summary>
    /// The colour a summary-box figure should be, given whether it is negative.
    /// </summary>
    /// <remarks>
    /// Reuses NegativeToHighlightColorConverter's sibling rather than looking the keys up here, so
    /// code-written tiles and bound tiles cannot disagree — including the fallback when a key is
    /// missing, which is "looks unstyled" rather than "throws".
    ///
    /// LoanAppNegativeValueOnHighlightColor, not LoanAppNegativeValueColor: these figures sit on
    /// the highlight card, which needs its own red to stay legible against that background. Both
    /// keys are defined in all four themes.
    /// </remarks>
    private static readonly NegativeToThemeColorConverter TileFigureColourConverter =
        new() { NegativeKey = "LoanAppNegativeValueOnHighlightColor" };

    private static Color TileFigureColour(bool isNegative) =>
        (Color)TileFigureColourConverter.Convert(
            isNegative, typeof(Color), "LoanAppHighlightFgColor", CultureInfo.InvariantCulture);

    private void UpdateProjectionSummaryBar()
    {
        var sym = _budgetViewModel.CurrencySymbol ?? "$";
        var incomeTotal = _budgetViewModel.Income.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;
        var expenseTotal = _budgetViewModel.Expense.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;

        // The line break is part of the text written here, rather than the separate
        // `<Span Text="{Binding NewLine}" />` the other three-box summaries use. Those Spans sit
        // inside a FormattedString assigned to a BindableProperty of a custom control, and on this
        // tab the break did not appear — the figure ran straight into its caption. A Span is not a
        // VisualElement and does not carry its own BindingContext, so whether that binding resolves
        // depends on what the FormattedString inherited and when, which differs here because this
        // tab's BindingContext is reassigned to the Budget view model at runtime rather than being
        // the page's own.
        //
        // Writing the break alongside the figure removes the question. The amounts were already
        // being set from code, so this costs nothing and cannot silently stop working.
        ProjectionIncomeSpan.Text = $"{sym}{incomeTotal:N0}{Environment.NewLine}";
        ProjectionExpenseSpan.Text = $"{sym}{expenseTotal:N0}{Environment.NewLine}";

        // Projected income goes negative whenever the deductions outweigh the income, which the
        // two "take off" switches make easy to do. Coloured here rather than by a binding because
        // these Spans are written from code — and set on every refresh, which is also when a
        // theme change would be picked up, so it stays correct without a DynamicResource.
        //
        // Same converter the Budget summary boxes bind, so the key resolution and its fallback are
        // shared rather than reimplemented.
        // The expense total cannot currently go below zero (it only ever has costs added to it),
        // but it is passed through the same call rather than left alone: if that ever changes, it
        // picks the colouring up on its own instead of being the one tile that quietly does not.
        ProjectionIncomeSpan.TextColor = TileFigureColour(incomeTotal < 0);
        ProjectionExpenseSpan.TextColor = TileFigureColour(expenseTotal < 0);

        // The horizon is stated in the intro sentence rather than in a summary box of its own —
        // see the note on LblProjectionIntro in the XAML.
        //
        // No longer says "at the rates below": the rates now live in a panel that is collapsed by
        // default and sits under the chart, so "below" pointed at nothing visible. The settings
        // summary line on that panel's header states them instead, which is where someone looking
        // for them would now be. Shortened too, because this sentence is pinned and every line it
        // takes is a line the chart does not get.
        var years = _budgetViewModel.ProjectionYears;
        LblProjectionIntro.Text =
            $"Where your money goes over the next {years} year{(years == 1 ? "" : "s")}.";

        LblProjectionYears.Text = years.ToString();

        // Sync slider switch states to VM values
        SwitchIncludeExpenses.IsOn = _budgetViewModel.Income.IncludeExpenses;
        SwitchIncludePropertyExpenses.IsOn = _budgetViewModel.Income.IncludePropertyExpenses;
        SwitchExpensePropertyExpense.IsOn = _budgetViewModel.Expense.IncludePropertyExpenses;
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

    /// <remarks>
    /// Sets IncludePropertyExpenses, NOT ShowPropertyExpense. This switch did nothing at all
    /// before: it wrote ShowPropertyExpense while ExpenseViewModel.UpdateProjectionData reads
    /// IncludePropertyExpenses, so the projection never saw the change — no movement in the
    /// totals, the chart or the tables, and no error either.
    ///
    /// IncludePropertyExpenses is the projection flag (its backing field is literally
    /// _includePropertyExpensesInProjection, and its setter kicks off a projection rebuild).
    /// ShowPropertyExpense is the BUDGET Expenses tab's own display toggle — writing it from here
    /// would also have silently changed that tab's headline totals, which is not this switch's
    /// business.
    /// </remarks>
    private void OnExpensePropertyExpenseChanged(object sender, Syncfusion.Maui.Buttons.SwitchStateChangedEventArgs e)
    {
        _budgetViewModel.Expense.IncludePropertyExpenses = e.NewValue == true;
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

                // Expense FIRST, then Income — the order matters and it used to be the other way
                // round. "Take off my everyday spending" makes IncomeViewModel.UpdateProjectionData
                // read ExpenseSummary.TransactionRecords.IncomeExpenseSummary.TotalYearly, and that
                // total is only refreshed by the SumUpData() call inside the EXPENSE view model's
                // own UpdateProjectionData. Running income first meant it deducted whatever the
                // expense total happened to be from the previous pass.
                _budgetViewModel.Expense.UpdateProjectionData();
                _budgetViewModel.Income.UpdateProjectionData();
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
            // Scrim the build-and-push only, not page.Completion — see OnSeeScheduleTapped.
            // Configure() is inside it because that is where the scenario's own view is created
            // and where the explainer card is appended, which is the slow part on a first open.
            ScenarioPage? page = null;

            // Titles are short nouns — "Rate change", "Offset account" — so they need the article
            // and the word "scenario" around them to read as a sentence.
            var scrimMessage = $"Opening the {ScenarioExplanations.For(scenario).Title.ToLowerInvariant()} scenario…";

            await BusyOverlay.RunWithScrimAsync(scrimMessage, async () =>
            {
                page = ServiceLocator.GetService<ScenarioPage>();
                page.Configure(scenario);

                await Navigation.PushModalAsync(page);
            });

            if (page is not null) await page.Completion;
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
