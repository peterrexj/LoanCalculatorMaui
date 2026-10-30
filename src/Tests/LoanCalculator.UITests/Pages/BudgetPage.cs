using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

public sealed class BudgetPage(AppDriver app) : PageBase(app)
{
    public const string IncomeAddFab = "BudgetIncomeAddFab";
    public const string ExpenseAddFab = "BudgetExpenseAddFab";

    public const string IncomeMonthlyTotal = "BudgetIncomeMonthlyTotal";
    public const string ExpenseMonthlyTotal = "BudgetExpenseMonthlyTotal";
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

    // Anchored on the caption, not a total: with no budget data the Summary tab shows an
    // empty-state panel instead of the totals, so a total is not proof the tab rendered.
    public void OpenSummaryTab()
    {
        EnsureOnBudget();
        OpenTab("BudgetTabSummary", "BudgetSummaryCaption");
    }

    /// <summary>True when the Summary tab is showing totals rather than its empty state.</summary>
    public bool HasSummaryTotals()
    {
        OpenSummaryTab();
        if (App.Exists(IncomeMonthlyTotal, TimeSpan.FromSeconds(2))) return true;

        try
        {
            // Probe, not navigation: an empty budget legitimately has no totals, so keep the hunt
            // short rather than swiping the whole page to confirm a "no".
            OpenSummaryTab();
            App.ScrollTo(IncomeMonthlyTotal, maxSwipes: 6);
            return true;
        }
        catch (AssertionException)
        {
            return false;
        }
    }

    /// <summary>
    /// Brings the Summary totals on screen. Required on Android, where the accessibility tree
    /// omits ScrollView children that are far off-screen entirely — so they are not merely
    /// invisible, they are absent, and nothing can auto-scroll to them.
    /// </summary>
    public void ScrollToTotals()
    {
        OpenSummaryTab();
        App.ScrollTo(IncomeMonthlyTotal);
    }

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

    public decimal MonthlyIncome() => ReadTotal(IncomeMonthlyTotal);

    public decimal MonthlyExpenses() => ReadTotal(ExpenseMonthlyTotal);

    public decimal MonthlyNet() => ReadTotal(NetMonthly);

    /// <summary>
    /// A total from the Summary tab, or 0 when the budget is empty — an empty budget shows a
    /// placeholder instead of the totals, and callers measuring a delta need a baseline either
    /// way. <see cref="HasSummaryTotals"/> scrolls them into view.
    /// </summary>
    private decimal ReadTotal(string automationId) =>
        HasSummaryTotals() ? App.Number(automationId) : 0m;

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
