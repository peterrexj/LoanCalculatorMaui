using LoanCalculator.Core.StampDuty.AustralianStates;

namespace LoanCalculator.UnitTests.StampDuty
{
    [TestFixture]
    public class StampDutyWaTests
    {
        private StampDutyWa _calculator;

        [SetUp]
        public void Setup() => _calculator = new StampDutyWa();

        private const double MortgageReg = 203;
        private const double TransferFee = 233;

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

        // Range 1: 0–120,000 @ 1.9%
        [Test]
        public void CalculateCharges_Range1_80k()
        {
            double expected = Math.Round(80000 * 0.019, 0);
            var result = _calculator.CalculateCharges(80000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 2: 120,001–150,000 @ 2.85%
        [Test]
        public void CalculateCharges_Range2_130k()
        {
            double expected = Math.Round(120000 * 0.019 + 10000 * 0.0285, 0);
            var result = _calculator.CalculateCharges(130000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 3: 150,001–360,000 @ 3.8%
        [Test]
        public void CalculateCharges_Range3_250k()
        {
            double expected = Math.Round(
                120000 * 0.019 +
                30000 * 0.0285 +
                100000 * 0.038, 0);
            var result = _calculator.CalculateCharges(250000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 4: 360,001–725,000 @ 4.75%
        [Test]
        public void CalculateCharges_Range4_500k()
        {
            double expected = Math.Round(
                120000 * 0.019 +
                30000 * 0.0285 +
                210000 * 0.038 +
                140000 * 0.0475, 0);
            var result = _calculator.CalculateCharges(500000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 5: 725,001+ @ 5.15%
        [Test]
        public void CalculateCharges_Range5_1m()
        {
            double expected = Math.Round(
                120000 * 0.019 +
                30000 * 0.0285 +
                210000 * 0.038 +
                365000 * 0.0475 +
                275000 * 0.0515, 0);
            var result = _calculator.CalculateCharges(1000000);
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
