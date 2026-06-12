using LoanCalculator.Core.StampDuty.AustralianStates;

namespace LoanCalculator.UnitTests.StampDuty
{
    [TestFixture]
    public class StampDutyActTests
    {
        private StampDutyAct _calculator;

        [SetUp]
        public void Setup() => _calculator = new StampDutyAct();

        private const double MortgageReg = 166;
        private const double TransferFee = 446;

        [Test]
        public void CalculateCharges_HasThreeExpenseEntries()
        {
            var result = _calculator.CalculateCharges(500000);
            // 1 placeholder "Mortgage charges" + Mortgage Registration + Transfer Fee
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(3));
        }

        [Test]
        public void CalculateCharges_MortgageRegistrationFee_IsCorrect()
        {
            var result = _calculator.CalculateCharges(500000);
            var reg = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Mortgage Registration");
            Assert.That(reg, Is.Not.Null);
            Assert.That(reg.Expense, Is.EqualTo(MortgageReg));
        }

        [Test]
        public void CalculateCharges_TransferFee_IsCorrect()
        {
            var result = _calculator.CalculateCharges(500000);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee, Is.Not.Null);
            Assert.That(fee.Expense, Is.EqualTo(TransferFee));
        }

        // Range 1: 0–260,000 @ 0.6%
        [Test]
        public void CalculateCharges_Range1_100k()
        {
            var result = _calculator.CalculateCharges(100000);
            Assert.That(result.StampDuty, Is.EqualTo(Math.Round(100000 * 0.006, 0)));
        }

        [Test]
        public void CalculateCharges_Range1_200k()
        {
            var result = _calculator.CalculateCharges(200000);
            Assert.That(result.StampDuty, Is.EqualTo(Math.Round(200000 * 0.006, 0)));
        }

        // Range 2: 260,001–300,000 @ 2.2%
        [Test]
        public void CalculateCharges_Range2_280k()
        {
            // 260,000 × 0.6% + 20,000 × 2.2%
            double expected = Math.Round(260000 * 0.006 + 20000 * 0.022, 0);
            var result = _calculator.CalculateCharges(280000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 3: 300,001–500,000 @ 3.4%
        [Test]
        public void CalculateCharges_Range3_400k()
        {
            double expected = Math.Round(260000 * 0.006 + 40000 * 0.022 + 100000 * 0.034, 0);
            var result = _calculator.CalculateCharges(400000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 4: 500,001–750,000 @ 4.32%
        [Test]
        public void CalculateCharges_Range4_600k()
        {
            double expected = Math.Round(
                260000 * 0.006 +
                40000 * 0.022 +
                200000 * 0.034 +
                100000 * 0.0432, 0);
            var result = _calculator.CalculateCharges(600000);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 5: 750,001–1,000,000 @ 5.9%
        [Test]
        public void CalculateCharges_Range5_800k()
        {
            double expected = Math.Round(
                260000 * 0.006 +
                40000 * 0.022 +
                200000 * 0.034 +
                250000 * 0.0432 +
                50000 * 0.059, 0);
            var result = _calculator.CalculateCharges(800000);
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
