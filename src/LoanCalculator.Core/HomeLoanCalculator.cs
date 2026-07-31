using System.Globalization;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income.Summary;
using LoanCalculator.Core.StampDuty;

namespace LoanCalculator.Core
{
    public class HomeLoanCalculator
    {
        private static StampDutyCalculator _stampDutyCalculator;
        public static StampDutyCalculator StampDutyCalculator => _stampDutyCalculator ?? new StampDutyCalculator();

        public static PaymentOutput? CalculateHomeLoan(double principal, HomeLoanRepaymentInput repaymentInput)
        {
            PaymentOutput? paymentOutput = new PaymentOutput();

            if (principal <= 0 || repaymentInput.TotalNumberOfPaymentsWithInTermPeriod <= 0 || repaymentInput.TotalNumberPaymentPerYear <= 0)
                return paymentOutput;

            if (repaymentInput.InterestRate == 0)
            {
                paymentOutput.InterestRatePercentage = 0;
                paymentOutput.TermInterestRate = 0;

                paymentOutput.Numerator = 0;
                paymentOutput.Denominator = 0;

                paymentOutput.TermPayment = principal / repaymentInput.TotalNumberOfPaymentsWithInTermPeriod;

                // Calculate the total payment
                paymentOutput.TotalPayment = paymentOutput.TermPayment * repaymentInput.TotalNumberOfPaymentsWithInTermPeriod;
                paymentOutput.TotalInterestPayment = paymentOutput.TotalPayment - principal;
                paymentOutput.TotalNumberPaymentPerYear = repaymentInput.TotalNumberPaymentPerYear;

                return paymentOutput;
            }

            // Convert the interest rate from a percentage to a decimal
            paymentOutput.InterestRatePercentage = repaymentInput.InterestRate / 100.0;

            // Calculate the monthly interest rate
            paymentOutput.TermInterestRate = paymentOutput.InterestRatePercentage / repaymentInput.TotalNumberPaymentPerYear;

            // Calculate the monthly payment
            paymentOutput.Numerator = paymentOutput.TermInterestRate * Math.Pow((1 + paymentOutput.TermInterestRate), repaymentInput.TotalNumberOfPaymentsWithInTermPeriod);
            paymentOutput.Denominator = Math.Pow((1 + paymentOutput.TermInterestRate), repaymentInput.TotalNumberOfPaymentsWithInTermPeriod) - 1;
            paymentOutput.TermPayment = principal * (paymentOutput.Numerator / paymentOutput.Denominator);

            // Calculate the total payment
            paymentOutput.TotalPayment = paymentOutput.TermPayment * repaymentInput.TotalNumberOfPaymentsWithInTermPeriod;
            paymentOutput.TotalInterestPayment = paymentOutput.TotalPayment - principal;
            paymentOutput.TotalNumberPaymentPerYear = repaymentInput.TotalNumberPaymentPerYear;

            return paymentOutput;
        }
        public static PaymentSummary? CalculateHomeLoanPayments(double principal, HomeLoanRepaymentInput repaymentInput)
        {
            PaymentSummary? paymentSummary = new PaymentSummary();
            paymentSummary.PaymentTerms = new List<PaymentPerTermOutput>();

            paymentSummary.Payment = CalculateHomeLoan(principal, repaymentInput);

            // Calculate the breakdown of principal and interest for each payment
            double remainingPrincipal = principal;
            double totalPaymentInTerm = 0;
            for (int i = 1; i <= repaymentInput.TotalNumberOfPaymentsWithInTermPeriod; i++)
            {
                PaymentPerTermOutput payment = new PaymentPerTermOutput();
                payment.TermNumber = i;

                // Calculate the interest amount for this payment
                double interestAmount = remainingPrincipal * paymentSummary.Payment.TermInterestRate;
                payment.InterestAmount = interestAmount;

                // Calculate the principal amount for this payment
                double principalAmount = paymentSummary.Payment.TermPayment - interestAmount;
                payment.PrincipalAmount = principalAmount;

                // Calculate the remaining principal after this payment
                remainingPrincipal -= principalAmount;
                payment.PaymentAmount = paymentSummary.Payment.TermPayment;

                totalPaymentInTerm += paymentSummary.Payment.TermPayment;
                payment.TotalPayments += totalPaymentInTerm;

                paymentSummary.PaymentTerms.Add(payment);
            }
            return paymentSummary;
        }

