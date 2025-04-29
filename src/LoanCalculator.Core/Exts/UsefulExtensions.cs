using System.Globalization;

namespace LoanCalculator.Core.Exts
{
    public static class UsefulExtensions
    {
        public static string ToCurrency(this double value)
        {
            return value.ToString("C", CultureInfo.CurrentCulture);
        }

        public static string ToCustomCurrencyRounded(this double value)
        {
            return $"{Models.Helper.CurrencySymbol}{value:N0}";
        }
    }
}
