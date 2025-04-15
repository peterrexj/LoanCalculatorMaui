namespace LoanCalculator.Core.Models
{
    public class RangePercentage
    {
        public double StartRange { get; set; }
        public double EndRange { get; set; }
        public double Percentage { get; set; }
        public double PercentageCalc => Percentage > 0 ? Percentage / 100 : 0;
    }
}
