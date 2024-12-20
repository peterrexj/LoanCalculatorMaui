namespace LoanCalculatorMaui.Extensions
{
    public class CalcHelper
    {
        public static string ConvertValueShortKandM(double value)
        {
            double Thousand = 1000;
            double Million = 1000000;

            if (value >= Million)
                return (value / Million).ToString("0.##") + "M";
            else if (value >= Thousand)
                return (value / Thousand).ToString("0.##") + "K";
            else
                return value.ToString();
        }
    }
}
