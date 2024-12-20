using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoanCalculator.Models.AdditionalExpense
{
    public class OtherExpenseBase
    {
        protected List<AdditionalExpenseEntry> ExpenseEntries { get; set; }
        public double Total { get; set; }

        public OtherExpenseBase()
        {
            
        }

        protected AdditionalExpenseEntry GetEntry(string name)
        {
            if (ExpenseEntries == null)
            {
                ExpenseEntries = new List<AdditionalExpenseEntry>();
            }

            if (ExpenseEntries.Any(e => e.Name == name) == false)
                ExpenseEntries.Add(new AdditionalExpenseEntry { Name = name, Expense = 0 });

            return ExpenseEntries.First(f => f.Name == name);
        }

        public void SumUpData()
        {
            Total = 0;
            if (ExpenseEntries != null)
            {
                foreach (var item in ExpenseEntries)
                {
                    Total += item.Expense;
                }
            }
        }
    }
}
