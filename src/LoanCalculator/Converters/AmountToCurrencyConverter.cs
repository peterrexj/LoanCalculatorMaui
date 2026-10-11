using LoanCalculator.Core.Models;

namespace LoanCalculatorMaui.Converters
{
    // Formats a positive amount with the current currency symbol and thousands separators,
    // e.g. 1300000 -> "$1,300,000". Empty for zero/negative, matching NumberToWordsConverter's
    // "nothing to show yet" convention so the pair reads consistently wherever they sit together.
    public class AmountToCurrencyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            double number;
            try { number = value == null ? 0 : System.Convert.ToDouble(value); } catch { return string.Empty; }
            return number > 0 ? $"{Helper.CurrencySymbol}{number:N0}" : string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}
