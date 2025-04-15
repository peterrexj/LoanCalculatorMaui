using LoanCalculator.Core.Models.ViewModels;

namespace LoanCalculatorMaui.Controls;

public partial class InsightsItemView : ContentView
{
	public InsightsItemView()
	{
		InitializeComponent();

        InnerInsightsValueLabel.SetBinding(Label.TextColorProperty, new Binding("TextColor", source: this));
        InnerInsightsNameLabel.SetBinding(Label.TextColorProperty, new Binding("TextColor", source: this));
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
        control.InnerInsightsDescriptionLabel.Text = value.Description;
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
}