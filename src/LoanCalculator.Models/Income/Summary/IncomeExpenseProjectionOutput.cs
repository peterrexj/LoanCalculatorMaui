using System;

namespace LoanCalculator.Models.Income.Summary
{
    public class IncomeExpenseProjectionOutput
    {
        public int TermNumber { get; set; }
        public DateTime DateTimeOfPayment { get; set; }
        
        public double GrowthPercentage { get; set; }
        public double TermStartAmount { get; set; } //This amount will be term's start value based on the previous year accumulation
        public double TermAdjustments { get; set; } //This amount is what is adjusted for the term's expense either as a deduction or addition
        public double IncomeExpenseAmount { get; set; }

        public double TermGrowthAmount { get; set; } 
        public double TermEndAmount { get; set; } 

        public string TermStartAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(TermStartAmount, 0):N0}";
        public string TermGrowthAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(TermGrowthAmount, 0):N0}";
        public string IncomeExpenseAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(IncomeExpenseAmount, 0):N0}";
        public string TermAdjustmentAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(TermAdjustments, 0):N0}";

        public int YearOfPayment => DateTimeOfPayment.Year;
        public string PaymentPeriod { get; set; }
    }
}
