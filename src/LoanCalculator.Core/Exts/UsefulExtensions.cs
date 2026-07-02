using System.Globalization;

namespace LoanCalculator.Core.Exts
{
    public static class UsefulExtensions
    {
        public static string ToCurrency(this double value)
        {
            // Keep the minus sign in front of the currency symbol: "-$1,234.00", not "$-1,234.00".
            var sign = value < 0 ? "-" : string.Empty;
            return $"{sign}{Models.Helper.CurrencySymbol}{Math.Abs(value).ToString("N0", CultureInfo.CurrentCulture)}";
        }

        public static string ToCustomCurrencyRounded(this double value)
        {
            // Keep the minus sign in front of the currency symbol: "-$6,328", not "$-6,328".
            var sign = value < 0 ? "-" : string.Empty;
            return $"{sign}{Models.Helper.CurrencySymbol}{Math.Abs(value):N0}";
        }

        // Rounded currency with an explicit leading sign for both directions: "+$915" / "-$915".
        // Use for deltas/differences where showing the "+" is meaningful.
        public static string ToSignedCurrencyRounded(this double value)
        {
            var sign = value < 0 ? "-" : "+";
            return $"{sign}{Models.Helper.CurrencySymbol}{Math.Abs(value):N0}";
        }
    }
}
