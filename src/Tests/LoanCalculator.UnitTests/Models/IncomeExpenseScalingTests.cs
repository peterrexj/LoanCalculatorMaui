using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;

namespace LoanCalculator.UnitTests.Models
{
    /// <summary>
    /// Covers <see cref="IncomeExpenseScaling.ScaleToNet"/>, the "income after expenses"
    /// breakdown the PDF draws. Written before the generator was switched over to it, so these
    /// are the guard for that change — the PDF has no other automated coverage.
    /// </summary>
    [TestFixture]
    public class IncomeExpenseScalingTests
    {
        private static IncomeExpenseBase BuildIncome()
        {
            var records = new Incomes { IncomeExpenseEntries = [] };
            records.Add("Salary", 6_000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            records.Add("Rental", 2_000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            records.SumUpData();
            return records;
        }

        [Test]
        public void ScaleToNet_DoesNotTouchTheSource()
        {
            var source = BuildIncome();

            source.ScaleToNet(netMonthly: 3_000, netYearly: 36_000);

            Assert.Multiple(() =>
            {
                Assert.That(source.IncomeExpenseSummary.TotalMonthly, Is.EqualTo(8_000).Within(0.01),
                    "the live records must not be mutated — that was the old getter's defect");
                Assert.That(source.IncomeExpenseSummary.TotalYearly, Is.EqualTo(96_000).Within(0.01));
                Assert.That(source.IncomeExpenseEntries!.Sum(e => e.Amount), Is.EqualTo(8_000).Within(0.01));
            });
        }

        /// <summary>
        /// The invariant that matters for the rendered table: the "Total" row comes from the
        /// summary while the body rows come from the entries, so the two must agree.
        /// </summary>
        [Test]
        public void ScaleToNet_SummaryTotalAgreesWithTheRows()
        {
            var copy = BuildIncome().ScaleToNet(netMonthly: 3_000, netYearly: 36_000)!;

            Assert.Multiple(() =>
            {
                Assert.That(copy.IncomeExpenseEntries!.Sum(e => e.AmountYearly), Is.EqualTo(36_000).Within(0.01),
                    "rows must sum to the net yearly total");
                Assert.That(copy.IncomeExpenseSummary.TotalYearly, Is.EqualTo(36_000).Within(0.01));
                Assert.That(copy.IncomeExpenseSummary.TotalMonthly, Is.EqualTo(3_000).Within(0.01));
            });
        }

        [Test]
        public void ScaleToNet_ApportionsByEachEntrysShareOfTheGross()
        {
            var copy = BuildIncome().ScaleToNet(netMonthly: 4_000, netYearly: 48_000)!;

            // Salary was 6000/8000 = 75%, Rental 2000/8000 = 25%.
            var salary = copy.IncomeExpenseEntries!.First(e => e.Name == "Salary");
            var rental = copy.IncomeExpenseEntries!.First(e => e.Name == "Rental");

            Assert.Multiple(() =>
            {
                Assert.That(salary.AmountYearly, Is.EqualTo(36_000).Within(0.01));
                Assert.That(rental.AmountYearly, Is.EqualTo(12_000).Within(0.01));
                Assert.That(salary.Frequency, Is.EqualTo(TimeFrequencyEnum.Yearly));
            });
        }

        /// <summary>
        /// Derived frequencies hang off <c>TotalYearly</c>, so they must reflect the net too —
        /// they are two of the five cells in the table's Total row.
        /// </summary>
        [Test]
        public void ScaleToNet_DerivedWeeklyAndFortnightlyFollowTheNet()
        {
            var copy = BuildIncome().ScaleToNet(netMonthly: 3_000, netYearly: 36_000)!;

            // ModelHelper rounds these to whole units, so 36000/52 prints as 692, not 692.31.
            Assert.Multiple(() =>
            {
                Assert.That(copy.IncomeExpenseSummary.TotalWeekly,
                    Is.EqualTo(Math.Round(36_000d / 52)).Within(0.01));
                Assert.That(copy.IncomeExpenseSummary.TotalFortnightly,
                    Is.EqualTo(Math.Round(36_000d / 26)).Within(0.01));
            });
        }

        /// <summary>
        /// A negative net is the normal case this table exists to show (expenses exceeding
        /// income), and it must not be clamped or sign-flipped.
        /// </summary>
        [Test]
        public void ScaleToNet_NegativeNet_KeepsTheSignOnRowsAndTotal()
        {
            var copy = BuildIncome().ScaleToNet(netMonthly: -1_500, netYearly: -18_000)!;

            Assert.Multiple(() =>
            {
                Assert.That(copy.IncomeExpenseSummary.TotalYearly, Is.EqualTo(-18_000).Within(0.01));
                Assert.That(copy.IncomeExpenseEntries!.Sum(e => e.AmountYearly), Is.EqualTo(-18_000).Within(0.01));
                Assert.That(copy.IncomeExpenseEntries!.All(e => e.Amount < 0), Is.True);
            });
        }

        [Test]
        public void ScaleToNet_NoEntries_ReturnsACopyAndDoesNotThrow()
        {
            var empty = new Incomes { IncomeExpenseEntries = [] };

            var copy = empty.ScaleToNet(netMonthly: 100, netYearly: 1_200);

            Assert.That(copy, Is.Not.Null);
            Assert.That(copy!.IncomeExpenseEntries, Is.Empty);
        }

        [Test]
        public void ScaleToNet_NullSource_ReturnsNull() =>
            Assert.That(((IncomeExpenseBase?)null).ScaleToNet(1, 12), Is.Null);
    }
}
