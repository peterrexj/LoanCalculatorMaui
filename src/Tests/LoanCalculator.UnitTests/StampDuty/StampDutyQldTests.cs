using LoanCalculator.Core.StampDuty.AustralianStates;

namespace LoanCalculator.UnitTests.StampDuty
{
    [TestFixture]
    public class StampDutyQldTests
    {
        private StampDutyQld _calculator;

        [SetUp]
        public void Setup() => _calculator = new StampDutyQld();

        private const double MortgageReg = 224;
        private const double TransferFee = 940;

        [Test]
        public void CalculateCharges_HasThreeExpenseEntries()
        {
            var result = _calculator.CalculateCharges(300000);
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(3));
        }

        [Test]
        public void CalculateCharges_MortgageRegistrationFee_IsCorrect()
        {
            var result = _calculator.CalculateCharges(300000);
            var reg = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Mortgage Registration");
            Assert.That(reg?.Expense, Is.EqualTo(MortgageReg));
        }

        [Test]
        public void CalculateCharges_TransferFee_IsCorrect()
        {
            var result = _calculator.CalculateCharges(300000);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee?.Expense, Is.EqualTo(TransferFee));
        }

        // Range 1: 5,000–75,000 @ 1.5%
        [Test]
        public void CalculateCharges_Range1_50k()
        {
            // 45,000 in range × 1.5%
            double expected = Math.Round(45000 * 0.015, 0);
            var result = _calculator.CalculateCharges(50000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 2: 75,001–540,000 @ 3.5%
        [Test]
        public void CalculateCharges_Range2_200k()
        {
            double expected = Math.Round(70000 * 0.015 + 125000 * 0.035, 0);
            var result = _calculator.CalculateCharges(200000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 3: 540,001–1,000,000 @ 4.5%
        [Test]
        public void CalculateCharges_Range3_700k()
        {
            double expected = Math.Round(70000 * 0.015 + 465000 * 0.035 + 160000 * 0.045, 0);
            var result = _calculator.CalculateCharges(700000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 4: 1,000,001+ @ 5.75%
        [Test]
        public void CalculateCharges_Range4_1_5m()
        {
            double expected = Math.Round(
                70000 * 0.015 +
                465000 * 0.035 +
                460000 * 0.045 +
                500000 * 0.0575, 0);
            var result = _calculator.CalculateCharges(1500000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateCharges_Total_EqualsStampDutyPlusFees()
        {
            var result = _calculator.CalculateCharges(500000);
            Assert.That(result.Total, Is.EqualTo(result.StampDuty + MortgageReg + TransferFee));
        }
    }
}
