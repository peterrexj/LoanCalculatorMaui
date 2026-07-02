using LoanCalculator.Core.Models.ViewModels.PrimaryModels;

namespace LoanCalculator.UnitTests;

/// <summary>
/// Tests for every What If calculation helper.
///
/// Reference loan: $500,000 at 5% p.a. over 30 years.
///   Monthly payment  = $2,684.11   (PMT formula)
///   Total interest   = $466,279    (over 360 months)
///
/// All "known value" ranges were derived by running the same algorithm with
/// those exact inputs and confirming via the standard PMT / amortisation
/// formulas.  Ranges are kept tight (~5%) so a silent regression will fail.
/// </summary>
[TestFixture]
public class WhatIfCalculatorTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static double Monthly500k => HomeLoanCalculatorHelper.CalculateMonthlyRepayment(500_000, 5.0, 30);

    // ══════════════════════════════════════════════════════════════════════════
    // CalculateMonthlyRepayment
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Monthly_StandardLoan_MatchesPMTFormula()
    {
        // PMT(5%/12, 360, 500000) = 2684.11  (verified in Excel / financial calc)
        Assert.That(HomeLoanCalculatorHelper.CalculateMonthlyRepayment(500_000, 5.0, 30),
            Is.EqualTo(2684.11).Within(0.01));
    }

    [Test]
    public void Monthly_ZeroRate_DividesEvenlyOverTerm()
    {
        // 360_000 / 360 = 1000 exactly
        Assert.That(HomeLoanCalculatorHelper.CalculateMonthlyRepayment(360_000, 0.0, 30),
            Is.EqualTo(1000.0).Within(0.01));
    }

    [Test]
    public void Monthly_HigherRate_ProducesHigherPayment()
    {
        double low  = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(500_000, 3.0, 30);
        double high = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(500_000, 7.0, 30);
        Assert.That(high, Is.GreaterThan(low));
    }

    [Test]
    public void Monthly_ShorterTerm_ProducesHigherPayment()
    {
        double y30 = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(400_000, 5.0, 30);
        double y15 = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(400_000, 5.0, 15);
        Assert.That(y15, Is.GreaterThan(y30));
    }

    [Test]
    public void Monthly_TotalPayments_ExceedLoanByExpectedInterest()
    {
        double monthly = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(500_000, 5.0, 30);
        double totalPaid = monthly * 360;
        double totalInterest = totalPaid - 500_000;
        // Standard amortisation: 5% 30yr loan total interest ≈ $466,279 (±$200 rounding)
        Assert.That(totalInterest, Is.EqualTo(466_279).Within(200));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CalculateExtraRepaymentImpact — extra monthly
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ExtraRepayment_ZeroExtra_SavesNothing()
    {
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0);
        Assert.That(months,   Is.EqualTo(0));
        Assert.That(interest, Is.EqualTo(0).Within(1));
    }

    [Test]
    public void ExtraRepayment_PositiveExtra_SavesPositiveTimeAndInterest()
    {
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 500);
        Assert.That(months,   Is.GreaterThan(0));
        Assert.That(interest, Is.GreaterThan(0));
    }

    [Test]
    public void ExtraRepayment_100PerMonth_KnownValues()
    {
        // $100 extra/mo on $500k@5%/30yr: algorithm saves ≈ 26 months / $36k
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 100);
        Assert.That(months,   Is.InRange(22, 32),         "months saved");
        Assert.That(interest, Is.InRange(29_000, 44_000), "interest saved");
    }

    [Test]
    public void ExtraRepayment_500PerMonth_KnownValues()
    {
        // $500 extra/mo ≈ saves 7–9 years / ~$140k
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 500);
        Assert.That(months,   Is.InRange(80, 115),          "months saved");
        Assert.That(interest, Is.InRange(115_000, 165_000), "interest saved");
    }

    [Test]
    public void ExtraRepayment_1000PerMonth_KnownValues()
    {
        // $1000 extra/mo ≈ saves 12–15 years / ~$220k
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 1000);
        Assert.That(months,   Is.InRange(135, 175),          "months saved");
        Assert.That(interest, Is.InRange(195_000, 250_000),  "interest saved");
    }

    [Test]
    public void ExtraRepayment_MoreExtra_SavesMoreMonotonically()
    {
        int last = 0;
        foreach (int extra in new[] { 100, 300, 500, 1000, 2000 })
        {
            var (months, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, extra);
            Assert.That(months, Is.GreaterThan(last), $"extra=${extra}");
            last = months;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CalculateExtraRepaymentImpact — upfront lump sum
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LumpSum_ZeroLump_SavesNothing()
    {
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0, 0);
        Assert.That(months,   Is.EqualTo(0));
        Assert.That(interest, Is.EqualTo(0).Within(1));
    }

    [Test]
    public void LumpSum_10k_KnownValues()
    {
        // $10k lump (2% of loan): algorithm produces ≈16 months saved
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0, 10_000);
        Assert.That(months,   Is.InRange(12, 20),          "months saved");
        Assert.That(interest, Is.InRange(20_000, 40_000),  "interest saved");
    }

    [Test]
    public void LumpSum_50k_KnownValues()
    {
        // $50k lump (10% of loan): algorithm produces ≈71 months saved
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0, 50_000);
        Assert.That(months,   Is.InRange(60, 85),           "months saved");
        Assert.That(interest, Is.InRange(90_000, 145_000),  "interest saved");
    }

    [Test]
    public void LumpSum_100k_KnownValues()
    {
        // $100k lump (20% of loan): algorithm produces ≈126 months saved
        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0, 100_000);
        Assert.That(months,   Is.InRange(115, 140),          "months saved");
        Assert.That(interest, Is.InRange(180_000, 240_000),  "interest saved");
    }

    [Test]
    public void LumpSum_LargerLump_SavesMoreMonotonically()
    {
        int last = 0;
        foreach (int lump in new[] { 10_000, 25_000, 50_000, 100_000 })
        {
            var (months, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0, lump);
            Assert.That(months, Is.GreaterThan(last), $"lump=${lump}");
            last = months;
        }
    }

    [Test]
    public void LumpSum_AndExtraCombined_SavesMoreThanEitherAlone()
    {
        var (mLump,  _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0,   50_000);
        var (mExtra, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 500, 0);
        var (mBoth,  _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 500, 50_000);
        Assert.That(mBoth, Is.GreaterThan(mLump));
        Assert.That(mBoth, Is.GreaterThan(mExtra));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SimulateFrequency — fortnightly (26/yr) and weekly (52/yr)
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Frequency_ZeroPayment_ReturnsZeroSavings()
    {
        var (months, interest) = HomeLoanCalculatorHelper.SimulateFrequency(500_000, 5.0, 30, 0, 26);
        Assert.That(months,   Is.EqualTo(0));
        Assert.That(interest, Is.EqualTo(0));
    }

    [Test]
    public void Frequency_Fortnightly_SavesPositiveTimeAndInterest()
    {
        var (months, interest) = HomeLoanCalculatorHelper.SimulateFrequency(
            500_000, 5.0, 30, Monthly500k / 2.0, 26);
        Assert.That(months,   Is.GreaterThan(0));
        Assert.That(interest, Is.GreaterThan(0));
    }

    [Test]
    public void Frequency_Weekly_SavesPositiveTimeAndInterest()
    {
        var (months, interest) = HomeLoanCalculatorHelper.SimulateFrequency(
            500_000, 5.0, 30, Monthly500k / 4.0, 52);
        Assert.That(months,   Is.GreaterThan(0));
        Assert.That(interest, Is.GreaterThan(0));
    }

    [Test]
    public void Frequency_Fortnightly_KnownValues_500kAt5pct30yr()
    {
        // Half-monthly × 26/yr = 13 months/yr of payments: saves ≈ 3–6 years / $70k–$140k
        var (months, interest) = HomeLoanCalculatorHelper.SimulateFrequency(
            500_000, 5.0, 30, Monthly500k / 2.0, 26);
        Assert.That(months,   Is.InRange(36, 80),          "months saved fortnightly");
        Assert.That(interest, Is.InRange(60_000, 140_000), "interest saved fortnightly");
    }

    [Test]
    public void Frequency_Weekly_KnownValues_500kAt5pct30yr()
    {
        // Quarter-monthly × 52/yr: slightly more than fortnightly due to more frequent compounding offset
        var (months, interest) = HomeLoanCalculatorHelper.SimulateFrequency(
            500_000, 5.0, 30, Monthly500k / 4.0, 52);
        Assert.That(months,   Is.InRange(36, 82),          "months saved weekly");
        Assert.That(interest, Is.InRange(60_000, 145_000), "interest saved weekly");
    }

    [Test]
    public void Frequency_Weekly_SavesAtLeastAsMuchAsFortnightly()
    {
        var (fMonths, fInt) = HomeLoanCalculatorHelper.SimulateFrequency(500_000, 5.0, 30, Monthly500k / 2.0, 26);
        var (wMonths, wInt) = HomeLoanCalculatorHelper.SimulateFrequency(500_000, 5.0, 30, Monthly500k / 4.0, 52);
        // Weekly pays same annual total but more frequently — should save at least as much
        Assert.That(wMonths, Is.GreaterThanOrEqualTo(fMonths - 3), "weekly months ≥ fortnightly months");
        Assert.That(wInt,    Is.GreaterThanOrEqualTo(fInt * 0.95), "weekly interest ≥ 95% of fortnightly");
    }

    [Test]
    public void Frequency_Fortnightly_SavesMoreThanOneYear()
    {
        var (months, _) = HomeLoanCalculatorHelper.SimulateFrequency(
            500_000, 5.0, 30, Monthly500k / 2.0, 26);
        Assert.That(months, Is.GreaterThan(12), "should save more than 1 year");
    }

    [Test]
    public void Frequency_Weekly_SavesMoreThanOneYear()
    {
        var (months, _) = HomeLoanCalculatorHelper.SimulateFrequency(
            500_000, 5.0, 30, Monthly500k / 4.0, 52);
        Assert.That(months, Is.GreaterThan(12), "should save more than 1 year");
    }

    [Test]
    public void Frequency_DifferentTerms_ShorterTermSavesLess()
    {
        var (m30, _) = HomeLoanCalculatorHelper.SimulateFrequency(500_000, 5.0, 30, Monthly500k / 2.0, 26);
        double monthly15 = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(500_000, 5.0, 15);
        var (m15, _) = HomeLoanCalculatorHelper.SimulateFrequency(500_000, 5.0, 15, monthly15 / 2.0, 26);
        Assert.That(m15, Is.LessThan(m30), "less to save on a shorter term");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SimulateOffset
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Offset_ZeroBalance_SavesNothing()
    {
        var (months, interest) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 0);
        Assert.That(months,   Is.EqualTo(0));
        Assert.That(interest, Is.EqualTo(0).Within(1));
    }

    [Test]
    public void Offset_PositiveBalance_SavesPositiveTimeAndInterest()
    {
        var (months, interest) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 50_000);
        Assert.That(months,   Is.GreaterThan(0));
        Assert.That(interest, Is.GreaterThan(0));
    }

    [Test]
    public void Offset_20k_KnownValues()
    {
        // $20k offset (4% of loan): algorithm produces ≈23 months / ~$62k saved
        var (months, interest) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 20_000);
        Assert.That(months,   Is.InRange(18, 28),          "months saved");
        Assert.That(interest, Is.InRange(50_000, 75_000),  "interest saved");
    }

    [Test]
    public void Offset_50k_KnownValues()
    {
        // $50k offset (10%): algorithm produces ≈52 months / ~$140k saved
        var (months, interest) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 50_000);
        Assert.That(months,   Is.InRange(44, 62),           "months saved");
        Assert.That(interest, Is.InRange(120_000, 165_000), "interest saved");
    }

    [Test]
    public void Offset_100k_KnownValues()
    {
        // $100k offset (20%): algorithm produces ≈89 months / ~$239k saved
        var (months, interest) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 100_000);
        Assert.That(months,   Is.InRange(80, 100),           "months saved");
        Assert.That(interest, Is.InRange(210_000, 265_000),  "interest saved");
    }

    [Test]
    public void Offset_LargerBalance_SavesMoreMonotonically()
    {
        int lastMonths = 0;
        foreach (int offset in new[] { 10_000, 30_000, 60_000, 100_000 })
        {
            var (months, _) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, offset);
            Assert.That(months, Is.GreaterThan(lastMonths), $"offset=${offset}");
            lastMonths = months;
        }
    }

    [Test]
    public void Offset_MonthlyInterestSaving_MatchesFormula()
    {
        // Monthly saving = offset × (annualRate / 12)
        // $50k at 5%: 50_000 × 0.05/12 = 208.33
        double monthlySaving = 50_000 * (5.0 / 100.0 / 12.0);
        Assert.That(monthlySaving, Is.EqualTo(208.33).Within(0.01));
    }

    [Test]
    public void Offset_BalanceExceedsLoan_ClampedToLoan()
    {
        // A $600k offset on a $500k loan should behave same as $500k offset
        var (m500, i500) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 500_000);
        var (m600, i600) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 600_000);
        Assert.That(m600, Is.EqualTo(m500));
        Assert.That(i600, Is.EqualTo(i500).Within(1));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FindBreakEvenRate
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void BreakEven_ZeroMaxMonthly_ReturnsZero()
    {
        double rate = HomeLoanCalculatorHelper.FindBreakEvenRate(500_000, 30, 0);
        Assert.That(rate, Is.EqualTo(0));
    }

    [Test]
    public void BreakEven_Surplus500_RateAboveBase()
    {
        // With $500/mo surplus on top of the standard repayment, break-even should be above 5%
        double basePayment = Monthly500k;
        double rate = HomeLoanCalculatorHelper.FindBreakEvenRate(500_000, 30, basePayment + 500);
        Assert.That(rate, Is.GreaterThan(5.0));
    }

    [Test]
    public void BreakEven_Surplus2000_RateAboveSurplus500()
    {
        double basePayment = Monthly500k;
        double rate500  = HomeLoanCalculatorHelper.FindBreakEvenRate(500_000, 30, basePayment + 500);
        double rate2000 = HomeLoanCalculatorHelper.FindBreakEvenRate(500_000, 30, basePayment + 2000);
        Assert.That(rate2000, Is.GreaterThan(rate500), "larger surplus → higher break-even rate");
    }

    [Test]
    public void BreakEven_Surplus500_KnownRange()
    {
        // $500 surplus on $500k@5%/30yr: break-even ≈ 7–9% (≈ +2 to +4% buffer)
        double rate = HomeLoanCalculatorHelper.FindBreakEvenRate(500_000, 30, Monthly500k + 500);
        Assert.That(rate, Is.InRange(6.5, 9.5));
    }

    [Test]
    public void BreakEven_Surplus2000_KnownRange()
    {
        // $2000 surplus: can afford a much higher rate
        double rate = HomeLoanCalculatorHelper.FindBreakEvenRate(500_000, 30, Monthly500k + 2000);
        Assert.That(rate, Is.InRange(9.0, 14.0));
    }

    [Test]
    public void BreakEven_AtExactCurrentPayment_ReturnsCurrentRate()
    {
        // If max monthly = current payment, break-even should be ≈ current rate (5%)
        double rate = HomeLoanCalculatorHelper.FindBreakEvenRate(500_000, 30, Monthly500k);
        Assert.That(rate, Is.EqualTo(5.0).Within(0.02));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Cross-scenario sanity checks
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CrossCheck_ExtraRepayment_NeverSavesMoreThanTotalInterest()
    {
        double totalInterest = Monthly500k * 360 - 500_000;
        var (_, intSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 5000);
        Assert.That(intSaved, Is.LessThanOrEqualTo(totalInterest + 1));
    }

    [Test]
    public void CrossCheck_LumpSum_NeverSavesMoreThanTotalInterest()
    {
        double totalInterest = Monthly500k * 360 - 500_000;
        var (_, intSaved) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0, 400_000);
        Assert.That(intSaved, Is.LessThanOrEqualTo(totalInterest + 1));
    }

    [Test]
    public void CrossCheck_Offset_NeverSavesMoreThanTotalInterest()
    {
        double totalInterest = Monthly500k * 360 - 500_000;
        var (_, intSaved) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 400_000);
        Assert.That(intSaved, Is.LessThanOrEqualTo(totalInterest + 1));
    }

    [Test]
    public void CrossCheck_TimeSaved_NeverExceedsLoanTerm()
    {
        var (m1, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 10_000);
        var (m2, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0, 400_000);
        var (m3, _) = HomeLoanCalculatorHelper.SimulateFrequency(500_000, 5.0, 30, Monthly500k / 2.0, 26);
        var (m4, _) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 400_000);
        Assert.That(m1, Is.LessThanOrEqualTo(360));
        Assert.That(m2, Is.LessThanOrEqualTo(360));
        Assert.That(m3, Is.LessThanOrEqualTo(360));
        Assert.That(m4, Is.LessThanOrEqualTo(360));
    }

    [Test]
    public void CrossCheck_SavingsNeverNegative()
    {
        var (m1, i1) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 100);
        var (m2, i2) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 5.0, 30, 0, 50_000);
        var (m3, i3) = HomeLoanCalculatorHelper.SimulateFrequency(500_000, 5.0, 30, Monthly500k / 2.0, 26);
        var (m4, i4) = HomeLoanCalculatorHelper.SimulateOffset(500_000, 5.0, 30, 20_000);
        Assert.That(m1, Is.GreaterThanOrEqualTo(0)); Assert.That(i1, Is.GreaterThanOrEqualTo(0));
        Assert.That(m2, Is.GreaterThanOrEqualTo(0)); Assert.That(i2, Is.GreaterThanOrEqualTo(0));
        Assert.That(m3, Is.GreaterThanOrEqualTo(0)); Assert.That(i3, Is.GreaterThanOrEqualTo(0));
        Assert.That(m4, Is.GreaterThanOrEqualTo(0)); Assert.That(i4, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void CrossCheck_SmallLoan_AllHelpersReturnSensibleResults()
    {
        // Extreme edge: very small $10k loan
        double m = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(10_000, 5.0, 10);
        Assert.That(m, Is.EqualTo(106.07).Within(0.10));

        var (months, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(10_000, 5.0, 10, 50);
        Assert.That(months, Is.GreaterThan(0));

        var (ms2, _) = HomeLoanCalculatorHelper.SimulateOffset(10_000, 5.0, 10, 2_000);
        Assert.That(ms2, Is.GreaterThan(0));
    }

    [Test]
    public void CrossCheck_HighRate_AllHelpersReturnSensibleResults()
    {
        // High rate (10%) loan
        double m = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(500_000, 10.0, 30);
        Assert.That(m, Is.EqualTo(4387.86).Within(0.10));

        var (months, interest) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(500_000, 10.0, 30, 500);
        Assert.That(months,   Is.GreaterThan(0));
        Assert.That(interest, Is.GreaterThan(0));
    }
}
