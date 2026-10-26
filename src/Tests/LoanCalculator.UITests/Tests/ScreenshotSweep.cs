using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;
using NUnit.Framework.Interfaces;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// Walks every reachable surface and screenshots it, for design review rather than assertion.
/// Output lands in <c>UITestConfig.ArtifactDirectory</c> as an ordered, stably-named set.
/// </summary>
/// <remarks>
/// <para>
/// Not a test: it asserts almost nothing, so it is fenced behind its own category and excluded
/// from the suite's own categories. Run it deliberately:
/// <c>./run-uitests.sh --ios --no-build --filter "TestCategory=Screens"</c>
/// </para>
/// <para>
/// Each surface is captured through a <see cref="Capture"/> wrapper that records what succeeded
/// and swallows what did not, then reports both at the end. A sweep that cannot reach one surface
/// still returns every other screenshot — the opposite of a test, where the first failure should
/// stop everything. The summary is the deliverable: it says which surfaces need capturing by hand
/// rather than leaving a silent gap in the set.
/// </para>
/// <para>
/// Populated and empty states are separate tests because they want opposite preconditions. Empty
/// runs first (<see cref="OrderAttribute"/>) so the wipe cannot destroy the data the populated
/// sweep seeds.
/// </para>
/// </remarks>
[TestFixture]
[Category("Screens")]
public class ScreenshotSweep : UITestBase
{
    private readonly List<string> _captured = [];
    private readonly List<string> _missed = [];

    /// <summary>
    /// NUnit reuses one fixture instance across both tests, so without this the second
    /// report re-lists everything the first one captured and its count looks inflated.
    /// </summary>
    [SetUp]
    public void ResetSweepLog()
    {
        _captured.Clear();
        _missed.Clear();
    }

    /// <summary>
    /// Navigates and captures, downgrading any failure to a recorded miss. Returns whether it
    /// worked, so a caller can skip dependent steps (e.g. not try to dismiss a modal that
    /// never opened).
    /// </summary>
    private bool Capture(string surfaceName, Action navigate)
    {
        try
        {
            navigate();
            Artifacts.Capture(App, surfaceName);
            _captured.Add(surfaceName);
            return true;
        }
        catch (Exception ex)
        {
            // One unreachable surface must not cost us the rest of the sweep.
            _missed.Add($"{surfaceName} — {ex.GetType().Name}: {Summarise(ex.Message)}");
            TestContext.Progress.WriteLine($"[sweep] MISSED {surfaceName}: {ex.Message}");
            return false;
        }
    }

    private static string Summarise(string message)
    {
        var firstLine = message.Split('\n')[0].Trim();
        return firstLine.Length <= 140 ? firstLine : firstLine[..140] + "…";
    }

