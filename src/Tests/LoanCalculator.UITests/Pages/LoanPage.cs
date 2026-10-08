using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

public sealed class LoanPage(AppDriver app) : PageBase(app)
{
    /// <summary>
    /// The Loan tab's only FAB. Quick Setup and Quick Input merged into one Loan Details page, so
    /// the old FabQuickInput is gone; this id also serves as the tab's readiness anchor.
    /// </summary>
    public const string LoanDetailsFab = "FabWizard";

    /// <summary>Confirms and closes the Loan Details page.</summary>
    public const string ApplyButton = "QuickInputApplyButton";

    public const string AssetEntry = "AssetEntry";
    public const string DepositEntry = "DepositEntry";
    public const string LoanFormatted = "LoanFormatted";
    public const string LoanWords = "LoanWords";

    /// <summary>
    /// The three-box summary on the Asset tab. Its content is built from <c>Span</c>s, which
    /// cannot be tagged individually, so it is read as one block — it contains the repayment
    /// amount, the selected frequency, the term and the interest rate.
    /// </summary>
    public const string SummaryBox = "LoanSummaryBox";

    public const string InterestRateLabel = "LoanInterestRateLabel";
    public const string InterestRateEntry = "LoanInterestRateEntry";
    public const string InterestRateMinus = "LoanInterestRateMinus";
    public const string InterestRatePlus = "LoanInterestRatePlus";
    public const string FrequencyControl = "LoanFrequencyControl";

    // ── Loan Details page ────────────────────────────────────────────────────────
    //
    // One modal ContentPage for all loan data entry. Asset, deposit and loan amount are always
    // present; upfront, running cost, income and expenses appear only while still empty, so a test
    // must not assume those four are on screen.

    public void OpenLoanDetails()
    {
        App.Tap(LoanDetailsFab);
        App.WaitUntilVisible(AssetEntry, TimeSpan.FromSeconds(15));
    }

    public void ApplyLoanDetails()
    {
        App.Tap(ApplyButton);
        App.WaitUntilGone(AssetEntry, TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Fills asset and deposit on the Loan Details page and applies. The loan amount is derived
    /// from them, so it is deliberately not set here — writing it would override the deposit.
    /// </summary>
    public void EnterAssetAndDeposit(string asset, string deposit)
    {
        OpenLoanDetails();
        App.EnterText(AssetEntry, asset);
        App.EnterText(DepositEntry, deposit);
        ApplyLoanDetails();
    }

    // ── Asset tab controls ───────────────────────────────────────────────────────

    /// <summary>The whole summary block as text — assert on what it contains.</summary>
    public string SummaryText()
    {
        OpenAssetTab();
        App.ScrollTo(SummaryBox);
        return App.TextWithin(SummaryBox);
    }

    /// <summary>
    /// Sets the interest rate. The rate is shown as a label until tapped, which swaps in an
    /// entry — so the label has to be tapped first.
    /// </summary>
    public void SetInterestRate(string rate)
    {
        OpenAssetTab();
        App.ScrollTo(InterestRateLabel);
        App.Tap(InterestRateLabel);
        App.EnterText(InterestRateEntry, rate);
    }

    /// <summary>Monthly / Fortnightly / Weekly, in the order the control renders them.</summary>
    public static readonly string[] Frequencies = ["Monthly", "Fortnightly", "Weekly"];

    /// <summary>
    /// Selects a repayment frequency. Tapped by segment position rather than by label: Syncfusion
    /// draws the segment text, so it is visible but not findable. The position comes from the
    /// control's own bounds — see AppDriver.TapSegment.
    /// </summary>
    public void SelectRepaymentFrequency(string frequency)
    {
        var index = Array.IndexOf(Frequencies, frequency);
        if (index < 0)
            throw new ArgumentException($"Unknown frequency '{frequency}'.", nameof(frequency));

        OpenAssetTab();
        App.TapSegment(FrequencyControl, index, Frequencies.Length);
    }

    // ── First-run setup ──────────────────────────────────────────────────────────

    /// <summary>
    /// Completes first-run setup, which is what every new user sees — the same Loan Details page
    /// the FAB opens, auto-launched after the disclaimer.
    /// </summary>
    /// <remarks>
    /// Fills only asset and deposit, then Calculate. Upfront, running costs, income and expenses
    /// are optional, and on this page they are shown <em>only while empty</em>, so a test must not
    /// assume they are present. The fields use the same <c>AssetEntry</c>/<c>DepositEntry</c> ids as
    /// the rest of the suite: the old <c>WizardAsset</c>/<c>WizardDeposit</c> ids disappeared when
    /// the two pages merged, since one control cannot carry both.
    /// </remarks>
    public void CompleteWizard(string asset, string deposit)
    {
        App.Tap(LoanDetailsFab);
        App.WaitUntilVisible(AssetEntry, TimeSpan.FromSeconds(15));

        App.EnterText(AssetEntry, asset);
        App.EnterText(DepositEntry, deposit);

        // Calculate sits at the end of the scrollable content, below the forced-present software
        // keyboard, so it has to be scrolled into view. No DismissKeyboard() here: EnterText
        // already attempts it, and forceSimulatorSoftwareKeyboardPresence keeps the keyboard on
        // screen regardless — ScrollTo is keyboard-aware instead.
        App.ScrollTo("WizardCalculateButton");

        App.Tap("WizardCalculateButton");
        App.WaitUntilGone("WizardCalculateButton", TimeSpan.FromSeconds(15));
    }

    // ── In-page tabs ─────────────────────────────────────────────────────────────

    public void OpenAssetTab() => OpenTab("LoanTabAsset", LoanDetailsFab);

    public void OpenAmortisationTab() => OpenTab("LoanTabAmortisation", "LoanAmortisationGrid");

    // Anchored on the export button, not the chart: the chart sits inside a collapsed
    // expander further down the tab, so it is in the tree but never visible on arrival.
    public void OpenInsightsTab() => OpenTab("LoanTabInsights", "LoanInsightsExportButton");
}
