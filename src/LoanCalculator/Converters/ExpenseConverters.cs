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
}
