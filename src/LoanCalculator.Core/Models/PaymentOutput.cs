using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.Core.Models
{
    public class PaymentOutput
    {
        public double InterestRatePercentage { get; set; }
        public double TermInterestRate { get; set; }
        public double Numerator { get; set; }
        public double Denominator { get; set; }
        public double TermPayment { get; set; }

        public double TermPaymentWeekly
        {
            get
            {
                if (TotalNumberPaymentPerYear == 12)
                {
                    return ModelHelper.ConvertAmountToWeeklyFrequency(TermPayment, TimeFrequencyEnum.Monthly).Round2();
                }
                else if (TotalNumberPaymentPerYear == 24)
                {
                    return ModelHelper.ConvertAmountToWeeklyFrequency(TermPayment, TimeFrequencyEnum.Fortnightly).Round2();
                }
                else
                {
                    return TermPaymentRounded;
                }
            }
        }

        public double TermPaymentFortnightly
        {
            get
            {
                if (TotalNumberPaymentPerYear == 12)
                {
                    return ModelHelper.ConvertAmountToFortnightlyFrequency(TermPayment, TimeFrequencyEnum.Monthly).Round2();
                }
                else if (TotalNumberPaymentPerYear == 52)
                {
                    return ModelHelper.ConvertAmountToFortnightlyFrequency(TermPayment, TimeFrequencyEnum.Weekly).Round2();
                }
                else
                {
                    return TermPaymentRounded;
                }
            }
        }
        public double TermPaymentMonthly
        {
            get
            {
                if (TotalNumberPaymentPerYear == 24)
                {
                    return ModelHelper.ConvertAmountToMonthlyFrequency(TermPayment, TimeFrequencyEnum.Fortnightly).Round2();
                }
                else if (TotalNumberPaymentPerYear == 52)
                {
                    return ModelHelper.ConvertAmountToMonthlyFrequency(TermPayment, TimeFrequencyEnum.Weekly).Round2();
                }
                else
                {
                    return TermPaymentRounded;
                }
            }
        }
        public double TermPaymentYearly
        {
            get
            {
                if (TotalNumberPaymentPerYear == 24)
                {
                    return ModelHelper.ConvertAmountToYearlyFrequency(TermPayment, TimeFrequencyEnum.Fortnightly).Round2();
                }
                else if (TotalNumberPaymentPerYear == 52)
                {
                    return ModelHelper.ConvertAmountToYearlyFrequency(TermPayment, TimeFrequencyEnum.Weekly).Round2();
                }
                else if (TotalNumberPaymentPerYear == 12)
                {
                    return ModelHelper.ConvertAmountToYearlyFrequency(TermPayment, TimeFrequencyEnum.Monthly).Round2();
                }
                else
                {
                    return TermPaymentRounded;
                }
            }
        }
        public string TermPaymentYearlyWithComma => TermPaymentYearly.WithComma();

        public double TotalPayment { get; set; }
        public double TotalInterestPayment { get; set; }
        public int TotalNumberPaymentPerYear { get; set; }

        public double TotalInterestPaymentRounded => TotalInterestPayment.Round2();
        public string TotalInterestPaymentRoundedWithComma => TotalInterestPaymentRounded.WithComma();

        public double TotalPaymentRounded => TotalPayment.Round2();
        public string TotalPaymentRoundedWithComma => TotalPaymentRounded.WithComma();
        public double TermPaymentRounded => TermPayment.Round2();
        public string TermPaymentRoundedWithComma => TermPaymentRounded.WithComma();

    }
}
