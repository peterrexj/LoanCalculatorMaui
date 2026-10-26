namespace LoanCalculator.Core.Models.Enums
{
    /// <summary>Which of the nine What If scenarios is being shown.</summary>
    /// <remarks>
    /// Lives in Core rather than beside <c>ScenarioPage</c>, which declared it, so that
    /// <see cref="Constants.ScenarioExplanations"/> can be keyed off it and unit-tested — the test
    /// project references Core only.
    ///
    /// <b>These member names are load-bearing strings.</b> The scenario index rows in
    /// <c>WhatIfView.xaml</c> carry them as <c>CommandParameter</c> literals which
    /// <c>OnScenarioTapped</c> resolves with <c>Enum.TryParse</c>, so renaming a member silently
    /// stops that row opening. <c>ScenarioExplanationsTests</c> pins the round-trip.
    /// </remarks>
    public enum WhatIfScenario
    {
        RateChange,
        ExtraRepayment,
        TermComparison,
        DepositScenarios,
        LumpSum,
        RepaymentFrequency,
        Offset,
        StressTest,
        Combined,
    }
}
