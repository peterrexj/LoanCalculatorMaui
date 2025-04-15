using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.Core.Models.Income
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
        public double AmountWeekly => ModelHelper.ConvertAmountToWeeklyFrequency(Amount, Frequency);
        public double AmountFortnightly => ModelHelper.ConvertAmountToFortnightlyFrequency(Amount, Frequency);

        public int TimeFrequencyIndex => IncomeExpenseHelper.TimeFrequencyToIndex(Frequency);

        public double Percentage { get; set; }
    }
}
