using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

[TestFixture]
[Category("Settings")]
public class SettingsTests : UITestBase
{
    [SetUp]
    public void GoToSettingsTab() => Tabs.GoTo(AppTab.Settings);

    /// <summary>
    /// Theme files are embedded resources loaded by key at runtime, so a key missing from
    /// one theme only fails when that theme is selected and a page that uses the key is
    /// rendered. This walks every theme and re-renders every tab to catch exactly that.
    /// </summary>
    [Test]
    public void EveryThemeRendersEveryTab()
    {
        SkipOnAndroid("tapping an SfComboBox does not open its drop-down through "
            + "UiAutomator2 — the list never enters the element tree, so no option can be "
            + "selected. Same family as the SyncfusionIosTouchFix overlay problem.");

        try
        {
            foreach (var theme in SettingsPage.Themes)
            {
                Settings.SelectTheme(theme);

                foreach (var tab in new[] { AppTab.Loan, AppTab.Budget, AppTab.WhatIf, AppTab.Settings })
                {
                    Tabs.GoTo(tab);
                    Assert.That(Tabs.IsOn(tab), Is.True,
                        $"The {tab} tab failed to render under the {theme} theme.");
                }
            }
        }
        finally
        {
            // Leave the device on the default theme so later tests and manual runs are predictable.
            Tabs.GoTo(AppTab.Settings);
            Settings.SelectTheme("Dark");
        }
    }

    /// <summary>
    /// A persisted non-default theme has to be loaded from its embedded XAML during startup,
    /// before any page is drawn. If that load fails the app breaks on launch, so relaunching
    /// into a saved theme and re-rendering every tab is the assertion that matters.
    /// (The stored value itself is not readable: see SettingsPage.)
    /// </summary>
    [Test]
    public void AppRelaunchesCleanlyIntoAPersistedTheme()
    {
        SkipOnAndroid("tapping an SfComboBox does not open its drop-down through "
            + "UiAutomator2 — the list never enters the element tree, so no option can be "
            + "selected. Same family as the SyncfusionIosTouchFix overlay problem.");

        try
        {
            Settings.SelectTheme("Forest");

            App.RestartApp();
            Launch.WaitUntilReady();

            foreach (var tab in new[] { AppTab.Loan, AppTab.Budget, AppTab.WhatIf, AppTab.Settings })
            {
                Tabs.GoTo(tab);
                Assert.That(Tabs.IsOn(tab), Is.True,
                    $"After relaunching into the Forest theme, the {tab} tab failed to render.");
            }
        }
        finally
        {
            Tabs.GoTo(AppTab.Settings);
            Settings.SelectTheme("Dark");
        }
    }

    /// <summary>
    /// The font picker loads 13 TTFs and applies the choice through a DefaultFontFamily
    /// DynamicResource — structurally the same risk as the theme system, so it gets the same
    /// treatment: switch, then re-render every tab.
    /// </summary>
    [Test]
    public void EveryTabRendersAfterChangingTheFont()
    {
        SkipOnAndroid("tapping an SfComboBox does not open its drop-down through UiAutomator2.");

        try
        {
            Settings.SelectFont("Merriweather");

            foreach (var tab in new[] { AppTab.Loan, AppTab.Budget, AppTab.WhatIf, AppTab.Settings })
            {
                Tabs.GoTo(tab);
                Assert.That(Tabs.IsOn(tab), Is.True,
                    $"The {tab} tab failed to render after switching to the Merriweather font.");
            }
        }
        finally
        {
            Tabs.GoTo(AppTab.Settings);
            Settings.SelectFont("Lato");
        }
    }

    /// <summary>
    /// The chosen currency's symbol is pushed through Helper.CurrencySymbol into every formatted
    /// amount in the app, so a break here is visible on every screen at once.
    /// </summary>
    [Test]
    public void ChangingTheCurrencyChangesTheSymbolOnDisplayedAmounts()
    {
        SkipOnAndroid("tapping an SfComboBox does not open its drop-down through UiAutomator2.");

        try
        {
            Settings.SelectCurrency("Euro");

            Tabs.GoTo(AppTab.Loan);
            Loan.OpenLoanDetails();
            App.EnterText(LoanPage.AssetEntry, "650000");
            App.EnterText(LoanPage.DepositEntry, "130000");

            var euro = App.WaitForText(
                LoanPage.LoanFormatted,
                shown => shown.Contains('€'),
                "showed the euro symbol after the currency was changed to Euro");

            // Close the popup before leaving: it is modal and would cover the tab bar, so the
            // finally block below could not reach Settings to restore the currency.
            Loan.ApplyLoanDetails();

            Assert.That(euro, Does.Contain("€"));
        }
        finally
        {
            Tabs.GoTo(AppTab.Settings);
            Settings.SelectCurrency("US Dollar");
        }
    }

    [Test]
    public void SettingsPageRendersItsAppearancePickers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(App.Exists(SettingsPage.ThemeCombo), Is.True, "Theme picker missing.");
            Assert.That(App.Exists(SettingsPage.FontCombo), Is.True, "Font picker missing.");
            Assert.That(App.Exists(SettingsPage.CurrencyCombo), Is.True, "Currency picker missing.");
        });
    }
}
