using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.AdditionalExpense;

namespace LoanCalculator.Core.StampDuty.AustralianStates
{
    public class StampDutyWa : StampDutyCalcBase
    {
        public List<RangePercentage> Ranges { get; set; } = new List<RangePercentage>()
        {
            new RangePercentage(){ StartRange = 0, EndRange = 120000, Percentage = 1.9 },
            new RangePercentage(){ StartRange = 120001, EndRange = 150000, Percentage = 2.85 },
            new RangePercentage(){ StartRange = 150001, EndRange = 360000, Percentage = 3.8 },
            new RangePercentage(){ StartRange = 360001, EndRange = 725000, Percentage = 4.75 },
            new RangePercentage(){ StartRange = 725001, EndRange = double.MaxValue, Percentage = 5.15 }
        };

        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();

            stampDutyOutput.StampDuty = base.Calculate(amount, Ranges);
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 203));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", 233));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
