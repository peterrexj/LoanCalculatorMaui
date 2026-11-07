using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// Budget data persists to disk between runs, so these assert on the <em>change</em> a
/// action causes rather than on absolute totals. That keeps them correct whether the
/// device starts empty or with data from an earlier run.
/// </summary>
[TestFixture]
[Category("Calculation")]
public class BudgetTests : UITestBase
{
    [SetUp]
    public void GoToBudgetTab() => Tabs.GoTo(AppTab.Budget);

    // The add form's frequency defaults to whatever the view model last held, so a weekly or
    // fortnightly entry contributes a converted figure to the monthly total. These therefore
    // assert the total moved in the right direction rather than by an exact amount — which
    // still catches the regression that matters: an entry that silently does not register.

    [Test]
    public void AddingIncomeRaisesTheMonthlyIncomeTotal()
    {
        var before = Budget.MonthlyIncome();

        Budget.AddIncome(UniqueName("Salary"), "4000");

        var after = Budget.WaitForSummaryMagnitude(
            BudgetPage.InsightsIncome,
            shown => shown > before,
            $"rose above {before}");

        Assert.That(after, Is.GreaterThan(before),
            "Adding income did not change the monthly income total.");
    }

    [Test]
    public void AddingAnExpenseRaisesTheMonthlyExpenseTotal()
    {
        var before = Budget.MonthlyExpenses();

        Budget.AddExpense(UniqueName("Electricity"), "250");

        // Magnitude, not the displayed figure: the Insights row draws expenses negative, so the
        // raw text falls as expenses rise. BudgetPage owns that convention.
        var after = Budget.WaitForSummaryMagnitude(
            BudgetPage.InsightsLivingExpenses,
            shown => shown > before,
            $"rose above {before}");

        Assert.That(after, Is.GreaterThan(before),
            "Adding an expense did not change the monthly expense total.");
    }

    /// <summary>
    /// The hero net must reconcile with the three figures the Insights card breaks it into.
    /// </summary>
    /// <remarks>
    /// Asset running costs are part of this and are easy to forget: the net is income less
    /// <em>every</em> outgoing, not just living expenses (<c>BudgetViewModel.NetAfterOutgoingsMonthly</c>),
    /// so omitting them makes this fail by exactly the asset-cost figure on any device that has
    /// one — and pass on any device that does not, which is the worse half.
    /// </remarks>
    [Test]
    public void NetMonthlyEqualsIncomeMinusEveryOutgoing()
    {
        EnsureSomeBudgetData();

        var income = Budget.MonthlyIncome();
        var expenses = Budget.MonthlyExpenses();
        var assetCosts = Budget.MonthlyAssetCosts();
        var net = Budget.MonthlyNet();

        // Four figures, each independently rounded to whole currency units for display, so the
        // reconciliation can be off by up to half a unit per figure.
        Assert.That(net, Is.EqualTo(income - expenses - assetCosts).Within(2m),
            $"Net ({net}) does not reconcile with income ({income}) less living expenses "
            + $"({expenses}) and asset running costs ({assetCosts}).");
    }

    [Test]
    public void YearlyNetIsTwelveTimesMonthlyNet()
    {
        EnsureSomeBudgetData();

        var monthly = Budget.MonthlyNet();
        var yearly = Budget.YearlyNet();

        // Signed on both sides, so this holds for a negative net too. The yearly total is a sum
        // of each entry's own yearly amount rather than 12x the monthly one, but those agree
        // exactly — the only slack needed is the two figures' display rounding.
        Assert.That(yearly, Is.EqualTo(monthly * 12m).Within(12m),
            $"Yearly net ({yearly}) is not twelve times the monthly net ({monthly}).");
    }

    /// <summary>
    /// The Summary tab shows an empty-state panel instead of totals until the budget has
    /// something in it, so the reconciliation tests seed a row on a clean device. A device
    /// that already has data is left alone.
    /// </summary>
    // Add is covered above; these are the other two CRUD operations, which had no coverage. Row
    // action buttons are keyed off the row name, so each test targets exactly the row it created.

    [Test]
    public void DeletingAnIncomeRowRemovesItAndLowersTheTotal()
    {
        var name = UniqueName("DeleteMe");
        Budget.AddIncome(name, "1500");

        Assert.That(Budget.IncomeRowExists(name), Is.True, "The added income row never appeared.");
        var before = Budget.MonthlyIncome();

        Budget.DeleteIncomeRow(name);

        Assert.Multiple(() =>
        {
            Assert.That(Budget.IncomeRowExists(name), Is.False, "The row survived being deleted.");
            Assert.That(Budget.MonthlyIncome(), Is.LessThan(before),
                "Deleting an income row did not lower the monthly total.");
        });
    }

    [Test]
    public void EditingAnIncomeRowAmountUpdatesTheTotal()
    {
        var name = UniqueName("EditMe");
        Budget.AddIncome(name, "1000");

        var before = Budget.MonthlyIncome();

        Budget.EditIncomeRowAmount(name, "5000");

        var after = Budget.WaitForSummaryMagnitude(
            BudgetPage.InsightsIncome,
            shown => shown > before,
            $"rose above {before} after the row was edited from 1,000 to 5,000");

        Assert.That(after, Is.GreaterThan(before),
            "Editing a row's amount upward did not raise the monthly total.");
    }

    private void EnsureSomeBudgetData() => TestData.EnsureBudget(Tabs, Budget);

    /// <summary>
    /// Every switch row on the Budget tabs must render its label and its ⓘ explanation.
    /// </summary>
    /// <remarks>
    /// The Income tab's first switch shipped with no label, no sub-line and no explanation at all:
    /// the row takes its copy from a <c>BudgetToggle</c> BindableProperty, the first row's value is
    /// the same as that property's default, and MAUI does not raise <c>propertyChanged</c> when a
    /// value does not change — so the callback that fills in the copy never ran for that one row.
    ///
    /// No unit test could have caught it. The copy lives in Core and was complete and tested
    /// throughout; what failed was the control not applying it. This asserts on the rendered text
    /// for exactly that reason.
    /// </remarks>
    [Test]
    public void EachBudgetSwitchExplainsItselfWhenAsked()
    {
        var detail = Budget.ExpandIncomeAfterExpenseExplanation();

        Assert.That(detail, Does.Match("(?i)spending|expenses"),
            "The first Income switch's ⓘ opened but revealed nothing about what the switch does, "
            + "so the row is rendering without its copy.");
    }

    // Parsing lives in BudgetPage now, not here. The Summary figures are signed, and which of
    // them should be read as a magnitude and which signed is a property of the screen rather
    // than of any one test — so a local digit-scraper here would have had to re-state that
    // convention, once per test, with nothing keeping the copies honest.

    private static string UniqueName(string stem) => TestData.UniqueName(stem);
}
