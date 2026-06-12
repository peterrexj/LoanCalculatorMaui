using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;

namespace LoanCalculator.UnitTests.Models.Enums
{
    [TestFixture]
    public class EnumHelperTests
    {
        // ── EnumHelper<TimeFrequencyEnum> via ToIndex / FromIndex ────────────

        [Test]
        public void ToIndex_Weekly_ReturnsCorrectIndex()
        {
            int idx = EnumHelper<TimeFrequencyEnum>.ToIndex(TimeFrequencyEnum.Weekly);
            Assert.That(idx, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void FromIndex_RoundTrips_AllFrequencies()
        {
            foreach (TimeFrequencyEnum freq in Enum.GetValues(typeof(TimeFrequencyEnum)))
            {
                int idx = EnumHelper<TimeFrequencyEnum>.ToIndex(freq);
                var back = EnumHelper<TimeFrequencyEnum>.FromIndex(idx);
                Assert.That(back, Is.EqualTo(freq), $"Round-trip failed for {freq}");
            }
        }

        [Test]
        public void FromString_ValidName_ReturnsMember()
        {
            var result = EnumHelper<TimeFrequencyEnum>.FromString("Monthly");
            Assert.That(result, Is.EqualTo(TimeFrequencyEnum.Monthly));
        }

        [Test]
        public void FromString_Null_ReturnsDefault()
        {
            var result = EnumHelper<TimeFrequencyEnum>.FromString(null);
            Assert.That(result, Is.EqualTo(default(TimeFrequencyEnum)));
        }

        [Test]
        public void List_ContainsAllMembers()
        {
            var names = EnumHelper<TimeFrequencyEnum>.List;
            var expected = Enum.GetNames(typeof(TimeFrequencyEnum));
            Assert.That(names, Is.EquivalentTo(expected));
        }

        // ── IncomeExpenseHelper ───────────────────────────────────────────────

        [Test]
        public void TimeFrequencyToIndex_RoundTrips_AllFrequencies()
        {
            foreach (TimeFrequencyEnum freq in Enum.GetValues(typeof(TimeFrequencyEnum)))
            {
                int idx = IncomeExpenseHelper.TimeFrequencyToIndex(freq);
                var back = IncomeExpenseHelper.TimeFrequencyFromIndex(idx);
                Assert.That(back, Is.EqualTo(freq));
            }
        }

        [Test]
        public void TimeFrequencyFromString_Monthly_ReturnsMonthly()
        {
            var result = IncomeExpenseHelper.TimeFrequencyFromString("Monthly");
            Assert.That(result, Is.EqualTo(TimeFrequencyEnum.Monthly));
        }

        [Test]
        public void TimeFrequencyFromString_Weekly_ReturnsWeekly()
        {
            var result = IncomeExpenseHelper.TimeFrequencyFromString("Weekly");
            Assert.That(result, Is.EqualTo(TimeFrequencyEnum.Weekly));
        }

        [Test]
        public void TimeFrequencies_CountMatchesEnumValues()
        {
            var list = IncomeExpenseHelper.TimeFrequencies;
            Assert.That(list.Count, Is.EqualTo(Enum.GetNames(typeof(TimeFrequencyEnum)).Length));
        }

        [Test]
        public void TimeFrequencies_ContainsAllNames()
        {
            var list = IncomeExpenseHelper.TimeFrequencies;
            Assert.That(list, Contains.Item("Monthly"));
            Assert.That(list, Contains.Item("Weekly"));
            Assert.That(list, Contains.Item("Fortnightly"));
            Assert.That(list, Contains.Item("Yearly"));
        }

        [Test]
        public void TimeFrequencyToIndex_Monthly_IsConsistentWithFromIndex()
        {
            int idx = IncomeExpenseHelper.TimeFrequencyToIndex(TimeFrequencyEnum.Monthly);
            Assert.That(IncomeExpenseHelper.TimeFrequencyFromIndex(idx), Is.EqualTo(TimeFrequencyEnum.Monthly));
        }
    }
}
