using LoanCalculator.Core;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income.Summary;

namespace LoanCalculator.UnitTests
{
    [TestFixture]
    public class HomeLoanCalculatorTests
    {
        private static HomeLoanRepaymentInput MakeInput(double interestRate, int termYears, int paymentsPerYear) =>
            new HomeLoanRepaymentInput
            {
                InterestRate = interestRate,
                LoanTermInYears = termYears,
                TotalNumberPaymentPerYear = paymentsPerYear
            };

        // ── CalculateHomeLoan ────────────────────────────────────────────────

        [Test]
        public void CalculateHomeLoan_InvalidPrincipal_ReturnsEmptyOutput()
        {
            var result = HomeLoanCalculator.CalculateHomeLoan(0, MakeInput(5, 30, 12));
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TermPayment, Is.EqualTo(0));
        }

        [Test]
        public void CalculateHomeLoan_NegativePrincipal_ReturnsEmptyOutput()
        {
            var result = HomeLoanCalculator.CalculateHomeLoan(-100000, MakeInput(5, 30, 12));
            Assert.That(result.TermPayment, Is.EqualTo(0));
        }

        [Test]
        public void CalculateHomeLoan_ZeroPaymentsPerYear_ReturnsEmptyOutput()
        {
            var result = HomeLoanCalculator.CalculateHomeLoan(500000, MakeInput(5, 30, 0));
            Assert.That(result.TermPayment, Is.EqualTo(0));
        }

        [Test]
        public void CalculateHomeLoan_ZeroTermYears_ReturnsEmptyOutput()
        {
            var result = HomeLoanCalculator.CalculateHomeLoan(500000, MakeInput(5, 0, 12));
            Assert.That(result.TermPayment, Is.EqualTo(0));
        }

