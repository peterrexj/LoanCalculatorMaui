namespace LoanCalculator.Core.Models
{
    public class PaymentAmortisationOutput
    {
        public int TermNumber { get; set; }
        public DateTime DateTimeOfPayment { get; set; }
        public double PaymentAmount { get; set; }
        public double InterestAmount { get; set; }
        public double PrincipalAmount { get; set; }
        public double BalanceAmount { get; set; }
        public string PrincipalAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(PrincipalAmount, 2):N2}";
        public string InterestAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(InterestAmount, 2):N2}";
        public string BalanceAmountRounded => $"{Helper.CurrencySymbol}{Math.Round(BalanceAmount, 2):N2}";
        public int YearOfPayment => DateTimeOfPayment.Year;
        public string PaymentPeriod { get; set; }
    }
}
