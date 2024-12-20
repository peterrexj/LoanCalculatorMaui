using System;

namespace LoanCalculator.Models.Income.Summary
{
    public class IncomeExpenseProjectionOutput
    {
        public int TermNumber { get; set; }
        public DateTime DateTimeOfPayment { get; set; }
        
        public double GrowthPercentage { get; set; }
        public double TermStartAmount { get; set; } //This amount will be term's start value based on the previous year accumulation
        public double TermGrowthAmount => TermStartAmount * GrowthPercentage;
        public double TermEndAmount => TermStartAmount + TermGrowthAmount;
        public double AccumulatedAmount => IncomeExpenseAmount - TermEndAmount;

        public double IncomeExpenseAmount { get; set; }

        public string TermStartAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(TermStartAmount, 0):N0}";
        public string TermGrowthAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(TermGrowthAmount, 0):N0}";
        public string AccumulatedAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(AccumulatedAmount, 0):N0}";
        public string IncomeExpenseAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(IncomeExpenseAmount, 0):N0}";

        public int YearOfPayment => DateTimeOfPayment.Year;
        public string PaymentPeriod { get; set; }
    }
}