        [Test]
        public void CalculateHomeLoan_ZeroInterestRate_DividesEvenlyAcrossTerms()
        {
            double principal = 360000;
            var input = MakeInput(0, 30, 12); // 360 monthly payments

            var result = HomeLoanCalculator.CalculateHomeLoan(principal, input);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.InterestRatePercentage, Is.EqualTo(0));
            Assert.That(result.TermInterestRate, Is.EqualTo(0));
            Assert.That(result.TermPayment, Is.EqualTo(1000).Within(0.01));
            Assert.That(result.TotalPayment, Is.EqualTo(principal).Within(0.01));
            Assert.That(result.TotalInterestPayment, Is.EqualTo(0).Within(0.01));
        }

        [Test]
        public void CalculateHomeLoan_StandardMonthly_MatchesPMTFormula()
        {
            // Standard 30-year mortgage at 6% monthly: PMT ≈ $2,997.75
            double principal = 500000;
            var input = MakeInput(6, 30, 12);

            var result = HomeLoanCalculator.CalculateHomeLoan(principal, input);

            Assert.That(result.TermPayment, Is.EqualTo(2997.75).Within(0.50));
            Assert.That(result.TotalPayment, Is.GreaterThan(principal));
            Assert.That(result.TotalInterestPayment, Is.GreaterThan(0));
            Assert.That(result.TotalNumberPaymentPerYear, Is.EqualTo(12));
        }

        [Test]
        public void CalculateHomeLoan_FortnightlyPayments_LowerTermPaymentThanMonthly()
        {
            double principal = 500000;
            var monthly = HomeLoanCalculator.CalculateHomeLoan(principal, MakeInput(6, 30, 12));
            var fortnightly = HomeLoanCalculator.CalculateHomeLoan(principal, MakeInput(6, 30, 26));

            // Fortnightly payment is lower per-term than monthly (26 payments vs 12 per year)
            Assert.That(fortnightly.TermPayment, Is.LessThan(monthly.TermPayment));
            // Rough check: fortnightly ≈ monthly * 12/26
            Assert.That(fortnightly.TermPayment, Is.EqualTo(monthly.TermPayment * 12.0 / 26).Within(200));
        }

        [Test]
        public void CalculateHomeLoan_WeeklyPayments_LowerTotalInterestThanMonthly()
        {
            double principal = 500000;
            var monthly = HomeLoanCalculator.CalculateHomeLoan(principal, MakeInput(6, 30, 12));
            var weekly = HomeLoanCalculator.CalculateHomeLoan(principal, MakeInput(6, 30, 52));

            Assert.That(weekly.TotalInterestPayment, Is.LessThan(monthly.TotalInterestPayment));
        }

        [Test]
        public void CalculateHomeLoan_HigherInterestRate_IncreasesTermPayment()
        {
            double principal = 400000;
            var low = HomeLoanCalculator.CalculateHomeLoan(principal, MakeInput(3, 25, 12));
            var high = HomeLoanCalculator.CalculateHomeLoan(principal, MakeInput(8, 25, 12));

            Assert.That(high.TermPayment, Is.GreaterThan(low.TermPayment));
            Assert.That(high.TotalInterestPayment, Is.GreaterThan(low.TotalInterestPayment));
        }

        [Test]
        public void CalculateHomeLoan_ShorterTerm_IncreasesTermPayment()
        {
            double principal = 400000;
            var long30 = HomeLoanCalculator.CalculateHomeLoan(principal, MakeInput(5, 30, 12));
            var short15 = HomeLoanCalculator.CalculateHomeLoan(principal, MakeInput(5, 15, 12));

            Assert.That(short15.TermPayment, Is.GreaterThan(long30.TermPayment));
            Assert.That(short15.TotalInterestPayment, Is.LessThan(long30.TotalInterestPayment));
        }

        [Test]
        public void CalculateHomeLoan_InterestRatePercentageIsRateOver100()
        {
            var result = HomeLoanCalculator.CalculateHomeLoan(300000, MakeInput(6, 30, 12));
            Assert.That(result.InterestRatePercentage, Is.EqualTo(0.06).Within(0.0001));
        }

        [Test]
        public void CalculateHomeLoan_TermInterestRateIsAnnualRateDividedByPaymentsPerYear()
        {
            var result = HomeLoanCalculator.CalculateHomeLoan(300000, MakeInput(6, 30, 12));
            Assert.That(result.TermInterestRate, Is.EqualTo(0.06 / 12).Within(0.000001));
        }

        // ── CalculateHomeLoanPayments ────────────────────────────────────────

        [Test]
        public void CalculateHomeLoanPayments_TermCountMatchesLoanTerm()
        {
            var input = MakeInput(5, 25, 12);
            var summary = HomeLoanCalculator.CalculateHomeLoanPayments(300000, input);

            Assert.That(summary.PaymentTerms.Count, Is.EqualTo(25 * 12));
        }

        [Test]
        public void CalculateHomeLoanPayments_FirstPayment_HasCorrectStructure()
        {
            var summary = HomeLoanCalculator.CalculateHomeLoanPayments(300000, MakeInput(6, 30, 12));

            var first = summary.PaymentTerms[0];
            Assert.That(first.TermNumber, Is.EqualTo(1));
            Assert.That(first.InterestAmount, Is.GreaterThan(0));
            Assert.That(first.PrincipalAmount, Is.GreaterThan(0));
            Assert.That(first.InterestAmount + first.PrincipalAmount,
                Is.EqualTo(first.PaymentAmount).Within(0.01));
        }

        [Test]
        public void CalculateHomeLoanPayments_InterestDecreasesPrincipalIncreasesOverTime()
        {
            var summary = HomeLoanCalculator.CalculateHomeLoanPayments(400000, MakeInput(5, 30, 12));

            var first = summary.PaymentTerms[0];
            var last = summary.PaymentTerms[^1];

            Assert.That(last.InterestAmount, Is.LessThan(first.InterestAmount));
            Assert.That(last.PrincipalAmount, Is.GreaterThan(first.PrincipalAmount));
        }

        [Test]
        public void CalculateHomeLoanPayments_AllPaymentAmountsAreEqual()
        {
            var summary = HomeLoanCalculator.CalculateHomeLoanPayments(300000, MakeInput(5, 20, 12));
            var firstPayment = summary.PaymentTerms[0].PaymentAmount;

            Assert.That(summary.PaymentTerms.All(t => Math.Abs(t.PaymentAmount - firstPayment) < 0.01), Is.True);
        }

        [Test]
        public void CalculateHomeLoanPayments_ZeroInterest_AllPrincipalNoInterest()
        {
            var summary = HomeLoanCalculator.CalculateHomeLoanPayments(120000, MakeInput(0, 10, 12));

            Assert.That(summary.PaymentTerms.All(t => t.InterestAmount == 0), Is.True);
            Assert.That(summary.PaymentTerms.All(t => t.PrincipalAmount > 0), Is.True);
        }

        // ── UpdateLoanPaymentAmortizationDataByYear ──────────────────────────

        [Test]
        public void UpdateLoanPaymentAmortizationDataByYear_NullSummary_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByYear(null));
        }

        [Test]
        public void UpdateLoanPaymentAmortizationDataByYear_PopulatesYearlyTerms()
        {
            var summary = HomeLoanCalculator.CalculateHomeLoanPayments(400000, MakeInput(5, 30, 12));
            HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByYear(summary);

            // 30 years + 1 opening entry
            Assert.That(summary.PaymentAmortizationTerms.Count, Is.EqualTo(31));
        }

        [Test]
        public void UpdateLoanPaymentAmortizationDataByYear_FirstEntryIsOpeningBalance()
        {
            var summary = HomeLoanCalculator.CalculateHomeLoanPayments(400000, MakeInput(5, 30, 12));
            HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByYear(summary);

            var opening = summary.PaymentAmortizationTerms[0];
            Assert.That(opening.TermNumber, Is.EqualTo(0));
            Assert.That(opening.PaymentAmount, Is.EqualTo(0));
        }

        [Test]
        public void UpdateLoanPaymentAmortizationDataByYear_LastEntryHasZeroBalance()
        {
            var summary = HomeLoanCalculator.CalculateHomeLoanPayments(400000, MakeInput(5, 30, 12));
            HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByYear(summary);

            Assert.That(summary.PaymentAmortizationTerms.Last().BalanceAmount, Is.EqualTo(0));
        }

        // ── UpdateExpenseProjectionDataByYear ────────────────────────────────

        [Test]
        public void UpdateExpenseProjectionDataByYear_CreatesCorrectNumberOfTerms()
        {
            var summary = new IncomeExpenseSummary
            {
                TotalYearly = 50000,
                AnnualGrowthRate = 3,
                NumberOfYearsProjection = 5
            };

            HomeLoanCalculator.UpdateExpenseProjectionDataByYear(summary, 0);

            // Year 0 + 4 growth years = 5 entries (loop is year=1..N-1, so N-1 growth years + the initial)
            Assert.That(summary.ProjectionTerms.Count, Is.EqualTo(5));
        }

        [Test]
        public void UpdateExpenseProjectionDataByYear_FirstTermHasZeroGrowth()
        {
            var summary = new IncomeExpenseSummary
            {
                TotalYearly = 60000,
                AnnualGrowthRate = 5,
                NumberOfYearsProjection = 3
            };

            HomeLoanCalculator.UpdateExpenseProjectionDataByYear(summary, 0);

            var first = summary.ProjectionTerms[0];
            Assert.That(first.GrowthPercentage, Is.EqualTo(0));
            Assert.That(first.TermGrowthAmount, Is.EqualTo(0));
        }

        [Test]
        public void UpdateExpenseProjectionDataByYear_SubsequentTermsGrowByRate()
        {
            double totalYearly = 50000;
            var summary = new IncomeExpenseSummary
            {
                TotalYearly = totalYearly,
                AnnualGrowthRate = 10, // 10% → AnnualGrowthRatePercentage = 0.10
                NumberOfYearsProjection = 3
            };

            HomeLoanCalculator.UpdateExpenseProjectionDataByYear(summary, 0);

            var second = summary.ProjectionTerms[1];
            Assert.That(second.GrowthPercentage, Is.EqualTo(0.10).Within(0.001));
            Assert.That(second.TermGrowthAmount, Is.EqualTo(totalYearly * 0.10).Within(0.01));
        }

        [Test]
        public void UpdateExpenseProjectionDataByYear_ZeroGrowthRate_AmountStaysFlat()
        {
            var summary = new IncomeExpenseSummary
            {
                TotalYearly = 40000,
                AnnualGrowthRate = 0,
                NumberOfYearsProjection = 4
            };

            HomeLoanCalculator.UpdateExpenseProjectionDataByYear(summary, 0);

            foreach (var term in summary.ProjectionTerms.Skip(1))
            {
                Assert.That(term.TermGrowthAmount, Is.EqualTo(0));
                Assert.That(term.TermEndAmount, Is.EqualTo(term.TermStartAmount));
            }
        }

        [Test]
        public void UpdateExpenseProjectionDataByYear_AdditionalExpensesApplied()
        {
            var summary = new IncomeExpenseSummary
            {
                TotalYearly = 30000,
                AnnualGrowthRate = 0,
                NumberOfYearsProjection = 2
            };

            HomeLoanCalculator.UpdateExpenseProjectionDataByYear(summary, 5000);

            // First term TermAdjustments = TotalYearly + additional
            Assert.That(summary.ProjectionTerms[0].TermAdjustments, Is.EqualTo(35000));
        }

        // ── UpdateIncomeExpenseProjectionDataByYear ──────────────────────────

        [Test]
        public void UpdateIncomeExpenseProjectionDataByYear_CreatesCorrectNumberOfTerms()
        {
            var summary = new IncomeExpenseSummary
            {
                TotalYearly = 100000,
                AnnualGrowthRate = 3,
                NumberOfYearsProjection = 10
            };

            HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear(summary);

            Assert.That(summary.ProjectionTerms.Count, Is.EqualTo(10));
        }

        [Test]
        public void UpdateIncomeExpenseProjectionDataByYear_PersonalExpenseReducesAdjustments()
        {
            double totalYearly = 80000;
            double personalExpense = 20000;
            var summary = new IncomeExpenseSummary
            {
                TotalYearly = totalYearly,
                AnnualGrowthRate = 0,
                NumberOfYearsProjection = 2
            };

            HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear(summary, personalExpense);

            Assert.That(summary.ProjectionTerms[0].TermAdjustments, Is.EqualTo(totalYearly - personalExpense));
        }

        [Test]
        public void UpdateIncomeExpenseProjectionDataByYear_GrowthIncreasesEndAmount()
        {
            double totalYearly = 80000;
            var summary = new IncomeExpenseSummary
            {
                TotalYearly = totalYearly,
                AnnualGrowthRate = 5,
                NumberOfYearsProjection = 3
            };

            HomeLoanCalculator.UpdateIncomeExpenseProjectionDataByYear(summary);

            var second = summary.ProjectionTerms[1];
            Assert.That(second.TermEndAmount, Is.GreaterThan(totalYearly));
        }
    }
}
