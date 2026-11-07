using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

public sealed class BudgetPage(AppDriver app) : PageBase(app)
{
    public const string IncomeAddFab = "BudgetIncomeAddFab";
    public const string ExpenseAddFab = "BudgetExpenseAddFab";

    // ── Summary tab figures ──────────────────────────────────────────────────────
    //
    // The hero card states the net; the Insights card below it breaks that net down into the
    // three figures it is made of. There is no longer a single "monthly income total" label:
    // BudgetIncomeMonthlyTotal / BudgetExpenseMonthlyTotal were removed in the redesign and
    // these rows replaced them.

    public const string InsightsIncome = "BudgetInsightsIncome";
    public const string InsightsLivingExpenses = "BudgetInsightsLivingExpenses";

    /// <summary>
    /// The asset's own running costs — maintenance, insurance, rates. A separate figure from
    /// living expenses, and one the net DOES subtract (see <see cref="MonthlyNet"/>), so a
    /// reconciliation that omits it is wrong by exactly this amount.
    /// </summary>
    public const string InsightsAssetCosts = "BudgetInsightsAssetCosts";

    public const string NetMonthly = "BudgetNetMonthly";
    public const string NetYearly = "BudgetNetYearly";
    public const string AffordabilityVerdict = "BudgetAffordabilityVerdict";

    /// <summary>The "income after expenses" switch on the Income tab.</summary>
    public const string IncomeAfterExpenseSwitch = "BudgetIncomeAfterExpenseSwitch";

    /// <summary>
    /// The three-box summary blocks at the top of the Income and Expenses tabs. Their content is
    /// built from <c>Span</c>s, which cannot be tagged individually, so each is read as one block
    /// — the same approach as <c>LoanPage.SummaryBox</c>.
    /// </summary>
    public const string IncomeTabSummaryBox = "BudgetIncomeTabSummaryBox";

    public const string ExpenseTabSummaryBox = "BudgetExpenseTabSummaryBox";

    // ── In-page tabs ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the Budget destination first. Since Projection moved to What If, a caller can
    /// easily be standing on another shell tab by the time it asks for a Budget sub-tab — and
    /// tapping a sub-tab header that belongs to a page you are not on just times out.
    /// </summary>
    private void EnsureOnBudget() => new ShellTabs(App).GoTo(AppTab.Budget);

    public void OpenIncomeTab()
    {
        EnsureOnBudget();
        OpenTab("BudgetTabIncome", IncomeAddFab);
    }

    public void OpenExpensesTab()
    {
        EnsureOnBudget();
        OpenTab("BudgetTabExpenses", ExpenseAddFab);
    }

    /// <summary>
    /// Elements belonging to the Summary tab's content, spread down its whole length — hero at the
    /// top, caption mid-page, Insights below that, the verdict near the bottom. Any one of them
    /// being <em>visible</em> proves the tab is the active one.
    /// </summary>
    /// <remarks>
    /// A set rather than a single anchor, because the Summary tab is a long ScrollView and this
    /// has to hold at <b>any</b> scroll offset. Two separate things defeat a single anchor:
    /// <list type="bullet">
    /// <item>the tab keeps its scroll position, so a mid-page anchor is scrolled out of view —
    /// and worse, partially <em>behind the pinned tab strip</em>, where it is in the tree with
    /// <c>visible="false"</c>; and</item>
    /// <item>the hero card, three-box row and chart are all hidden on an empty budget, so a
    /// total is not proof the tab rendered either.</item>
    /// </list>
    /// <see cref="InsightsIncome"/> and the caption survive an empty budget; the other two survive
    /// being scrolled to the bottom. Between them, something is always on screen.
    /// </remarks>
    private static readonly string[] SummaryTabMarkers =
        [NetMonthly, "BudgetSummaryCaption", InsightsIncome, AffordabilityVerdict];

    /// <summary>
    /// Selects the Summary tab, and is a genuine no-op when it is already selected <em>at any
    /// scroll offset</em>.
    /// </summary>
    /// <remarks>
    /// Does not use <c>OpenTab</c>. That waits for one fixed anchor to become visible, and
    /// re-tapping an already-selected Syncfusion tab does not scroll it back to the top — so once
    /// a reader had scrolled down to the Insights rows, every later call sat waiting 20s for an
    /// anchor that was never going to reappear. It then failed as "'BudgetSummaryCaption' IS in
    /// the element tree but is not visible", which reads as a locator or keyboard problem rather
    /// than as the tab already being open and merely scrolled.
    /// </remarks>
    public void OpenSummaryTab()
    {
        EnsureOnBudget();

        if (OnSummaryTab()) return;

        // A pushed page covers the tab strip too, so clear it before reaching for a tab.
        ModalPages.DismissAny(App);
        App.Tap("BudgetTabSummary");

        App.WaitUntil(
            OnSummaryTab,
            "the Budget Summary tab to render its content",
            TimeSpan.FromSeconds(20));
    }

