using LoanCalculator.Core.Models.Enums;
using System.ComponentModel;

namespace LoanCalculator.Core.Models.Income
{
    public class IncomeExpense : INotifyPropertyChanged, IDisposable
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public IncomeExpense()
        {
            Helper.CurrencySymbolChanged += OnCurrencySymbolChanged;
        }

        private void OnCurrencySymbolChanged(object? sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AmountString)));
        }

        public void Dispose()
        {
            Helper.CurrencySymbolChanged -= OnCurrencySymbolChanged;
        }
    }
}
