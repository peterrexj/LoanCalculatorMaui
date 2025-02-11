using LoanCalculator.Models;
using LoanCalculator.Models.AdditionalExpense;

namespace LoanCalculator.Core.StampDuty.AustralianStates
{
    public class StampDutyQld : StampDutyCalcBase
    {
        public List<RangePercentage> Ranges { get; set; } = new List<RangePercentage>()
        {
            //new RangePercentage(){ StartRange = 0, EndRange = 260000, Percentage = 0.6 },
            new RangePercentage(){ StartRange = 5000, EndRange = 75000, Percentage = 1.5 },
            new RangePercentage(){ StartRange = 75001, EndRange = 540000, Percentage = 3.5 },
            new RangePercentage(){ StartRange = 540001, EndRange = 1000000, Percentage = 4.5 },
            new RangePercentage(){ StartRange = 1000001, EndRange = double.MaxValue, Percentage = 5.75 }
        };

        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();

            stampDutyOutput.StampDuty = base.Calculate(amount, Ranges);
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 224.00));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", 940.00));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