    private bool OnSummaryTab() =>
        SummaryTabMarkers.Any(id => App.Exists(id, TimeSpan.FromMilliseconds(400)));

    /// <summary>True when the Summary tab is showing totals rather than its empty state.</summary>
    /// <remarks>
    /// Probes the hero <see cref="NetMonthly"/> and <b>not</b> an Insights row. Only the hero
    /// card, the three-box row and the chart are hidden on an empty budget
    /// (<c>BudgetView.xaml.cs</c>, the <c>HasData</c> block); the Insights card is left visible
    /// and renders zeroes — so probing it would return true on a wiped device and quietly defeat
    /// the empty-budget baseline <see cref="ReadMagnitude"/> depends on.
    /// </remarks>
    public bool HasSummaryTotals() => TryReveal(NetMonthly, out _);

    // OpenProjectionTab moved to WhatIfPage — Projection is a What If sub-tab now.

    // Moved from LoanPage.OpenRunningCostTab — Running Cost became this "Asset costs" tab. The
    // filter box is the only unconditional element in this tab's content — the summary figures
    // are Spans (not addressable) and the list and its empty-state Border swap out depending on
    // whether any expenses are recorded.
    public void OpenAssetCostsTab()
    {
        EnsureOnBudget();
        OpenTab("BudgetTabAssetCosts", "LoanRunningCostFilter");
    }

    // ── Add income / expense ─────────────────────────────────────────────────────

    public void AddIncome(string name, string amount)
    {
        OpenIncomeTab();
        App.Tap(IncomeAddFab);
        App.WaitUntilVisible("BudgetIncomeNameEntry", TimeSpan.FromSeconds(15));

        App.EnterText("BudgetIncomeNameEntry", name);
        App.EnterText("BudgetIncomeAmountEntry", amount);

        App.Tap("BudgetIncomeSaveButton");
        App.WaitUntilGone("BudgetIncomeNameEntry", TimeSpan.FromSeconds(15));
    }

    public void AddExpense(string name, string amount)
    {
        OpenExpensesTab();
        App.Tap(ExpenseAddFab);
        App.WaitUntilVisible("BudgetExpenseNameEntry", TimeSpan.FromSeconds(15));

        App.EnterText("BudgetExpenseNameEntry", name);
        App.EnterText("BudgetExpenseAmountEntry", amount);

        App.Tap("BudgetExpenseSaveButton");
        App.WaitUntilGone("BudgetExpenseNameEntry", TimeSpan.FromSeconds(15));
    }

    // ── Editing and deleting rows ────────────────────────────────────────────────
    //
    // Rows are keyed off their Name — "IncomeRow_<name>" — so a test that chose the name can
    // address exactly its own row. Use names without spaces.
    //
    // The rows used to carry an edit and a delete button each ("IncomeEdit_<name>" /
    // "IncomeDelete_<name>"). They are now a single tap target that opens the entry page, per the
    // prototype, and **delete moved onto that page**. So deleting is two steps here rather than
    // one, and both helpers below go through the same row tap.

    public bool IncomeRowExists(string name)
    {
        OpenIncomeTab();
        return App.Exists($"IncomeRow_{name}", TimeSpan.FromSeconds(3));
    }

    public void DeleteIncomeRow(string name)
    {
        OpenIncomeTab();
        var row = $"IncomeRow_{name}";
        App.ScrollTo(row);
        App.Tap(row);

        // Delete lives at the end of the entry form, so it needs scrolling to — unlike Save,
        // which is in the header precisely because the keyboard covers the bottom of the page.
        App.WaitUntilVisible("BudgetIncomeAmountEntry", TimeSpan.FromSeconds(15));
        App.ScrollTo("BudgetIncomeDeleteButton");
        App.Tap("BudgetIncomeDeleteButton");

        // Deleting raises a native "Do you wish to delete ...?" confirmation, the same one
        // Settings' delete buttons raise. It is modal, so skipping it does not merely fail this
        // step — it leaves an alert covering every later lookup, which surfaces as unrelated
        // "could not find ..." failures further along the test.
        App.TapText("Yes", TimeSpan.FromSeconds(10));
        App.WaitUntilTextGoneIfPresent("Yes", TimeSpan.FromSeconds(10));

        App.WaitUntilGone("BudgetIncomeAmountEntry", TimeSpan.FromSeconds(15));
        App.WaitUntilGone(row, TimeSpan.FromSeconds(15));
    }

