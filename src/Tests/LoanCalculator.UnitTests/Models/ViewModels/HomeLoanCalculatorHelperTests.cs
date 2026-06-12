using LoanCalculator.Core.Models.ViewModels.PrimaryModels;

namespace LoanCalculator.UnitTests.Models.ViewModels
{
    [TestFixture]
    public class HomeLoanCalculatorHelperTests
    {
        // ── CalculateMonthlyRepayment ─────────────────────────────────────────

        [Test]
        public void CalculateMonthlyRepayment_ZeroRate_DividesEvenlyOverTerm()
        {
            // 0% rate: simply principal / (years * 12)
            double result = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(360000, 0, 30);
            Assert.That(result, Is.EqualTo(1000).Within(0.01));
        }

        [Test]
        public void CalculateMonthlyRepayment_NegativeRate_TreatedAsZero()
        {
            double result = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(240000, -1, 20);
            Assert.That(result, Is.EqualTo(1000).Within(0.01));
        }

        [Test]
        public void CalculateMonthlyRepayment_StandardLoan_MatchesPMTFormula()
        {
            // $500k at 6% p.a. for 30 years → ≈$2997.75
            double result = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(500000, 6, 30);
            Assert.That(result, Is.EqualTo(2997.75).Within(1));
        }

        [Test]
        public void CalculateMonthlyRepayment_HigherRate_IncreasesPayment()
        {
            double low = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(400000, 3, 25);
            double high = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(400000, 8, 25);
            Assert.That(high, Is.GreaterThan(low));
        }

        [Test]
        public void CalculateMonthlyRepayment_ShorterTerm_IncreasesPayment()
        {
            double long30 = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(400000, 5, 30);
            double short15 = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(400000, 5, 15);
            Assert.That(short15, Is.GreaterThan(long30));
        }

        [Test]
        public void CalculateMonthlyRepayment_LargerLoan_IncreasesPayment()
        {
            double small = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(200000, 5, 25);
            double large = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(600000, 5, 25);
            Assert.That(large, Is.GreaterThan(small));
        }

        // ── CalculateExtraRepaymentImpact ─────────────────────────────────────

        [Test]
        public void CalculateExtraRepaymentImpact_ZeroRate_ReturnsZeroSavings()
        {
            var (monthsSaved, interestSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(300000, 0, 25, 500);
            Assert.That(monthsSaved, Is.EqualTo(0));
            Assert.That(interestSaved, Is.EqualTo(0));
        }

        [Test]
        public void CalculateExtraRepaymentImpact_ZeroExtra_ReturnsZeroSavings()
        {
            var (monthsSaved, interestSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(300000, 5, 25, 0);
            Assert.That(monthsSaved, Is.EqualTo(0));
            Assert.That(interestSaved, Is.EqualTo(0));
        }

        [Test]
        public void CalculateExtraRepaymentImpact_NegativeExtra_ReturnsZeroSavings()
        {
            var (monthsSaved, interestSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(300000, 5, 25, -100);
            Assert.That(monthsSaved, Is.EqualTo(0));
            Assert.That(interestSaved, Is.EqualTo(0));
        }

        [Test]
        public void CalculateExtraRepaymentImpact_WithExtra_SavesMonths()
        {
            // $500/mo extra on a $400k, 5%, 30yr loan should save meaningful months
            var (monthsSaved, interestSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(400000, 5, 30, 500);
            Assert.That(monthsSaved, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateExtraRepaymentImpact_WithExtra_SavesInterest()
        {
            var (monthsSaved, interestSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(400000, 5, 30, 500);
            Assert.That(interestSaved, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateExtraRepaymentImpact_MoreExtra_SavesMoreMonths()
        {
            var (months500, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(400000, 5, 30, 500);
            var (months1000, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(400000, 5, 30, 1000);
            Assert.That(months1000, Is.GreaterThan(months500));
        }

        [Test]
        public void CalculateExtraRepaymentImpact_MoreExtra_SavesMoreInterest()
        {
            var (_, interest500) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(400000, 5, 30, 500);
            var (_, interest1000) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(400000, 5, 30, 1000);
            Assert.That(interest1000, Is.GreaterThan(interest500));
        }

        [Test]
        public void CalculateExtraRepaymentImpact_MonthsSavedNotNegative()
        {
            var (monthsSaved, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(300000, 5, 25, 100);
            Assert.That(monthsSaved, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void CalculateExtraRepaymentImpact_InterestSavedNotNegative()
        {
            var (_, interestSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(300000, 5, 25, 100);
            Assert.That(interestSaved, Is.GreaterThanOrEqualTo(0));
        }
    }
}
