using LoanCalculator.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoanCalculator.Models
{
    public static class ModelHelper
    {
        public static double ConvertAmountToMonthlyFrequency(double amount, TimeFrequencyEnum timeFrequency)
        {
            switch (timeFrequency)
            {
                case TimeFrequencyEnum.Yearly:
                    return Math.Round(amount / 12, 0);
                case TimeFrequencyEnum.BiYearly:
                    return Math.Round((amount * 2) / 12, 0);
                case TimeFrequencyEnum.Quarter:
                    return Math.Round((amount * 4) / 12, 0);
                case TimeFrequencyEnum.Fortnightly:
                    return Math.Round((amount * 26) / 12, 0);
                case TimeFrequencyEnum.Monthly:
                    return Math.Round(amount, 0);
                case TimeFrequencyEnum.Weekly:
                    return Math.Round((amount * 52) / 12, 0);
                case TimeFrequencyEnum.Daily:
                    return Math.Round((amount * 5 * 52) / 12, 0);
                case TimeFrequencyEnum.Hourly:
                    return Math.Round((amount * 8 * 5 * 52) / 12, 0);
                default:
                    return Math.Round(amount, 0);
            }
        }

        public static double ConvertAmountToYearlyFrequency(double amount, TimeFrequencyEnum timeFrequency)
        {
            switch (timeFrequency)
            {
                case TimeFrequencyEnum.Yearly:
                    return Math.Round(amount, 0);
                case TimeFrequencyEnum.BiYearly:
                    return Math.Round(amount * 2, 0);
                case TimeFrequencyEnum.Quarter:
                    return Math.Round(amount * 4, 0);
                case TimeFrequencyEnum.Fortnightly:
                    return Math.Round(amount * 26, 0);
                case TimeFrequencyEnum.Monthly:
                    return Math.Round(amount * 12, 0);
                case TimeFrequencyEnum.Weekly:
                    return Math.Round(amount * 52, 0);
                case TimeFrequencyEnum.Daily:
                    return Math.Round(amount * 5 * 52, 0);
                case TimeFrequencyEnum.Hourly:
                    return Math.Round(amount * 8 * 5 * 52, 0);
                default:
                    return Math.Round(amount, 0);
            }
        }
    }
}
