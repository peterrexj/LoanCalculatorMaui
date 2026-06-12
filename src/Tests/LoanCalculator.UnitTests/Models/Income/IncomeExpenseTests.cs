using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;

namespace LoanCalculator.UnitTests.Models.Income
{
    [TestFixture]
    public class IncomeExpenseTests
    {
        [SetUp]
        public void Setup()
        {
            Helper.CurrencySymbol = "$";
        }

        // ── AmountMonthly — all frequencies ──────────────────────────────────

        [Test]
        public void AmountMonthly_Monthly_ReturnsSameAmount()
        {
            var e = new IncomeExpense { Amount = 1200, Frequency = TimeFrequencyEnum.Monthly };
            Assert.That(e.AmountMonthly, Is.EqualTo(1200).Within(1));
        }

        [Test]
        public void AmountMonthly_Yearly_ReturnsTwelfth()
        {
            var e = new IncomeExpense { Amount = 12000, Frequency = TimeFrequencyEnum.Yearly };
            Assert.That(e.AmountMonthly, Is.EqualTo(1000).Within(1));
        }

        [Test]
        public void AmountMonthly_Weekly_Returns52Over12()
        {
            var e = new IncomeExpense { Amount = 100, Frequency = TimeFrequencyEnum.Weekly };
            // 100 * 52 / 12 ≈ 433
            Assert.That(e.AmountMonthly, Is.EqualTo(Math.Round(100 * 52.0 / 12)).Within(1));
        }

        [Test]
        public void AmountMonthly_Fortnightly_Returns26Over12()
        {
            var e = new IncomeExpense { Amount = 600, Frequency = TimeFrequencyEnum.Fortnightly };
            // 600 * 26 / 12 = 1300
            Assert.That(e.AmountMonthly, Is.EqualTo(1300).Within(1));
        }

        [Test]
        public void AmountMonthly_Quarter_Returns4Over12()
        {
            var e = new IncomeExpense { Amount = 3000, Frequency = TimeFrequencyEnum.Quarter };
            // 3000 * 4 / 12 = 1000
            Assert.That(e.AmountMonthly, Is.EqualTo(1000).Within(1));
        }

        [Test]
        public void AmountMonthly_BiYearly_Returns2Over12()
        {
            var e = new IncomeExpense { Amount = 6000, Frequency = TimeFrequencyEnum.BiYearly };
            // 6000 * 2 / 12 = 1000
            Assert.That(e.AmountMonthly, Is.EqualTo(1000).Within(1));
        }

        // ── AmountYearly — all frequencies ───────────────────────────────────

        [Test]
        public void AmountYearly_Monthly_ReturnsTwelveX()
        {
            var e = new IncomeExpense { Amount = 1000, Frequency = TimeFrequencyEnum.Monthly };
            Assert.That(e.AmountYearly, Is.EqualTo(12000).Within(1));
        }

        [Test]
        public void AmountYearly_Yearly_ReturnsSameAmount()
        {
            var e = new IncomeExpense { Amount = 60000, Frequency = TimeFrequencyEnum.Yearly };
            Assert.That(e.AmountYearly, Is.EqualTo(60000).Within(1));
        }

        [Test]
        public void AmountYearly_Weekly_Returns52X()
        {
            var e = new IncomeExpense { Amount = 100, Frequency = TimeFrequencyEnum.Weekly };
            Assert.That(e.AmountYearly, Is.EqualTo(100 * 52).Within(1));
        }

        [Test]
        public void AmountYearly_Fortnightly_Returns26X()
        {
            var e = new IncomeExpense { Amount = 500, Frequency = TimeFrequencyEnum.Fortnightly };
            Assert.That(e.AmountYearly, Is.EqualTo(500 * 26).Within(1));
        }

        // ── AmountWeekly ───────────────────────────────────────────────────────

        [Test]
        public void AmountWeekly_Weekly_ReturnsSameAmount()
        {
            var e = new IncomeExpense { Amount = 250, Frequency = TimeFrequencyEnum.Weekly };
            Assert.That(e.AmountWeekly, Is.EqualTo(250).Within(1));
        }

        [Test]
        public void AmountWeekly_Monthly_Returns12Over52()
        {
            var e = new IncomeExpense { Amount = 1300, Frequency = TimeFrequencyEnum.Monthly };
            // 1300 * 12 / 52 = 300
            Assert.That(e.AmountWeekly, Is.EqualTo(300).Within(1));
        }

