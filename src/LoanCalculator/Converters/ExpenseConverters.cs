namespace LoanCalculatorMaui.Converters
{
    // Maps a theme name string to a left-to-right gradient brush using that theme's colours.
    public class ThemeNameToGradientConverter : IValueConverter
    {
        private static readonly Dictionary<string, (string Start, string End)> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dark"]   = ("#222831", "#31424F"),
            ["Light"]  = ("#E2E8F0", "#F8FAFC"),
            ["Forest"] = ("#0e2424", "#183d3d"),
            ["Warm"]   = ("#FBEDED", "#F1D9D9"),
        };

        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value is string name && _map.TryGetValue(name, out var stops))
                {
                    var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(stops.Start), 0f));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(stops.End),   1f));
                    return brush;
                }
            }
            catch { }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Maps a theme name string to the appropriate foreground text colour for that theme.
    public class ThemeNameToTextColorConverter : IValueConverter
    {
        private static readonly Dictionary<string, Color> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dark"]   = Color.FromArgb("#DDE6ED"),
            ["Light"]  = Color.FromArgb("#1e2123"),
            ["Forest"] = Color.FromArgb("#bed0c9"),
            ["Warm"]   = Color.FromArgb("#4A2424"),
        };

        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is string name && _map.TryGetValue(name, out var c) ? c : Colors.White;

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }


    // Converts IsEditMode (bool) → button label: true = "Update", false = "Add"
    public class BoolToAddUpdateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is true ? "Update" : "Add";

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Converts IsEditMode (bool) → popup header title
    public class BoolToEditAddTitleConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            var noun = parameter as string ?? "Expense";
            return value is true ? $"Edit {noun}" : $"Add {noun}";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Converts a collection count (int) → true when count == 0 (show empty state)
    public class IntToIsEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is int count && count == 0;

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Converts IsHeader (bool) → FontAttributes.Bold or None
    public class BoolToFontAttributesConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is true ? FontAttributes.Bold : FontAttributes.None;

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Converts IsHeader (bool) → larger font size for headers vs body
    public class BoolToDisclaimerFontSizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            bool isTablet = DeviceInfo.Idiom == DeviceIdiom.Tablet;
            return value is true ? (isTablet ? 22.0 : 15.0) : (isTablet ? 18.0 : 13.0);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Converts IsHeader (bool) → tighter top margin for body paragraphs, spacious for headers
    public class BoolToDisclaimerMarginConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            bool isTablet = DeviceInfo.Idiom == DeviceIdiom.Tablet;
            return value is true
                ? new Thickness(0, isTablet ? 20 : 14, 0, isTablet ? 4 : 2)
                : new Thickness(0, 0, 0, isTablet ? 10 : 6);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}
