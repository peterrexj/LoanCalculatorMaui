using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

public sealed class SettingsPage(AppDriver app) : PageBase(app)
{
    public const string PageTitle = "SettingsPageTitle";
    public const string ThemeCombo = "SettingsThemeCombo";
    public const string FontCombo = "SettingsFontCombo";
    public const string CurrencyCombo = "SettingsCurrencyCombo";

    public static readonly string[] Themes = ["Dark", "Light", "Forest", "Warm"];

    /// <summary>
    /// The opaque, full-screen overlay this page raises while a font or text-size change is
    /// applied. Waiting for it to clear is not optional: it covers <see cref="PageTitle"/>, which
    /// is what <c>ShellTabs</c> uses to decide it has arrived on Settings, and it is not
    /// input-transparent — so a change left mid-apply fails the *next* step as "could not find
    /// the tab", pointing nowhere near the cause.
    /// </summary>
    private const string BusyOverlay = "SettingsBusyOverlay";

    // There is deliberately no SelectedTheme()/SelectedCurrency() reader: Syncfusion draws a
    // collapsed SfComboBox's value instead of exposing it as an accessible text node, so it
    // cannot be read through Appium on either platform. Assert on what a selection changes.

    public void SelectTheme(string theme)
    {
        App.ScrollTo(ThemeCombo);
        SelectFromCombo(ThemeCombo, theme);
    }

    public void SelectCurrency(string currencyDisplayText)
    {
        App.ScrollTo(CurrencyCombo);
        SelectFromCombo(CurrencyCombo, currencyDisplayText);
    }

    public void SelectFont(string fontName)
    {
        App.ScrollTo(FontCombo);
        SelectFromCombo(FontCombo, fontName);
        SettleAppearanceChange();
    }

    // ── Text size ────────────────────────────────────────────────────────────────

    public const string TextSizeSmall = "SettingsTextSizeSmall";
    public const string TextSizeDefault = "SettingsTextSizeDefault";
    public const string TextSizeLarge = "SettingsTextSizeLarge";
    public const string TextSizeExtraLarge = "SettingsTextSizeExtraLarge";

    /// <summary>
    /// The four chips, in the order Settings renders them. <see cref="TextSizeDefault"/> is the
    /// one to restore to — the choice persists in Preferences, so a test that leaves another
    /// selected changes the starting state of every later test and every later run, exactly as
    /// the theme and font pickers do.
    /// </summary>
    public static readonly string[] TextSizes =
        [TextSizeSmall, TextSizeDefault, TextSizeLarge, TextSizeExtraLarge];

    /// <summary>
    /// Picks a text size. Unlike the three pickers above this is four tappable
    /// <c>Border</c> chips rather than an <c>SfComboBox</c>, so it works on <b>Android</b> too.
    /// </summary>
    /// <remarks>
    /// There is no reader here for the same reason the combos have none, by a different
    /// mechanism: the selected chip is marked by a stroke-thickness and background
    /// <c>DataTrigger</c>, i.e. drawn, with no text node saying which one is on. Assert on what
    /// the choice changes — every page re-rendering — not on the chip.
    /// </remarks>
    public void SelectTextSize(string chipAutomationId)
    {
        App.ScrollTo(chipAutomationId);
        App.Tap(chipAutomationId);
        SettleAppearanceChange();
    }

    /// <summary>
    /// Waits out the apply overlay. Tolerates it never appearing: selecting the value that is
    /// already selected is a no-op that never raises it, and a fast apply can be over before the
    /// first poll.
    /// </summary>
    private void SettleAppearanceChange() =>
        App.WaitUntilGone(BusyOverlay, TimeSpan.FromSeconds(30));

    // ── Delete your data ─────────────────────────────────────────────────────────

    public const string DeleteDataHeader = "SettingsDeleteDataHeader";
    public const string DeleteLoanButton = "SettingsDeleteLoanButton";
    public const string DeleteExpensesButton = "SettingsDeleteExpensesButton";
    public const string DeleteIncomeButton = "SettingsDeleteIncomeButton";
    public const string DeleteAllDataButton = "SettingsDeleteAllDataButton";

    /// <summary>
    /// The delete buttons live in a collapsed SfExpander, so it has to be opened first. Tapping
    /// the header again would collapse it, so this is a no-op once the buttons are showing.
    /// </summary>
    public void OpenDeleteDataSection()
    {
        if (App.Exists(DeleteLoanButton, TimeSpan.FromSeconds(1))) return;

        App.ScrollTo(DeleteDataHeader, maxSwipes: 30);
        App.Tap(DeleteDataHeader);
        App.WaitUntilVisible(DeleteLoanButton, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Every delete button raises a native "Do you wish to delete the data?" confirmation. It is
    /// modal, so leaving it up blocks every later lookup — which presents as unrelated
    /// "could not find the tab" failures further along the test.
    /// </summary>
    private void ConfirmDelete()
    {
        App.TapText("Yes", TimeSpan.FromSeconds(10));
        App.WaitUntilTextGoneIfPresent("Yes", TimeSpan.FromSeconds(10));

        // Deleting data here invalidates whatever TestData thinks is seeded. Doing it at the point of
        // deletion rather than in each caller means a test cannot forget — forgetting it made two
        // stress-test cases fail in opposite directions, which took a while to understand.
        TestData.MarkWiped();
    }

    public void DeleteAllData()
    {
        OpenDeleteDataSection();
        App.ScrollTo(DeleteAllDataButton);
        App.Tap(DeleteAllDataButton);
        ConfirmDelete();
    }

    public void DeleteLoanData()
    {
        OpenDeleteDataSection();
        App.ScrollTo(DeleteLoanButton);
        App.Tap(DeleteLoanButton);
        ConfirmDelete();
    }

    public void DeleteIncomeData()
    {
        OpenDeleteDataSection();
        App.ScrollTo(DeleteIncomeButton);
        App.Tap(DeleteIncomeButton);
        ConfirmDelete();
    }

    public void DeleteExpenseData()
    {
        OpenDeleteDataSection();
        App.ScrollTo(DeleteExpensesButton);
        App.Tap(DeleteExpensesButton);
        ConfirmDelete();
    }

    /// <summary>True when the All Data button is offered — it is gated by IsAllDataDeleteVisible.</summary>
    public bool CanDeleteAllData()
    {
        OpenDeleteDataSection();
        return App.Exists(DeleteAllDataButton, TimeSpan.FromSeconds(2));
    }
}
