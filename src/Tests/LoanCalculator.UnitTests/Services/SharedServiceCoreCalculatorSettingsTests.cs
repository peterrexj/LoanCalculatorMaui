using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Services
{
    /// <summary>
    /// Verifies the configurable calculator setting defaults returned by SharedServiceCore.
    ///
    /// In the test environment Preferences is unavailable (no MAUI platform), so every
    /// Get*() method falls into its try/catch and returns the hardcoded default.  These
    /// tests lock down those default values so an accidental change is caught immediately,
    /// and confirm the try/catch fallback is wired correctly.
    /// </summary>
    [TestFixture]
    public class SharedServiceCoreCalculatorSettingsTests
    {
        // ── Calculator defaults ───────────────────────────────────────────────

        [Test]
        public void GetMaxLoanTermYears_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetMaxLoanTermYears(), Is.EqualTo(30));

        [Test]
        public void GetDefaultInterestRate_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetDefaultInterestRate(), Is.EqualTo(5.0));

        [Test]
        public void GetDefaultLoanTermYears_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetDefaultLoanTermYears(), Is.EqualTo(30));

        [Test]
        public void GetDefaultProjectionYears_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetDefaultProjectionYears(), Is.EqualTo(10));

        [Test]
        public void GetMaxProjectionYears_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetMaxProjectionYears(), Is.EqualTo(25));

        // ── What-If starting values ───────────────────────────────────────────

        [Test]
        public void GetWhatIfRateDelta_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetWhatIfRateDelta(), Is.EqualTo(0.5));

        [Test]
        public void GetWhatIfExtraRepayment_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetWhatIfExtraRepayment(), Is.EqualTo(500.0));

        [Test]
        public void GetWhatIfLumpSum_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetWhatIfLumpSum(), Is.EqualTo(10000.0));

        [Test]
        public void GetWhatIfOffsetBalance_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetWhatIfOffsetBalance(), Is.EqualTo(20000.0));

        // ── Stepper increments ────────────────────────────────────────────────

        [Test]
        public void GetLoanRateStep_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetLoanRateStep(), Is.EqualTo(0.05));

        [Test]
        public void GetWhatIfRateStep_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetWhatIfRateStep(), Is.EqualTo(0.25));

        [Test]
        public void GetWhatIfMonthlyStep_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetWhatIfMonthlyStep(), Is.EqualTo(100.0));

        [Test]
        public void GetWhatIfLumpSumStep_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetWhatIfLumpSumStep(), Is.EqualTo(5000.0));

        [Test]
        public void GetWhatIfOffsetStep_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetWhatIfOffsetStep(), Is.EqualTo(5000.0));

        [Test]
        public void GetGrowthRateStep_ReturnsDefault()
            => Assert.That(SharedServiceCore.GetGrowthRateStep(), Is.EqualTo(0.5));

        // ── Range sanity checks ───────────────────────────────────────────────
        // Defaults must sit inside their own valid ranges; if someone changes a default
        // to an out-of-range value the corresponding Set* would immediately clamp it.

        [Test]
        public void GetMaxLoanTermYears_IsWithinValidRange()
            => Assert.That(SharedServiceCore.GetMaxLoanTermYears(), Is.InRange(10, 50));

        [Test]
        public void GetDefaultLoanTermYears_DoesNotExceedMaxLoanTermYears()
            => Assert.That(
                SharedServiceCore.GetDefaultLoanTermYears(),
                Is.LessThanOrEqualTo(SharedServiceCore.GetMaxLoanTermYears()));

        [Test]
        public void GetDefaultLoanTermYears_IsAtLeastOne()
            => Assert.That(SharedServiceCore.GetDefaultLoanTermYears(), Is.GreaterThanOrEqualTo(1));

        [Test]
        public void GetDefaultInterestRate_IsPositive()
            => Assert.That(SharedServiceCore.GetDefaultInterestRate(), Is.GreaterThan(0));

        [Test]
        public void GetDefaultInterestRate_IsWithinValidRange()
            => Assert.That(SharedServiceCore.GetDefaultInterestRate(), Is.InRange(0.01, 30.0));

        [Test]
        public void GetDefaultProjectionYears_IsAtLeastOne()
            => Assert.That(SharedServiceCore.GetDefaultProjectionYears(), Is.GreaterThanOrEqualTo(1));

        [Test]
        public void GetDefaultProjectionYears_DoesNotExceedMaxProjectionYears()
            => Assert.That(
                SharedServiceCore.GetDefaultProjectionYears(),
                Is.LessThanOrEqualTo(SharedServiceCore.GetMaxProjectionYears()));

        [Test]
        public void GetMaxProjectionYears_IsWithinValidRange()
            => Assert.That(SharedServiceCore.GetMaxProjectionYears(), Is.InRange(5, 50));

        [Test]
        public void GetWhatIfRateDelta_IsPositive()
            => Assert.That(SharedServiceCore.GetWhatIfRateDelta(), Is.GreaterThan(0));

        [Test]
        public void GetWhatIfExtraRepayment_IsPositive()
            => Assert.That(SharedServiceCore.GetWhatIfExtraRepayment(), Is.GreaterThan(0));

        [Test]
        public void GetWhatIfLumpSum_IsPositive()
            => Assert.That(SharedServiceCore.GetWhatIfLumpSum(), Is.GreaterThan(0));

        [Test]
        public void GetWhatIfOffsetBalance_IsNonNegative()
            => Assert.That(SharedServiceCore.GetWhatIfOffsetBalance(), Is.GreaterThanOrEqualTo(0));

        [Test]
        public void AllStepSizes_ArePositive()
        {
            Assert.That(SharedServiceCore.GetLoanRateStep(), Is.GreaterThan(0), "LoanRateStep");
            Assert.That(SharedServiceCore.GetWhatIfRateStep(), Is.GreaterThan(0), "WhatIfRateStep");
            Assert.That(SharedServiceCore.GetWhatIfMonthlyStep(), Is.GreaterThan(0), "WhatIfMonthlyStep");
            Assert.That(SharedServiceCore.GetWhatIfLumpSumStep(), Is.GreaterThan(0), "WhatIfLumpSumStep");
            Assert.That(SharedServiceCore.GetWhatIfOffsetStep(), Is.GreaterThan(0), "WhatIfOffsetStep");
            Assert.That(SharedServiceCore.GetGrowthRateStep(), Is.GreaterThan(0), "GrowthRateStep");
        }

        [Test]
        public void LoanRateStep_IsWithinValidRange()
            => Assert.That(SharedServiceCore.GetLoanRateStep(), Is.InRange(0.01, 1.0));

        [Test]
        public void WhatIfRateStep_IsWithinValidRange()
            => Assert.That(SharedServiceCore.GetWhatIfRateStep(), Is.InRange(0.01, 1.0));

        [Test]
        public void GrowthRateStep_IsWithinValidRange()
            => Assert.That(SharedServiceCore.GetGrowthRateStep(), Is.InRange(0.1, 5.0));
    }
}
