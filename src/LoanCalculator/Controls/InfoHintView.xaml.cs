namespace LoanCalculatorMaui.Controls;

/// <summary>
/// An inline "ⓘ &lt;prompt&gt;" strip that expands to an explanation of whatever it sits beneath.
/// </summary>
/// <remarks>
/// Place one directly under each control it describes. See the comment in the markup for why the
/// explanations are distributed like this rather than collected into one card per screen, and why
/// this is a tappable Label rather than an SfExpander.
/// </remarks>
public partial class InfoHintView : ContentView
{
    /// <summary>The tappable line, e.g. "What these two totals mean".</summary>
    public static readonly BindableProperty PromptProperty =
        BindableProperty.Create(nameof(Prompt), typeof(string), typeof(InfoHintView), "What this means");

    /// <summary>The explanation revealed by tapping. Usually bound with x:Static from Core.</summary>
    public static readonly BindableProperty DetailProperty =
        BindableProperty.Create(nameof(Detail), typeof(string), typeof(InfoHintView), string.Empty);

    /// <summary>
    /// Optional: the same explanation stated in the reader's own figures.
    /// </summary>
    /// <remarks>
    /// Kept out of the static copy in Core because it cannot live there — it needs live numbers,
    /// and that copy is what a unit test can pin. The host binds a view-model property instead,
    /// which also keeps the sentence current as the figures move.
    /// </remarks>
    public static readonly BindableProperty LiveExampleProperty =
        BindableProperty.Create(nameof(LiveExample), typeof(string), typeof(InfoHintView), string.Empty);

    public static readonly BindableProperty InfoAutomationIdProperty =
        BindableProperty.Create(nameof(InfoAutomationId), typeof(string), typeof(InfoHintView), string.Empty);

    public static readonly BindableProperty DetailAutomationIdProperty =
        BindableProperty.Create(nameof(DetailAutomationId), typeof(string), typeof(InfoHintView), string.Empty);

    public InfoHintView()
    {
        InitializeComponent();
    }

    public string Prompt
    {
        get => (string)GetValue(PromptProperty);
        set => SetValue(PromptProperty, value);
    }

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public string LiveExample
    {
        get => (string)GetValue(LiveExampleProperty);
        set => SetValue(LiveExampleProperty, value);
    }

    public string InfoAutomationId
    {
        get => (string)GetValue(InfoAutomationIdProperty);
        set => SetValue(InfoAutomationIdProperty, value);
    }

    public string DetailAutomationId
    {
        get => (string)GetValue(DetailAutomationIdProperty);
        set => SetValue(DetailAutomationIdProperty, value);
    }

    private void OnHintTapped(object? sender, TappedEventArgs e)
    {
        var show = !DetailLabel.IsVisible;

        DetailLabel.IsVisible = show;

        // Only when there is something to say. An empty Label still takes its margins, which would
        // leave a gap under the explanation on every hint that has no live example.
        LiveExampleLabel.IsVisible = show && !string.IsNullOrWhiteSpace(LiveExample);
    }
}
