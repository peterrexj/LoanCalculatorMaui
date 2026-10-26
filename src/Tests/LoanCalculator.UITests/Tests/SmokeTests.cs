using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// The regression net for "a change broke the app". These do not check any maths — they
/// check that every screen still builds and renders on both platforms, which is what
/// silently breaks when XAML, themes, or DI wiring change.
/// </summary>
[TestFixture]
[Category("Smoke")]
public class SmokeTests : UITestBase
{
    [Test]
    public void AppLaunchesToTheLoanTab()
    {
        Assert.That(Tabs.IsOn(AppTab.Loan), Is.True, "The app did not land on the Loan tab.");
    }

    [Test]
    [TestCase(AppTab.Loan)]
    [TestCase(AppTab.Budget)]
    [TestCase(AppTab.WhatIf)]
    [TestCase(AppTab.Settings)]
    public void EveryTabRenders(AppTab tab)
    {
        Tabs.GoTo(tab);
        Assert.That(Tabs.IsOn(tab), Is.True, $"The {tab} tab did not render its content.");
    }

    [Test]
    public void TabsCanBeVisitedInSequenceWithoutCrashing()
    {
        // Catches disposal/rebind faults that only appear when pages are re-entered — the
        // cross-tab dirty-flag refresh path in particular.
        AppTab[] route =
        [
            AppTab.Budget, AppTab.WhatIf, AppTab.Settings,
            AppTab.Loan, AppTab.Settings, AppTab.Budget, AppTab.Loan,
        ];

        foreach (var tab in route)
        {
            Tabs.GoTo(tab);
            Assert.That(Tabs.IsOn(tab), Is.True, $"Lost the {tab} tab part-way through the route.");
        }
    }

    [Test]
    public void LoanPageInPageTabsAllRender()
    {
        // Loan has no in-page tabs any more — it is a single screen. What this now covers is
        // that the two views which LEFT it are still reachable, and that returning to Loan
        // restores its content. The schedule is a pushed page; Insights is a What If sub-tab.
        Tabs.GoTo(AppTab.Loan);

        Loan.OpenAmortisationTab();
        Assert.That(App.Exists(LoanPage.AmortisationGrid), Is.True,
            "The repayment schedule page did not render its grid.");
        Loan.CloseAmortisation();

        WhatIf.OpenInsightsTab();
        Assert.That(App.Exists("LoanInsightsExportButton"), Is.True,
            "Insights did not render after moving to What If.");

        Tabs.GoTo(AppTab.Loan);
        Loan.OpenAssetTab();
        Assert.That(App.Exists(LoanPage.LoanDetailsFab), Is.True,
            "Returning to Loan did not restore its content.");
    }

    [Test]
    public void BudgetInPageTabsAllRender()
    {
        Tabs.GoTo(AppTab.Budget);

        Budget.OpenIncomeTab();
        Budget.OpenExpensesTab();
        Budget.OpenSummaryTab();
        WhatIf.OpenProjectionTab();
        Budget.OpenIncomeTab();

        Assert.That(App.Exists(BudgetPage.IncomeAddFab), Is.True,
            "Returning to the Income tab did not restore its content.");
    }

    [Test]
    public void QuickInputPopupOpensAndCloses()
    {
        Tabs.GoTo(AppTab.Loan);

        Loan.OpenLoanDetails();
        Assert.That(App.Exists(LoanPage.AssetEntry), Is.True, "Quick Input did not open.");

        Loan.ApplyLoanDetails();
        Assert.That(App.Exists(LoanPage.AssetEntry, TimeSpan.FromSeconds(2)), Is.False,
            "Quick Input did not close after Apply.");
    }
}
