using LoanCalculator.Core.StampDuty.AustralianStates;

namespace LoanCalculator.UnitTests.StampDuty
{
    [TestFixture]
    public class StampDutyNswTests
    {
        private StampDutyNsw _stampDutyNsw;

        [SetUp]
        public void Setup()
        {
            _stampDutyNsw = new StampDutyNsw();
        }

        [Test]
        public void CalculateCharges_HasThreeExpenseEntries()
        {
            var result = _stampDutyNsw.CalculateCharges(100000);
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(3));
        }

        [Test]
        public void CalculateCharges_MortgageRegistrationFee_IsCorrect()
        {
            var result = _stampDutyNsw.CalculateCharges(100000);
            var reg = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Mortgage Registration");
            Assert.That(reg?.Expense, Is.EqualTo(166));
        }

        [Test]
        public void CalculateCharges_TransferFee_IsCorrect()
        {
            var result = _stampDutyNsw.CalculateCharges(100000);
            var fee = result.ExpenseEntries.FirstOrDefault(e => e.Name == "Transfer Fee");
            Assert.That(fee?.Expense, Is.EqualTo(166));
        }

        // Range 1: 0–17,000 @ 1.25%
        [Test]
        public void CalculateCharges_AmountInFirstRange_ReturnsCorrectStampDuty()
        {
            double amount = 10000;
            double expected = Math.Round(10000 * 0.0125, 0);
            var result = _stampDutyNsw.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 2: 17,001–36,000 @ 1.5%
        [Test]
        public void CalculateCharges_AmountInSecondRange_ReturnsCorrectStampDuty()
        {
            double amount = 20000;
            // StampDutyCalcBase accumulates then rounds with MidpointRounding.AwayFromZero.
            // StartRange = 17001, so second range portion = (20000 - 17001) * 0.015 = 44.985
            double expected = Math.Round(17000 * 0.0125 + (amount - 17001) * 0.015, 0, MidpointRounding.AwayFromZero);
            var result = _stampDutyNsw.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 3: 36,001–97,000 @ 1.75%
        [Test]
        public void CalculateCharges_AmountInThirdRange_ReturnsCorrectStampDuty()
        {
            double amount = 50000;
            double expected = Math.Round(17000 * 0.0125 + 19000 * 0.015 + 14000 * 0.0175, 0);
            var result = _stampDutyNsw.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 4: 97,001–364,000 @ 3.5%
        [Test]
        public void CalculateCharges_AmountInFourthRange_ReturnsCorrectStampDuty()
        {
            double amount = 100000;
            double expected = Math.Round(17000 * 0.0125 + 19000 * 0.015 + 61000 * 0.0175 + 3000 * 0.035, 0);
            var result = _stampDutyNsw.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 5: 364,001–1,212,000 @ 4.5%
        [Test]
        public void CalculateCharges_AmountInFifthRange_ReturnsCorrectStampDuty()
        {
            double amount = 500000;
            double expected = Math.Round(
                17000 * 0.0125 +
                19000 * 0.015 +
                61000 * 0.0175 +
                267000 * 0.035 +
                136000 * 0.045, 0);
            var result = _stampDutyNsw.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        // Range 6: 1,212,001+ @ 5.5%
        [Test]
        public void CalculateCharges_AmountInSixthRange_ReturnsCorrectStampDuty()
        {
            double amount = 2000000;
            double expected = Math.Round(
                17000 * 0.0125 +
                19000 * 0.015 +
                61000 * 0.0175 +
                267000 * 0.035 +
                848000 * 0.045 +
                788000 * 0.055, 0);
            var result = _stampDutyNsw.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateCharges_LargeAmount_5m()
        {
            double amount = 5000000;
            double expected = Math.Round(
                17000 * 0.0125 +
                19000 * 0.015 +
                61000 * 0.0175 +
                267000 * 0.035 +
                848000 * 0.045 +
                3788000 * 0.055, 0);
            var result = _stampDutyNsw.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }

        [Test]
        public void CalculateCharges_Total_EqualsStampDutyPlusFees()
        {
            var result = _stampDutyNsw.CalculateCharges(500000);
            Assert.That(result.Total, Is.EqualTo(result.StampDuty + 166 + 166));
        }

        [Test]
        public void CalculateCharges_BoundaryAt17k()
        {
            double amount = 17000;
            double expected = Math.Round(17000 * 0.0125, 0);
            var result = _stampDutyNsw.CalculateCharges(amount);
            Assert.That(result.StampDuty, Is.EqualTo(expected));
        }
    }
}
