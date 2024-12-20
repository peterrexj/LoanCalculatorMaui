using LoanCalculator.Models.AdditionalExpense;
using LoanCalculator.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Calculator.StampDuty
{
    public class StampDutyTas : StampDutyCalcBase
    {
        public List<RangePercentage> Ranges { get; set; } = new List<RangePercentage>()
        {
            new RangePercentage(){ StartRange = 0, EndRange = 3000, Percentage = 0.5 },
            new RangePercentage(){ StartRange = 3001, EndRange = 25000, Percentage = 1.75 },
            new RangePercentage(){ StartRange = 25001, EndRange = 75000, Percentage = 2.25 },
            new RangePercentage(){ StartRange = 75001, EndRange = 200000, Percentage = 3.5 },
            new RangePercentage(){ StartRange = 200001, EndRange = 375000, Percentage = 4.0 },
            new RangePercentage(){ StartRange = 375001, EndRange = 725000, Percentage = 4.25 },
            new RangePercentage(){ StartRange = 725001, EndRange = double.MaxValue, Percentage = 4.5 }
        };

        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();

            stampDutyOutput.StampDuty = base.Calculate(amount, Ranges);
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 153+189));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", 234));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
