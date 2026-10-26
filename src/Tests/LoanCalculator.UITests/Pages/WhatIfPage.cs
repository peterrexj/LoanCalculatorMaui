using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

/// <summary>
/// Drives the What If destination.
/// </summary>
/// <remarks>
/// The nine scenarios used to be nine cards on one long scroll, so every control was reachable
/// with a <c>ScrollTo</c>. They are now behind a tap on the scenario index, each opening its own
/// page. That change is absorbed HERE rather than in the tests: every method below opens the
/// scenario it needs first, so the test bodies that call them did not have to change at all.
/// </remarks>
public sealed class WhatIfPage(AppDriver app) : PageBase(app)
{
    public const string PageTitle = "WhatIfPageTitle";
    public const string NoDataPanel = "WhatIfNoDataPanel";

    // ── Scenario index rows ──────────────────────────────────────────────────
    public const string ScenarioRateChange = "WhatIfScenarioRateChange";
    public const string ScenarioStressTest = "WhatIfScenarioStressTest";

    /// <summary>
    /// The headline figure on the Rate change index row. Each row carries its own delta, so this
    /// is what proves a row rendered *with its figure* rather than merely being present — the
    /// thing the old nine-card assertion on <see cref="RateDeltaValue"/> was really checking.
    /// </summary>
    /// <remarks>
    /// Every delta binds an outcome property that defaults to <c>"--"</c>, never <c>""</c>. That
    /// matters for finding them: an empty label lays out at zero height and iOS does not report it
    /// as a *visible* element, so a failed binding here surfaces as "no element with that
    /// AutomationId" and sends you looking at the markup attribute rather than the binding path.
    /// Three rows (Loan term, Deposit scenarios, Repayment frequency) carry no delta yet — their
    /// scenarios are three-column comparisons with no single canonical summary figure.
    /// </remarks>
    public const string ScenarioRateChangeDelta = "WhatIfScenarioRateChangeDelta";

    public const string ScenarioExtraRepaymentDelta = "WhatIfScenarioExtraRepaymentDelta";
    public const string ScenarioLumpSumDelta = "WhatIfScenarioLumpSumDelta";
    public const string ScenarioOffsetDelta = "WhatIfScenarioOffsetDelta";
    public const string ScenarioCombinedDelta = "WhatIfScenarioCombinedDelta";
    public const string ScenarioStressTestDelta = "WhatIfScenarioStressTestDelta";
    public const string ScenarioTermComparisonDelta = "WhatIfScenarioTermComparisonDelta";
    public const string ScenarioDepositScenariosDelta = "WhatIfScenarioDepositScenariosDelta";
    public const string ScenarioRepaymentFrequencyDelta = "WhatIfScenarioRepaymentFrequencyDelta";

    /// <summary>
    /// Every scenario row's headline figure, in the order the index shows them. All nine carry one
    /// now; the three comparison scenarios (term, deposit, frequency) gained theirs last.
    /// </summary>
    /// <remarks>
    /// <c>NeedsBudget</c> marks the rows that cannot produce a figure from a loan alone. The
    /// stress test measures headroom against income and expenses, so with only a loan seeded its
    /// scenario is unavailable and the row shows the placeholder — correctly. Asserting a real
    /// figure there without budget data would be asserting a bug into existence.
    /// </remarks>
    public static readonly (string Row, string Id, bool NeedsBudget)[] AllScenarioDeltas =
    [
        ("Rate change",         ScenarioRateChangeDelta,          false),
        ("Extra repayment",     ScenarioExtraRepaymentDelta,      false),
        ("Loan term",           ScenarioTermComparisonDelta,      false),
        ("Deposit",             ScenarioDepositScenariosDelta,    false),
        ("Lump sum",            ScenarioLumpSumDelta,             false),
        ("Repayment frequency", ScenarioRepaymentFrequencyDelta,  false),
        ("Offset",              ScenarioOffsetDelta,              false),
        ("Stress test",         ScenarioStressTestDelta,          true),
        ("Combined",            ScenarioCombinedDelta,            false),
    ];