    /// <summary>
    /// Writes the run report beside the PNGs as well as to the console. The file is the
    /// reliable half: <see cref="TestContext.Progress"/> is silently dropped by the default
    /// console logger, so a sweep that skipped a surface looked indistinguishable from one
    /// that captured everything.
    /// </summary>
    [TearDown]
    public void ReportSweep()
    {
        var lines = new List<string>
        {
            $"{TestContext.CurrentContext.Test.Name} — captured {_captured.Count}, missed {_missed.Count}",
        };
        lines.AddRange(_captured.Select(name => $"  ok      {name}"));
        lines.AddRange(_missed.Select(miss => $"  MISSED  {miss}"));

        foreach (var line in lines) TestContext.Progress.WriteLine($"[sweep] {line}");

        try
        {
            Directory.CreateDirectory(UITestConfig.ArtifactDirectory);
            File.AppendAllLines(
                Path.Combine(UITestConfig.ArtifactDirectory, "sweep-report.txt"),
                lines.Append(string.Empty));
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[sweep] could not write report: {ex.Message}");
        }

        // An empty sweep is worth failing on — it means navigation is broken rather than one
        // screen having moved. But only assert when the test itself passed: if the body already
        // threw, adding a second failure here REPLACES the real exception in the runner output
        // with this assertion, which is exactly how a splash-screen hang once got reported as
        // "the sweep captured nothing" with no stack trace pointing at the actual cause.
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed)
            Assert.That(_captured, Is.Not.Empty, "The sweep captured nothing at all.");
    }

    /// <summary>
    /// The "no data yet" states, which need the opposite precondition to everything else.
    /// Wipes first, so these cannot be contaminated by a previous run's seeded data.
    /// </summary>
    [Test]
    [Order(1)]
    public void CaptureEmptyStates()
    {
        // DeleteAllData opens the collapsed section, confirms the native prompt and calls
        // TestData.MarkWiped itself, so none of that is repeated here. It does not navigate,
        // though — it scrolls within whatever page is showing.
        Tabs.GoTo(AppTab.Settings);
        Settings.DeleteAllData();

        // No What If capture here, deliberately. Deleting data clears the loan JSON but
        // leaves the live form populated (CLAUDE.md), and What If computes from that live
        // LoanViewModel — so its no-data panel never appears in-session and a capture named
        // "whatif-empty" was silently identical to the populated 09-whatif. That state is
        // only reachable on a genuinely fresh install (UITEST_FRESH_INSTALL=true), which
        // wants its own run rather than a misleading file in this one.
        Capture("31-budget-income-empty", () =>
        {
            Tabs.GoTo(AppTab.Budget);
            Budget.OpenIncomeTab();
        });
        // Not an "empty loan": deleting loan data clears only the JSON file and deliberately
        // leaves the live form populated (CLAUDE.md), so there is no in-session empty-loan
        // state to capture. What this does show is the affordability panel locked for want of
        // income/expenses, which is a state worth having.
        Capture("32-loan-asset-no-budget", () =>
        {
            Tabs.GoTo(AppTab.Loan);
            Loan.OpenAssetTab();
        });
    }

    /// <summary>Every surface that wants real figures on screen.</summary>
    [Test]
    [Order(2)]
    public void CapturePopulatedSurfaces()
    {
        TestData.EnsureLoan(Tabs, Loan);
        TestData.EnsureBudget(Tabs, Budget);

        // ── Loan ────────────────────────────────────────────────────────────
        Capture("01-loan-asset", () =>
        {
            Tabs.GoTo(AppTab.Loan);
            Loan.OpenAssetTab();
        });
        Capture("02-loan-amortisation", Loan.OpenAmortisationTab);
        Capture("04-whatif-insights", WhatIf.OpenInsightsTab);

        // Return to Loan. The Insights capture above leaves us on What If, and the modal
        // captures further down start from Loan. OpenAssetTab navigates there itself now, so
        // this is a real return rather than a probe — an earlier revision of this line assumed
        // it navigated when it only probed, which failed the sweep here instead of stranding
        // the modal captures as it had the run before.
        Loan.OpenAssetTab();

        // ── Budget ──────────────────────────────────────────────────────────
        Capture("05-budget-income", () =>
        {
            Tabs.GoTo(AppTab.Budget);
            Budget.OpenIncomeTab();
        });
        Capture("06-budget-expenses", Budget.OpenExpensesTab);
        Capture("07-budget-summary", Budget.OpenSummaryTab);
        Capture("08-whatif-projection", WhatIf.OpenProjectionTab);
        Capture("03-budget-asset-costs", Budget.OpenAssetCostsTab);

        // ── What If & Settings ──────────────────────────────────────────────
        //
        // Open the Answer sub-tab explicitly. A bare GoTo(AppTab.WhatIf) captures whichever
        // sub-tab was last selected, and the Projection capture above leaves Projection
        // showing — so this file was silently a second copy of 08 rather than the landing view.
        Capture("09-whatif-answer", WhatIf.OpenAnswerTab);

        // The scenario index and one scenario page: the two surfaces the redesign actually
        // introduced, and the ones with prototypes to compare against
        // (whatif-scenarios.html, whatif-scenario.html). Neither was captured before.
        Capture("09b-whatif-scenarios", WhatIf.OpenScenariosTab);
        if (Capture("09c-whatif-scenario-rate", () =>
            {
                WhatIf.OpenScenariosTab();
                App.ScrollTo(WhatIfPage.ScenarioRateChange);
                App.Tap(WhatIfPage.ScenarioRateChange);
                App.WaitUntilVisible(WhatIfPage.RateDeltaValue, TimeSpan.FromSeconds(20));
            }))
        {
            // Leave the index showing rather than a scenario, so Settings navigation below
            // is not starting from behind a pushed page.
            WhatIf.CloseScenario();
        }

        Capture("10-settings", () => Tabs.GoTo(AppTab.Settings));

        CaptureSettingsPopups();
        CaptureModals();
    }

    private void CaptureSettingsPopups()
    {
        // Both rows live in the Support card well down the page, so each needs scrolling into
        // view before it can be tapped. Both popups are dismissed by their own footer button
        // rather than by navigating away.
        if (Capture("11-settings-disclaimer", () =>
            {
                Tabs.GoTo(AppTab.Settings);
                App.ScrollTo("SettingsDisclaimerRow");
                App.Tap("SettingsDisclaimerRow");
                App.WaitUntilVisible("SettingsDisclaimerCloseButton", TimeSpan.FromSeconds(10));
            }))
        {
            Dismiss("SettingsDisclaimerCloseButton");
        }

        if (Capture("12-settings-privacy", () =>
            {
                Tabs.GoTo(AppTab.Settings);
                App.ScrollTo("SettingsPrivacyRow");
                App.Tap("SettingsPrivacyRow");
            }))
        {
            Dismiss("SettingsPrivacyCloseButton");
        }
    }

    private void CaptureModals()
    {
        // Each modal is dismissed in the same step that captures it, so a modal left open
        // cannot strand the surfaces that follow.
        if (Capture("20-loan-details", () =>
            {
                Tabs.GoTo(AppTab.Loan);
                Loan.OpenAssetTab();
                App.Tap(LoanPage.LoanDetailsFab);
                App.WaitUntilVisible("AssetEntry", TimeSpan.FromSeconds(15));
            }))
        {
            Dismiss("WizardCancelButton");
        }

        if (Capture("21-upfront-costs", () =>
            {
                Tabs.GoTo(AppTab.Loan);
                Loan.OpenAssetTab();
                App.ScrollTo("LoanUpfrontCostsCard");
                App.Tap("LoanUpfrontCostsCard");
                App.WaitUntilVisible("MortgageEntry", TimeSpan.FromSeconds(15));
            }))
        {
            Dismiss("UpfrontDoneButton");
        }

        if (Capture("22-transaction-entry", () =>
            {
                Tabs.GoTo(AppTab.Budget);
                Budget.OpenIncomeTab();
                App.Tap(BudgetPage.IncomeAddFab);
                App.WaitUntilVisible("BudgetIncomeAmountEntry", TimeSpan.FromSeconds(15));
            }))
        {
            Dismiss("TransactionEntryCancelButton");
        }
    }

    /// <summary>
    /// Closes a modal without failing the sweep if the button has moved — the screenshot is
    /// already saved by this point, and the next surface re-navigates from a tab anyway.
    /// </summary>
    private void Dismiss(string automationId)
    {
        try
        {
            App.Tap(automationId);
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine(
                $"[sweep] could not dismiss via '{automationId}': {ex.Message}");
        }
    }
}
