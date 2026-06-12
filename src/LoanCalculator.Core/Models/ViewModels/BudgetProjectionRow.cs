namespace LoanCalculator.Core.Models.ViewModels
{
    public class BudgetProjectionRow
    {
        public string Period { get; set; } = string.Empty;
        public string Income { get; set; } = "--";
        public string Expense { get; set; } = "--";
        public string Net { get; set; } = "--";
        public bool NetIsPositive { get; set; }
    }
}
