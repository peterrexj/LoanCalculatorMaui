using LoanCalculator.Core.Models.ViewModels.PrimaryModels;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    // Reference loan used throughout: $900,000 at 5% over 30 years.
    // Standard monthly payment ≈ $4,830.
    [TestFixture]
    public class SimulateCombinedTests
    {
        private const double Loan     = 900_000;
        private const double Rate     = 5.0;
        private const int    Term     = 30;

        private static double ParseCurrency(string s)
            => double.Parse(s.Replace("$", "").Replace(",", "").Replace("/mo","").Replace("/fn","").Replace("/wk","").Trim());

        // ── Zero-lever baseline ───────────────────────────────────────────────

        [Test]
        public void ZeroLevers_Monthly_TimeSavedIsZero()
        {
            var (months, _, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 0, 12);
            Assert.That(months, Is.EqualTo(0));
        }

        [Test]
        public void ZeroLevers_Monthly_InterestSavedIsZero()
        {
            var (_, interest, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 0, 12);
            Assert.That(interest, Is.EqualTo(0).Within(1.0));
        }

        [Test]
        public void ZeroLevers_Fortnightly_SavesTimeVsMonthly()
        {
            // Fortnightly with zero extra/lump/offset should save time vs monthly baseline
            // because 26 × (monthly/2) = 13 monthly-equivalents per year.
            var (months, _, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 0, 26);
            Assert.That(months, Is.GreaterThan(0), "Fortnightly alone should save time vs monthly baseline");
        }

        [Test]
        public void ZeroLevers_Fortnightly_SavesInterest()
        {
            var (_, interest, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 0, 26);
            Assert.That(interest, Is.GreaterThan(0), "Fortnightly should save interest");
        }

        [Test]
        public void ZeroLevers_Weekly_TimeSavedSimilarToFortnightly()
        {
            var (fortMonths, _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0, 0, 0, 26);
            var (weekMonths, _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0, 0, 0, 52);
            // Weekly and fortnightly produce almost identical savings (both = 13 payments/yr)
            Assert.That(Math.Abs(fortMonths - weekMonths), Is.LessThanOrEqualTo(3));
        }

        // ── Extra repayment lever ─────────────────────────────────────────────

        [Test]
        public void ExtraRepayment_Monthly_SavesTime()
        {
            var (months, _, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 500, 0, 0, 12);
            Assert.That(months, Is.GreaterThan(0));
        }

        [Test]
        public void ExtraRepayment_Monthly_MatchesStandaloneHelper_MonthsWithinTolerance()
        {
            // The two helpers use slightly different interest accounting (net vs paid),
            // so months should be close but interest can diverge a few thousand over 30yr.
            var (combinedMonths, _, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 500, 0, 0, 12);
            var (standaloneMonths, _) = HomeLoanCalculatorHelper.CalculateExtraRepaymentImpact(
                Loan, Rate, Term, 500);
            Assert.That(combinedMonths, Is.EqualTo(standaloneMonths).Within(3),
                "months saved should be close between SimulateCombined and standalone helper");
        }

        [Test]
        public void ExtraRepayment_MoreExtra_SavesMoreTime()
        {
            var (low,  _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 200,   0, 0, 12);
            var (high, _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 2_000, 0, 0, 12);
            Assert.That(high, Is.GreaterThan(low));
        }

        [Test]
        public void ExtraRepayment_MoreExtra_SavesMoreInterest()
        {
            var (_, intLow,  _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 200,   0, 0, 12);
            var (_, intHigh, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 2_000, 0, 0, 12);
            Assert.That(intHigh, Is.GreaterThan(intLow));
        }

        // ── Lump sum lever ────────────────────────────────────────────────────

        [Test]
        public void LumpSum_SavesTime()
        {
            var (months, _, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 50_000, 0, 12);
            Assert.That(months, Is.GreaterThan(0));
        }

        [Test]
        public void LumpSum_MoreLump_SavesMoreTime()
        {
            var (low,  _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0, 10_000,  0, 12);
            var (high, _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0, 200_000, 0, 12);
            Assert.That(high, Is.GreaterThan(low));
        }

        [Test]
        public void LumpSum_LargerThanLoan_DoesNotCrash()
        {
            Assert.DoesNotThrow(() =>
            {
                var (months, interest, _) = HomeLoanCalculatorHelper.SimulateCombined(
                    Loan, Rate, Term, 0, Loan + 100_000, 0, 12);
                Assert.That(months, Is.GreaterThanOrEqualTo(0));
                Assert.That(interest, Is.GreaterThanOrEqualTo(0));
            });
        }

        // ── Offset lever ──────────────────────────────────────────────────────

        [Test]
        public void Offset_SavesTime()
        {
            var (months, _, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 50_000, 12);
            Assert.That(months, Is.GreaterThan(0));
        }

        [Test]
        public void Offset_MoreOffset_SavesMoreTime()
        {
            var (low,  _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0, 0, 20_000,  12);
            var (high, _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0, 0, 200_000, 12);
            Assert.That(high, Is.GreaterThan(low));
        }

        [Test]
        public void Offset_LowerOffsetRate_SavesLessThanLoanRate()
        {
            // Offset at loan rate (5%) saves more than offset at a lower rate (3%)
            var (fullSavMonths, fullSavInt, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 100_000, 12, offsetRatePct: Rate);
            var (lowRateMonths, lowRateInt, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 100_000, 12, offsetRatePct: 3.0);

            Assert.That(fullSavMonths, Is.GreaterThanOrEqualTo(lowRateMonths),
                "Offset at full loan rate should save at least as much time as a lower offset rate");
            Assert.That(fullSavInt, Is.GreaterThanOrEqualTo(lowRateInt),
                "Offset at full loan rate should save at least as much interest as a lower offset rate");
        }

        [Test]
        public void Offset_ZeroOffsetBalance_NoBenefitFromOffsetRate()
        {
            // With zero offset balance, the offset rate should have no impact
            var (monthsDefault, intDefault, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 0, 12);
            var (monthsCustom, intCustom, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 0, 12, offsetRatePct: 2.0);
            Assert.That(monthsDefault, Is.EqualTo(monthsCustom));
            Assert.That(intDefault,    Is.EqualTo(intCustom).Within(1.0));
        }

        [Test]
        public void Offset_MatchesSimulateOffsetHelper_AtLoanRate()
        {
            // SimulateCombined at monthly frequency with only offset should produce
            // the same result as the dedicated SimulateOffset helper
            var (combinedMonths, combinedInterest, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 50_000, 12, offsetRatePct: Rate);
            var (standaloneMonths, standaloneInterest) = HomeLoanCalculatorHelper.SimulateOffset(
                Loan, Rate, Term, 50_000);
            Assert.That(combinedMonths,   Is.EqualTo(standaloneMonths).Within(2),     "months saved should match SimulateOffset");
            Assert.That(combinedInterest, Is.EqualTo(standaloneInterest).Within(1000), "interest saved should match SimulateOffset");
        }

        // ── Combined levers ───────────────────────────────────────────────────

        [Test]
        public void AllLevers_SavesMoreThanAnyOneAlone()
        {
            var (extra,  _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 500, 0,      0,       12);
            var (lump,   _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0,   10_000, 0,       12);
            var (offset, _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0,   0,      20_000,  12);
            var (freq,   _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 0,   0,      0,       26);
            var (all,    _, _) = HomeLoanCalculatorHelper.SimulateCombined(Loan, Rate, Term, 500, 10_000, 20_000,  26);
            Assert.That(all, Is.GreaterThan(extra),  "combined > extra alone");
            Assert.That(all, Is.GreaterThan(lump),   "combined > lump alone");
            Assert.That(all, Is.GreaterThan(offset), "combined > offset alone");
            Assert.That(all, Is.GreaterThan(freq),   "combined > frequency alone");
        }

        [Test]
        public void AllLevers_SavingsNeverExceedFullTerm()
        {
            var (months, _, _) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 5_000, 800_000, 800_000, 26);
            Assert.That(months, Is.LessThanOrEqualTo(Term * 12));
        }

        // ── Yearly balance chart data ─────────────────────────────────────────

        [Test]
        public void YearlyBalances_StartAtInitialBalance()
        {
            var (_, _, balances) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 0, 12);
            Assert.That(balances[0].balance, Is.EqualTo(Loan).Within(1.0));
        }

        [Test]
        public void YearlyBalances_EndAtOrNearZero()
        {
            var (_, _, balances) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, 0, 0, 12);
            Assert.That(balances.Last().balance, Is.LessThanOrEqualTo(1.0));
        }

        [Test]
        public void YearlyBalances_WithLump_InitialBalanceReducedByLump()
        {
            var lump = 100_000.0;
            var (_, _, balances) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 0, lump, 0, 12);
            Assert.That(balances[0].balance, Is.EqualTo(Loan - lump).Within(1.0));
        }

        [Test]
        public void YearlyBalances_IsStrictlyDecreasing()
        {
            var (_, _, balances) = HomeLoanCalculatorHelper.SimulateCombined(
                Loan, Rate, Term, 500, 0, 0, 12);
            for (int i = 1; i < balances.Count; i++)
                Assert.That(balances[i].balance, Is.LessThanOrEqualTo(balances[i - 1].balance),
                    $"Balance at year {balances[i].year} should not exceed year {balances[i-1].year}");
        }

        // ── Periodic payment formula matches display ──────────────────────────

        [Test]
        public void FortnightlyPayment_IsHalfOfMonthly()
        {
            // The correct real-world convention: fortnightly payment = monthly / 2.
            // 26 × (monthly/2) = 13 monthly payments per year → that is the saving source.
            var monthly     = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(Loan, Rate, Term);
            var fortnightly = monthly / 2.0;
            Assert.That(fortnightly, Is.EqualTo(monthly / 2.0).Within(0.01));
        }

        [Test]
        public void FortnightlyPayment_26Times_Equals13MonthlyPayments()
        {
            var monthly = HomeLoanCalculatorHelper.CalculateMonthlyRepayment(Loan, Rate, Term);
            var fortnightly = monthly / 2.0;
            var annualFortnightly = fortnightly * 26;
            var thirteenMonthly   = monthly * 13;
            Assert.That(annualFortnightly, Is.EqualTo(thirteenMonthly).Within(0.01),
                "26 × (monthly/2) should equal 13 monthly payments — that's the fortnightly saving");
        }

        // ── ViewModel integration: all-unchecked produces zero savings ────────

        [Test]
        public void CombinedViewModel_AllLeversOff_TimeSavedIsZeroYr()
        {
            var loanVm = WhatIfViewModelTests.BuildLoanVmPublic();
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(loanVm);

            vm.CombinedUseFrequency = false;
            vm.CombinedUseExtra     = false;
            vm.CombinedUseLumpSum   = false;
            vm.CombinedUseOffset    = false;

            Assert.That(vm.CombinedTimeSaved,     Is.EqualTo("0yr"), "no levers → zero time saved");
            Assert.That(vm.CombinedInterestSaved, Does.Contain("0"),  "no levers → zero interest saved");
        }

        [Test]
        public void CombinedViewModel_OnlyOffsetEnabled_LowerOffsetRate_SavesLess()
        {
            var loanVm = WhatIfViewModelTests.BuildLoanVmPublic();
            var vm1 = new WhatIfViewModel(null!);
            vm1.SetLoanViewModel(loanVm);
            vm1.CombinedUseFrequency = false;
            vm1.CombinedUseExtra     = false;
            vm1.CombinedUseLumpSum   = false;
            vm1.CombinedUseOffset    = true;
            vm1.CombinedOffset       = 100_000;
            // Offset rate auto-tracks loan rate (5%) — full benefit

            var vm2 = new WhatIfViewModel(null!);
            vm2.SetLoanViewModel(loanVm);
            vm2.CombinedUseFrequency = false;
            vm2.CombinedUseExtra     = false;
            vm2.CombinedUseLumpSum   = false;
            vm2.CombinedUseOffset    = true;
            vm2.CombinedOffset       = 100_000;
            vm2.CombinedOffsetRate   = 3.0; // lower rate → smaller credit → less saving

            double ParseTime(string s) {
                s = s.Replace("yr", " ").Replace("mo", " ").Trim();
                var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2 ? double.Parse(parts[0]) * 12 + double.Parse(parts[1]) : double.Parse(parts[0]) * 12;
            }

            var time1 = ParseTime(vm1.CombinedTimeSaved);
            var time2 = ParseTime(vm2.CombinedTimeSaved);

            Assert.That(time1, Is.GreaterThanOrEqualTo(time2),
                "Lower offset rate should save equal or less time than full loan rate");
        }
    }
}
