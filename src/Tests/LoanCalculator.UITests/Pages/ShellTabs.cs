using LoanCalculator.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace LoanCalculator.UITests.Pages;

public enum AppTab
{
    Loan,
    Budget,
    WhatIf,
    Settings,
}

/// <summary>
/// Drives the four-tab Shell tab bar. MAUI does not put an AutomationId on Shell tabs, so
/// tabs are found by their title. Several pages also contain a label with the same words,
/// so matches are disambiguated by screen position: the tab bar is always the bottom-most
/// thing on screen.
/// </summary>
public sealed class ShellTabs(AppDriver app)
{
    private static string TitleOf(AppTab tab) => tab switch
    {
        AppTab.Loan => "Loan",
        AppTab.Budget => "Budget",
        AppTab.WhatIf => "What If",
        AppTab.Settings => "Settings",
        _ => throw new ArgumentOutOfRangeException(nameof(tab)),
    };

    /// <summary>An element that only exists once the tab's page has rendered.</summary>
    internal static string AnchorOf(AppTab tab) => tab switch
    {
        // FabWizard is the Loan tab's only FAB since Quick Setup and Quick Input merged.
        AppTab.Loan => "FabWizard",
        AppTab.Budget => "BudgetTabIncome",
        AppTab.WhatIf => "WhatIfPageTitle",
        AppTab.Settings => "SettingsPageTitle",
        _ => throw new ArgumentOutOfRangeException(nameof(tab)),
    };

    /// <summary>
    /// Switches tab. Every page here is a ScrollView, so a page-title anchor scrolls out of view
    /// once a test works further down the page — which is why arrival tolerates the page being
    /// scrolled and, if needed, scrolls back up to the anchor.
    /// <para>
    /// The scroll-back happens <b>only</b> after the tab has been tapped. Doing it in the
    /// "am I already here?" pre-check meant every ordinary tab switch blind-swiped up to eight
    /// times just to discover it was not already on that tab — a lot of wasted wall-clock for a
    /// question a single visibility check answers.
    /// </para>
    /// </summary>
    public void GoTo(AppTab tab)
    {
        var anchor = AnchorOf(tab);

        // Fast path, no swiping: already here and at a position where the anchor shows.
        if (app.Exists(anchor, TimeSpan.FromMilliseconds(600))) return;

        FindTabButton(TitleOf(tab)).Click();

        // Confirm arrival rather than assuming the tap took — a tap that lands during a page
        // transition is silently swallowed, and this turns that into a clear failure.
        if (app.Exists(anchor, TimeSpan.FromSeconds(25))) return;

        // Landed, but the page is still scrolled from an earlier visit. Recover once, which also
        // leaves the page at a consistent starting position for whatever runs next.
        app.ScrollTo(anchor, maxSwipes: 8, up: true);
    }

    /// <summary>
    /// Cheap check with no scrolling. Safe to assert on straight after <see cref="GoTo"/>, which
    /// leaves the page scrolled to its anchor.
    /// </summary>
    public bool IsOn(AppTab tab) => app.Exists(AnchorOf(tab), TimeSpan.FromSeconds(1));

    private AppiumElement FindTabButton(string title)
    {
        var deadline = DateTime.UtcNow + UITestConfig.DefaultTimeout;

        while (true)
        {
            var candidate = LowestMatchOnScreen(title);
            if (candidate is not null) return candidate;

            if (DateTime.UtcNow >= deadline)
            {
                throw new AssertionException(
                    $"Could not find the '{title}' tab in the Shell tab bar on {app.Platform}. " +
                    "Check the tab titles in AppShell.xaml still match ShellTabs.TitleOf.");
            }

            Thread.Sleep(250);
        }
    }

    private AppiumElement? LowestMatchOnScreen(string title)
    {
        var by = MobileBy.XPath(
            $"//*[@text='{title}' or @label='{title}' or @name='{title}' or @content-desc='{title}']");

        try
        {
            return app.Raw.FindElements(by)
                .Cast<AppiumElement>()
                .Where(element => element.Displayed)
                .OrderByDescending(element => element.Location.Y)
                .FirstOrDefault();
        }
        catch (WebDriverException)
        {
            // Tree rebuilt mid-query — the caller retries.
            return null;
        }
    }
}
