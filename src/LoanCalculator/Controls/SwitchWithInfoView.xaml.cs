using LoanCalculator.Core.Constants;

namespace LoanCalculatorMaui.Controls;

/// <summary>
/// A Budget switch row with a tappable ⓘ revealing what the switch does.
/// </summary>
/// <remarks>
/// Set <see cref="Toggle"/> and the title, sub-line and detail all come from
/// <see cref="BudgetToggleExplanations"/> in Core, so the copy is unit-tested and the four rows
/// cannot drift apart in wording the way the hand-written ones had.
/// </remarks>
public partial class SwitchWithInfoView : ContentView
{
    /// <summary>
    /// TwoWay by default: this wraps a switch, and a one-way binding would show the view model's
    /// value while silently dropping the user's taps.
    /// </summary>
    public static readonly BindableProperty IsOnProperty =
        BindableProperty.Create(
            nameof(IsOn), typeof(bool), typeof(SwitchWithInfoView),
            defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Which switch this is. Supplies all three pieces of copy.</summary>
    public static readonly BindableProperty ToggleProperty =
        BindableProperty.Create(
            nameof(Toggle), typeof(BudgetToggle), typeof(SwitchWithInfoView),
            defaultValue: BudgetToggle.IncomeAfterExpenses,
            propertyChanged: OnToggleChanged);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SwitchWithInfoView), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(SwitchWithInfoView), string.Empty);

    public static readonly BindableProperty DetailProperty =
        BindableProperty.Create(nameof(Detail), typeof(string), typeof(SwitchWithInfoView), string.Empty);

    /// <summary>
    /// The same explanation restated in the reader's own figures, supplied by the host.
    /// </summary>
    /// <remarks>
    /// Not part of <see cref="BudgetToggleExplanations"/>, because it cannot be: it needs live
    /// numbers, and that table is static copy a unit test can pin. The host binds a view-model
    /// property instead, which also keeps the sentence current as the figures move.
    /// </remarks>
    public static readonly BindableProperty LiveExampleProperty =
        BindableProperty.Create(nameof(LiveExample), typeof(string), typeof(SwitchWithInfoView), string.Empty);

    // Supplied per row so the UI suite can address a specific switch, its ⓘ and its detail. Not
    // derived from Toggle, because the existing suite already depends on the switch ids in use.
    public static readonly BindableProperty SwitchAutomationIdProperty =
        BindableProperty.Create(nameof(SwitchAutomationId), typeof(string), typeof(SwitchWithInfoView), string.Empty);

    public static readonly BindableProperty InfoAutomationIdProperty =
        BindableProperty.Create(nameof(InfoAutomationId), typeof(string), typeof(SwitchWithInfoView), string.Empty);

    public static readonly BindableProperty DetailAutomationIdProperty =
        BindableProperty.Create(nameof(DetailAutomationId), typeof(string), typeof(SwitchWithInfoView), string.Empty);

    public SwitchWithInfoView()
    {
        InitializeComponent();

        // Apply the copy for whatever Toggle currently is, which the propertyChanged callback
        // below will NOT do. A BindableProperty only raises propertyChanged when the value
        // actually changes, so a row setting Toggle to the same value as ToggleProperty's default
        // — IncomeAfterExpenses, i.e. the first row on the Income tab — never triggered it and
        // rendered with no label, no sub-line and no ⓘ detail at all, while every other row
        // worked. Resolving it here as well is what makes the default case behave like the rest.
        ApplyExplanation(Toggle);
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public BudgetToggle Toggle
    {
        get => (BudgetToggle)GetValue(ToggleProperty);
        set => SetValue(ToggleProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
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

    public string SwitchAutomationId
    {
        get => (string)GetValue(SwitchAutomationIdProperty);
        set => SetValue(SwitchAutomationIdProperty, value);
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

    private static void OnToggleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SwitchWithInfoView view || newValue is not BudgetToggle toggle) return;

        view.ApplyExplanation(toggle);
    }

    private void ApplyExplanation(BudgetToggle toggle)
    {
        var explanation = BudgetToggleExplanations.For(toggle);

        Title = explanation.Title;
        Subtitle = explanation.Subtitle;
        Detail = explanation.Detail;
    }

    private void OnInfoTapped(object? sender, TappedEventArgs e)
    {
        var show = !DetailLabel.IsVisible;

        DetailLabel.IsVisible = show;

        // Only when there is something to say. An empty Label still occupies its margins, which
        // would leave a gap under the detail on any row without a live example.
        LiveExampleLabel.IsVisible = show && !string.IsNullOrWhiteSpace(LiveExample);
    }
}
