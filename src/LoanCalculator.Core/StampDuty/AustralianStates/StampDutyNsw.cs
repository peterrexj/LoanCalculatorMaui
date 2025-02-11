using LoanCalculator.Models;
using LoanCalculator.Models.AdditionalExpense;
using LoanCalculator.Models.Enums;

namespace LoanCalculator.Core.StampDuty.AustralianStates
{
    public class StampDutyNsw : StampDutyCalcBase
    {
        public List<RangePercentage> Ranges { get; set; } = new List<RangePercentage>()
        {
            new RangePercentage(){ StartRange = 0, EndRange = 17000, Percentage = 1.25 },
            new RangePercentage(){ StartRange = 17001, EndRange = 36000, Percentage = 1.5 },
            new RangePercentage(){ StartRange = 36001, EndRange = 97000, Percentage = 1.75 },
            new RangePercentage(){ StartRange = 97001, EndRange = 364000, Percentage = 3.5 },
            new RangePercentage(){ StartRange = 364001, EndRange = 1212000, Percentage = 4.5 },
            new RangePercentage(){ StartRange = 1212001, EndRange = double.MaxValue, Percentage = 5.5 },
        };

        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();

            stampDutyOutput.SetState(AustralianStatesEnum.NSW);
            stampDutyOutput.StampDuty = base.Calculate(amount, Ranges);
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 166));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", 166));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
