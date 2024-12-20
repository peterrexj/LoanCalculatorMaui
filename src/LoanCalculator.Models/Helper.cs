using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LoanCalculator.Models
{
    public static class Helper
    {
        public static string CurrencySymbol { get; set; } = new RegionInfo(CultureInfo.CurrentCulture.Name).CurrencySymbol;

        public static string WithComma(this double value) => value.ToString("N0");
        public static double Round2(this double value) => Math.Round(value, 2);
        public static double Round0(this double value) => Math.Round(value, 0);

    }
}
