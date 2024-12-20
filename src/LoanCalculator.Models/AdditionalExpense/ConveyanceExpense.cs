using System;
using System.Collections.Generic;
using System.Text;

namespace LoanCalculator.Models.AdditionalExpense
{
    public class ConveyanceExpense : OtherExpenseBase
    {
        public double ConveyancerFee
        {
            get => GetEntry("ConveyancerFee").Expense;
            set => GetEntry("ConveyancerFee").Expense = value;
        }
        public ConveyanceExpense() : base() { }
    }
}
