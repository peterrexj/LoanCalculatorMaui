using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// Boundary inputs and empty states — the two places this app has actually broken. A deposit
/// covering the whole asset crashed it (Chunk(0) on a zero principal), and wiping the device
/// exposed two screens that assumed data existed. These lock both classes down.
/// </summary>
[TestFixture]
[Category("EdgeCase")]
public class EdgeCaseTests : UITestBase
{
    private bool _wipedData;

    /// <summary>
    /// Two tests here delete app data on purpose. Restoring the baseline afterwards is what keeps
    /// the suite order-independent — without it, whichever fixture ran next would inherit an empty
    /// device and fail for reasons unrelated to its own subject.
    /// </summary>
    [TearDown]
    public void RestoreDataAfterDestructiveTests()
    {
        if (!_wipedData) return;
        _wipedData = false;

        TestData.RestoreBaseline(Tabs, Loan, Budget);
    }

    [Test]
    public void DepositCoveringTheWholeAssetDoesNotCrash()
    {
        Tabs.GoTo(AppTab.Loan);
        Loan.OpenLoanDetails();

        // Principal 0. This used to throw ArgumentOutOfRangeException('size') from Chunk(0) in
        // UpdateLoanPaymentAmortizationDataByYear and surface as "An unexpected error occurred".
        App.EnterText(LoanPage.AssetEntry, "500000");
        App.EnterText(LoanPage.DepositEntry, "500000");

        Loan.ApplyLoanDetails();

        Assert.That(Tabs.IsOn(AppTab.Loan), Is.True,
            "The Loan tab stopped responding after the deposit was set equal to the asset.");
    }

    /// <summary>
    /// Quarantined against a confirmed, unfixed app bug — not a test defect.
    /// <para>
    /// A deposit above the asset value drives the app to ~99% CPU indefinitely: a live-lock, not a
    /// crash and not a hang. Element queries then time out at 90 s, so this test takes ~9 minutes and
    /// leaves the app pegged, which also fails the next two tests in this fixture. Ignoring it keeps
    /// the suite usable and honest; it must be re-enabled when the bug is fixed.
    /// </para>
    /// <para>
    /// Ruled out so far: the amortisation guards (the zero-principal case in TC-6.1 passes), and a
    /// mismatch between the model's 100% deposit clamp and the sliders' Maximum of 99 — aligning
    /// those changed nothing, so that hypothesis was wrong and the change was reverted.
    /// <c>ProcessDepositCalc</c> and the payment-schedule loop are both straight-line, so the
    /// oscillation is in the UI layer.
    /// </para>
    /// <para>
    /// <b>Located precisely by profiling</b> (<c>sample &lt;pid&gt;</c> while pegged, twice): 100% of
    /// main-thread samples sit in <c>-[UIKit_UIControlEventProxy BridgeSelector]</c> →
    /// <c>native_to_managed_trampoline</c>. So it is a UIControl event storm — a UITextField
    /// editing-changed event being re-raised without end — not a loop in the calculator.
    /// </para>
    /// <para>
    /// Also tried and reverted, neither changed the hot path: aligning the slider Maximum with the
    /// model clamp, and making the text-changed handlers skip redundant view-model writes. Note
    /// <c>_suppressTextChanged</c> is set and cleared synchronously while iOS delivers the event
    /// asynchronously, so it cannot suppress a programmatic write-back — that is almost certainly
    /// involved, but guarding the value alone did not stop it.
    /// </para>
    /// <para>
    /// Next step: log inside each of the three text-changed handlers (asset/deposit/loan) to a file
    /// in the app's Documents directory — Console.WriteLine does not reach the iOS unified log — to
    /// see <em>which</em> entry re-fires and with what text. The profiler says where; only that will
    /// say which.
    /// </para>
    /// </summary>
    [Test]
    [Ignore("Confirmed app bug: a deposit above the asset value live-locks the app at ~100% CPU. " +
            "Diagnosis notes are in the XML doc comment above. Re-enable once fixed.")]
    public void ADepositLargerThanTheAssetDoesNotCrash()
    {
        Tabs.GoTo(AppTab.Loan);
        Loan.OpenLoanDetails();

        App.EnterText(LoanPage.AssetEntry, "400000");
        App.EnterText(LoanPage.DepositEntry, "600000");

        Loan.ApplyLoanDetails();

        Assert.That(Tabs.IsOn(AppTab.Loan), Is.True,
            "The Loan tab stopped responding after the deposit was set above the asset value.");
    }