        public static void UpdateLoanPaymentAmortizationDataByYear(PaymentSummary? paymentSummary)
        {
            if (paymentSummary == null || paymentSummary.Payment == null || paymentSummary.PaymentTerms == null || paymentSummary.PaymentTerms.Count == 0) return;

            var inYearData = paymentSummary.PaymentTerms.Chunk(paymentSummary.Payment.TotalNumberPaymentPerYear).ToList();
            paymentSummary.PaymentAmortizationTerms = new List<PaymentAmortisationOutput>();
            var incrementDate = new DateTime(DateTime.Now.Year, 01, 01);

            // Outstanding balance starts at the loan principal (total repayable minus total interest)
            double remainingBalance = paymentSummary.Payment.TotalPayment - paymentSummary.Payment.TotalInterestPayment;

            paymentSummary.PaymentAmortizationTerms.Add(new PaymentAmortisationOutput
            {
                TermNumber = 0,
                DateTimeOfPayment = incrementDate,
                InterestAmount = 0,
                PrincipalAmount = 0,
                PaymentAmount = 0,
                PaymentPeriod = incrementDate.Year.ToString(),
                BalanceAmount = remainingBalance
            });

            for (int i = 0; i < inYearData.Count(); i++)
            {
                var yearPrincipal = inYearData[i].Sum(x => x.PrincipalAmount);
                remainingBalance -= yearPrincipal;

                var amortizationOutput = new PaymentAmortisationOutput
                {
                    TermNumber = i + 1,
                    DateTimeOfPayment = incrementDate.AddYears(i + 1),
                    InterestAmount = inYearData[i].Sum(x => x.InterestAmount),
                    PrincipalAmount = yearPrincipal,
                    PaymentAmount = inYearData[i].Sum(x => x.PaymentAmount),
                    BalanceAmount = remainingBalance
                };
                amortizationOutput.PaymentPeriod = amortizationOutput.DateTimeOfPayment.Year.ToString();
                paymentSummary.PaymentAmortizationTerms.Add(amortizationOutput);
            }
            paymentSummary.PaymentAmortizationTerms.Last().BalanceAmount = 0;
        }
        public static void UpdateLoanPaymentAmortizationDataByTerm(PaymentSummary? paymentSummary)
        {
            paymentSummary.PaymentAmortizationTerms = new List<PaymentAmortisationOutput>();
            var incrementDate = new DateTime(DateTime.Now.Year, 01, 01);

            paymentSummary.PaymentAmortizationTerms.Add(new PaymentAmortisationOutput
            {
                TermNumber = 0,
                DateTimeOfPayment = incrementDate,
                InterestAmount = 0,
                PrincipalAmount = 0,
                PaymentAmount = 0,
                PaymentPeriod = incrementDate.Year.ToString(),
                BalanceAmount = paymentSummary.Payment.TotalPayment
            });

            int periodCounter = 1;
            int currentYear = incrementDate.Year;

            for (int i = 0; i < paymentSummary.PaymentTerms.Count; i++)
            {
                PaymentAmortisationOutput amortizationOutput = new PaymentAmortisationOutput();
                amortizationOutput.TermNumber = i + 1;
                if (i % paymentSummary.Payment.TotalNumberPaymentPerYear == 0)
                {
                    currentYear++;
                    periodCounter = 0;
                    incrementDate = new DateTime(currentYear, 01, 01);
                }
                else
                {
                    if (paymentSummary.Payment.TotalNumberPaymentPerYear == 12)
                    {
                        incrementDate = new DateTime(currentYear, periodCounter + 1, 01);
                    }
                    else if (paymentSummary.Payment.TotalNumberPaymentPerYear == 24)
                    {
                        incrementDate = new DateTime(currentYear, ((i / 2) % 12) + 1, 01);
                    }
                    else if (paymentSummary.Payment.TotalNumberPaymentPerYear == 52)
                    {
                        incrementDate = new DateTime(currentYear, 01, 01).AddDays(periodCounter * 7);
                    }
                    else
                    {

                    }
                }

                periodCounter++;

                amortizationOutput.DateTimeOfPayment = incrementDate;


                //amortizationOutput.DateTimeOfPayment = new DateTime(currentYear, periodCounter, 01) //need to change logic to get from increment. compute date here for all types
                amortizationOutput.InterestAmount = paymentSummary.PaymentTerms[i].InterestAmount;
                amortizationOutput.PrincipalAmount = paymentSummary.PaymentTerms[i].PrincipalAmount;
                amortizationOutput.PaymentAmount = paymentSummary.PaymentTerms[i].PaymentAmount;

                amortizationOutput.PaymentPeriod = ReadableDateOnDataTableAmortization(amortizationOutput.DateTimeOfPayment, paymentSummary.Payment.TotalNumberPaymentPerYear);

                paymentSummary.PaymentAmortizationTerms.Add(amortizationOutput);
                amortizationOutput.BalanceAmount = paymentSummary.Payment.TotalPayment - paymentSummary.PaymentAmortizationTerms.Sum(f => f.PaymentAmount);
            }
            paymentSummary.PaymentAmortizationTerms.Last().BalanceAmount = 0;
        }
        private static string ReadableDateOnDataTableAmortization(DateTime dateTime, int totalPaymentsPerYear)
        {
            if (totalPaymentsPerYear == 12)
            {
                return $"{dateTime.Year}, {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateTime.Month)}";
            }
            else if (totalPaymentsPerYear == 24)
            {
                return $"{dateTime.Year}, {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateTime.Month)}";
            }
            else if (totalPaymentsPerYear == 52)
            {
                return $"{dateTime.Year}, {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateTime.Month)}, Week {CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dateTime, CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule, CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek)}";
            }
            else
            {
                return dateTime.Year.ToString();
            }
        }


