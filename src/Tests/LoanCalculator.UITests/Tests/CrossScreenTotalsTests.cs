using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// The two TECH-DEBT D1 regressions, driven through the real UI.
/// </summary>
/// <remarks>
/// <para>
/// Both mechanisms are already pinned deterministically by the unit suite
/// (<c>AffordabilityOrderDependenceTests</c>, <c>IncomeExpenseGrossTotalsTests</c>,
/// <c>BudgetColdStartLoadTests</c>). These exist for what unit tests cannot reach: real disk
/// persistence, the splash pre-warm, the page lifecycle and the bindings. Both were user-visible
/// bugs that shipped, which is what earns them the UI suite's time.
/// </para>
/// <para>
/// <b>Everything here is asserted as an invariant or a delta, never an absolute total.</b> Budget
/// data persists between runs, so a device may arrive with figures from an earlier run — and these
/// two defects are both "a figure changed when it should not have", which is exactly what a delta
/// expresses. That also makes them independent of the currency symbol and of the unsigned net
/// display.
/// </para>
/// </remarks>
[TestFixture]
[Category("Calculation")]
public class CrossScreenTotalsTests : UITestBase
{
    private bool _toggledOn;

    /// <summary>
    /// Leaves the "income after expenses" toggle off. It persists, so a test that flipped it would
    /// otherwise change the starting state of every later test and run.
    /// </summary>
    [TearDown]
    public void RestoreToggle()
    {
        if (!_toggledOn) return;

        try
        {
            // The test may have failed anywhere, including on another shell tab, so navigate
            // before touching Budget's in-page tabs.
            Tabs.GoTo(AppTab.Budget);
            Budget.TurnOffIncomeAfterExpenses();
        }
        catch (Exception ex)
        {
            // Never let cleanup replace the real failure message. A leftover toggle costs the
            // next run a wrong starting state; a throwing teardown costs the diagnosis.
            TestContext.Out.WriteLine($"[teardown] could not reset the income toggle: {ex.Message}");
        }
        finally
        {
            _toggledOn = false;
        }
    }

    /// <summary>
    /// The affordability estimate must be available on the first Loan page appearance after a
    /// relaunch, without visiting Budget first.
    /// </summary>
    /// <remarks>
    /// <para>TECH-DEBT D1a. The splash pre-warm loaded income/expenses without summing them, and
    /// the flag gating the affordability box reads the yearly totals — which are not persisted. So
    /// the box stayed in its "record your income and expenses" state for the whole session until
    /// something recalculated.</para>
    /// <para><b>Visiting Budget first masks this</b>, because that recalculates. Hence the Loan tab
    /// is the first thing touched in the new session.</para>
    /// <para>Asserts on the caption rather than an amount deliberately: the caption switches
    /// between "record" and "estimate" wording on exactly the flag under test, and a word survives
    /// the currency, rounding and pre-existing-data differences an amount would not.</para>
    /// </remarks>
    [Test]
    public void AffordabilityIsAvailableOnTheFirstLoanPageAfterARestart()
    {
        TestData.EnsureBudget(Tabs, Budget);

        App.RestartApp();
        Launch.WaitUntilReady();

        // First screen touched in the new session — no Budget visit to mask the defect.
        Tabs.GoTo(AppTab.Loan);
        var summary = Loan.SummaryText();

        Assert.That(summary, Does.Contain("estimate").IgnoreCase,
            "after a relaunch with a saved budget the Loan page must already show the "
            + $"affordability estimate. Summary box read: '{summary}'");
        Assert.That(summary, Does.Not.Contain("record").IgnoreCase,
            "the box is still prompting for income and expenses that are already recorded");
    }

    /// <summary>
    /// The Income tab's "income after expenses" switch is a display preference. It must not change
    /// any other screen's figures.
    /// </summary>
    /// <remarks>
    /// TECH-DEBT D1. Turning it on wrote the deduction into the summary object shared by every
    /// screen, so Budget's Net subtracted the expenses a second time and the Expenses tab's
    /// "Monthly Income" box showed the reduced — sometimes negative — figure.
    /// </remarks>
    [Test]
    public void IncomeAfterExpensesToggleDoesNotChangeOtherScreens()
    {
        TestData.EnsureBudget(Tabs, Budget);

        // EnsureBudget latches on a static and returns BEFORE navigating once another test has
        // seeded, so it cannot be relied on to leave us on the Budget page — the first run of
        // this fixture failed here, on the Loan page, looking for a Budget tab. Navigate
        // explicitly, as BudgetTests does in its [SetUp].
        Tabs.GoTo(AppTab.Budget);

        Budget.OpenSummaryTab();
        Budget.ScrollToTotals();
        var netBefore = App.Number(BudgetPage.NetMonthly);
        var summaryIncomeBefore = App.Number(BudgetPage.IncomeMonthlyTotal);

        var incomeTabBefore = Budget.IncomeTabSummaryText();
        var expenseTabBefore = Budget.ExpenseTabSummaryText();

        Budget.TurnOnIncomeAfterExpenses();
        _toggledOn = true;

        // Confirm the switch actually took effect BEFORE asserting it had no side effects.
        // SfSwitch draws its own state rather than exposing it, so a tap that silently missed
        // would make every assertion below pass for the wrong reason.
        Assume.That(Budget.IncomeTabSummaryText(), Is.Not.EqualTo(incomeTabBefore),
            "the Income tab's figures did not change, so the switch tap did not register and this "
            + "test proves nothing");

        Budget.OpenSummaryTab();
        Budget.ScrollToTotals();

        Assert.Multiple(() =>
        {
            Assert.That(App.Number(BudgetPage.NetMonthly), Is.EqualTo(netBefore).Within(1m),
                "Budget's Net changed when the Income tab's display toggle was flipped — it is "
                + "subtracting the expenses twice");
            Assert.That(App.Number(BudgetPage.IncomeMonthlyTotal),
                Is.EqualTo(summaryIncomeBefore).Within(1m),
                "Budget's Summary income changed; it means the income the user entered");
        });

        Assert.That(Budget.ExpenseTabSummaryText(), Is.EqualTo(expenseTabBefore),
            "the Expenses tab's figures changed — its \"Monthly Income\" means the income the user "
            + "entered, so the Income tab's display preference must not touch it");
    }
}
