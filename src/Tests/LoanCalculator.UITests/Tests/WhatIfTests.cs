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

    /// <summary>
    /// The nine scenarios are an index of rows now, not nine stacked cards, so this asserts the
    /// index renders rather than that any one card does.
    /// </summary>
    /// <remarks>
    /// It asserts from the <b>Scenarios</b> sub-tab, which matters: both the placeholder and the
    /// index live there, and arriving at What If lands on Answer. Asserting from Answer passed
    /// vacuously — neither element exists on that tab, so the placeholder read as "hidden"
    /// whether or not a loan had been entered.
    /// </remarks>
    /// <summary>
    /// The affordability verdict on Loan is a link through to the full answer.
    /// </summary>
    /// <remarks>
    /// Deliberately leaves What If on a *different* sub-tab first. Shell keeps the page alive, so
    /// without an explicit request the link lands wherever the user last was — and a link that
    /// promises the answer dropping you on Projection is the defect this guards.
    /// </remarks>
    [Test]
    public void TappingTheLoanVerdictOpensTheWhatIfAnswerTab()
    {
        SeedLoan();

        WhatIf.OpenProjectionTab();
        Loan.TapVerdictLink();

        Assert.That(App.Exists("WhatIfAnswerCard", TimeSpan.FromSeconds(15)), Is.True,
            "Tapping the affordability verdict did not land on What If ▸ Answer.");
    }

    [Test]
    public void ScenarioIndexAppearsOnceALoanExists()
    {
        SeedLoan();
        WhatIf.OpenScenariosTab();

        Assert.Multiple(() =>
        {
            Assert.That(WhatIf.ShowsNoDataPlaceholder(), Is.False,
                "The 'no loan data' placeholder was still showing after a loan was entered.");

            // Each row's headline figure, one assertion per row. A failed binding path renders an
            // empty label rather than throwing, so asserting the figure — not just the row — is
            // what catches it. Named individually so the failure says which row broke.
            foreach (var (row, id, needsBudget) in WhatIfPage.AllScenarioDeltas)
            {
                var delta = App.ScrollTo(id).Text;

                Assert.That(delta, Is.Not.Empty,
                    $"The {row} row rendered without its headline delta figure.");

                // "--" is the un-computed placeholder. Every scenario that derives from the loan
                // alone must have a real figure by now, so a placeholder means the binding
                // resolved but nothing computed. The stress test is exempt: it measures headroom
                // against income and expenses, and this test seeds only a loan.
                if (!needsBudget)
                {
                    Assert.That(delta, Is.Not.EqualTo("--"),
                        $"The {row} row is still showing the placeholder with a loan entered.");
                }
            }
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

    /// <summary>
    /// Every scenario carries a collapsed explanation card; this proves one opens and renders.
    /// </summary>
    /// <remarks>
    /// The assertion is on the card's <b>text</b>, not on the element existing, because the
    /// failure this guards against is a card that opens empty: the content is supplied as a
    /// <c>ScenarioExplanation</c> from Core, and a card wired without one would render its
    /// headings and no prose — which looks like a styling fault rather than missing data. Exactly
    /// the way the Insights panels failed when nothing set their refresh flag.
    ///
    /// If this fails on iOS with the header found but the body never appearing, the cause is
    /// almost certainly <c>ScenarioPage.ApplyExpanderTouchFix</c> not landing rather than this
    /// locator — a Syncfusion drawing overlay swallowing the header tap is silent and is the
    /// documented trap for SfExpander on iOS.
    /// </remarks>
    [Test]
    public void EachScenarioExplainsItselfInPlainLanguage()
    {
        SeedLoan();
        Tabs.GoTo(AppTab.WhatIf);

        // Any scenario would do; Rate change is the one every other test already opens, so it is
        // the cheapest to reach from a warm state.
        WhatIf.IncreaseRate(steps: 1);

        var body = WhatIf.ExpandExplanation();

        Assert.That(body, Does.Match("(?i)rate"),
            "The Rate change explanation opened but does not mention rates, so the card is "
            + "showing another scenario's content or none at all.");
    }

    private static decimal ParseMoney(string text)
    {
        var cleaned = new string(text.Where(c => char.IsDigit(c) || c is '.').ToArray());
        return decimal.TryParse(cleaned, out var value) ? value : -1m;
    }
}
