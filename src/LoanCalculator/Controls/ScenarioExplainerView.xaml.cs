using LoanCalculator.Core.Constants;

namespace LoanCalculatorMaui.Controls;

/// <summary>
/// The collapsed "Understanding this scenario" card appended below every What If scenario.
/// Content is supplied as a <see cref="ScenarioExplanation"/>; the personalised closing line comes
/// from the inherited <c>WhatIfViewModel.ScenarioExplainLine</c> binding.
/// </summary>
public partial class ScenarioExplainerView : ContentView
{
    public static readonly BindableProperty ExplanationProperty =
        BindableProperty.Create(
            nameof(Explanation),
            typeof(ScenarioExplanation),
            typeof(ScenarioExplainerView),
            propertyChanged: OnExplanationChanged);

    public ScenarioExplainerView() => InitializeComponent();

    public ScenarioExplanation? Explanation
    {
        get => (ScenarioExplanation?)GetValue(ExplanationProperty);
        set => SetValue(ExplanationProperty, value);
    }

    /// <summary>
    /// Collapses the example block when the scenario has none, rather than leaving a "FOR EXAMPLE"
    /// heading above nothing. Only four of the nine carry one.
    /// </summary>
    private static void OnExplanationChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not ScenarioExplainerView view) return;

        var example = (newValue as ScenarioExplanation)?.Example;
        view.ExampleSection.IsVisible = !string.IsNullOrWhiteSpace(example);
    }
}
