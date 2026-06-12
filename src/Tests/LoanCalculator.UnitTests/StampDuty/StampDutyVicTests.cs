using LoanCalculator.Core.StampDuty.AustralianStates;

namespace LoanCalculator.UnitTests.StampDuty
{
    [TestFixture]
    public class StampDutyVicTests
    {
        private StampDutyVic _calculator;

        [SetUp]
        public void Setup() => _calculator = new StampDutyVic();

        private const double MortgageReg = 129;
        private const double TransferFee = 925;

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

        // Range 1: 0–25,000 @ 1.4%
        [Test]
        public void CalculateCharges_Range1_10k()
        {
            double expected = Math.Round(10000 * 0.014, 0);
            var result = _calculator.CalculateCharges(10000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 2: 25,001–130,000 @ 2.4%
        [Test]
        public void CalculateCharges_Range2_80k()
        {
            double expected = Math.Round(25000 * 0.014 + 55000 * 0.024, 0);
            var result = _calculator.CalculateCharges(80000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 3: 130,001–440,000 @ 5.0%
        [Test]
        public void CalculateCharges_Range3_300k()
        {
            double expected = Math.Round(25000 * 0.014 + 105000 * 0.024 + 170000 * 0.050, 0);
            var result = _calculator.CalculateCharges(300000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 4: 440,001–550,000 @ 6.0%
        [Test]
        public void CalculateCharges_Range4_500k()
        {
            double expected = Math.Round(
                25000 * 0.014 +
                105000 * 0.024 +
                310000 * 0.050 +
                60000 * 0.060, 0);
            var result = _calculator.CalculateCharges(500000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 5: 550,001+ @ 6.5%
        [Test]
        public void CalculateCharges_Range5_750k()
        {
            double expected = Math.Round(
                25000 * 0.014 +
                105000 * 0.024 +
                310000 * 0.050 +
                110000 * 0.060 +
                200000 * 0.065, 0);
            var result = _calculator.CalculateCharges(750000);
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
