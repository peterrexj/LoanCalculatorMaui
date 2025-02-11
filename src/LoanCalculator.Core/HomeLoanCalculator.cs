using Calculator.StampDuty;
using LoanCalculator.Models;
using LoanCalculator.Models.Income.Summary;
using System.Globalization;

namespace Calculator
{
    public class HomeLoanCalculator
    {
        private static StampDutyCalculator _stampDutyCalculator;
        public static StampDutyCalculator StampDutyCalculator => _stampDutyCalculator ?? new StampDutyCalculator();

        public static PaymentOutput? CalculateHomeLoan(double principal, HomeLoanRepaymentInput repaymentInput)
        {
            PaymentOutput? paymentOutput = new PaymentOutput();

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

            for (int i = 0; i < inYearData.Count(); i++)
            {
                var amortizationOutput = new PaymentAmortisationOutput
                {
                    TermNumber = i + 1,
                    DateTimeOfPayment = incrementDate.AddYears(i + 1),
                    InterestAmount = inYearData[i].Sum(x => x.InterestAmount),
                    PrincipalAmount = inYearData[i].Sum(x => x.PrincipalAmount),
                    PaymentAmount = inYearData[i].Sum(x => x.PaymentAmount)
                };
                amortizationOutput.PaymentPeriod = amortizationOutput.DateTimeOfPayment.Year.ToString();
                paymentSummary.PaymentAmortizationTerms.Add(amortizationOutput);

                amortizationOutput.BalanceAmount = paymentSummary.Payment.TotalPayment - paymentSummary.PaymentAmortizationTerms.Sum(f => f.PaymentAmount);
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


        /// <summary>
        /// Refer: Reference_IncomeExpenseProjectionCalculation.xlsx
        /// </summary>
        /// <param name="incomeExpenseSummary"></param>
        /// <param name="summaryAdditionalDataNotRequired"></param>
        public static void UpdateIncomeExpenseProjectionDataByYear(IncomeExpenseSummary incomeExpenseSummary, IncomeExpenseSummary summaryAdditionalDataNotRequired = null)
        {
            if (summaryAdditionalDataNotRequired == null) summaryAdditionalDataNotRequired = new IncomeExpenseSummary();

            incomeExpenseSummary.ProjectionTerms = new List<IncomeExpenseProjectionOutput>();

            var incrementDate = new DateTime(DateTime.Now.Year, 01, 01);

            incomeExpenseSummary.ProjectionTerms.Add(new IncomeExpenseProjectionOutput
            {
                TermNumber = 0,
                GrowthPercentage = incomeExpenseSummary.AnnualGrowthRatePercentage,
                TermStartAmount = incomeExpenseSummary.TotalYearly - summaryAdditionalDataNotRequired.TotalYearly,
                DateTimeOfPayment = incrementDate,
                PaymentPeriod = incrementDate.Year.ToString(),
            });

            incomeExpenseSummary.ProjectionTerms.Last().IncomeExpenseAmount = incomeExpenseSummary.ProjectionTerms.Sum(f => f.TermEndAmount);

            for (int year = 0; year < incomeExpenseSummary.NumberOfYearsProjection; year++)
            {
                IncomeExpenseProjectionOutput amortizationOutput = new IncomeExpenseProjectionOutput();

                amortizationOutput.TermNumber = year + 1;
                amortizationOutput.DateTimeOfPayment = incrementDate.AddYears(year + 1);
                amortizationOutput.GrowthPercentage = incomeExpenseSummary.AnnualGrowthRatePercentage;
                amortizationOutput.TermStartAmount = incomeExpenseSummary.ProjectionTerms.Last().TermEndAmount;
                amortizationOutput.PaymentPeriod = amortizationOutput.DateTimeOfPayment.Year.ToString();
                incomeExpenseSummary.ProjectionTerms.Add(amortizationOutput);

                incomeExpenseSummary.ProjectionTerms.Last().IncomeExpenseAmount = incomeExpenseSummary.ProjectionTerms.Sum(f => f.TermEndAmount);
            }
        }
    }
}
