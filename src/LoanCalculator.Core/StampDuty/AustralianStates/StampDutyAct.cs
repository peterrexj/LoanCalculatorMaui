using LoanCalculator.Models.AdditionalExpense;
using LoanCalculator.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Calculator.StampDuty.AustralianStates
{
    public class StampDutyAct : StampDutyCalcBase
    {
        public List<RangePercentage> Ranges { get; set; } = new List<RangePercentage>()
        {
            new RangePercentage(){ StartRange = 0, EndRange = 260000, Percentage = 0.6 },
            new RangePercentage(){ StartRange = 260001, EndRange = 300000, Percentage = 2.2 },
            new RangePercentage(){ StartRange = 300001, EndRange = 500000, Percentage = 3.4 },
            new RangePercentage(){ StartRange = 500001, EndRange = 750000, Percentage = 4.32 },
            new RangePercentage(){ StartRange = 750001, EndRange = 1000000, Percentage = 5.9 },
            new RangePercentage(){ StartRange = 1000000, EndRange = 1455000, Percentage = 6.4 },
            new RangePercentage(){ StartRange = 1455000, EndRange = double.MaxValue, Percentage = 4.54 }
        };

        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();

            stampDutyOutput.StampDuty = base.Calculate(amount, Ranges);
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 166.00));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", 446.00));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
