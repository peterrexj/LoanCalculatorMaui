namespace LoanCalculator.Core.Models.AdditionalExpense
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
