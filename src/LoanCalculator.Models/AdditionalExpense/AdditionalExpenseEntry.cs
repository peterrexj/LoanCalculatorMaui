using System;
using System.Collections.Generic;
using System.Text;

namespace LoanCalculator.Models.AdditionalExpense
{
    public class AdditionalExpenseEntry
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public double Expense { get; set; }

        public static AdditionalExpenseEntry Add(string name, double expense)
        {
            return new AdditionalExpenseEntry { Name = name, Expense = expense };
        }
    }
}
