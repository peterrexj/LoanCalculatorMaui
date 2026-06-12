using LoanCalculator.Core.StampDuty.AustralianStates;

namespace LoanCalculator.UnitTests.StampDuty
{
    [TestFixture]
    public class StampDutySaTests
    {
        private StampDutySa _calculator;

        [SetUp]
        public void Setup() => _calculator = new StampDutySa();

        private const double MortgageReg = 187;

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

        // Range 1: 0–12,000 @ 1.0%
        [Test]
        public void CalculateCharges_Range1_5k()
        {
            double expected = Math.Round(5000 * 0.01, 0);
            var result = _calculator.CalculateCharges(5000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 2: 12,001–30,000 @ 2.0%
        [Test]
        public void CalculateCharges_Range2_20k()
        {
            double expected = Math.Round(12000 * 0.01 + 8000 * 0.02, 0);
            var result = _calculator.CalculateCharges(20000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 5: 100,001–200,000 @ 4.0%
        [Test]
        public void CalculateCharges_Range5_150k()
        {
            double expected = Math.Round(
                12000 * 0.01 +
                18000 * 0.02 +
                20000 * 0.03 +
                50000 * 0.035 +
                50000 * 0.04, 0);
            var result = _calculator.CalculateCharges(150000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 9: 500,001+ @ 5.5%
        [Test]
        public void CalculateCharges_Range9_600k()
        {
            double expected = Math.Round(
                12000 * 0.01 +
                18000 * 0.02 +
                20000 * 0.03 +
                50000 * 0.035 +
                100000 * 0.04 +
                50000 * 0.0425 +
                50000 * 0.0475 +
                200000 * 0.05 +
                100000 * 0.055, 0);
            var result = _calculator.CalculateCharges(600000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Land transfer fee: <5000 → $179
        [Test]
        public void CalculateCharges_LandTransferFee_BelowThreshold_Is179()
        {
            var result = _calculator.CalculateCharges(3000);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee?.Expense, Is.EqualTo(179));
        }

        // Land transfer fee: 5000–19999 → $200
        [Test]
        public void CalculateCharges_LandTransferFee_5kTo20k_Is200()
        {
            var result = _calculator.CalculateCharges(10000);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee?.Expense, Is.EqualTo(200));
        }

        // Land transfer fee: 20000–39999 → $220
        [Test]
        public void CalculateCharges_LandTransferFee_20kTo40k_Is220()
        {
            var result = _calculator.CalculateCharges(30000);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee?.Expense, Is.EqualTo(220));
        }

        // Land transfer fee: 40000–49999 → $309
        [Test]
        public void CalculateCharges_LandTransferFee_40kTo50k_Is309()
        {
            var result = _calculator.CalculateCharges(45000);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee?.Expense, Is.EqualTo(309));
        }

        // Land transfer fee: >50000 → 309 + (amount-50000) * 91.5/10000
        [Test]
        public void CalculateCharges_LandTransferFee_Above50k_UsesFormula()
        {
            double amount = 600000;
            double expectedFee = 309 + (amount - 50000) * (91.5 / 10000);
            var result = _calculator.CalculateCharges(amount);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee?.Expense, Is.EqualTo(expectedFee).Within(0.01));
        }

        [Test]
        public void CalculateCharges_Total_EqualsStampDutyPlusFees()
        {
            double amount = 300000;
            double expectedTransferFee = 309 + (amount - 50000) * (91.5 / 10000);
            var result = _calculator.CalculateCharges(amount);
            Assert.That(result.Total, Is.EqualTo(result.StampDuty + MortgageReg + expectedTransferFee).Within(0.01));
        }
    }
}
