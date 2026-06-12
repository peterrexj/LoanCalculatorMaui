using LoanCalculator.Core.Models;

namespace LoanCalculator.UnitTests.Models
{
    [TestFixture]
    public class PaymentOutputTests
    {
        private static PaymentOutput Make(double termPayment, int paymentsPerYear) =>
            new PaymentOutput { TermPayment = termPayment, TotalNumberPaymentPerYear = paymentsPerYear };

        // ── TermPaymentWeekly ────────────────────────────────────────────────

        [Test]
        public void TermPaymentWeekly_Monthly12_ConvertsFromMonthly()
        {
            var p = Make(2000, 12);
            double expected = ModelHelper.ConvertAmountToWeeklyFrequency(2000, Core.Models.Enums.TimeFrequencyEnum.Monthly).Round2();
            Assert.That(p.TermPaymentWeekly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentWeekly_Fortnightly24_ConvertsFromFortnightly()
        {
            var p = Make(1000, 24);
            double expected = ModelHelper.ConvertAmountToWeeklyFrequency(1000, Core.Models.Enums.TimeFrequencyEnum.Fortnightly).Round2();
            Assert.That(p.TermPaymentWeekly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentWeekly_Weekly52_FallsThroughToRounded()
        {
            var p = Make(500, 52);
            Assert.That(p.TermPaymentWeekly, Is.EqualTo(p.TermPaymentRounded));
        }

        [Test]
        public void TermPaymentWeekly_Unknown_FallsThroughToRounded()
        {
            var p = Make(800, 26);
            Assert.That(p.TermPaymentWeekly, Is.EqualTo(p.TermPaymentRounded));
        }

        // ── TermPaymentFortnightly ───────────────────────────────────────────

        [Test]
        public void TermPaymentFortnightly_Monthly12_ConvertsFromMonthly()
        {
            var p = Make(2000, 12);
            double expected = ModelHelper.ConvertAmountToFortnightlyFrequency(2000, Core.Models.Enums.TimeFrequencyEnum.Monthly).Round2();
            Assert.That(p.TermPaymentFortnightly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentFortnightly_Weekly52_ConvertsFromWeekly()
        {
            var p = Make(500, 52);
            double expected = ModelHelper.ConvertAmountToFortnightlyFrequency(500, Core.Models.Enums.TimeFrequencyEnum.Weekly).Round2();
            Assert.That(p.TermPaymentFortnightly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentFortnightly_Fortnightly24_FallsThroughToRounded()
        {
            var p = Make(1000, 24);
            Assert.That(p.TermPaymentFortnightly, Is.EqualTo(p.TermPaymentRounded));
        }

        [Test]
        public void TermPaymentFortnightly_Unknown_FallsThroughToRounded()
        {
            var p = Make(900, 26);
            Assert.That(p.TermPaymentFortnightly, Is.EqualTo(p.TermPaymentRounded));
        }

        // ── TermPaymentMonthly ───────────────────────────────────────────────

        [Test]
        public void TermPaymentMonthly_Fortnightly24_ConvertsFromFortnightly()
        {
            var p = Make(1000, 24);
            double expected = ModelHelper.ConvertAmountToMonthlyFrequency(1000, Core.Models.Enums.TimeFrequencyEnum.Fortnightly).Round2();
            Assert.That(p.TermPaymentMonthly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentMonthly_Weekly52_ConvertsFromWeekly()
        {
            var p = Make(500, 52);
            double expected = ModelHelper.ConvertAmountToMonthlyFrequency(500, Core.Models.Enums.TimeFrequencyEnum.Weekly).Round2();
            Assert.That(p.TermPaymentMonthly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentMonthly_Monthly12_FallsThroughToRounded()
        {
            var p = Make(2000, 12);
            Assert.That(p.TermPaymentMonthly, Is.EqualTo(p.TermPaymentRounded));
        }

        // ── TermPaymentYearly ────────────────────────────────────────────────

        [Test]
        public void TermPaymentYearly_Monthly12_ConvertsFromMonthly()
        {
            var p = Make(2000, 12);
            double expected = ModelHelper.ConvertAmountToYearlyFrequency(2000, Core.Models.Enums.TimeFrequencyEnum.Monthly).Round2();
            Assert.That(p.TermPaymentYearly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentYearly_Fortnightly24_ConvertsFromFortnightly()
        {
            var p = Make(1000, 24);
            double expected = ModelHelper.ConvertAmountToYearlyFrequency(1000, Core.Models.Enums.TimeFrequencyEnum.Fortnightly).Round2();
            Assert.That(p.TermPaymentYearly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentYearly_Weekly52_ConvertsFromWeekly()
        {
            var p = Make(500, 52);
            double expected = ModelHelper.ConvertAmountToYearlyFrequency(500, Core.Models.Enums.TimeFrequencyEnum.Weekly).Round2();
            Assert.That(p.TermPaymentYearly, Is.EqualTo(expected));
        }

        [Test]
        public void TermPaymentYearly_Unknown_FallsThroughToRounded()
        {
            var p = Make(700, 26);
            Assert.That(p.TermPaymentYearly, Is.EqualTo(p.TermPaymentRounded));
        }

        // ── Rounded helpers ──────────────────────────────────────────────────

        [Test]
        public void TermPaymentRounded_RoundsTwoDecimals()
        {
            var p = Make(1234.5678, 12);
            Assert.That(p.TermPaymentRounded, Is.EqualTo(1234.57).Within(0.001));
        }

        [Test]
        public void TotalPaymentRounded_RoundsTwoDecimals()
        {
            var p = new PaymentOutput { TotalPayment = 999999.999 };
            Assert.That(p.TotalPaymentRounded, Is.EqualTo(1000000.0).Within(0.001));
        }

        [Test]
        public void TotalInterestPaymentRounded_RoundsTwoDecimals()
        {
            var p = new PaymentOutput { TotalInterestPayment = 123456.7891 };
            Assert.That(p.TotalInterestPaymentRounded, Is.EqualTo(123456.79).Within(0.001));
        }

        // ── WithComma string properties ──────────────────────────────────────

        [Test]
        public void TermPaymentYearlyWithComma_FormatsWithThousandsSeparator()
        {
            var p = Make(2000, 12); // yearly = 24,000
            Assert.That(p.TermPaymentYearlyWithComma, Does.Contain(","));
        }

        [Test]
        public void TermPaymentRoundedWithComma_FormatsWithThousandsSeparator()
        {
            var p = Make(2997.75, 12);
            Assert.That(p.TermPaymentRoundedWithComma, Does.Contain(","));
        }
    }
}
