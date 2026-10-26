using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

/// <summary>
/// Dismisses whichever pushed page is covering the shell.
/// </summary>
/// <remarks>
/// <para>Every modal in this app covers the tab bar, so one left open blocks <b>all</b>
/// navigation — and the failure does not look like "a modal is open", it looks like
/// "could not find the Budget tab", several steps later and in an unrelated test.</para>
/// <para>The redesign added four pushed pages (schedule, scenario, upfront costs, transaction
/// entry) on top of the two that already existed, so navigation now clears the way first rather
/// than each page object remembering to tidy up after itself. <see cref="AppLaunch"/> does the
/// same thing, but only once at launch.</para>
/// </remarks>
internal static class ModalPages
{
    /// <summary>
    /// Dismiss buttons, in the order they should be tried. The two legal popups come first
    /// because they are full-screen and sit above anything else.
    /// </summary>
    private static readonly string[] DismissButtons =
    [
        "SettingsDisclaimerCloseButton",
        "SettingsPrivacyCloseButton",
        "ScenarioBackButton",
        "AmortisationBackButton",
        "TransactionEntryCancelButton",
        "UpfrontDoneButton",
        "WizardCancelButton",
    ];

    /// <summary>
    /// Closes any open pushed page. A no-op when none is open, and cheap in that case — one
    /// short existence probe per candidate.
    /// </summary>
    internal static void DismissAny(AppDriver app)
    {
        // Bounded rather than while(true): a modal that will not dismiss should surface as the
        // caller's own "could not find X" failure, not as an infinite loop here.
        for (var attempt = 0; attempt < DismissButtons.Length; attempt++)
        {
            var open = DismissButtons.FirstOrDefault(
                id => app.Exists(id, TimeSpan.FromMilliseconds(250)));

            if (open is null) return;

            try
            {
                app.Tap(open);
                app.WaitUntilGone(open, TimeSpan.FromSeconds(10));
            }
            catch (Exception)
            {
                // Could not dismiss it. Leave it to the caller's own lookup to fail with a
                // message about what it was actually looking for.
                return;
            }
        }
    }
}
