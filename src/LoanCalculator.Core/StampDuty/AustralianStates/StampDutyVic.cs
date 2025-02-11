using LoanCalculator.Models;
using LoanCalculator.Models.AdditionalExpense;

namespace LoanCalculator.Core.StampDuty.AustralianStates
{
    public class StampDutyVic : StampDutyCalcBase
    {
        public List<RangePercentage> Ranges { get; set; } = new List<RangePercentage>()
        {
            new RangePercentage(){ StartRange = 0, EndRange = 25000, Percentage = 1.4 },
            new RangePercentage(){ StartRange = 25001, EndRange = 130000, Percentage = 2.4 },
            new RangePercentage(){ StartRange = 130001, EndRange = 440000, Percentage = 5.0 },
            new RangePercentage(){ StartRange = 440001, EndRange = 550000, Percentage = 6.0 },
            new RangePercentage(){ StartRange = 550001, EndRange = double.MaxValue, Percentage = 6.5 }
        };

        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();

            stampDutyOutput.StampDuty = base.Calculate(amount, Ranges);
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 129));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", 925));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
