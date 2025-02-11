using LoanCalculator.Models;

namespace LoanCalculator.Core.StampDuty
{
    public class StampDutyCalcBase
    {
        protected virtual double Calculate(double amount, List<RangePercentage> range)
        {
            double total = 0;
            foreach (var item in range.OrderBy(r => r.StartRange))
            {
                if (amount < item.EndRange)
                {
                    total += (amount - item.StartRange) * item.PercentageCalc;
                    break;
                }
                else
                {
                    total += (item.EndRange - item.StartRange) * item.PercentageCalc;
                }
            }
            return Math.Round(total, 0, MidpointRounding.AwayFromZero);
        }
    }
}
