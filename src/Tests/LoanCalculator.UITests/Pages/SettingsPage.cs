using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

public sealed class SettingsPage(AppDriver app) : PageBase(app)
{
    public const string PageTitle = "SettingsPageTitle";
    public const string ThemeCombo = "SettingsThemeCombo";
    public const string FontCombo = "SettingsFontCombo";
    public const string CurrencyCombo = "SettingsCurrencyCombo";

    public static readonly string[] Themes = ["Dark", "Light", "Forest", "Warm"];

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
    }

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
