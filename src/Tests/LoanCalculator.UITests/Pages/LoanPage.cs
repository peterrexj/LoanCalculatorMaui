using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

public sealed class LoanPage(AppDriver app) : PageBase(app)
{
    /// <summary>
    /// The Loan tab's only FAB. Quick Setup and Quick Input merged into one Loan Details page, so
    /// the old FabQuickInput is gone; this id also serves as the tab's readiness anchor.
    /// </summary>
    public const string LoanDetailsFab = "FabWizard";

    // The repayment schedule is a pushed page now, not a tab.
    public const string ScheduleCard = "LoanScheduleCard";
    public const string AmortisationGrid = "LoanAmortisationGrid";

    /// <summary>
    /// Box 3 of the summary row — the affordability verdict, which links to What If ▸ Answer.
    /// Addressed separately from <see cref="SummaryBox"/> because that id covers the whole
    /// three-box row, whose centre is box 2.
    /// </summary>
    public const string VerdictLink = "LoanVerdictLink";
    private const string AmortisationBack = "AmortisationBackButton";

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

        // Blur the field, which is what actually puts the keyboard away. EnterText already calls
        // DismissKeyboard(), but this field raises iOS's NUMERIC keypad, which has no Return/Done
        // key — so XCUITest's HideKeyboard() throws and DismissKeyboard() deliberately swallows
        // that, leaving the keypad up.
        //
        // That strands anything in the bottom ~230pt of the screen. Measured from a failure
        // capture: the keypad occupied y 583–816 and FabWizard sat at y 715–771, reported
        // visible="false" while still enabled="true". ScrollTo cannot rescue it either, because
        // the FAB lives OUTSIDE the page's ScrollView — so it failed as "scrolled 8 times without
        // bringing FabWizard into view", which reads like a missing AutomationId.
        //
        // SummaryBox is the blur target because it is inert (display-only, no gesture recognisers)
        // and sits at the top of the page, so the tap cannot be swallowed by the keypad it is
        // trying to dismiss.
        App.Tap(SummaryBox);
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
    /// Fills only asset and deposit, then taps Done. Upfront, running costs, income and expenses
    /// are optional, and on this page they are shown <em>only while empty</em>, so a test must not
    /// assume they are present. The fields use the same <c>AssetEntry</c>/<c>DepositEntry</c> ids as
    /// the rest of the suite: the old <c>WizardAsset</c>/<c>WizardDeposit</c> ids disappeared when
    /// the two pages merged, since one control cannot carry both.
    ///
    /// This used to scroll to and tap a separate "Calculate" button at the end of the scrollable
    /// content, which sat under the forced-present software keyboard on some devices (see
    /// TROUBLESHOOTING.md). That button was removed — Done, in the header, is never hidden and
    /// calls the same handler — so this is now identical to <see cref="ApplyLoanDetails"/>.
    /// </remarks>
    public void CompleteWizard(string asset, string deposit)
    {
        App.Tap(LoanDetailsFab);
        App.WaitUntilVisible(AssetEntry, TimeSpan.FromSeconds(15));

        App.EnterText(AssetEntry, asset);
        App.EnterText(DepositEntry, deposit);

        ApplyLoanDetails();
    }

    // ── In-page tabs ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the Loan page's own content is showing. Kept under its original name so the
    /// five call sites did not change, but Loan has no tab strip any longer — Asset IS the
    /// page. So this dismisses anything pushed over it and scrolls the FAB back into view,
    /// rather than tapping a tab that no longer exists.
    /// </summary>
    public void OpenAssetTab()
    {
        ModalPages.DismissAny(App);

        // Navigate to Loan first. This method only ever *probed* for Loan's content, so calling
        // it while standing on another tab scrolled a page that does not contain the anchor and
        // burnt the whole swipe budget — which is how the screenshot sweep died immediately
        // after its Insights capture left it on What If. Owning the navigation here matches
        // WhatIfPage.EnsureOnWhatIf and BudgetPage.EnsureOnBudget, and keeps every call site
        // correct no matter where it starts from.
        new ShellTabs(App).GoTo(AppTab.Loan);

        // Anchored on SummaryBox, NOT on the FAB. The FAB is the wrong shape of anchor twice
        // over: it sits in the bottom-right corner, which is precisely the region an open
        // keyboard occludes, and it lives outside the page's ScrollView, so the ScrollTo
        // fallback below can never bring it back. A keyboard left up by an earlier step
        // therefore produced "scrolled 8 times without bringing FabWizard into view" — a
        // message that points at the markup rather than at the keyboard.
        //
        // SummaryBox is inside the ScrollView and at the top of the page, so it is both a
        // truthful "the Loan page's content is showing" signal and one that scrolling can
        // actually satisfy.
        if (App.Exists(SummaryBox, TimeSpan.FromMilliseconds(600))) return;
        App.ScrollTo(SummaryBox, maxSwipes: 8, up: true);
    }

    /// <summary>Taps the affordability verdict, which navigates to What If ▸ Answer.</summary>
    public void TapVerdictLink()
    {
        OpenAssetTab();
        App.ScrollTo(VerdictLink);
        App.Tap(VerdictLink);
    }

    /// <summary>
    /// Opens the repayment schedule. Kept under its original name so the five call sites did not
    /// have to change, but it is no longer a tab: the schedule became a pushed page reached by a
    /// row on the Asset screen, because it is a reference table rather than something you edit.
    /// </summary>
    public void OpenAmortisationTab()
    {
        if (App.Exists(AmortisationGrid, TimeSpan.FromMilliseconds(600))) return;

        OpenAssetTab();
        App.ScrollTo(ScheduleCard);
        App.Tap(ScheduleCard);
        App.WaitUntilVisible(AmortisationGrid, TimeSpan.FromSeconds(20));
    }

    /// <summary>Returns from the schedule page. Safe to call when it is not open.</summary>
    public void CloseAmortisation()
    {
        if (App.Exists(AmortisationBack, TimeSpan.FromMilliseconds(600)))
            App.Tap(AmortisationBack);
    }

    /// <summary>
    /// Automation id of a schedule decade's expander header, e.g. <c>2026–2035</c> → the id built
    /// by <c>AmortisationDecade.AutomationId</c>. The schedule is decade disclosures now rather
    /// than a five-row pager, so a decade — not a page number — is the unit a test addresses.
    /// </summary>
    public static string DecadeHeader(int fromYear, int toYear) =>
        fromYear == toYear
            ? $"LoanScheduleDecade_{fromYear}"
            : $"LoanScheduleDecade_{fromYear}_{toYear}";

    /// <summary>
    /// Expands a decade on the schedule. The first decade renders already open (the prototype
    /// opens it so the rows are visibly disclosable), so tapping it would COLLAPSE it — hence
    /// this takes the decade to open rather than an index, and the caller picks a later one.
    /// </summary>
    public void OpenScheduleDecade(int fromYear, int toYear)
    {
        OpenAmortisationTab();

        var id = DecadeHeader(fromYear, toYear);
        App.ScrollTo(id);
        App.Tap(id);
    }

    // Anchored on the export button, not the chart: the chart sits inside a collapsed
    // expander further down the tab, so it is in the tree but never visible on arrival.
    // OpenInsightsTab moved to WhatIfPage — Insights is a What If sub-tab now.
}