        public static void UpdateExpenseProjectionDataByYear(IncomeExpenseSummary expenseSummary,
            double additionalExpensesFromNewProperty
            )
        {
            expenseSummary.ProjectionTerms = new List<IncomeExpenseProjectionOutput>();

            var incrementDate = new DateTime(DateTime.Now.Year, 01, 01);

            expenseSummary.ProjectionTerms.Add(new IncomeExpenseProjectionOutput
            {
                TermNumber = 0,
                DateTimeOfPayment = incrementDate,
                PaymentPeriod = incrementDate.Year.ToString(),

                GrowthPercentage = 0,
                TermGrowthAmount = 0,
                TermStartAmount = expenseSummary.TotalYearly ,
                TermAdjustments = expenseSummary.TotalYearly + additionalExpensesFromNewProperty,
                TermEndAmount = expenseSummary.TotalYearly,
            });

            expenseSummary.ProjectionTerms.Last().IncomeExpenseAmount = expenseSummary.ProjectionTerms.Sum(f => f.TermAdjustments);

            for (int year = 1; year < expenseSummary.NumberOfYearsProjection; year++)
            {
                IncomeExpenseProjectionOutput amortizationOutput = new IncomeExpenseProjectionOutput
                {
                    TermNumber = year + 1,
                    DateTimeOfPayment = incrementDate.AddYears(year + 1),
                    PaymentPeriod = incrementDate.AddYears(year + 1).Year.ToString(),

                    GrowthPercentage = expenseSummary.AnnualGrowthRatePercentage,
                    TermStartAmount = expenseSummary.ProjectionTerms.Last().TermEndAmount,
                };

                if (amortizationOutput.GrowthPercentage != 0)
                {
                    amortizationOutput.TermGrowthAmount = amortizationOutput.TermStartAmount * amortizationOutput.GrowthPercentage;
                    amortizationOutput.TermEndAmount = amortizationOutput.TermStartAmount + amortizationOutput.TermGrowthAmount;
                }
                else
                {
                    amortizationOutput.TermGrowthAmount = 0;
                    amortizationOutput.TermEndAmount = amortizationOutput.TermStartAmount;
                }

                amortizationOutput.TermAdjustments = amortizationOutput.TermEndAmount + additionalExpensesFromNewProperty;

                expenseSummary.ProjectionTerms.Add(amortizationOutput);

                expenseSummary.ProjectionTerms.Last().IncomeExpenseAmount = expenseSummary.ProjectionTerms.Sum(f => f.TermAdjustments);
            }
        }

