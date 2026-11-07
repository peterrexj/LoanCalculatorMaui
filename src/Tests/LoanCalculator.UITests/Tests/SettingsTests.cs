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

    /// <summary>
    /// The text-size choice rewrites every scalable font-size resource in the app
    /// (<c>FontScaleHandler</c>, 21 keys) and every <c>DynamicResource</c> consumer re-measures
    /// in response — so it is the same structural risk as the theme and font systems and gets the
    /// same treatment: switch, then re-render every tab.
    /// </summary>
    /// <remarks>
    /// <b>Not skipped on Android</b>, and that is the point of it. The chips are plain tappable
    /// <c>Border</c>s rather than an <c>SfComboBox</c>, so unlike every other test in this
    /// fixture this one genuinely runs there — which is the only Android coverage the appearance
    /// settings have at all.
    /// </remarks>
    [Test]
    public void EveryTextSizeRendersEveryTab()
    {
        try
        {
            foreach (var size in SettingsPage.TextSizes)
            {
                Tabs.GoTo(AppTab.Settings);
                Settings.SelectTextSize(size);

                foreach (var tab in new[] { AppTab.Loan, AppTab.Budget, AppTab.WhatIf, AppTab.Settings })
                {
                    Tabs.GoTo(tab);
                    Assert.That(Tabs.IsOn(tab), Is.True,
                        $"The {tab} tab failed to render at the '{size}' text size.");
                }
            }
        }
        finally
        {
            // The choice persists, so leaving it anywhere but Default changes the starting state
            // of every later test and every later run.
            Tabs.GoTo(AppTab.Settings);
            Settings.SelectTextSize(SettingsPage.TextSizeDefault);
        }
    }

    /// <summary>
    /// At the largest text size the modal forms must still be completable — which means their
    /// header buttons must still be reachable and still say what they do.
    /// </summary>
    /// <remarks>
    /// This is the regression that shipped: the Loan Details and Transaction Entry header buttons
    /// were a fixed 36x72, and at Extra Large on Android the native button's Material insets left
    /// so little room inside that box that "Done" rendered as "Don" with the glyphs cut off. iOS
    /// has no such inset and still fit, so it presented as an Android rendering defect rather than
    /// as markup that had stopped being big enough. A clipped or shrunken button is also an
    /// unreliable tap target, so the failure here is the modal never closing — not a cosmetic
    /// diff, which is why this is assertable at all.
    /// </remarks>
    [Test]
    public void ExtraLargeTextKeepsTheModalFormsUsable()
    {
        try
        {
            Settings.SelectTextSize(SettingsPage.TextSizeExtraLarge);

            Tabs.GoTo(AppTab.Loan);
            Loan.OpenLoanDetails();

            App.EnterText(LoanPage.AssetEntry, "680000");
            App.EnterText(LoanPage.DepositEntry, "136000");

            // ApplyLoanDetails taps the header button and waits for the page to go. If the button
            // is clipped or mis-measured the tap misses and this throws, leaving the modal up.
            Loan.ApplyLoanDetails();

            Assert.That(App.Exists(LoanPage.AssetEntry, TimeSpan.FromSeconds(2)), Is.False,
                "The Loan Details page did not close at the Extra Large text size — its header "
                + "Done button is most likely clipped or mis-measured (see the fixed-size button "
                + "note in LoanDetailsPage.xaml).");
        }
        finally
        {
            // Dismiss anything still up first, or Settings is unreachable and the restore — the
            // thing that keeps every later test honest — silently fails too.
            ModalPages.DismissAny(App);
            Tabs.GoTo(AppTab.Settings);
            Settings.SelectTextSize(SettingsPage.TextSizeDefault);
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

        // The chips sit below the three pickers in the same Appearance card, so they can start
        // off-screen — and all four share one HorizontalStackLayout, so bringing the first into
        // view brings the rest. Scrolled outside the Assert.Multiple above because ScrollTo
        // throws rather than recording a failure, which would abandon the remaining chips.
        App.ScrollTo(SettingsPage.TextSizeSmall);

        Assert.Multiple(() =>
        {
            foreach (var size in SettingsPage.TextSizes)
                Assert.That(App.Exists(size), Is.True, $"Text size chip '{size}' missing.");
        });
    }
}
