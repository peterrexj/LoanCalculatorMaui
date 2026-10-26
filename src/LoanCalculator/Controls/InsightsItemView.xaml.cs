using LoanCalculator.Core.Models.ViewModels;

namespace LoanCalculatorMaui.Controls;

public partial class InsightsItemView : ContentView
{
	public InsightsItemView()
	{
		InitializeComponent();

        InnerInsightsValueLabel.SetBinding(Label.TextColorProperty, new Binding("TextColor", source: this));
        InnerInsightsNameLabel.SetBinding(Span.TextColorProperty, new Binding("TextColor", source: this));
        InnerInsightsInfoToggle.SetBinding(Span.TextColorProperty, new Binding("TextColor", source: this));
        InnerInsightsShortLabel.SetBinding(Label.TextColorProperty, new Binding("TextColor", source: this));
        InnerInsightsDescriptionLabel.SetBinding(Label.TextColorProperty, new Binding("TextColor", source: this));
    }

    public static readonly BindableProperty InsightItemProperty =
        BindableProperty.Create(
            propertyName: nameof(InsightItem), returnType: typeof(InsightsViewModel),
            declaringType: typeof(InsightsItemView), defaultValue: default(InsightsViewModel),
            propertyChanged: OnInsightItemPropertyChanged);

    private static void OnInsightItemPropertyChanged(BindableObject bindable, object oldValue, object? newValue)
    {
        var control = (InsightsItemView)bindable;
        if (newValue == null) return;
        if (newValue is not InsightsViewModel value) return;

        control.InnerInsightsValueLabel.Text = value.Value;
        control.InnerInsightsNameLabel.Text = value.Name;

        // Short summary shown by default; fall back to the full description if no short text.
        var shortText = string.IsNullOrWhiteSpace(value.ShortDescription)
            ? value.Description
            : value.ShortDescription;
        control.InnerInsightsShortLabel.Text = shortText;
        control.InnerInsightsDescriptionLabel.Text = value.Description;

        // Only offer the ⓘ expand when there is extra detail beyond the short summary.
        var hasMoreDetail = !string.IsNullOrWhiteSpace(value.Description)
            && !string.Equals(value.Description, shortText, System.StringComparison.Ordinal);
        control.InnerInsightsInfoToggle.Text = hasMoreDetail ? "ⓘ" : string.Empty;

        // Reset to collapsed when the item is (re)assigned.
        control.InnerInsightsDescriptionLabel.IsVisible = false;
    }

    private void OnInfoToggleTapped(object? sender, System.EventArgs e)
    {
        // Toggle only when there is a full description to reveal.
        if (string.IsNullOrWhiteSpace(InnerInsightsDescriptionLabel.Text)) return;
        InnerInsightsDescriptionLabel.IsVisible = !InnerInsightsDescriptionLabel.IsVisible;
    }

    public InsightsViewModel InsightItem
    {
        get => (InsightsViewModel)GetValue(InsightItemProperty);
        set => SetValue(InsightItemProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(InsightsItemView), Colors.Black);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    /// <summary>
    /// Whether to draw the hairline above this row. Set false on the FIRST row of each panel —
    /// the separator divides rows from each other, and one at the top of the card would be
    /// dividing the first row from the panel header, which already has its own edge.
    /// </summary>
    /// <remarks>
    /// Defaults to true so adding a row anywhere but the top needs no thought. The control cannot
    /// work this out for itself: it has no reliable view of its position among its siblings at the
    /// point the separator must be decided.
    /// </remarks>
    public static readonly BindableProperty ShowSeparatorProperty =
        BindableProperty.Create(
            nameof(ShowSeparator),
            typeof(bool),
            typeof(InsightsItemView),
            true,
            propertyChanged: OnShowSeparatorChanged);

    public bool ShowSeparator
    {
        get => (bool)GetValue(ShowSeparatorProperty);
        set => SetValue(ShowSeparatorProperty, value);
    }

    private static void OnShowSeparatorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is InsightsItemView view && newValue is bool show)
            view.RowSeparator.IsVisible = show;
    }
}
