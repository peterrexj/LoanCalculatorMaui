using LoanCalculator.UITests.Infrastructure;

// Deliberately the root namespace, not .Infrastructure: a SetUpFixture only applies to its
// own namespace and the namespaces below it, so from here it covers .Tests as well.
namespace LoanCalculator.UITests;

/// <summary>One Appium server and one driver session for the whole run.</summary>
[SetUpFixture]
public class AppiumSetup
{
    private static AppiumServer? _server;
    private static AppDriver? _app;

    public static AppDriver App => _app ?? throw new InvalidOperationException(
        "The Appium session was never created. Look for the OneTimeSetUp failure above.");

    [OneTimeSetUp]
    public void StartSession()
    {
        if (!UITestConfig.IsEnabled)
        {
            Assert.Ignore(
                "UI tests are opt-in because they need a booted simulator/emulator. " +
                "Run them with src/Tests/LoanCalculator.UITests/run-uitests.sh --ios (or --android), " +
                "or set UITEST_PLATFORM yourself.");
        }

        TestContext.Progress.WriteLine(UITestConfig.Describe());
        Directory.CreateDirectory(UITestConfig.ArtifactDirectory);

        _server = AppiumServer.StartOrAttach(UITestConfig.AppiumUri, UITestConfig.AutoStartAppium);
        _app = AppDriver.Create();

        TestContext.Progress.WriteLine($"[session] driver ready on {UITestConfig.Platform}");
    }

    [OneTimeTearDown]
    public void EndSession()
    {
        _app?.Dispose();
        _app = null;

        _server?.Dispose();
        _server = null;
    }
}
