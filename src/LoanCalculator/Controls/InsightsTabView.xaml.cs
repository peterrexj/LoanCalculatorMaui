namespace LoanCalculatorMaui.Controls;

/// <summary>
/// The What If page's Insights tab, extracted so it is only built when that tab is first opened.
/// </summary>
/// <remarks>
/// There is deliberately no logic here. Everything this tab shows comes from
/// <c>LoanViewModel.InsightsDetails</c> and the nine chart collections, all of it through
/// bindings — the markup had no code-behind references at all while it lived in WhatIfView.xaml
/// (its three <c>x:Name</c>s were never touched from C#), which is what made it the cheapest of
/// the four tabs to defer.
///
/// The caller supplies the <c>BindingContext</c> (a <c>LoanViewModel</c>) and, on iOS, re-applies
/// <c>SyncfusionIosTouchFix.ApplyToExpanders</c> — see <c>WhatIfView.RefreshInsights</c>.
/// </remarks>
public partial class InsightsTabView : ContentView
{
    public InsightsTabView()
    {
        InitializeComponent();
    }
}
