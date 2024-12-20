using System.Globalization;
using LoanCalculator.Models;
using LoanCalculator.Models.Charts;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.ViewModel;

namespace LoanCalculatorMaui.Converters
{
    public class DataMarkerToMoneyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && parameter.ToString() == "Label")
            {
                if (value is List<object>)
                {
                    return "Others";
                }
                else
                {
                    if (value != null)
                    {
                        return (value as DataModel).Category;
                    }
                }
            }
            else
            {
                if (value is List<object>)
                {
                    return (value as List<object>).Sum(item => (item as DataModel).Value).ToString() + "%";
                }
                else if (value is ChartDataModel)
                {
                    return $"{Helper.CurrencySymbol}{CalcHelper.ConvertValueShortKandM((value as ChartDataModel).Value)}";
                }
                else
                {
                    if (value != null)
                    {
                        return $"{Helper.CurrencySymbol}{CalcHelper.ConvertValueShortKandM((value as DataModel).Value)}";
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
