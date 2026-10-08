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

    public void OpenIncomeTab() => OpenTab("BudgetTabIncome", IncomeAddFab);

    public void OpenExpensesTab() => OpenTab("BudgetTabExpenses", ExpenseAddFab);

    // Anchored on the caption, not a total: with no budget data the Summary tab shows an
    // empty-state panel instead of the totals, so a total is not proof the tab rendered.
    public void OpenSummaryTab() => OpenTab("BudgetTabSummary", "BudgetSummaryCaption");

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

    public void OpenProjectionTab() => OpenTab("BudgetTabProjection", "BudgetProjectionYearsSlider");

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
    // Row action buttons are keyed off the row's Name — "IncomeDelete_<name>" — so a test that
    // chose the name can address exactly its own row. Use names without spaces.

    public bool IncomeRowExists(string name)
    {
        OpenIncomeTab();
        return App.Exists($"IncomeDelete_{name}", TimeSpan.FromSeconds(3));
    }

    public void DeleteIncomeRow(string name)
    {
        OpenIncomeTab();
        var deleteButton = $"IncomeDelete_{name}";
        App.ScrollTo(deleteButton);
        App.Tap(deleteButton);
        App.WaitUntilGone(deleteButton, TimeSpan.FromSeconds(15));
    }

    /// <summary>Opens a row's edit form, replaces the amount, and saves.</summary>
    public void EditIncomeRowAmount(string name, string newAmount)
    {
        OpenIncomeTab();
        var editButton = $"IncomeEdit_{name}";
        App.ScrollTo(editButton);
        App.Tap(editButton);

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
