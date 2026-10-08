using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

/// <summary>
/// Gets the app from "just launched" to "ready to test": waits out the animated splash, then
/// clears whichever first-run gates are in the way. On a device with saved data there are
/// none; on a truly fresh install there are two, and both block every other interaction —
/// the launch disclaimer, then the Quick Setup wizard.
/// </summary>
public sealed class AppLaunch(AppDriver app)
{
    private const string DisclaimerAccept = "DisclaimerAcceptButton";
    private const string WizardCancel = "WizardCancelButton";

    public void WaitUntilReady()
    {
        var landing = ShellTabs.AnchorOf(AppTab.Loan);
        var deadline = DateTime.UtcNow + UITestConfig.LaunchTimeout;

        while (DateTime.UtcNow < deadline)
        {
            // The disclaimer is modal and full-screen, so it wins if several are present.
            if (app.Exists(DisclaimerAccept, TimeSpan.FromMilliseconds(500)))
            {
                app.Tap(DisclaimerAccept);
                app.WaitUntilGone(DisclaimerAccept, TimeSpan.FromSeconds(15));
                continue;
            }

            // Fresh install: the Loan Details page auto-opens as a modal over the shell. Dismiss
            // it so tests start from the same place they would on a device that has been used.
            // One page means this id appears exactly once — it was once on all three popup steps,
            // relying on only the open one being visible.
            if (app.Exists(WizardCancel, TimeSpan.FromMilliseconds(500)))
            {
                app.Tap(WizardCancel);
                app.WaitUntilGone(WizardCancel, TimeSpan.FromSeconds(15));
                continue;
            }

            if (app.Exists(landing, TimeSpan.FromMilliseconds(500))) return;

            Thread.Sleep(500);
        }

        throw new AssertionException(
            $"The app did not reach the Loan tab within {UITestConfig.LaunchTimeout.TotalSeconds:0}s. " +
            "It may have crashed on launch, or stalled on the splash screen — " +
            "check the captured page source artifact.");
    }
}
