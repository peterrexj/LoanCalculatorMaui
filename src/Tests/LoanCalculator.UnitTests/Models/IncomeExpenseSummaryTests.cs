using LoanCalculator.Core.Models.Income.Summary;

namespace LoanCalculator.UnitTests.Models
{
    [TestFixture]
    public class IncomeExpenseSummaryTests
    {
        // ── AnnualGrowthRatePercentage ────────────────────────────────────────

        [Test]
        public void AnnualGrowthRatePercentage_CollapsesHalfPointSteps_SoNotUsableForDisplay()
        {
            // Regression guard: the Budget growth-rate +/- buttons step by 0.5. Rounding the
            // maths fraction to 2 decimals makes 2.0 and 2.5 render identically, which made
            // every other tap look like it did nothing. Labels must show AnnualGrowthRate.
            var at2 = new IncomeExpenseSummary { AnnualGrowthRate = 2.0 };
            var at2Point5 = new IncomeExpenseSummary { AnnualGrowthRate = 2.5 };

            Assert.That(at2.AnnualGrowthRatePercentage,
                Is.EqualTo(at2Point5.AnnualGrowthRatePercentage),
                "the fraction collapses across a 0.5 step — this is why it must not be displayed");

            Assert.That($"{at2.AnnualGrowthRate:0.##}%",
                Is.Not.EqualTo($"{at2Point5.AnnualGrowthRate:0.##}%"),
                "the displayed value must advance on every tap");
        }

        [Test]
        public void AnnualGrowthRatePercentage_ConvertsFromHundredBasis()
        {
            var summary = new IncomeExpenseSummary { AnnualGrowthRate = 5 };
            Assert.That(summary.AnnualGrowthRatePercentage, Is.EqualTo(0.05).Within(0.001));
        }

        [Test]
        public void AnnualGrowthRatePercentage_Zero_ReturnsZero()
        {
            var summary = new IncomeExpenseSummary { AnnualGrowthRate = 0 };
            Assert.That(summary.AnnualGrowthRatePercentage, Is.EqualTo(0));
        }

        [Test]
        public void AnnualGrowthRatePercentage_IsRoundedToTwoDecimals()
        {
            var summary = new IncomeExpenseSummary { AnnualGrowthRate = 3.333 };
            // Math.Round(3.333/100, 2) = 0.03
            Assert.That(summary.AnnualGrowthRatePercentage, Is.EqualTo(0.03).Within(0.001));
        }

        // ── TotalWeekly ───────────────────────────────────────────────────────

        [Test]
        public void TotalWeekly_DerivedfromTotalYearly()
        {
            var summary = new IncomeExpenseSummary { TotalYearly = 52000 };
            Assert.That(summary.TotalWeekly, Is.EqualTo(1000));
        }

        [Test]
        public void TotalWeekly_Zero_ReturnsZero()
        {
            var summary = new IncomeExpenseSummary { TotalYearly = 0 };
            Assert.That(summary.TotalWeekly, Is.EqualTo(0));
        }

        [Test]
        public void TotalWeekly_MatchesModelHelperOutput()
        {
            double yearly = 80000;
            var summary = new IncomeExpenseSummary { TotalYearly = yearly };
            double expected = Core.Models.ModelHelper.ConvertAmountToWeeklyFrequency(yearly, Core.Models.Enums.TimeFrequencyEnum.Yearly);
            Assert.That(summary.TotalWeekly, Is.EqualTo(expected));
        }

        // ── TotalFortnightly ──────────────────────────────────────────────────

        [Test]
        public void TotalFortnightly_DerivedfromTotalYearly()
        {
            var summary = new IncomeExpenseSummary { TotalYearly = 26000 };
            Assert.That(summary.TotalFortnightly, Is.EqualTo(1000));
        }

        [Test]
        public void TotalFortnightly_Zero_ReturnsZero()
        {
            var summary = new IncomeExpenseSummary { TotalYearly = 0 };
            Assert.That(summary.TotalFortnightly, Is.EqualTo(0));
        }

        // ── TotalMonthlyWithComma / TotalYearlyWithComma ──────────────────────

        [Test]
        public void TotalMonthlyWithComma_FormatsWithThousandsSeparator()
        {
            var summary = new IncomeExpenseSummary { TotalMonthly = 3500 };
            Assert.That(summary.TotalMonthlyWithComma, Is.EqualTo("3,500"));
        }

        [Test]
        public void TotalYearlyWithComma_FormatsWithThousandsSeparator()
        {
            var summary = new IncomeExpenseSummary { TotalYearly = 120000 };
            Assert.That(summary.TotalYearlyWithComma, Is.EqualTo("120,000"));
        }

        // ── ProjectTotalYearly ────────────────────────────────────────────────

        [Test]
        public void ProjectTotalYearly_NullProjectionTerms_ReturnsZero()
        {
            var summary = new IncomeExpenseSummary();
            Assert.That(summary.ProjectTotalYearly, Is.EqualTo(0));
        }

        [Test]
        public void ProjectTotalYearly_ReturnsLastTermIncomeExpenseAmount()
        {
            var summary = new IncomeExpenseSummary
            {
                ProjectionTerms = new List<IncomeExpenseProjectionOutput>
                {
                    new IncomeExpenseProjectionOutput { IncomeExpenseAmount = 50000 },
                    new IncomeExpenseProjectionOutput { IncomeExpenseAmount = 75000 }
                }
            };
            Assert.That(summary.ProjectTotalYearly, Is.EqualTo(75000));
        }
    }
}