        [Test]
        public void AmountWeekly_Fortnightly_ReturnsHalf()
        {
            var e = new IncomeExpense { Amount = 600, Frequency = TimeFrequencyEnum.Fortnightly };
            Assert.That(e.AmountWeekly, Is.EqualTo(300).Within(1));
        }

        [Test]
        public void AmountWeekly_Yearly_ReturnsDividedBy52()
        {
            var e = new IncomeExpense { Amount = 52000, Frequency = TimeFrequencyEnum.Yearly };
            Assert.That(e.AmountWeekly, Is.EqualTo(1000).Within(1));
        }

        // ── AmountFortnightly ─────────────────────────────────────────────────

        [Test]
        public void AmountFortnightly_Fortnightly_ReturnsSameAmount()
        {
            var e = new IncomeExpense { Amount = 800, Frequency = TimeFrequencyEnum.Fortnightly };
            Assert.That(e.AmountFortnightly, Is.EqualTo(800).Within(1));
        }

        [Test]
        public void AmountFortnightly_Monthly_Returns12Over26()
        {
            var e = new IncomeExpense { Amount = 1300, Frequency = TimeFrequencyEnum.Monthly };
            // 1300 * 12 / 26 = 600
            Assert.That(e.AmountFortnightly, Is.EqualTo(600).Within(1));
        }

        [Test]
        public void AmountFortnightly_Weekly_ReturnsDoubleWeekly()
        {
            var e = new IncomeExpense { Amount = 500, Frequency = TimeFrequencyEnum.Weekly };
            Assert.That(e.AmountFortnightly, Is.EqualTo(1000).Within(1));
        }

        // ── AmountString ──────────────────────────────────────────────────────

        [Test]
        public void AmountString_FormatsWithCurrencySymbol()
        {
            Helper.CurrencySymbol = "$";
            var e = new IncomeExpense { Amount = 1500 };
            Assert.That(e.AmountString, Does.StartWith("$"));
        }

        [Test]
        public void AmountString_FormatsWithCommaForLargeAmount()
        {
            var e = new IncomeExpense { Amount = 5000 };
            Assert.That(e.AmountString, Does.Contain("5,000"));
        }

        [Test]
        public void AmountString_ZeroAmount_ShowsZero()
        {
            var e = new IncomeExpense { Amount = 0 };
            Assert.That(e.AmountString, Does.EndWith("0"));
        }

        // ── TimeFrequencyIndex ────────────────────────────────────────────────

        [Test]
        public void TimeFrequencyIndex_Monthly_ReturnsCorrectIndex()
        {
            var e = new IncomeExpense { Frequency = TimeFrequencyEnum.Monthly };
            Assert.That(e.TimeFrequencyIndex, Is.EqualTo((int)TimeFrequencyEnum.Monthly));
        }

        [Test]
        public void TimeFrequencyIndex_Weekly_ReturnsCorrectIndex()
        {
            var e = new IncomeExpense { Frequency = TimeFrequencyEnum.Weekly };
            Assert.That(e.TimeFrequencyIndex, Is.EqualTo((int)TimeFrequencyEnum.Weekly));
        }

        [Test]
        public void TimeFrequencyIndex_Yearly_ReturnsCorrectIndex()
        {
            var e = new IncomeExpense { Frequency = TimeFrequencyEnum.Yearly };
            Assert.That(e.TimeFrequencyIndex, Is.EqualTo((int)TimeFrequencyEnum.Yearly));
        }

        // ── ModelHelper conversion consistency ────────────────────────────────

        [Test]
        public void MonthlyToYearly_ConsistentWith12Multiplier()
        {
            var e = new IncomeExpense { Amount = 1000, Frequency = TimeFrequencyEnum.Monthly };
            Assert.That(e.AmountYearly, Is.EqualTo(e.AmountMonthly * 12).Within(1));
        }

        [Test]
        public void WeeklyToFortnightly_ConsistentWithDoubling()
        {
            var e = new IncomeExpense { Amount = 200, Frequency = TimeFrequencyEnum.Weekly };
            Assert.That(e.AmountFortnightly, Is.EqualTo(e.AmountWeekly * 2).Within(1));
        }
    }
}
