using LoanCalculator.Models.Enums;

namespace LoanCalculator.Models.Income
{
    public class IncomeExpense
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Amount { get; set; }
        public string AmountString => $"{Helper.CurrencySymbol}{Amount:N0}";

        public TimeFrequencyEnum Frequency { get; set; }
        public double AmountMonthly => ModelHelper.ConvertAmountToMonthlyFrequency(Amount, Frequency);
        public double AmountYearly => ModelHelper.ConvertAmountToYearlyFrequency(Amount, Frequency);

        public int TimeFrequencyIndex => IncomeExpenseHelper.TimeFrequencyToIndex(Frequency);
    }
}
