namespace LoanCalculatorMaui.Converters
{
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
