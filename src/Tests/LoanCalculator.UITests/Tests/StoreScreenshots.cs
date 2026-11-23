using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// Captures as many marketing-grade surfaces as it can reach, with realistic seeded figures, for
/// store listings, the website and ads. Unlike <see cref="ScreenshotSweep"/> (design review, minimal
/// data) this one seeds a believable household budget first so charts and totals look populated.
/// </summary>
/// <remarks>
/// Run: <c>UITEST_ARTIFACTS=&lt;dir&gt; ./run-uitests.sh --ios --filter "TestCategory=Store"</c>.
/// Like the sweep, every surface is captured through <see cref="Capture"/> so one unreachable screen
/// costs a line in the report rather than the whole run.
/// <para>
/// Themes: iOS repeats the hero surfaces in all four themes. Android cannot drive the theme
/// dropdown under UiAutomator2 (see CLAUDE.md — Syncfusion combos), so it captures the default
/// theme only.
/// </para>
/// </remarks>
[TestFixture]
[Category("Store")]
public class StoreScreenshots : CaptureFixtureBase
{
    protected override string ReportFileName => "store-report.txt";

    private static readonly (string Name, string Amount)[] Incomes =
    [
        ("Salary", "7800"),
        ("Freelance", "1400"),
        ("Rental", "950"),
    ];

    private static readonly (string Name, string Amount)[] Expenses =
    [
        ("Groceries", "950"),
        ("Utilities", "340"),
        ("Transport", "420"),
        ("Insurance", "290"),
        ("Subscriptions", "85"),
        ("Dining", "380"),
        ("Childcare", "600"),
    ];

    private static bool _seeded;

    /// <summary>
    /// Tops the budget up with a believable household, once per run. Deliberately does NOT wipe
    /// first: after Delete All Data the Loan tab has no summary box (the same reason the sweep
    /// misses its "no budget" shot), and Android's confirmation dialog has no "Yes" button.
    /// </summary>
    private void SeedMarketingData()
    {
        if (_seeded) return;

        // Never TestData.EnsureBudget here: it adds throwaway "SeedIncome…"/"SeedExpense…" rows
        // which then appear, by name, in the screenshots and in the exported PDF.
        TestData.EnsureLoan(Tabs, Loan);

        Tabs.GoTo(AppTab.Budget);
        RemoveSeedRows();

        if (!Budget.IncomeRowExists(Incomes[0].Name))
            foreach (var (name, amount) in Incomes) Budget.AddIncome(name, amount);
        if (!App.Exists($"ExpenseRow_{Expenses[0].Name}", TimeSpan.FromSeconds(2)))
            foreach (var (name, amount) in Expenses) Budget.AddExpense(name, amount);

        _seeded = true;
    }

    /// <summary>
    /// Deletes the timestamped test rows an earlier sweep left behind. They are found from the page
    /// source because their names embed the time they were created and so cannot be known here.
    /// </summary>
    private void RemoveSeedRows()
    {
        foreach (var (isIncome, tab) in new (bool, Action)[] { (true, Budget.OpenIncomeTab), (false, Budget.OpenExpensesTab) })
        {
            tab();
            var kind = isIncome ? "Income" : "Expense";
            var names = System.Text.RegularExpressions.Regex
                .Matches(App.PageSource, $@"{kind}Row_(Seed\w+)")
                .Select(m => m.Groups[1].Value).Distinct().ToList();

            foreach (var name in names)
            {
                Try($"delete {kind} {name}", () =>
                {
                    var row = $"{kind}Row_{name}";
                    App.ScrollTo(row);
                    App.Tap(row);
                    App.WaitUntilVisible($"Budget{kind}AmountEntry", TimeSpan.FromSeconds(15));
                    App.ScrollTo($"Budget{kind}DeleteButton");
                    App.Tap($"Budget{kind}DeleteButton");
                    // The confirmation's button text differs by platform ("Yes" / "YES").
                    try { App.TapText("Yes", TimeSpan.FromSeconds(6)); }
                    catch { App.TapText("YES", TimeSpan.FromSeconds(6)); }
                    App.WaitUntilGone($"Budget{kind}AmountEntry", TimeSpan.FromSeconds(15));
                });
            }
        }
    }

