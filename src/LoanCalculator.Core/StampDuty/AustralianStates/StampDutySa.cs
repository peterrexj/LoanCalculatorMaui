using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.AdditionalExpense;

namespace LoanCalculator.Core.StampDuty.AustralianStates
{
    public class StampDutySa : StampDutyCalcBase
    {
        public List<RangePercentage> Ranges { get; set; } = new List<RangePercentage>()
        {
            new RangePercentage(){ StartRange = 0, EndRange = 12000, Percentage = 1.0 },
            new RangePercentage(){ StartRange = 12001, EndRange = 30000, Percentage = 2.0 },
            new RangePercentage(){ StartRange = 30001, EndRange = 50000, Percentage = 3.0 },
            new RangePercentage(){ StartRange = 50001, EndRange = 100000, Percentage = 3.5 },
            new RangePercentage(){ StartRange = 100001, EndRange = 200000, Percentage = 4.0 },
            new RangePercentage(){ StartRange = 200001, EndRange = 250000, Percentage = 4.25 },
            new RangePercentage(){ StartRange = 250001, EndRange = 300000, Percentage = 4.75 },
            new RangePercentage(){ StartRange = 300001, EndRange = 500000, Percentage = 5.0 },
            new RangePercentage(){ StartRange = 500001, EndRange = double.MaxValue, Percentage = 5.5 }
        };


        protected virtual double CalculateLandTransferFee(double amount)
        {
            if (amount < 5000)
            {
                return 179;
            }
            else if (amount < 20000)
            {
                return 200;
            }
            else if (amount < 40000)
            {
                return 220;
            }
            else if (amount < 50000)
            {
                return 309;
            }
            else
            {
                return 309 + (amount - 50000) * (91.5 / 10000);
            }
        }

        public StampDutyOutput CalculateCharges(double amount)
        {
            var stampDutyOutput = new StampDutyOutput();

            stampDutyOutput.StampDuty = base.Calculate(amount, Ranges);
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Mortgage Registration", 187));
            stampDutyOutput.ExpenseEntries.Add(AdditionalExpenseEntry.Add("Transfer Fee", CalculateLandTransferFee(amount)));

            stampDutyOutput.SumUpData();

            return stampDutyOutput;
        }
    }
}
