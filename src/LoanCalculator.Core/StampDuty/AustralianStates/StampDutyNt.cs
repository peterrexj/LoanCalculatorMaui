using LoanCalculator.Models.AdditionalExpense;
using LoanCalculator.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Calculator.StampDuty
{
    public class StampDutyNt : StampDutyCalcBase
    {
        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();
            if (amount < 525000)
            {
                stampDutyOutput.StampDuty = (0.06571441 * ((amount / 1000) * (amount / 1000))) + (15 * (amount / 1000));
            }
            else if (amount < 2999999)
            {
                stampDutyOutput.StampDuty = (amount * 4.95) / 100;
            }
            else if (amount < 4999999)
            {
                stampDutyOutput.StampDuty = (amount * 5.75) / 100;
            }
            else
            {
                stampDutyOutput.StampDuty = (amount * 5.95) / 100;
            }
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 165));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", 165));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
