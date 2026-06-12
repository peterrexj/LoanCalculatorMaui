using LoanCalculator.Core.StampDuty.AustralianStates;

namespace LoanCalculator.UnitTests.StampDuty
{
    [TestFixture]
    public class StampDutyNtTests
    {
        private StampDutyNt _calculator;

        [SetUp]
        public void Setup() => _calculator = new StampDutyNt();

        private const double MortgageReg = 165;
        private const double TransferFee = 165;

        [Test]
        public void CalculateCharges_HasThreeExpenseEntries()
        {
            var result = _calculator.CalculateCharges(500000);
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(3));
        }

        [Test]
        public void CalculateCharges_MortgageRegistrationFee_IsCorrect()
        {
            var result = _calculator.CalculateCharges(500000);
            var reg = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Mortgage Registration");
            Assert.That(reg?.Expense, Is.EqualTo(MortgageReg));
        }

        [Test]
        public void CalculateCharges_TransferFee_IsCorrect()
        {
            var result = _calculator.CalculateCharges(500000);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee?.Expense, Is.EqualTo(TransferFee));
        }

        // NT quadratic formula for amount < 525,000:
        // SD = 0.06571441 × (amount/1000)² + 15 × (amount/1000)
        [Test]
        public void CalculateCharges_Below525k_UsesQuadraticFormula()
        {
            double amount = 300000;
            double v = amount / 1000.0;
            double expected = 0.06571441 * v * v + 15 * v;
            var result = _calculator.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected).Within(1));
        }

        [Test]
        public void CalculateCharges_100k_QuadraticFormula()
        {
            double amount = 100000;
            double v = amount / 1000.0;
            double expected = 0.06571441 * v * v + 15 * v;
            var result = _calculator.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected).Within(1));
        }

        // 525,000 ≤ amount < 3,000,000 → amount × 4.95%
        [Test]
        public void CalculateCharges_Range2_1m()
        {
            double amount = 1000000;
            double expected = (amount * 4.95) / 100;
            var result = _calculator.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected).Within(0.01));
        }

        [Test]
        public void CalculateCharges_Range2_2m()
        {
            double amount = 2000000;
            double expected = (amount * 4.95) / 100;
            var result = _calculator.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected).Within(0.01));
        }

        // 3,000,000 ≤ amount < 5,000,000 → amount × 5.75%
        [Test]
        public void CalculateCharges_Range3_4m()
        {
            double amount = 4000000;
            double expected = (amount * 5.75) / 100;
            var result = _calculator.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected).Within(0.01));
        }

        // amount ≥ 5,000,000 → amount × 5.95%
        [Test]
        public void CalculateCharges_Range4_6m()
        {
            double amount = 6000000;
            double expected = (amount * 5.95) / 100;
            var result = _calculator.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected).Within(0.01));
        }

        [Test]
        public void CalculateCharges_Total_EqualsStampDutyPlusFees()
        {
            var result = _calculator.CalculateCharges(500000);
            Assert.That(result.Total, Is.EqualTo(result.StampDuty + MortgageReg + TransferFee).Within(0.01));
        }
    }
}