    /// <summary>The core listing set, in whichever theme is currently active.</summary>
    private void CaptureHeroSet(string prefix)
    {
        Capture($"{prefix}-01-loan", () => { Tabs.GoTo(AppTab.Loan); Loan.OpenAssetTab(); });
        Capture($"{prefix}-02-schedule", Loan.OpenAmortisationTab);
        Capture($"{prefix}-03-budget-income", () => { Tabs.GoTo(AppTab.Budget); Budget.OpenIncomeTab(); });
        Capture($"{prefix}-04-budget-expenses", Budget.OpenExpensesTab);
        Capture($"{prefix}-05-budget-summary", Budget.OpenSummaryTab);
        Capture($"{prefix}-06-whatif-answer", WhatIf.OpenAnswerTab);
        Capture($"{prefix}-07-whatif-projection", WhatIf.OpenProjectionTab);
        Capture($"{prefix}-08-whatif-insights", WhatIf.OpenInsightsTab);
    }

    [Test, Order(1)]
    public void CaptureDefaultTheme()
    {
        SeedMarketingData();
        CaptureHeroSet("default");

        // Extra surfaces beyond the hero set.
        Capture("default-09-budget-asset-costs", () => { Tabs.GoTo(AppTab.Budget); Budget.OpenAssetCostsTab(); });
        Capture("default-10-whatif-scenarios", WhatIf.OpenScenariosTab);

        // Every scenario page. Row ids are the delta ids minus the "Delta" suffix.
        var n = 11;
        foreach (var (label, deltaId, _) in WhatIfPage.AllScenarioDeltas)
        {
            var rowId = deltaId.EndsWith("Delta") ? deltaId[..^"Delta".Length] : deltaId;
            if (Capture($"default-{n}-scenario-{label.Replace(' ', '-').ToLowerInvariant()}", () =>
                {
                    WhatIf.OpenScenariosTab();
                    App.ScrollTo(rowId);
                    App.Tap(rowId);
                    App.WaitUntilVisible("ScenarioTitle", TimeSpan.FromSeconds(20));
                }))
            {
                WhatIf.CloseScenario();
            }
            n++;
        }

        Capture("default-20-loan-details", () =>
        {
            Tabs.GoTo(AppTab.Loan);
            Loan.OpenAssetTab();
            App.Tap(LoanPage.LoanDetailsFab);
            App.WaitUntilVisible("AssetEntry", TimeSpan.FromSeconds(15));
        });
        try { App.Tap("WizardCancelButton"); } catch { /* next surface re-navigates from a tab */ }

        Capture("default-21-add-income", () =>
        {
            Tabs.GoTo(AppTab.Budget);
            Budget.OpenIncomeTab();
            App.Tap(BudgetPage.IncomeAddFab);
            App.WaitUntilVisible("BudgetIncomeAmountEntry", TimeSpan.FromSeconds(15));
        });
        try { App.Tap("TransactionEntryCancelButton"); } catch { }

        Capture("default-22-settings", () => Tabs.GoTo(AppTab.Settings));
    }

    /// <summary>The same hero set in the three remaining themes. iOS only — see class remarks.</summary>
    [Test, Order(2)]
    public void CaptureOtherThemes()
    {
        if (App.Platform != TestPlatform.iOS)
            Assert.Ignore("Theme dropdown cannot be driven on Android under UiAutomator2.");

        SeedMarketingData();

        try
        {
            foreach (var theme in new[] { "Light", "Forest", "Warm" })
            {
                Tabs.GoTo(AppTab.Settings);
                Settings.SelectTheme(theme);
                CaptureHeroSet(theme.ToLowerInvariant());
                Capture($"{theme.ToLowerInvariant()}-09-settings", () => Tabs.GoTo(AppTab.Settings));
            }
        }
        finally
        {
            // Theme persists on the device; leave it on the default so later runs start from Dark.
            Tabs.GoTo(AppTab.Settings);
            Settings.SelectTheme("Dark");
        }
    }
}
