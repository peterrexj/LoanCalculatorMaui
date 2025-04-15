namespace LoanCalculator.Core.Models.ViewModels
{
    public class ChartDataModel(string name, double value)
    {
        public string Name { get; set; } = name;
        public double Value { get; set; } = value;
    }
}
