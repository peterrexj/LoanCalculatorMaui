using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void CalculateCharges_AmountInFirstRange_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 10000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(125));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(3));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(457));
        }

        [Test]
        public void CalculateCharges_AmountInSecondRange_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 20000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(275));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(2));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(607));
        }

        [Test]
        public void CalculateCharges_AmountInThirdRange_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 50000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(875));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(2));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(1207));
        }

        [Test]
        public void CalculateCharges_AmountInFourthRange_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 100000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(2475));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(2));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(2807));
        }

        [Test]
        public void CalculateCharges_AmountInFifthRange_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 500000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(17875));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(2));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(18207));
        }

        [Test]
        public void CalculateCharges_AmountInSixthRange_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 2000000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(110875));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(2));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(111207));
        }

        [Test]
        public void CalculateCharges_AmountInSeventhRange_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 5000000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(330875));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(2));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(331207));
        }

        [Test]
        public void CalculateCharges_AmountAtBoundary_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 15000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(187.5));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(2));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(519.5));
        }

        [Test]
        public void CalculateCharges_AmountAtUpperBoundary_ReturnsCorrectStampDuty()
        {
            // Arrange
            double amount = 3268000;

            // Act
            var result = _stampDutyNsw.CalculateCharges(amount);

            // Assert
            Assert.That(result.StampDuty, Is.EqualTo(178875));
            Assert.That(result.ExpenseEntries.Count, Is.EqualTo(2));
            Assert.That(result.ExpenseEntries[0].Expense, Is.EqualTo(166));
            Assert.That(result.ExpenseEntries[1].Expense, Is.EqualTo(166));
            Assert.That(result.Total, Is.EqualTo(179207));
        }
    }
}
