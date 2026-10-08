using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// The What If scenarios are all driven off the loan recorded on the Loan tab, so each test
/// seeds that first. This also exercises the cross-tab refresh path, which is where stale
/// summaries tend to show up.
/// </summary>
[TestFixture]
[Category("Calculation")]
public class WhatIfTests : UITestBase
{
    /// <summary>
    /// Every scenario needs a loan. Routed through TestData so it is seeded once per run rather
    /// than once per test, and so this fixture does not depend on another having run first.
    /// </summary>
    private void SeedLoan() => TestData.EnsureLoan(Tabs, Loan);

    [Test]
    public void ScenarioCardsAppearOnceALoanExists()
    {
        SeedLoan();
        Tabs.GoTo(AppTab.WhatIf);

        Assert.Multiple(() =>
        {
            Assert.That(WhatIf.ShowsNoDataPlaceholder(), Is.False,
                "The 'no loan data' placeholder was still showing after a loan was entered.");
            Assert.That(App.Exists(WhatIfPage.RateDeltaValue), Is.True,
                "The Rate Change card did not render.");
        });
    }

    [Test]
    public void RaisingTheRateDeltaRaisesTheProjectedRepayment()
    {
        SeedLoan();
        Tabs.GoTo(AppTab.WhatIf);

        var baseline = WhatIf.NewMonthlyRepayment();

        WhatIf.IncreaseRate(steps: 3);

        var raised = App.WaitForText(
            WhatIfPage.NewMonthly,
            shown => ParseMoney(shown) > baseline,
            $"rose above the baseline repayment of {baseline}");

        Assert.That(ParseMoney(raised), Is.GreaterThan(baseline),
            "A higher interest rate did not increase the projected monthly repayment.");
    }

    [Test]
    public void LoweringTheRateDeltaLowersTheProjectedRepayment()
    {
        SeedLoan();
        Tabs.GoTo(AppTab.WhatIf);

        var baseline = WhatIf.NewMonthlyRepayment();

        WhatIf.DecreaseRate(steps: 3);

        var lowered = App.WaitForText(
            WhatIfPage.NewMonthly,
            shown => ParseMoney(shown) < baseline,
            $"fell below the baseline repayment of {baseline}");

        Assert.That(ParseMoney(lowered), Is.LessThan(baseline),
            "A lower interest rate did not reduce the projected monthly repayment.");
    }

    [Test]
    public void RateDeltaStepsBackToZero()
    {
        SeedLoan();
        Tabs.GoTo(AppTab.WhatIf);

        WhatIf.IncreaseRate(steps: 2);
        var raised = WhatIf.RateDelta();

        WhatIf.DecreaseRate(steps: 2);
        var restored = WhatIf.RateDelta();

        Assert.That(restored, Is.Not.EqualTo(raised),
            "The rate delta did not change when stepping back down.");
    }

    /// <summary>
    /// The stress test needs a loan <em>and</em> budget data, so it is a second cross-tab handover
    /// — and the one most likely to show a stale prompt, since it depends on two other tabs.
    /// </summary>
    [Test]
    public void StressTestUnlocksOnceIncomeAndExpensesExist()
    {
        SeedLoan();

        TestData.EnsureBudget(Tabs, Budget);

        Tabs.GoTo(AppTab.WhatIf);

        Assert.That(WhatIf.StressTestResultsShowing(), Is.True,
            "The affordability stress test still showed its 'record your income & expenses' prompt " +
            "after both were recorded — the cross-tab refresh did not reach it.");
    }

    [Test]
    public void StressTestPromptsWhenThereIsNoBudgetData()
    {
        SeedLoan();

        Tabs.GoTo(AppTab.Settings);
        Settings.OpenDeleteDataSection();
        Settings.DeleteIncomeData();
        Settings.DeleteExpenseData();

        Tabs.GoTo(AppTab.WhatIf);

        Assert.That(WhatIf.StressTestResultsShowing(), Is.False,
            "With no income or expenses recorded, the stress test is still showing results — so " +
            "they are computed from nothing, or the card did not refresh after the data was deleted.");
    }

    private static decimal ParseMoney(string text)
    {
        var cleaned = new string(text.Where(c => char.IsDigit(c) || c is '.').ToArray());
        return decimal.TryParse(cleaned, out var value) ? value : -1m;
    }
}
