using LoanCalculator.Core.StampDuty.AustralianStates;

namespace LoanCalculator.UnitTests.StampDuty
{
    [TestFixture]
    public class StampDutyTasTests
    {
        private StampDutyTas _calculator;

        [SetUp]
        public void Setup() => _calculator = new StampDutyTas();

        private const double MortgageReg = 342; // 153+189
        private const double TransferFee = 234;

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

        // Range 1: 0–3,000 @ 0.5%
        [Test]
        public void CalculateCharges_Range1_1500()
        {
            double expected = Math.Round(1500 * 0.005, 0);
            var result = _calculator.CalculateCharges(1500);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 2: 3,001–25,000 @ 1.75%
        [Test]
        public void CalculateCharges_Range2_15k()
        {
            double expected = Math.Round(3000 * 0.005 + 12000 * 0.0175, 0);
            var result = _calculator.CalculateCharges(15000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 3: 25,001–75,000 @ 2.25%
        [Test]
        public void CalculateCharges_Range3_50k()
        {
            double expected = Math.Round(3000 * 0.005 + 22000 * 0.0175 + 25000 * 0.0225, 0);
            var result = _calculator.CalculateCharges(50000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 4: 75,001–200,000 @ 3.5%
        [Test]
        public void CalculateCharges_Range4_150k()
        {
            double expected = Math.Round(
                3000 * 0.005 +
                22000 * 0.0175 +
                50000 * 0.0225 +
                75000 * 0.035, 0);
            var result = _calculator.CalculateCharges(150000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 5: 200,001–375,000 @ 4.0%
        [Test]
        public void CalculateCharges_Range5_300k()
        {
            double expected = Math.Round(
                3000 * 0.005 +
                22000 * 0.0175 +
                50000 * 0.0225 +
                125000 * 0.035 +
                100000 * 0.04, 0);
            var result = _calculator.CalculateCharges(300000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 6: 375,001–725,000 @ 4.25%
        [Test]
        public void CalculateCharges_Range6_500k()
        {
            double expected = Math.Round(
                3000 * 0.005 +
                22000 * 0.0175 +
                50000 * 0.0225 +
                125000 * 0.035 +
                175000 * 0.04 +
                125000 * 0.0425, 0);
            var result = _calculator.CalculateCharges(500000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 7: 725,001+ @ 4.5%
        [Test]
        public void CalculateCharges_Range7_1m()
        {
            double expected = Math.Round(
                3000 * 0.005 +
                22000 * 0.0175 +
                50000 * 0.0225 +
                125000 * 0.035 +
                175000 * 0.04 +
                350000 * 0.0425 +
                275000 * 0.045, 0);
            var result = _calculator.CalculateCharges(1000000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateCharges_Total_EqualsStampDutyPlusFees()
        {
            var result = _calculator.CalculateCharges(300000);
            Assert.That(result.Total, Is.EqualTo(result.StampDuty + MortgageReg + TransferFee));
        }
    }
}
