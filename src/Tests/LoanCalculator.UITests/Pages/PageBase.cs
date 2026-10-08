using LoanCalculator.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace LoanCalculator.UITests.Pages;

public abstract class PageBase(AppDriver app)
{
    protected AppDriver App { get; } = app;

    /// <summary>
    /// Picks a value from an SfComboBox: tap to open the drop-down, then tap the row. Rows come
    /// from an ItemTemplate and carry no AutomationId, so they are matched by visible text — and by
    /// <em>fragment</em>, since labels carry decoration ("US Dollar (USD) $") a caller should not
    /// have to spell out.
    /// <para>
    /// Confirming the selection needs a different signal per platform, because the collapsed
    /// combo behaves differently: <b>Android</b> renders its value as real text, so it can be
    /// read back; <b>iOS</b> draws it, exposing no text node at all (the control's own label
    /// reports state — "Drop down button pressed" — and its subtree holds only the down-button),
    /// so there the option text vanishing is what proves the list closed. Counting occurrences
    /// works on neither: the open list shows every option.
    /// </para>
    /// </summary>
    protected void SelectFromCombo(string comboAutomationId, string optionText)
    {
        var readable = App.Platform == TestPlatform.Android;

        // Selecting the value that is already selected is a no-op with no observable change —
        // and on Android the collapsed combo's own text would be matched as if it were a list
        // row, so tapping it just reopens the list.
        if (readable && ComboText(comboAutomationId).Contains(optionText, StringComparison.OrdinalIgnoreCase))
            return;

        OpenDropDown(comboAutomationId);
        App.TapTextContaining(optionText, TimeSpan.FromSeconds(10));

        if (readable)
        {
            App.WaitUntil(
                () => ComboText(comboAutomationId).Contains(optionText, StringComparison.OrdinalIgnoreCase),
                $"'{comboAutomationId}' to show the selected value '{optionText}'",
                TimeSpan.FromSeconds(10));
        }
        else
        {
            App.WaitUntil(
                () => App.VisibleTextCount(optionText) == 0,
                $"the '{comboAutomationId}' drop-down to close",
                TimeSpan.FromSeconds(10));
        }
    }

    /// <summary>
    /// Opens the drop-down. MAUI gives the combo's arrow its own child, "&lt;id&gt;_DownButtonView";
    /// on Android tapping the container does not always open the list, whereas that button
    /// reliably does. Falls back to the container when the button is not present.
    /// </summary>
    private void OpenDropDown(string comboAutomationId)
    {
        var downButton = $"{comboAutomationId}_DownButtonView";

        if (App.Exists(downButton, TimeSpan.FromSeconds(2)))
        {
            App.Tap(downButton);
            return;
        }

        App.Tap(comboAutomationId);
    }

    /// <summary>
    /// The combo's displayed value on Android, where it is a real text node. Returns empty on
    /// iOS, which draws it instead — do not rely on this there.
    /// </summary>
    private string ComboText(string comboAutomationId)
    {
        var element = App.Find(comboAutomationId);

        if (!string.IsNullOrWhiteSpace(element.Text)) return element.Text;

        // The value sits in an EditText, not a TextView — SfComboBox is editable underneath.
        try
        {
            foreach (var child in element.FindElements(
                         MobileBy.XPath(".//android.widget.EditText | .//android.widget.TextView")))
                if (!string.IsNullOrWhiteSpace(child.Text)) return child.Text;
        }
        catch (WebDriverException)
        {
            // Subtree changed mid-query; treat as "nothing readable yet".
        }

        return string.Empty;
    }

    /// <summary>
    /// Taps an in-page Syncfusion tab and waits for something that only exists inside that
    /// tab's content.
    /// </summary>
    protected void OpenTab(string tabAutomationId, string contentAnchorAutomationId)
    {
        // A tab header stays visible whichever tab is selected, so using it as its own
        // anchor would make this return without ever tapping — a test that silently passes.
        if (tabAutomationId == contentAnchorAutomationId)
        {
            throw new ArgumentException(
                $"The content anchor for '{tabAutomationId}' must be an element inside the " +
                "tab's content, not the tab header itself.",
                nameof(contentAnchorAutomationId));
        }

        if (App.Exists(contentAnchorAutomationId, TimeSpan.FromMilliseconds(600))) return;

        App.Tap(tabAutomationId);
        App.WaitUntilVisible(contentAnchorAutomationId, TimeSpan.FromSeconds(20));
    }
}
