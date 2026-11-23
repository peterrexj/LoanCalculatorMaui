using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// Captures the screens for the written user guide, in the order a brand-new user meets them, from
/// the first-run disclaimer to the PDF export. Every figure on screen is built up from empty by the
/// steps below, so the guide's pictures, its numbers and the exported PDF all agree.
/// </summary>
/// <remarks>
/// <para>
/// <b>Run on a fresh install:</b>
/// <c>UITEST_ARTIFACTS=&lt;dir&gt; ./run-uitests.sh --ios --fresh --filter "TestCategory=Guide"</c>.
/// On a used device the first-run screens never appear and the "empty" captures are not empty.
/// </para>
/// <para>
/// One long test rather than many, deliberately: each step builds on the data the previous one
/// entered, and it must be the first test of the run so the first-run gates are still pending.
/// Files are named <c>guide-NN-…</c> so they sort into guide order.
/// </para>
/// </remarks>
[TestFixture]
[Category("Guide")]
public class GuideCapture : CaptureFixtureBase
{
    protected override string ReportFileName => "guide-report.txt";

    [OneTimeSetUp]
    public void ArmDisclaimerCapture() => AppLaunch.CaptureDisclaimerAs = "guide-01-first-run-disclaimer";

    /// <summary>
    /// Puts the numeric keypad away by tapping an inert label. The number pad has no Return key, so
    /// the usual hide-keyboard call does nothing, and a keypad over half the screen ruins a shot of
    /// a form someone is meant to be reading.
    /// </summary>
    private void Blur(string visibleLabelText) =>
        Try($"blur via '{visibleLabelText}'", () => { App.TapText(visibleLabelText, TimeSpan.FromSeconds(4)); Thread.Sleep(700); });