        /// <summary>
        /// Refer: Reference_IncomeExpenseProjectionCalculation.xlsx
        /// </summary>
        /// <param name="incomeSummary"></param>
        /// <param name="summaryAdditionalDataNotRequired"></param>
        /// <param name="personalExpense"></param>
        /// <param name="propertyExpense"></param>
        public static void UpdateIncomeExpenseProjectionDataByYear(
            IncomeExpenseSummary incomeSummary,
            double personalExpense = 0)
        {
            incomeSummary.ProjectionTerms = new List<IncomeExpenseProjectionOutput>();

            var incrementDate = new DateTime(DateTime.Now.Year, 01, 01);

            incomeSummary.ProjectionTerms.Add(new IncomeExpenseProjectionOutput
            {
                TermNumber = 0,
                DateTimeOfPayment = incrementDate,
                PaymentPeriod = incrementDate.Year.ToString(),

                GrowthPercentage = 0,
                TermGrowthAmount = 0,
                TermStartAmount = incomeSummary.TotalYearly,
                TermAdjustments = incomeSummary.TotalYearly - personalExpense,
                TermEndAmount = incomeSummary.TotalYearly,
            });

            incomeSummary.ProjectionTerms.Last().IncomeExpenseAmount = incomeSummary.ProjectionTerms.Sum(f => f.TermAdjustments);

            for (int year = 1; year < incomeSummary.NumberOfYearsProjection; year++)
            {
                IncomeExpenseProjectionOutput amortizationOutput = new IncomeExpenseProjectionOutput
                {
                    TermNumber = year + 1,
                    DateTimeOfPayment = incrementDate.AddYears(year + 1),
                    PaymentPeriod = incrementDate.AddYears(year + 1).Year.ToString(),

                    GrowthPercentage = incomeSummary.AnnualGrowthRatePercentage,
                    TermStartAmount = incomeSummary.ProjectionTerms.Last().TermEndAmount,
                };

                if (amortizationOutput.GrowthPercentage != 0)
                {
                    amortizationOutput.TermGrowthAmount = amortizationOutput.TermStartAmount * amortizationOutput.GrowthPercentage;
                    amortizationOutput.TermEndAmount = amortizationOutput.TermStartAmount + amortizationOutput.TermGrowthAmount;
                }
                else
                {
                    amortizationOutput.TermGrowthAmount = 0;
                    amortizationOutput.TermEndAmount = amortizationOutput.TermStartAmount;
                }
                
                amortizationOutput.TermAdjustments = amortizationOutput.TermEndAmount - personalExpense;
                
                incomeSummary.ProjectionTerms.Add(amortizationOutput);

                incomeSummary.ProjectionTerms.Last().IncomeExpenseAmount = incomeSummary.ProjectionTerms.Sum(f => f.TermAdjustments);
            }
        }

    }
}
