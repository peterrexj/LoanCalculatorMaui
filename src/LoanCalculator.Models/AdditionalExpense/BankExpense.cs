using System;
using System.Collections.Generic;
using System.Text;

namespace LoanCalculator.Models.AdditionalExpense
{
    public class BankExpense : OtherExpenseBase
    {
        public double BankSettlementFee
        {
            get => GetEntry("BankSettlementFee").Expense;
            set => GetEntry("BankSettlementFee").Expense = value;
        }
        public double LoanEstablishmentFee
        {
            get => GetEntry("LoanEstablishmentFee").Expense;
            set => GetEntry("LoanEstablishmentFee").Expense = value;
        }

        public BankExpense()
        {
            ExpenseEntries = new List<AdditionalExpenseEntry>();
        }

    }
}