    private const string ScenarioTitle = "ScenarioTitle";
    private const string ScenarioBack = "ScenarioBackButton";

    // ── Inside a scenario page ───────────────────────────────────────────────
    public const string RateDeltaMinus = "WhatIfRateDeltaMinus";
    public const string RateDeltaValue = "WhatIfRateDeltaValue";
    public const string RateDeltaPlus = "WhatIfRateDeltaPlus";
    public const string NewRate = "WhatIfRateNewRate";
    public const string NewMonthly = "WhatIfRateNewMonthly";

    public const string StressUnavailablePanel = "WhatIfStressUnavailablePanel";
    public const string StressResultsPanel = "WhatIfStressResultsPanel";

    /// <summary>Always present on the stress-test scenario, unlike either of its two panels.</summary>
    private const string StressTestCard = "WhatIfStressTestCard";

    /// <summary>
    /// Swipe budget for the two stress-test probes. One of them is *meant* to return false, so a
    /// full-page hunt would spend 12 swipes to learn what a short look already tells us.
    /// </summary>
    private const int ProbeSwipes = 6;

    // ── Sub-tabs ─────────────────────────────────────────────────────────────
    // Answer 0, Scenarios 1, Projection 2, Insights 3. Projection arrived from Budget and
    // Insights from Loan; both kept their original content anchors, so only the route changed.
    private const string TabAnswer = "WhatIfTabAnswer";
    private const string TabScenarios = "WhatIfTabScenarios";
    private const string TabProjection = "WhatIfTabProjection";
    private const string TabInsights = "WhatIfTabInsights";

    /// <summary>
    /// Each sub-tab helper navigates to the What If destination first. Projection and Insights
    /// arrived here from Budget and Loan, so their callers are often standing on a different
    /// shell tab — and tapping a sub-tab header that belongs to a page you are not on simply
    /// times out. Owning the navigation here is what let those call sites stay unchanged.
    /// </summary>
    private void EnsureOnWhatIf() => new ShellTabs(App).GoTo(AppTab.WhatIf);

    public void OpenAnswerTab()
    {
        EnsureOnWhatIf();
        OpenTab(TabAnswer, "WhatIfAnswerCard");
    }

    public void OpenScenariosTab()
    {
        EnsureOnWhatIf();
        OpenTab(TabScenarios, ScenarioRateChange);
    }

    /// <summary>Moved here from BudgetPage when Projection left Budget.</summary>
    public void OpenProjectionTab()
    {
        EnsureOnWhatIf();
        OpenTab(TabProjection, "BudgetProjectionYearsSlider");
    }

    /// <summary>Moved here from LoanPage when Insights left Loan.</summary>
    public void OpenInsightsTab()
    {
        EnsureOnWhatIf();
        OpenTab(TabInsights, "LoanInsightsExportButton");
    }

    /// <summary>The scenario index is hidden until the Loan tab has data.</summary>
    public bool ShowsNoDataPlaceholder() => App.Exists(NoDataPanel, TimeSpan.FromSeconds(3));

    /// <summary>
    /// Opens a scenario from the index, or does nothing if it is already open. Idempotent so
    /// consecutive calls against the same scenario cost one existence check rather than a
    /// dismiss-and-reopen cycle.
    /// </summary>
    private void OpenScenario(string rowAutomationId, string contentAnchor)
    {
        if (App.Exists(contentAnchor, TimeSpan.FromMilliseconds(600))) return;

        // A different scenario may be open; the index is only reachable once it is dismissed.
        if (App.Exists(ScenarioTitle, TimeSpan.FromMilliseconds(600)))
            App.Tap(ScenarioBack);

        // The index lives in the Scenarios SUB-TAB, and arriving at What If lands on Answer.
        // Without this the scroll hunts for a scenario row on a tab that does not contain any,
        // and burns its full swipe budget before failing.
        OpenScenariosTab();

        App.ScrollTo(rowAutomationId);
        App.Tap(rowAutomationId);
        App.WaitUntilVisible(contentAnchor, TimeSpan.FromSeconds(20));
    }

