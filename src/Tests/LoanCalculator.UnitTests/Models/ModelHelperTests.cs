using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.UnitTests.Models
{
    [TestFixture]
    public class ModelHelperTests
    {
        private const double Amount = 1000;
        private const double Delta = 1.0; // Math.Round to 0 decimals

        // ── ConvertAmountToYearlyFrequency ───────────────────────────────────

        [Test]
        public void ToYearly_FromYearly_ReturnsSameAmount()
            => Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(Amount, TimeFrequencyEnum.Yearly), Is.EqualTo(1000));

        [Test]
        public void ToYearly_FromBiYearly_DoublesAmount()
            => Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(Amount, TimeFrequencyEnum.BiYearly), Is.EqualTo(2000));

        [Test]
        public void ToYearly_FromQuarter_MultipliesBy4()
            => Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(Amount, TimeFrequencyEnum.Quarter), Is.EqualTo(4000));

        [Test]
        public void ToYearly_FromFortnightly_MultipliesBy26()
            => Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(Amount, TimeFrequencyEnum.Fortnightly), Is.EqualTo(26000));

        [Test]
        public void ToYearly_FromMonthly_MultipliesBy12()
            => Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(Amount, TimeFrequencyEnum.Monthly), Is.EqualTo(12000));

        [Test]
        public void ToYearly_FromWeekly_MultipliesBy52()
            => Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(Amount, TimeFrequencyEnum.Weekly), Is.EqualTo(52000));

        [Test]
        public void ToYearly_FromDaily_MultipliesBy5x52()
            => Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(Amount, TimeFrequencyEnum.Daily), Is.EqualTo(260000));

        [Test]
        public void ToYearly_FromHourly_MultipliesBy8x5x52()
            => Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(Amount, TimeFrequencyEnum.Hourly), Is.EqualTo(2080000));

        // ── ConvertAmountToMonthlyFrequency ──────────────────────────────────

        [Test]
        public void ToMonthly_FromYearly_DividesBy12()
            => Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(12000, TimeFrequencyEnum.Yearly), Is.EqualTo(1000));

        [Test]
        public void ToMonthly_FromBiYearly_IsDoubledOverTwelve()
        {
            // (amount * 2) / 12 = (600 * 2) / 12 = 100
            Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(600, TimeFrequencyEnum.BiYearly), Is.EqualTo(100));
        }

        [Test]
        public void ToMonthly_FromQuarter_IsQuadrupledOverTwelve()
        {
            // (300 * 4) / 12 = 100
            Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(300, TimeFrequencyEnum.Quarter), Is.EqualTo(100));
        }

        [Test]
        public void ToMonthly_FromFortnightly_IsFortnightlyOverTwelveScaled()
        {
            // (amount * 26) / 12
            double expected = Math.Round((1000 * 26.0) / 12, 0);
            Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(1000, TimeFrequencyEnum.Fortnightly), Is.EqualTo(expected));
        }

        [Test]
        public void ToMonthly_FromMonthly_ReturnsSameAmount()
            => Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(Amount, TimeFrequencyEnum.Monthly), Is.EqualTo(1000));

        [Test]
        public void ToMonthly_FromWeekly_IsWeeklyScaledBy52Over12()
        {
            double expected = Math.Round((1000 * 52.0) / 12, 0);
            Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(1000, TimeFrequencyEnum.Weekly), Is.EqualTo(expected));
        }

        [Test]
        public void ToMonthly_FromDaily_IncludesWorkingDays()
        {
            // (amount * 5 * 52) / 12
            double expected = Math.Round((1000 * 5.0 * 52) / 12, 0);
            Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(1000, TimeFrequencyEnum.Daily), Is.EqualTo(expected));
        }

        [Test]
        public void ToMonthly_FromHourly_IncludesWorkingHoursAndDays()
        {
            // (amount * 8 * 5 * 52) / 12
            double expected = Math.Round((100 * 8.0 * 5 * 52) / 12, 0);
            Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(100, TimeFrequencyEnum.Hourly), Is.EqualTo(expected));
        }

        // ── ConvertAmountToWeeklyFrequency ───────────────────────────────────

        [Test]
        public void ToWeekly_FromYearly_DividesBy52()
            => Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(52000, TimeFrequencyEnum.Yearly), Is.EqualTo(1000));

        [Test]
        public void ToWeekly_FromBiYearly_IsDoubledOver52()
        {
            // (500 * 2) / 52 ≈ 19
            double expected = Math.Round((500 * 2.0) / 52, 0);
            Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(500, TimeFrequencyEnum.BiYearly), Is.EqualTo(expected));
        }

        [Test]
        public void ToWeekly_FromQuarter_IsQuadrupledOver52()
        {
            double expected = Math.Round((500 * 4.0) / 52, 0);
            Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(500, TimeFrequencyEnum.Quarter), Is.EqualTo(expected));
        }

        [Test]
        public void ToWeekly_FromFortnightly_HalvesAmount()
            => Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(2000, TimeFrequencyEnum.Fortnightly), Is.EqualTo(1000));

        [Test]
        public void ToWeekly_FromMonthly_IsMonthlyScaledBy12Over52()
        {
            double expected = Math.Round((1000 * 12.0) / 52, 0);
            Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(1000, TimeFrequencyEnum.Monthly), Is.EqualTo(expected));
        }

        [Test]
        public void ToWeekly_FromWeekly_ReturnsSameAmount()
            => Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(Amount, TimeFrequencyEnum.Weekly), Is.EqualTo(1000));

        [Test]
        public void ToWeekly_FromDaily_MultipliesBy5()
            => Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(200, TimeFrequencyEnum.Daily), Is.EqualTo(1000));

        [Test]
        public void ToWeekly_FromHourly_MultipliesBy8x5()
            => Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(25, TimeFrequencyEnum.Hourly), Is.EqualTo(1000));

        // ── ConvertAmountToFortnightlyFrequency ──────────────────────────────

        [Test]
        public void ToFortnightly_FromYearly_DividesBy26()
            => Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(26000, TimeFrequencyEnum.Yearly), Is.EqualTo(1000));

        [Test]
        public void ToFortnightly_FromBiYearly_IsDoubledOver26()
        {
            double expected = Math.Round((500 * 2.0) / 26, 0);
            Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(500, TimeFrequencyEnum.BiYearly), Is.EqualTo(expected));
        }

        [Test]
        public void ToFortnightly_FromQuarter_IsQuadrupledOver26()
        {
            double expected = Math.Round((500 * 4.0) / 26, 0);
            Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(500, TimeFrequencyEnum.Quarter), Is.EqualTo(expected));
        }

        [Test]
        public void ToFortnightly_FromFortnightly_ReturnsSameAmount()
            => Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(Amount, TimeFrequencyEnum.Fortnightly), Is.EqualTo(1000));

        [Test]
        public void ToFortnightly_FromMonthly_IsMonthlyScaledBy12Over26()
        {
            double expected = Math.Round((1000 * 12.0) / 26, 0);
            Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(1000, TimeFrequencyEnum.Monthly), Is.EqualTo(expected));
        }

        [Test]
        public void ToFortnightly_FromWeekly_DoublesAmount()
            => Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(500, TimeFrequencyEnum.Weekly), Is.EqualTo(1000));

        [Test]
        public void ToFortnightly_FromDaily_MultipliesBy5x2()
            => Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(100, TimeFrequencyEnum.Daily), Is.EqualTo(1000));

        [Test]
        public void ToFortnightly_FromHourly_MultipliesBy8x5x2()
            => Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(12.5, TimeFrequencyEnum.Hourly), Is.EqualTo(1000));

        // ── Round-trip consistency ───────────────────────────────────────────

        [Test]
        public void RoundTrip_MonthlyToYearlyAndBack_IsConsistent()
        {
            double monthly = 2000;
            double yearly = ModelHelper.ConvertAmountToYearlyFrequency(monthly, TimeFrequencyEnum.Monthly);
            double backToMonthly = ModelHelper.ConvertAmountToMonthlyFrequency(yearly, TimeFrequencyEnum.Yearly);
            Assert.That(backToMonthly, Is.EqualTo(monthly).Within(Delta));
        }

        [Test]
        public void RoundTrip_WeeklyToYearlyAndBack_IsConsistent()
        {
            double weekly = 500;
            double yearly = ModelHelper.ConvertAmountToYearlyFrequency(weekly, TimeFrequencyEnum.Weekly);
            double backToWeekly = ModelHelper.ConvertAmountToWeeklyFrequency(yearly, TimeFrequencyEnum.Yearly);
            Assert.That(backToWeekly, Is.EqualTo(weekly).Within(Delta));
        }

        [Test]
        public void RoundTrip_FortnightlyToYearlyAndBack_IsConsistent()
        {
            double fortnightly = 800;
            double yearly = ModelHelper.ConvertAmountToYearlyFrequency(fortnightly, TimeFrequencyEnum.Fortnightly);
            double backToFort = ModelHelper.ConvertAmountToFortnightlyFrequency(yearly, TimeFrequencyEnum.Yearly);
            Assert.That(backToFort, Is.EqualTo(fortnightly).Within(Delta));
        }

        [Test]
        public void ZeroAmount_AllFrequencies_ReturnsZero()
        {
            foreach (TimeFrequencyEnum freq in Enum.GetValues(typeof(TimeFrequencyEnum)))
            {
                Assert.That(ModelHelper.ConvertAmountToMonthlyFrequency(0, freq), Is.EqualTo(0), $"Monthly from {freq}");
                Assert.That(ModelHelper.ConvertAmountToYearlyFrequency(0, freq), Is.EqualTo(0), $"Yearly from {freq}");
                Assert.That(ModelHelper.ConvertAmountToWeeklyFrequency(0, freq), Is.EqualTo(0), $"Weekly from {freq}");
                Assert.That(ModelHelper.ConvertAmountToFortnightlyFrequency(0, freq), Is.EqualTo(0), $"Fortnightly from {freq}");
            }
        }
    }
}
