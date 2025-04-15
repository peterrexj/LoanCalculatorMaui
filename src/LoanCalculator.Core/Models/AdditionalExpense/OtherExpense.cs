namespace LoanCalculator.Core.Models.AdditionalExpense
{
    public class OtherExpense : OtherExpenseBase
    {
        public OtherExpense() : base() { }

        public double OtherExpenses
        {
            get => GetEntry("Other expenses").Expense;
            set => GetEntry("Other expenses").Expense = value;
        }

        public double InspectionFee
        {
            get => GetEntry("InspectionFee").Expense;
            set => GetEntry("InspectionFee").Expense = value;
        }
    }
}