    /// <summary>Opens a row's edit form, replaces the amount, and saves.</summary>
    public void EditIncomeRowAmount(string name, string newAmount)
    {
        OpenIncomeTab();
        var row = $"IncomeRow_{name}";
        App.ScrollTo(row);
        App.Tap(row);

        App.WaitUntilVisible("BudgetIncomeAmountEntry", TimeSpan.FromSeconds(15));
        App.EnterText("BudgetIncomeAmountEntry", newAmount);

        App.Tap("BudgetIncomeSaveButton");
        App.WaitUntilGone("BudgetIncomeAmountEntry", TimeSpan.FromSeconds(15));
    }

    // ── Summary values ───────────────────────────────────────────────────────────

    // Every figure on this tab is now rendered WITH a sign — "+$6,000", "-$2,500", "-$0" — where
    // the old single Net label was unsigned. That makes the sign convention load-bearing for the
    // assertions, and getting it wrong inverts them silently rather than failing:
    //
    //   Income / Living expenses / Asset costs → returned as MAGNITUDES. Their sign is a fixed
    //     display convention (expenses are always drawn negative), not data, so a test asserting
    //     "expenses went up" wants the magnitude to rise. Reading them signed would make that
    //     assertion need `<`, which reads as a typo.
    //   Net (monthly and yearly) → returned SIGNED. There the sign IS the data: it is the verdict
    //     on whether more comes in than goes out.

    public decimal MonthlyIncome() => ReadMagnitude(InsightsIncome);

    public decimal MonthlyExpenses() => ReadMagnitude(InsightsLivingExpenses);

    public decimal MonthlyAssetCosts() => ReadMagnitude(InsightsAssetCosts);

    public decimal MonthlyNet() => ReadSigned(NetMonthly);

    public decimal YearlyNet() => ReadSigned(NetYearly);

    /// <summary>
    /// Waits for one of the Summary figures to settle, comparing on its MAGNITUDE — so callers
    /// stay out of the sign convention above. Returns the settled magnitude.
    /// </summary>
    public decimal WaitForSummaryMagnitude(
        string automationId,
        Func<decimal, bool> settled,
        string describeExpectation)
    {
        Reveal(automationId);

        var shown = App.WaitForText(
            automationId,
            text => settled(Magnitude(text)),
            describeExpectation);

        return Magnitude(shown);
    }

    /// <summary>
    /// A Summary figure's magnitude, or 0 when the budget is empty — an empty budget shows a
    /// placeholder instead of the totals, and callers measuring a delta need a baseline either way.
    /// </summary>
    private decimal ReadMagnitude(string automationId) =>
        TryReveal(automationId, out var text) ? Magnitude(text) : 0m;

    /// <summary><inheritdoc cref="ReadMagnitude"/> Keeps the displayed sign.</summary>
    private decimal ReadSigned(string automationId) =>
        TryReveal(automationId, out var text) ? Signed(text) : 0m;

    /// <summary>
    /// Brings one figure into view <b>from wherever the tab is currently scrolled</b> and returns
    /// its text.
    /// </summary>
    /// <remarks>
    /// <para>Searches down and then up, and that second direction is the whole point:
    /// <c>AppDriver.ScrollTo</c> only swipes one way, and these figures are at opposite ends of a
    /// long tab — the hero net at the very top, the Insights rows well below it. So reading the
    /// income row leaves the hero scrolled off the TOP of the viewport, and a downward-only hunt
    /// for it afterwards swipes further away from it, burns its budget and reports it as absent.
    /// </para>
    /// <para>That is not hypothetical: it is what made the reconciliation test read
    /// <c>11500, 0, 0, 0</c> — the first figure correct, then the "empty budget" baseline for
    /// every figure after it, because each subsequent read concluded the totals were not showing.
    /// It presents as an app inconsistency (income without a matching net) rather than as a
    /// scroll-position problem, so it is worth keeping each reader position-independent rather
    /// than relying on call order.</para>
    /// <para>Scrolling is required rather than optional on Android, where the accessibility tree
    /// omits far-off-screen ScrollView children entirely — they are absent, not merely invisible,
    /// and nothing can auto-scroll to them.</para>
    /// </remarks>
    private string Reveal(string automationId)
    {
        OpenSummaryTab();

        try
        {
            return App.ScrollTo(automationId, maxSwipes: 8).Text;
        }
        catch (AssertionException)
        {
            return App.ScrollTo(automationId, maxSwipes: 8, up: true).Text;
        }
    }

