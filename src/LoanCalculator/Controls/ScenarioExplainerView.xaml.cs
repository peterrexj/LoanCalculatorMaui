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

    /// <summary>Header on the collapsed strip. Defaults to the What If scenarios' wording.</summary>
    public static readonly BindableProperty HeaderTextProperty =
        BindableProperty.Create(
            nameof(HeaderText), typeof(string), typeof(ScenarioExplainerView),
            "Understanding this scenario");

    /// <summary>
    /// The closing line, stating what the content means for this user's own figures.
    /// </summary>
    /// <remarks>
    /// Supplied by the host rather than bound to a fixed path inside the control. It used to bind
    /// <c>WhatIfViewModel.ScenarioExplainLine</c> directly, which tied the card to pages using that
    /// view model and is why it could not be reused on the Answer tab.
    /// </remarks>
    public static readonly BindableProperty PersonalLineProperty =
        BindableProperty.Create(
            nameof(PersonalLine), typeof(string), typeof(ScenarioExplainerView), string.Empty,
            propertyChanged: OnPersonalLineChanged);

    public ScenarioExplainerView()
    {
        InitializeComponent();

        // A BindableProperty only raises propertyChanged when the value changes, and PersonalLine
        // defaults to empty — so a card that never sets it would keep the footer visible over
        // nothing. Same default-value trap the Budget switch rows hit.
        OnPersonalLineChanged(this, string.Empty, PersonalLine);
    }

    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public string PersonalLine
    {
        get => (string)GetValue(PersonalLineProperty);
        set => SetValue(PersonalLineProperty, value);
    }

    public ScenarioExplanation? Explanation
    {
        get => (ScenarioExplanation?)GetValue(ExplanationProperty);
        set => SetValue(ExplanationProperty, value);
    }

    /// <summary>
    /// Collapses the example block when the scenario has none, rather than leaving a "FOR EXAMPLE"
    /// heading above nothing. Only four of the nine carry one.
    /// </summary>
    /// <summary>
    /// Hides the whole "in your case" footer when there is no line for it.
    /// </summary>
    /// <remarks>
    /// Three elements, not one: hiding only the Label would leave its heading and the hairline
    /// above it, which is what the Answer card showed — a rule and a heading over nothing.
    /// </remarks>
    private static void OnPersonalLineChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not ScenarioExplainerView view) return;

        var hasLine = !string.IsNullOrWhiteSpace(newValue as string);

        view.PersonalLineRule.IsVisible = hasLine;
        view.PersonalLineHeading.IsVisible = hasLine;
        view.PersonalLineLabel.IsVisible = hasLine;
    }

    private static void OnExplanationChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not ScenarioExplainerView view) return;

        var example = (newValue as ScenarioExplanation)?.Example;
        view.ExampleSection.IsVisible = !string.IsNullOrWhiteSpace(example);
    }
}
