using System.Globalization;
using Microsoft.Maui.Graphics.Converters;

namespace LoanCalculatorMaui.Converters
{
    public class StringToColorConverter : IValueConverter
    {
        static ColorTypeConverter _ColorTypeConverter = new ColorTypeConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrWhiteSpace(colorString))
            {
                try
                {
                    // Assume the color string is in a custom format like "#009FFF,#ec2F4B"
                    var colors = colorString.Split(',');
                    if (colors.Length == 2)
                    {
                        var gradientBrush = new LinearGradientBrush
                        {
                            EndPoint = new Point(0, 1)
                        };

                        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(colors[0]), 0.1f));
                        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(colors[1]), 1.0f));

                        return gradientBrush;
                    }
                    else
                    {
                        // Fallback to a single color
                        var color = Color.FromArgb(colorString);
                        return new SolidColorBrush(color);
                    }
                }
                catch
                {
                    return Colors.Transparent; // Default fallback
                }
            }

            return Colors.Transparent; // Default fallback if binding fails
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.ToHex();
            }

            // Return null if the conversion fails
            return null;
        }
    }
}