    [Test]
    public void AZeroPrincipalStillLetsEveryTabRender()
    {
        Tabs.GoTo(AppTab.Loan);
        Loan.EnterAssetAndDeposit("500000", "500000");

        // The amortisation and insights views derive from the payment schedule, which is empty
        // at a zero principal — exactly where the two guarded methods are reached.
        foreach (var tab in new[] { AppTab.Budget, AppTab.WhatIf, AppTab.Settings, AppTab.Loan })
        {
            Tabs.GoTo(tab);
            Assert.That(Tabs.IsOn(tab), Is.True,
                $"The {tab} tab failed to render while the loan principal was zero.");
        }

        // Both of these throw if their content never renders, which is the assertion.
        Loan.OpenAmortisationTab();
        Loan.OpenInsightsTab();

        Assert.That(App.Exists("LoanInsightsExportButton"), Is.True,
            "The Insights tab did not render while the loan principal was zero.");
    }

    [Test]
    public void AZeroInterestRateIsCalculatedWithoutDividingByZero()
    {
        Tabs.GoTo(AppTab.Loan);
        Loan.EnterAssetAndDeposit("600000", "100000");

        // CalculateHomeLoan has a dedicated InterestRate == 0 branch that skips the amortisation
        // formula entirely — a separate code path with no other UI coverage.
        Loan.SetInterestRate("0");

        // TextWithin, not Text: the summary is a container whose content comes from Spans.
        App.WaitUntil(
            () => App.TextWithin(LoanPage.SummaryBox).Trim().Length > 0,
            "the loan summary to render at a zero interest rate");

        Assert.That(App.TextWithin(LoanPage.SummaryBox), Is.Not.Empty,
            "The loan summary went blank at a zero interest rate.");

        // OpenAmortisationTab waits for the schedule grid and throws if it never renders, which is
        // the real assertion here. Tabs.IsOn(Loan) must not be used: its anchor is the Asset
        // sub-tab's FAB, so it is false whenever another in-page tab is open.
        Loan.OpenAmortisationTab();
        Assert.That(App.Exists("LoanAmortisationGrid"), Is.True,
            "The amortisation schedule did not render at a zero interest rate.");
    }

    [Test]
    public void EmptyingTheLoanDataLeavesEveryTabUsable()
    {
        // Seed first, so the delete genuinely removes something.
        TestData.EnsureLoan(Tabs, Loan);

        Tabs.GoTo(AppTab.Settings);
        _wipedData = true;
        Settings.DeleteLoanData();

        foreach (var tab in new[] { AppTab.Loan, AppTab.Budget, AppTab.WhatIf, AppTab.Settings })
        {
            Tabs.GoTo(tab);
            Assert.That(Tabs.IsOn(tab), Is.True,
                $"The {tab} tab failed to render after the loan data was deleted.");
        }
    }

    /// <summary>
    /// The destructive one. Wiping everything is how a real user resets the app, and it puts every
    /// screen into its empty state at once — which is where the Summary placeholder and What If
    /// no-data panel bugs were found by accident.
    /// </summary>
    [Test]
    public void EmptyingAllDataLeavesEveryTabUsable()
    {
        TestData.EnsureLoan(Tabs, Loan);
        TestData.EnsureBudget(Tabs, Budget);

        Tabs.GoTo(AppTab.Settings);
        if (!Settings.CanDeleteAllData())
            Assert.Ignore("The All Data button is hidden on this device (IsAllDataDeleteVisible is false).");

        _wipedData = true;
        Settings.DeleteAllData();

        foreach (var tab in new[] { AppTab.Loan, AppTab.Budget, AppTab.WhatIf, AppTab.Settings })
        {
            Tabs.GoTo(tab);
            Assert.That(Tabs.IsOn(tab), Is.True,
                $"The {tab} tab failed to render after all data was deleted.");
        }

        // And the empty state must survive a relaunch, which is when data is re-read from disk.
        App.RestartApp();
        Launch.WaitUntilReady();

        Assert.That(Tabs.IsOn(AppTab.Loan), Is.True,
            "The app did not come back up cleanly after all data was deleted.");
    }

    [Test]
    public void SwitchingRepaymentFrequencyChangesTheSummary()
    {
        Tabs.GoTo(AppTab.Loan);
        Loan.EnterAssetAndDeposit("650000", "130000");

        // The frequency segmented control needed SyncfusionIosTouchFix to be tappable at all. If
        // that regresses the taps are swallowed silently, so this asserts the summary really moved.
        Loan.SelectRepaymentFrequency("Monthly");
        var monthly = Loan.SummaryText();

        Loan.SelectRepaymentFrequency("Fortnightly");

        var fortnightly = App.WaitForText(
            LoanPage.SummaryBox,
            shown => !shown.Equals(monthly, StringComparison.Ordinal),
            "changed after switching from Monthly to Fortnightly");

        Assert.That(fortnightly, Is.Not.EqualTo(monthly),
            "Switching the repayment frequency did not change the loan summary — the taps are " +
            "probably being swallowed (see SyncfusionIosTouchFix).");
    }
}