    /// <summary>Returns to the index if a scenario is open. Safe to call when it is not.</summary>
    public void CloseScenario()
    {
        if (App.Exists(ScenarioTitle, TimeSpan.FromMilliseconds(600)))
            App.Tap(ScenarioBack);
    }

    // ── The explanation card ─────────────────────────────────────────────────
    //
    // Appended below the scenario itself by ScenarioPage, so every scenario has one and none of
    // the existing in-card anchors moved.

    public const string ExplainerHeader = "ScenarioExplainerHeader";

    /// <summary>
    /// The card's body anchor. Deliberately the "why this matters" paragraph rather than the
    /// enclosing layout: a layout is not reliably exposed as an accessibility element, and
    /// <c>ScenarioExplanationsTests</c> guarantees this paragraph is non-empty for all nine
    /// scenarios — an empty Label would measure to zero height and be reported as absent.
    /// </summary>
    public const string ExplainerBody = "ScenarioExplainerBody";

    public const string ExplainerPersonalLine = "ScenarioExplainerPersonalLine";

    /// <summary>
    /// Expands the explanation card on whichever scenario is open, and returns its body text.
    /// </summary>
    /// <remarks>
    /// The card is collapsed by default and sits at the very bottom of the page, below the chart,
    /// so it always needs scrolling to. If the header taps but the body never appears on iOS,
    /// suspect <c>ScenarioPage.ApplyExpanderTouchFix</c> rather than this locator — a Syncfusion
    /// drawing overlay swallowing the header tap is the documented failure here, and it is silent.
    /// </remarks>
    public string ExpandExplanation()
    {
        App.ScrollTo(ExplainerHeader);

        if (!App.Exists(ExplainerBody, TimeSpan.FromMilliseconds(600)))
            App.Tap(ExplainerHeader);

        return App.WaitForText(
            ExplainerBody,
            shown => shown.Trim().Length > 0,
            "the explanation card opened and rendered its text");
    }

    // ── Rate change ──────────────────────────────────────────────────────────

    private void OpenRateChange() => OpenScenario(ScenarioRateChange, RateDeltaValue);

    public string RateDelta()
    {
        OpenRateChange();
        return App.ScrollTo(RateDeltaValue).Text;
    }

    public void IncreaseRate(int steps = 1)
    {
        OpenRateChange();
        App.ScrollTo(RateDeltaPlus);
        App.TapRepeatedly(RateDeltaPlus, steps);
    }

    public void DecreaseRate(int steps = 1)
    {
        OpenRateChange();
        App.ScrollTo(RateDeltaMinus);
        App.TapRepeatedly(RateDeltaMinus, steps);
    }

    public decimal NewMonthlyRepayment()
    {
        OpenRateChange();
        App.ScrollTo(NewMonthly);
        return App.Number(NewMonthly);
    }

    public string NewInterestRate()
    {
        OpenRateChange();
        return App.ScrollTo(NewRate).Text;
    }

    // ── Stress test ──────────────────────────────────────────────────────────
    //
    // The stress test needs income and expenses as well as a loan, so it shows a prompt until
    // the Budget tab has data. Both probes open the scenario first, and one of them is expected
    // to come back false — which is why neither asserts.

    public bool StressTestResultsShowing() => StressPanelShowing(StressResultsPanel);

    public bool StressTestPromptShowing() => StressPanelShowing(StressUnavailablePanel);

    private bool StressPanelShowing(string panel)
    {
        // Arrival is anchored on the card root, NOT on the panel being probed. The scenario
        // renders exactly one of the two panels, so anchoring on the probed one would make the
        // expected-false case wait out the full 20s timeout before concluding what a 6-swipe
        // look already answers.
        OpenScenario(ScenarioStressTest, StressTestCard);

        try
        {
            App.ScrollTo(panel, maxSwipes: ProbeSwipes);
            return true;
        }
        catch (AssertionException)
        {
            return false;
        }
    }
}
