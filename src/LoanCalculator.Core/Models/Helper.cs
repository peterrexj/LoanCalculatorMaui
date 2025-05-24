using System.Globalization;

namespace LoanCalculator.Core.Models
{
    public static class Helper
    {
        /// <summary>
        /// ... This event is triggered when the currency symbol changes.
        /// </summary>
        public static event EventHandler? CurrencySymbolChanged;

        private static string _currencySymbol = "$";
        public static string CurrencySymbol
        {
            get => _currencySymbol;
            set
            {
                if (_currencySymbol != value)
                {
                    _currencySymbol = value;
                    CurrencySymbolChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public static string WithComma(this double value) => value.ToString("N0");
        public static double Round2(this double value) => Math.Round(value, 2);
        public static double Round0(this double value) => Math.Round(value, 0);

    }
}
