using System.Globalization;

namespace LoanCalculatorMaui.Converters
{
    /// <summary>
    /// Maps an "is negative" bool to a theme colour: the negative colour when true, otherwise the
    /// colour named by <c>ConverterParameter</c> (or the default foreground if none is given).
    /// </summary>
    /// <remarks>
    /// Exists because <c>Span</c> is not a <c>VisualElement</c>, so it cannot carry a
    /// <c>DataTrigger</c> — and the three-box summary rows are built from Spans. Elsewhere a
    /// DataTrigger is the better tool and is used instead; this is for the Span case only.
    ///
    /// Resolves against <c>Application.Current.Resources</c> rather than taking Colors directly,
    /// so the result follows a theme change like every other <c>DynamicResource</c> would. A key
    /// that cannot be resolved falls through to the theme foreground rather than throwing: a
    /// mistyped parameter should mean "looks unstyled", not "the page fails to render".
    ///
    /// Pass <c>LoanAppNegativeValueOnHighlightColor</c> contexts their own key via the parameter;
    /// see the two keys' comment in the theme files for why there are two.
    /// </remarks>
    public class NegativeToThemeColorConverter : IValueConverter
    {
        /// <summary>Theme key used when the value is negative. Overridable for the highlight-card variant.</summary>
        public string NegativeKey { get; set; } = "LoanAppNegativeValueColor";

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var key = value is true
                ? NegativeKey
                : parameter as string ?? "LoanAppDefaultForegroundColor";

            return Lookup(key) ?? Lookup("LoanAppDefaultForegroundColor") ?? Colors.Grey;
        }

        private static Color? Lookup(string key)
        {
            var resources = Application.Current?.Resources;
            if (resources is null) return null;

            return resources.TryGetValue(key, out var found) && found is Color color ? color : null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