    /// <summary>
    /// <see cref="Reveal"/>, but treating "not on the tab at all" as the empty-budget case rather
    /// than a failure — an empty budget legitimately has no totals, and callers measuring a delta
    /// need a baseline either way.
    /// </summary>
    private bool TryReveal(string automationId, out string text)
    {
        try
        {
            text = Reveal(automationId);
            return true;
        }
        catch (AssertionException)
        {
            text = string.Empty;
            return false;
        }
    }

    /// <summary>-1 on unparseable text, which no real figure can be, so it fails an assertion
    /// loudly rather than passing as a plausible zero.</summary>
    private const decimal Unreadable = -1m;

    private static decimal Magnitude(string text)
    {
        var value = Signed(text);
        return value == Unreadable ? Unreadable : Math.Abs(value);
    }

    /// <summary>
    /// Keeps a minus sign and discards everything else that is not a digit or a decimal point, so
    /// `-$2,500` reads as -2500 and `+$42,000 a year` as 42000.
    /// </summary>
    private static decimal Signed(string text)
    {
        var digits = new string(text.Where(c => char.IsDigit(c) || c is '.' or '-').ToArray());
        return decimal.TryParse(digits, out var value) ? value : Unreadable;
    }

    public string Affordability()
    {
        OpenSummaryTab();
        return App.ScrollTo(AffordabilityVerdict).Text;
    }

    // ── "Income after expenses" toggle (TECH-DEBT D1) ────────────────────────────

    /// <summary>
    /// Turns on the Income tab's "income after expenses" display toggle and returns the Income
    /// tab's summary block afterwards.
    /// </summary>
    /// <remarks>
    /// This toggle used to write its deduction into the summary shared by every other screen, so
    /// turning it on changed Budget's Net and the Expenses tab's "Monthly Income". It is a
    /// Syncfusion <c>SfSwitch</c>: it can be tapped, but its state is drawn rather than exposed,
    /// so callers must verify the effect through the figures it changes rather than by reading
    /// the switch.
    /// </remarks>
    public void TurnOnIncomeAfterExpenses() => ToggleIncomeAfterExpenses();

    /// <summary>
    /// Returns the toggle to off. It persists, so a test that flipped it must put it back or it
    /// changes the starting state of every later test and run.
    /// </summary>
    public void TurnOffIncomeAfterExpenses() => ToggleIncomeAfterExpenses();

    private void ToggleIncomeAfterExpenses()
    {
        OpenIncomeTab();
        App.ScrollTo(IncomeAfterExpenseSwitch);
        App.Tap(IncomeAfterExpenseSwitch);
    }

    // ── The ⓘ explanation on a switch row ────────────────────────────────────

    public const string IncomeAfterExpenseInfo = "BudgetIncomeAfterExpenseInfo";
    public const string IncomeAfterExpenseDetail = "BudgetIncomeAfterExpenseDetail";

    /// <summary>
    /// Opens the ⓘ on the Income tab's FIRST switch and returns the explanation it reveals.
    /// </summary>
    /// <remarks>
    /// The first row specifically, because that is the one that failed: the row's copy is chosen by
    /// a <c>BudgetToggle</c> BindableProperty, and the first row's value is the same as that
    /// property's default — so MAUI saw no change, never raised <c>propertyChanged</c>, and the row
    /// rendered with no label, no sub-line and no detail while every other row was fine.
    ///
    /// Only a UI test can catch that class of bug. The copy itself lives in Core and was complete
    /// and unit-tested the whole time; what was broken was the control failing to apply it.
    /// </remarks>
    public string ExpandIncomeAfterExpenseExplanation()
    {
        OpenIncomeTab();
        App.ScrollTo(IncomeAfterExpenseInfo);
        App.Tap(IncomeAfterExpenseInfo);

        return App.WaitForText(
            IncomeAfterExpenseDetail,
            shown => shown.Trim().Length > 0,
            "the switch's ⓘ revealed its explanation");
    }

    /// <summary>The Income tab's summary block as text — assert on what it contains.</summary>
    public string IncomeTabSummaryText()
    {
        OpenIncomeTab();
        App.ScrollTo(IncomeTabSummaryBox);
        return App.TextWithin(IncomeTabSummaryBox);
    }

    /// <summary>The Expenses tab's summary block, which includes its "Monthly Income" figure.</summary>
    public string ExpenseTabSummaryText()
    {
        OpenExpensesTab();
        App.ScrollTo(ExpenseTabSummaryBox);
        return App.TextWithin(ExpenseTabSummaryBox);
    }
}
