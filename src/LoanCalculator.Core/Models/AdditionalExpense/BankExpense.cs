namespace LoanCalculator.Core.Models.AdditionalExpense
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
