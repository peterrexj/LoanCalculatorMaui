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

        Budget.ScrollToTotals();
        var after = App.WaitForText(
            BudgetPage.IncomeMonthlyTotal,
            shown => Digits(shown) > before,
            $"rose above {before}");

        Assert.That(Digits(after), Is.GreaterThan(before),
            "Adding income did not change the monthly income total.");
    }

    [Test]
    public void AddingAnExpenseRaisesTheMonthlyExpenseTotal()
    {
        var before = Budget.MonthlyExpenses();

        Budget.AddExpense(UniqueName("Electricity"), "250");

        Budget.ScrollToTotals();
        var after = App.WaitForText(
            BudgetPage.ExpenseMonthlyTotal,
            shown => Digits(shown) > before,
            $"rose above {before}");

        Assert.That(Digits(after), Is.GreaterThan(before),
            "Adding an expense did not change the monthly expense total.");
    }

    [Test]
    public void NetMonthlyEqualsIncomeMinusExpenses()
    {
        EnsureSomeBudgetData();
        Budget.ScrollToTotals();

        var income = App.Number(BudgetPage.IncomeMonthlyTotal);
        var expenses = App.Number(BudgetPage.ExpenseMonthlyTotal);
        var net = App.Number(BudgetPage.NetMonthly);

        // The displayed net is unsigned, so compare magnitudes.
        Assert.That(net, Is.EqualTo(Math.Abs(income - expenses)).Within(1m),
            $"Net ({net}) does not reconcile with income ({income}) minus expenses ({expenses}).");
    }

    [Test]
    public void YearlyNetIsTwelveTimesMonthlyNet()
    {
        EnsureSomeBudgetData();
        Budget.ScrollToTotals();

        var monthly = App.Number(BudgetPage.NetMonthly);
        var yearly = App.Number(BudgetPage.NetYearly);

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

        Budget.ScrollToTotals();
        var before = Budget.MonthlyIncome();

        Budget.EditIncomeRowAmount(name, "5000");

        Budget.ScrollToTotals();
        var after = App.WaitForText(
            BudgetPage.IncomeMonthlyTotal,
            shown => Digits(shown) > before,
            $"rose above {before} after the row was edited from 1,000 to 5,000");

        Assert.That(Digits(after), Is.GreaterThan(before),
            "Editing a row's amount upward did not raise the monthly total.");
    }

    private void EnsureSomeBudgetData() => TestData.EnsureBudget(Tabs, Budget);

    private static string UniqueName(string stem) => TestData.UniqueName(stem);

    private static decimal Digits(string text)
    {
        var cleaned = new string(text.Where(c => char.IsDigit(c) || c is '.' or '-').ToArray());
        return decimal.TryParse(cleaned, out var value) ? value : -1m;
    }
}