    [Test]
    public void WalkThroughTheWholeApp()
    {
        // ── 1. First run ────────────────────────────────────────────────────────────
        // The disclaimer was captured by AppLaunch as it was accepted. What is left is the
        // Loan tab with nothing entered, which is what the user sees right after.
        Capture("guide-02-loan-empty", () => { Tabs.GoTo(AppTab.Loan); Loan.OpenAssetTab(); });

        // ── 2. Quick setup: the one place asset, deposit and loan are typed ─────────
        Capture("guide-03-quick-setup-empty", () =>
        {
            App.Tap(LoanPage.LoanDetailsFab);
            App.WaitUntilVisible(LoanPage.AssetEntry, TimeSpan.FromSeconds(15));
        });
        Capture("guide-04-quick-setup-filled", () =>
        {
            App.EnterText(LoanPage.AssetEntry, "650000");
            App.EnterText(LoanPage.DepositEntry, "130000");
            Blur("Loan Details");
        });
        Try("apply quick setup", Loan.ApplyLoanDetails);

        // ── 3. The loan screen ──────────────────────────────────────────────────────
        Capture("guide-05-loan-overview", () => { Tabs.GoTo(AppTab.Loan); Loan.OpenAssetTab(); });
        Capture("guide-06-loan-rate-term-frequency", () => App.ScrollTo(LoanPage.FrequencyControl));
        Capture("guide-07-loan-rate-changed", () => { Loan.SetInterestRate("5.5"); Blur("Loan Term"); });
        Capture("guide-08-loan-fortnightly", () => Loan.SelectRepaymentFrequency("Fortnightly"));
        Try("back to monthly", () => Loan.SelectRepaymentFrequency("Monthly"));

        // ── 4. Upfront costs ────────────────────────────────────────────────────────
        if (Capture("guide-09-upfront-costs-empty", () =>
            {
                Loan.OpenAssetTab();
                App.ScrollTo("LoanUpfrontCostsCard");
                App.Tap("LoanUpfrontCostsCard");
                App.WaitUntilVisible("ConveyanceEntry", TimeSpan.FromSeconds(15));
            }))
        {
            Capture("guide-10-upfront-costs-filled", () =>
            {
                App.EnterText("ConveyanceEntry", "1800");
                App.EnterText("BankFeeEntry", "600");
                App.EnterText("InspectionEntry", "450");
                App.EnterText("OtherExpensesEntry", "2000");
                Blur("Upfront Costs");
            });
            Try("close upfront costs", () =>
            {
                // Done lives in the page header, which never scrolls — so no ScrollTo. With the
                // numeric keypad up, iOS reports the header button as not "visible" to a lookup
                // by id, so fall back to tapping the label text, which it still finds.
                try { App.Tap("UpfrontDoneButton", TimeSpan.FromSeconds(4)); }
                catch { App.TapText("Done", TimeSpan.FromSeconds(8)); }
                App.WaitUntilGone("UpfrontDoneButton", TimeSpan.FromSeconds(10));
            });
        }
        Capture("guide-11-loan-with-upfront", () => { Loan.OpenAssetTab(); });

        // ── 5. Repayment schedule ───────────────────────────────────────────────────
        if (Capture("guide-12-repayment-schedule", Loan.OpenAmortisationTab))
            Try("close schedule", Loan.CloseAmortisation);

        // ── 6. Budget: income ───────────────────────────────────────────────────────
        Capture("guide-13-budget-income-empty", () => { Tabs.GoTo(AppTab.Budget); Budget.OpenIncomeTab(); });
        if (Capture("guide-14-add-income-form", () =>
            {
                App.Tap(BudgetPage.IncomeAddFab);
                App.WaitUntilVisible("BudgetIncomeNameEntry", TimeSpan.FromSeconds(15));
            }))
        {
            Capture("guide-15-add-income-filled", () =>
            {
                App.EnterText("BudgetIncomeNameEntry", "Salary");
                App.EnterText("BudgetIncomeAmountEntry", "7800");
                Blur("Income Entry");
            });
            Try("save income", () =>
            {
                App.Tap("BudgetIncomeSaveButton");
                App.WaitUntilGone("BudgetIncomeNameEntry", TimeSpan.FromSeconds(15));
            });
        }
        Try("add Freelance", () => Budget.AddIncome("Freelance", "1400"));
        Try("add Rental", () => Budget.AddIncome("Rental", "950"));
        Capture("guide-16-income-list", Budget.OpenIncomeTab);

        // ── 7. Budget: expenses ─────────────────────────────────────────────────────
        Capture("guide-17-expenses-empty", Budget.OpenExpensesTab);
        if (Capture("guide-18-add-expense-form", () =>
            {
                App.Tap(BudgetPage.ExpenseAddFab);
                App.WaitUntilVisible("BudgetExpenseNameEntry", TimeSpan.FromSeconds(15));
            }))
        {
            Capture("guide-19-add-expense-filled", () =>
            {
                App.EnterText("BudgetExpenseNameEntry", "Groceries");
                App.EnterText("BudgetExpenseAmountEntry", "950");
                Blur("Expense Entry");
            });
            Try("save expense", () =>
            {
                App.Tap("BudgetExpenseSaveButton");
                App.WaitUntilGone("BudgetExpenseNameEntry", TimeSpan.FromSeconds(15));
            });
        }
        foreach (var (name, amount) in new[]
                 { ("Utilities", "340"), ("Transport", "420"), ("Insurance", "290"),
                   ("Subscriptions", "85"), ("Dining", "380"), ("Childcare", "600") })
            Try($"add {name}", () => Budget.AddExpense(name, amount));
        Capture("guide-20-expense-list", Budget.OpenExpensesTab);

        // ── 8. Budget: asset costs ──────────────────────────────────────────────────
        Capture("guide-21-asset-costs-empty", Budget.OpenAssetCostsTab);
        // The "+" on this tab has no AutomationId, so it is tapped by position (iOS points,
        // bottom-right above the tab bar). Adding one is a small XAML change if this proves fragile.
        if (Capture("guide-22-asset-cost-entry", () =>
            {
                App.TapAt(391, 824);
                App.WaitUntilVisible("AssetExpenseNameEntry", TimeSpan.FromSeconds(15));
            }))
        {
            Capture("guide-22b-asset-cost-filled", () =>
            {
                App.EnterText("AssetExpenseNameEntry", "Council rates");
                App.EnterText("AssetExpenseAmountEntry", "180");
                Blur("Asset Expense Entry");
            });
            Try("save asset cost", () =>
            {
                App.Tap("AssetExpenseSaveButton");
                App.WaitUntilGone("AssetExpenseNameEntry", TimeSpan.FromSeconds(15));
            });
            Capture("guide-23-asset-costs-filled", Budget.OpenAssetCostsTab);
        }

        // ── 9. Budget: summary ──────────────────────────────────────────────────────
        Capture("guide-24-budget-summary", Budget.OpenSummaryTab);

        // ── 10. What If ─────────────────────────────────────────────────────────────
        Capture("guide-25-whatif-answer", () => { Tabs.GoTo(AppTab.WhatIf); WhatIf.OpenAnswerTab(); });
        Capture("guide-26-whatif-scenarios", WhatIf.OpenScenariosTab);

        var n = 27;
        foreach (var (label, deltaId, _) in WhatIfPage.AllScenarioDeltas)
        {
            var rowId = deltaId.EndsWith("Delta") ? deltaId[..^"Delta".Length] : deltaId;
            if (Capture($"guide-{n}-scenario-{label.Replace(' ', '-').ToLowerInvariant()}", () =>
                {
                    WhatIf.OpenScenariosTab();
                    App.ScrollTo(rowId);
                    App.Tap(rowId);
                    App.WaitUntilVisible("ScenarioTitle", TimeSpan.FromSeconds(20));
                }))
            {
                Try("close scenario", WhatIf.CloseScenario);
            }
            n++;
        }

        Capture("guide-36-whatif-projection", WhatIf.OpenProjectionTab);
        Capture("guide-37-whatif-insights", WhatIf.OpenInsightsTab);

        // ── 11. Settings ────────────────────────────────────────────────────────────
        Capture("guide-38-settings", () => Tabs.GoTo(AppTab.Settings));
        Capture("guide-39-settings-support", () => App.ScrollTo("SettingsDisclaimerRow"));
        Capture("guide-40-settings-delete-data", Settings.OpenDeleteDataSection);

        // ── 11b. Explainers and expanded sections ───────────────────────────────────
        Capture("guide-44-loan-disclaimer-open", () =>
        {
            Tabs.GoTo(AppTab.Loan);
            Loan.OpenAssetTab();
            App.TapText("Disclaimer", TimeSpan.FromSeconds(6));
        });
        Try("close disclaimer", () => App.TapText("Disclaimer", TimeSpan.FromSeconds(4)));
        Capture("guide-45-budget-income-explainer", () =>
        {
            Tabs.GoTo(AppTab.Budget);
            Budget.OpenIncomeTab();
            Budget.ExpandIncomeAfterExpenseExplanation();
        });
        Capture("guide-46-whatif-understanding-numbers", () =>
        {
            Tabs.GoTo(AppTab.WhatIf);
            WhatIf.OpenAnswerTab();
            App.TapTextContaining("Understanding these numbers", TimeSpan.FromSeconds(8));
        });
        Capture("guide-47-scenario-explainer", () =>
        {
            WhatIf.OpenScenariosTab();
            App.ScrollTo(WhatIfPage.ScenarioRateChange);
            App.Tap(WhatIfPage.ScenarioRateChange);
            App.WaitUntilVisible("ScenarioTitle", TimeSpan.FromSeconds(20));
            WhatIf.ExpandExplanation();
        });
        Try("close scenario", WhatIf.CloseScenario);
        Capture("guide-48-whatif-projection-settings", () =>
        {
            WhatIf.OpenProjectionTab();
            App.TapTextContaining("ADJUST THE SETTINGS", TimeSpan.FromSeconds(8));
        });
        Capture("guide-49-whatif-insights-panel", () =>
        {
            WhatIf.OpenInsightsTab();
            App.TapTextContaining("Key Insights", TimeSpan.FromSeconds(8));
        });
        Capture("guide-50-settings-step-sizes", () =>
        {
            Tabs.GoTo(AppTab.Settings);
            App.TapTextContaining("How much each", TimeSpan.FromSeconds(8));
        });

        // ── 12. Export, last — it raises the system share sheet, which covers the app ──
        Capture("guide-41-export-card", () =>
        {
            Tabs.GoTo(AppTab.WhatIf);
            WhatIf.OpenAnswerTab();
            App.ScrollTo("WhatIfExportCard");
        });
        Capture("guide-42-export-share-sheet", () =>
        {
            App.Tap("WhatIfExportCard");
        }, settleMs: 1500);   // mid-build: shows the progress overlay
        Thread.Sleep(25000);   // building the 13-page PDF takes a while
        Capture("guide-43-export-share-sheet", () => { }, settleMs: 500);
    }
}
