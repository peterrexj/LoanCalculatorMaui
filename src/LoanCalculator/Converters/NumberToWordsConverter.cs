namespace LoanCalculatorMaui.Converters
{
    // Converts a numeric value to its English words representation
    // e.g. 1300000 -> "One Million, Three Hundred Thousand"
    public class NumberToWordsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return string.Empty;
            double number = 0;
            try { number = System.Convert.ToDouble(value); } catch { return string.Empty; }
            if (number <= 0) return string.Empty;
            return ToWords((long)Math.Round(number));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();

        private static string ToWords(long n)
        {
            if (n == 0) return "Zero";
            if (n < 0) return "Minus " + ToWords(-n);

            var parts = new List<string>();
            if (n >= 1_000_000_000) { parts.Add(ToWords(n / 1_000_000_000) + " Billion"); n %= 1_000_000_000; }
            if (n >= 1_000_000)     { parts.Add(ToWords(n / 1_000_000) + " Million"); n %= 1_000_000; }
            if (n >= 1_000)         { parts.Add(ToWords(n / 1_000) + " Thousand"); n %= 1_000; }
            if (n >= 100)           { parts.Add(Ones[n / 100] + " Hundred"); n %= 100; }
            if (n >= 20)            { parts.Add(Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "")); }
            else if (n > 0)         { parts.Add(Ones[n]); }

            return string.Join(", ", parts);
        }

        private static readonly string[] Ones =
        [
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
            "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen",
            "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        ];

        private static readonly string[] Tens =
        [
            "", "", "Twenty", "Thirty", "Forty", "Fifty",
            "Sixty", "Seventy", "Eighty", "Ninety"
        ];
    }
}
