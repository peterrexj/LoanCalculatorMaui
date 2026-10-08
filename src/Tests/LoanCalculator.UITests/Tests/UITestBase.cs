using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;
using NUnit.Framework.Interfaces;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// Restarts the app before each test so no test can be affected by where a previous one
/// left the UI, and dumps a screenshot plus the element tree on failure.
/// </summary>
public abstract class UITestBase
{
    protected AppDriver App => AppiumSetup.App;

    protected ShellTabs Tabs { get; private set; } = null!;
    protected AppLaunch Launch { get; private set; } = null!;
    protected LoanPage Loan { get; private set; } = null!;
    protected BudgetPage Budget { get; private set; } = null!;
    protected WhatIfPage WhatIf { get; private set; } = null!;
    protected SettingsPage Settings { get; private set; } = null!;

    [SetUp]
    public void BaseSetUp()
    {
        Tabs = new ShellTabs(App);
        Launch = new AppLaunch(App);
        Loan = new LoanPage(App);
        Budget = new BudgetPage(App);
        WhatIf = new WhatIfPage(App);
        Settings = new SettingsPage(App);

        if (UITestConfig.RestartBetweenTests) App.RestartApp();

        Launch.WaitUntilReady();
    }

    /// <summary>
    /// Marks a test as not runnable on Android, with the reason recorded in the run output.
    /// Reported as ignored rather than passing, so a known gap stays visible.
    /// </summary>
    protected void SkipOnAndroid(string reason)
    {
        if (App.Platform == TestPlatform.Android)
            Assert.Ignore($"Not supported on Android: {reason}");
    }

    [TearDown]
    public void BaseTearDown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            Artifacts.CaptureFailure(App, TestContext.CurrentContext.Test.Name);
    }
}
