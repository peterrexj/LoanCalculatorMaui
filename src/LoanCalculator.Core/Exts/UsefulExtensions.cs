using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanCalculator.Core.Exts
{
    public static class UsefulExtensions
    {
        public static string ToCurrency(this double value)
        {
            return value.ToString("C", CultureInfo.CurrentCulture);
        }
    }
}
