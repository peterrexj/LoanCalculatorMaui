using LoanCalculator.Models;
using LoanCalculator.Models.AdditionalExpense;
using System.Collections.Generic;

namespace Calculator.StampDuty.AustralianStates
{
    public class StampDutyNsw : StampDutyCalcBase
    {
        public List<RangePercentage> Ranges { get; set; } = new List<RangePercentage>()
        {
            new RangePercentage(){ StartRange = 0, EndRange = 15000, Percentage = 1.25 },
            new RangePercentage(){ StartRange = 15001, EndRange = 32000, Percentage = 1.5 },
            new RangePercentage(){ StartRange = 32001, EndRange = 87000, Percentage = 1.75 },
            new RangePercentage(){ StartRange = 87001, EndRange = 327000, Percentage = 3.5 },
            new RangePercentage(){ StartRange = 327001, EndRange = 1089000, Percentage = 4.5 },
            new RangePercentage(){ StartRange = 1089001, EndRange = 3268000, Percentage = 5.5 },
            new RangePercentage(){ StartRange = 3268001, EndRange = double.MaxValue, Percentage = 7.0 }
        };

        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();

            stampDutyOutput.StampDuty = base.Calculate(amount, Ranges);
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 166));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", 166));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
