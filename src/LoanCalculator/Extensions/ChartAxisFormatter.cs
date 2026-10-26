using Syncfusion.Maui.Charts;

namespace LoanCalculatorMaui.Extensions;

/// <summary>
/// Axis-label formatting for the Syncfusion charts.
/// </summary>
/// <remarks>
/// There were three byte-identical copies of the money formatter — in
/// <c>LoanView.OnAmortizationAxisLabelCreated</c>, <c>BudgetView.OnProjectionAxisLabelCreated</c>
/// and <c>WhatIfView.OnWhatIfAxisLabelCreated</c>. Splitting the What If scenarios into their own
/// views would have made that seven more, so it lives here instead. Each call site keeps its own
/// one-line handler, because Syncfusion wires these by event name from XAML.
/// </remarks>
internal static class ChartAxisFormatter
{
    /// <summary>
    /// Abbreviates a currency axis: 1,500,000 → "$1.5M", 30,000 → "$30K", 950 → "$950".
    /// Leaves anything non-numeric untouched, which is what keeps category axes safe.
    /// </summary>
    internal static void Money(ChartAxisLabelEventArgs e, string? currencySymbol)
    {
        if (!double.TryParse(e.Label, out var value)) return;

        var symbol = currencySymbol ?? "$";
        var magnitude = Math.Abs(value);

        e.Label = magnitude >= 1_000_000 ? $"{symbol}{value / 1_000_000:0.#}M"
                : magnitude >= 1_000     ? $"{symbol}{value / 1_000:0.#}K"
                                         : $"{symbol}{value:0}";
    }

    /// <summary>Formats a percentage axis: 5.25 → "5.25%".</summary>
    internal static void Rate(ChartAxisLabelEventArgs e)
    {
        if (double.TryParse(e.Label, out var value))
            e.Label = $"{value:0.##}%";
    }
}
