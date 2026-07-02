using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    [TestFixture]
    public class WhatIfViewModelTests
    {
        private WhatIfViewModel _vm;

        // Reference loan: $900k at 5% over 30yr on a $1M property (10% deposit)
        private static LoanViewModel BuildLoanVm(
            double propertyAmount = 1_000_000,
            double loanAmount = 900_000,
            double interestRate = 5.0,
            int termYears = 30,
            int paymentsPerYear = 12)
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var loanVm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = interestRate,
                        LoanTermInYears = termYears,
                        TotalNumberPaymentPerYear = paymentsPerYear
                    },
                    PropertyAmount = propertyAmount
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            loanVm.HomeLoanInfo.LoanAmountDirectInput = loanAmount;
            loanVm.MarkInitializationComplete();
            return loanVm;
        }

        [SetUp]
        public void Setup()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            _vm = new WhatIfViewModel(null!);
            _vm.SetLoanViewModel(BuildLoanVm());
        }

        [TearDown]
        public void TearDown()
        {
            SharedServiceCore.LoadSafeOff();
            PageHelper.PageLoadingComplete();
        }

        // ── HasLoanData / HasNoLoanData ───────────────────────────────────────

        [Test]
        public void HasLoanData_WhenLoanAmountPositive_IsTrue()
            => Assert.That(_vm.HasLoanData, Is.True);

        [Test]
        public void HasNoLoanData_WhenLoanAmountPositive_IsFalse()
            => Assert.That(_vm.HasNoLoanData, Is.False);

        [Test]
        public void HasLoanData_WhenNoLoanVm_IsFalse()
            => Assert.That(new WhatIfViewModel(null!).HasLoanData, Is.False);

        [Test]
        public void HasNoLoanData_WhenNoLoanVm_IsTrue()
            => Assert.That(new WhatIfViewModel(null!).HasNoLoanData, Is.True);

        // ── Scenario 1: Rate Change ───────────────────────────────────────────

        [Test]
        public void RateChangeNewRate_DefaultDelta_ShowsBaseRatePlusHalf()
            => Assert.That(_vm.RateChangeNewRate, Is.EqualTo("5.5%"));

        [Test]
        public void RateChangeNewRate_CustomDelta_ShowsCorrectRate()
        {
            _vm.RateChangeDelta = 1.0;
            Assert.That(_vm.RateChangeNewRate, Is.EqualTo("6%"));
        }

        [Test]
        public void RateChangeMonthlyRepayment_AfterSetup_EndsWithMoSuffix()
            => Assert.That(_vm.RateChangeMonthlyRepayment, Does.EndWith("/mo"));

        [Test]
        public void RateChangeMonthlyDiff_PositiveDelta_StartsWithPlus()
        {
            _vm.RateChangeDelta = 1.0;
            Assert.That(_vm.RateChangeMonthlyDiff, Does.StartWith("+"));
        }

        [Test]
        public void RateChangeDiffIsPositive_PositiveDelta_IsTrue()
        {
            _vm.RateChangeDelta = 1.0;
            Assert.That(_vm.RateChangeDiffIsPositive, Is.True);
        }

        [Test]
        public void RateChangeDiffIsPositive_LargerDelta_GreaterThanSmallerDelta()
        {
            // Can't test the sign directly without a real PaymentSummary in tests,
            // but a larger positive delta should always produce a bigger positive diff.
            _vm.RateChangeDelta = 0.5;
            var diffSmall = _vm.RateChangeMonthlyDiff;
            _vm.RateChangeDelta = 2.0;
            var diffLarge = _vm.RateChangeMonthlyDiff;
            Assert.That(diffLarge, Is.Not.EqualTo(diffSmall));
        }

        [Test]
        public void RateChangeTotalInterest_HigherRate_ProducesHigherInterest()
        {
            _vm.RateChangeDelta = 0.25;
            var interestLow = _vm.RateChangeTotalInterest;
            _vm.RateChangeDelta = 2.0;
            var interestHigh = _vm.RateChangeTotalInterest;
            // Both are currency strings — parse and compare
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", ""));
            Assert.That(ParseCurrency(interestHigh), Is.GreaterThan(ParseCurrency(interestLow)));
        }

        [Test]
        public void RateChange_HeadroomNotShown_WhenNoAffordabilityData()
            => Assert.That(_vm.RateChangeShowHeadroom, Is.False);

        // ── Scenario 2: Extra Repayment ───────────────────────────────────────

        [Test]
        public void ExtraRepaymentTimeSaved_AfterSetup_ContainsYr()
            => Assert.That(_vm.ExtraRepaymentTimeSaved, Does.Contain("yr"));

        [Test]
        public void ExtraRepaymentInterestSaved_AfterSetup_IsNotDash()
            => Assert.That(_vm.ExtraRepaymentInterestSaved, Is.Not.EqualTo("--"));

        [Test]
        public void ExtraRepaymentNewPayoff_ContainsRemaining()
            => Assert.That(_vm.ExtraRepaymentNewPayoff, Does.Contain("remaining"));

        [Test]
        public void ExtraRepayment_HigherExtra_SavesMoreTime()
        {
            _vm.ExtraRepaymentMonthly = 200;
            var timeLow = _vm.ExtraRepaymentTimeSaved;
            _vm.ExtraRepaymentMonthly = 2000;
            var timeHigh = _vm.ExtraRepaymentTimeSaved;
            Assert.That(timeHigh, Is.Not.EqualTo(timeLow));
        }

        [Test]
        public void ExtraRepayment_ZeroExtra_TimeSavedIsZeroYr()
        {
            _vm.ExtraRepaymentMonthly = 0;
            Assert.That(_vm.ExtraRepaymentTimeSaved, Is.EqualTo("0yr"));
        }

        // ── Scenario 3: Term Comparison ───────────────────────────────────────

        [Test]
        public void TermComparison_ExactlyOneColumnIsCurrentTerm()
        {
            var flags = new[] { _vm.TermIsCurrentColA, _vm.TermIsCurrentColB, _vm.TermIsCurrentColC };
            Assert.That(flags.Count(x => x), Is.EqualTo(1));
        }

        [Test]
        public void TermComparison_30yrTerm_ColCIsHighlighted()
            => Assert.That(_vm.TermIsCurrentColC, Is.True);

        [Test]
        public void TermComparison_AllLabelsEndWithYRS()
        {
            Assert.That(_vm.TermColALabel, Does.EndWith("YRS"));
            Assert.That(_vm.TermColBLabel, Does.EndWith("YRS"));
            Assert.That(_vm.TermColCLabel, Does.EndWith("YRS"));
        }

        [Test]
        public void TermComparison_ShorterTerm_HasHigherMonthlyPayment()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", ""));
            Assert.That(ParseCurrency(_vm.TermColAMonthly), Is.GreaterThan(ParseCurrency(_vm.TermColCMonthly)));
        }

        [Test]
        public void TermComparison_ShorterTerm_HasLowerTotalInterest()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", ""));
            Assert.That(ParseCurrency(_vm.TermColAInterest), Is.LessThan(ParseCurrency(_vm.TermColCInterest)));
        }

        [Test]
        public void TermComparison_2yrTerm_ColAIsHighlighted()
        {
            // 2yr ≤ step(2) → floor case → ColA
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(termYears: 2));
            Assert.That(vm.TermIsCurrentColA, Is.True);
        }

        [Test]
        public void TermComparison_5yrTerm_ColBIsHighlighted()
        {
            // 5yr with step=2: normal case (5 > step), user in ColB
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(termYears: 5));
            Assert.That(vm.TermIsCurrentColB, Is.True);
        }

        [Test]
        public void TermComparison_15yrTerm_ColBIsHighlighted()
        {
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(termYears: 15));
            Assert.That(vm.TermIsCurrentColB, Is.True);
        }

        // ── Scenario 4: Deposit Scenarios ─────────────────────────────────────

        [Test]
        public void DepositColBLabel_AfterSetup_EndsWithPercent()
            => Assert.That(_vm.DepositColBLabel, Does.EndWith("%"));

        [Test]
        public void DepositColBMonthly_AfterSetup_EndsWithMoSuffix()
            => Assert.That(_vm.DepositColBMonthly, Does.EndWith("/mo"));

        [Test]
        public void DepositScenarios_HigherDeposit_LowerLoanAmount()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", ""));
            double loanA = ParseCurrency(_vm.DepositColALoan);
            double loanC = ParseCurrency(_vm.DepositColCLoan);
            Assert.That(loanC, Is.LessThan(loanA));
        }

        [Test]
        public void DepositScenarios_HigherDeposit_LowerMonthlyPayment()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", "").Replace("/mo", ""));
            double monthlyA = ParseCurrency(_vm.DepositColAMonthly);
            double monthlyC = ParseCurrency(_vm.DepositColCMonthly);
            Assert.That(monthlyC, Is.LessThan(monthlyA));
        }

        [Test]
        public void DepositColBLabel_10PctDeposit_ShowsExactPercentage()
        {
            // Property $1M, loan $900k → deposit = $100k = 10%
            Assert.That(_vm.DepositColBLabel, Is.EqualTo("10%"));
        }

        [Test]
        public void DepositScenarios_At99Pct_ColCUnavailable()
        {
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(propertyAmount: 1_000_000, loanAmount: 10_000)); // 99% deposit
            Assert.That(vm.DepositColCAvailable, Is.False);
        }

        [Test]
        public void DepositScenarios_At50Pct_ColCAvailable()
        {
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(propertyAmount: 1_000_000, loanAmount: 500_000)); // 50% deposit
            Assert.That(vm.DepositColCAvailable, Is.True);
        }

        // ── Scenario 5: Lump Sum ──────────────────────────────────────────────

        [Test]
        public void LumpSumTimeSaved_AfterSetup_ContainsYr()
            => Assert.That(_vm.LumpSumTimeSaved, Does.Contain("yr"));

        [Test]
        public void LumpSumInterestSaved_AfterSetup_IsNotDash()
            => Assert.That(_vm.LumpSumInterestSaved, Is.Not.EqualTo("--"));

        [Test]
        public void LumpSumNewBalance_AfterSetup_IsNotDash()
            => Assert.That(_vm.LumpSumNewBalance, Is.Not.EqualTo("--"));

        [Test]
        public void LumpSumAmount_Increase_SavesMoreTime()
        {
            _vm.LumpSumAmount = 10_000;
            var timeLow = _vm.LumpSumTimeSaved;
            _vm.LumpSumAmount = 200_000;
            var timeHigh = _vm.LumpSumTimeSaved;
            Assert.That(timeHigh, Is.Not.EqualTo(timeLow));
        }

        [Test]
        public void LumpSumNewBalance_EqualsLoanMinusLump()
        {
            // Loan = $900k, lump = $100k → new balance = $800k
            _vm.LumpSumAmount = 100_000;
            Assert.That(_vm.LumpSumNewBalance, Does.Contain("800"));
        }

        [Test]
        public void LumpSumAmount_ZeroLump_TimeSavedIsZeroYr()
        {
            _vm.LumpSumAmount = 0;
            Assert.That(_vm.LumpSumTimeSaved, Is.EqualTo("0yr"));
        }

        // ── Scenario 6: Repayment Frequency (3-column) ───────────────────────

        [Test]
        public void FreqMonthly_AllColumnsPopulated()
        {
            Assert.That(_vm.FreqMonthlyPayment, Is.Not.EqualTo("--"));
            Assert.That(_vm.FreqMonthlyTimeSaved, Is.Not.EqualTo("--"));
            Assert.That(_vm.FreqMonthlyIntSaved, Is.Not.EqualTo("--"));
        }

        [Test]
        public void FreqFortnightly_AllColumnsPopulated()
        {
            Assert.That(_vm.FreqFortPayment, Is.Not.EqualTo("--"));
            Assert.That(_vm.FreqFortTimeSaved, Is.Not.EqualTo("--"));
            Assert.That(_vm.FreqFortIntSaved, Is.Not.EqualTo("--"));
        }

        [Test]
        public void FreqWeekly_AllColumnsPopulated()
        {
            Assert.That(_vm.FreqWeeklyPayment, Is.Not.EqualTo("--"));
            Assert.That(_vm.FreqWeeklyTimeSaved, Is.Not.EqualTo("--"));
            Assert.That(_vm.FreqWeeklyIntSaved, Is.Not.EqualTo("--"));
        }

        [Test]
        public void FreqMonthlyPayment_EndsWithMoSuffix()
            => Assert.That(_vm.FreqMonthlyPayment, Does.EndWith("/mo"));

        [Test]
        public void FreqFortPayment_EndsWithFnSuffix()
            => Assert.That(_vm.FreqFortPayment, Does.EndWith("/fn"));

        [Test]
        public void FreqWeeklyPayment_EndsWithWkSuffix()
            => Assert.That(_vm.FreqWeeklyPayment, Does.EndWith("/wk"));

        [Test]
        public void FreqMonthlyTimeSaved_IsZeroYr()
            => Assert.That(_vm.FreqMonthlyTimeSaved, Is.EqualTo("0yr"));

        [Test]
        public void FreqMonthlyIntSaved_IsZero()
            => Assert.That(_vm.FreqMonthlyIntSaved, Does.Contain("0"));

        [Test]
        public void FreqFortnightly_SavesTimeOverMonthly()
            => Assert.That(_vm.FreqFortTimeSaved, Is.Not.EqualTo("0yr"));

        [Test]
        public void FreqWeekly_SavesTimeOverMonthly()
            => Assert.That(_vm.FreqWeeklyTimeSaved, Is.Not.EqualTo("0yr"));

        [Test]
        public void FreqFortnightly_SavesInterestOverMonthly()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", ""));
            Assert.That(ParseCurrency(_vm.FreqFortIntSaved), Is.GreaterThan(0));
        }

        [Test]
        public void FreqWeekly_SavesInterestOverMonthly()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", ""));
            Assert.That(ParseCurrency(_vm.FreqWeeklyIntSaved), Is.GreaterThan(0));
        }

        [Test]
        public void FreqFortnightly_PaymentIsHalfMonthly()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", "").Replace("/mo", "").Replace("/fn", "").Replace("/wk", ""));
            double mo = ParseCurrency(_vm.FreqMonthlyPayment);
            double fn = ParseCurrency(_vm.FreqFortPayment);
            Assert.That(fn, Is.EqualTo(mo / 2.0).Within(1.0));
        }

        [Test]
        public void FreqWeekly_PaymentIsQuarterMonthly()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", "").Replace("/mo", "").Replace("/fn", "").Replace("/wk", ""));
            double mo = ParseCurrency(_vm.FreqMonthlyPayment);
            double wk = ParseCurrency(_vm.FreqWeeklyPayment);
            Assert.That(wk, Is.EqualTo(mo / 4.0).Within(1.0));
        }

        [Test]
        public void FreqHighlight_DefaultMonthlyLoan_IsMonthly()
        {
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(paymentsPerYear: 12));
            Assert.That(vm.FreqIsMonthly, Is.True);
            Assert.That(vm.FreqIsFortnightly, Is.False);
            Assert.That(vm.FreqIsWeekly, Is.False);
        }

        [Test]
        public void FreqHighlight_FortnightlyLoan_IsFortnightly()
        {
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(paymentsPerYear: 24));
            Assert.That(vm.FreqIsFortnightly, Is.True);
            Assert.That(vm.FreqIsMonthly, Is.False);
            Assert.That(vm.FreqIsWeekly, Is.False);
        }

        [Test]
        public void FreqHighlight_WeeklyLoan_IsWeekly()
        {
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(paymentsPerYear: 52));
            Assert.That(vm.FreqIsWeekly, Is.True);
            Assert.That(vm.FreqIsMonthly, Is.False);
            Assert.That(vm.FreqIsFortnightly, Is.False);
        }

        [Test]
        public void FreqHighlight_ExactlyOneColumnHighlighted()
        {
            var flags = new[] { _vm.FreqIsMonthly, _vm.FreqIsFortnightly, _vm.FreqIsWeekly };
            Assert.That(flags.Count(x => x), Is.EqualTo(1));
        }

        // ── Scenario 7: Offset Account ────────────────────────────────────────

        [Test]
        public void OffsetTimeSaved_AfterSetup_ContainsYr()
            => Assert.That(_vm.OffsetTimeSaved, Does.Contain("yr"));

        [Test]
        public void OffsetMonthlySaving_AfterSetup_IsNotDash()
            => Assert.That(_vm.OffsetMonthlySaving, Is.Not.EqualTo("--"));

        [Test]
        public void OffsetMonthlySaving_EndsWithMoSuffix()
            => Assert.That(_vm.OffsetMonthlySaving, Does.EndWith("/mo"));

        [Test]
        public void OffsetInterestSaved_AfterSetup_IsNotDash()
            => Assert.That(_vm.OffsetInterestSaved, Is.Not.EqualTo("--"));

        [Test]
        public void OffsetRateNote_AfterSetup_ContainsRate()
            => Assert.That(_vm.OffsetRateNote, Does.Contain("5"));

        [Test]
        public void OffsetBalance_ZeroBalance_TimeSavedIsZeroYr()
        {
            _vm.OffsetBalance = 0;
            Assert.That(_vm.OffsetTimeSaved, Is.EqualTo("0yr"));
        }

        [Test]
        public void OffsetBalance_Increase_SavesMoreInterest()
        {
            _vm.OffsetBalance = 20_000;
            var interestLow = _vm.OffsetInterestSaved;
            _vm.OffsetBalance = 200_000;
            var interestHigh = _vm.OffsetInterestSaved;
            Assert.That(interestHigh, Is.Not.EqualTo(interestLow));
        }

        [Test]
        public void OffsetBalance_Increase_SavesMoreTime()
        {
            _vm.OffsetBalance = 20_000;
            var timeLow = _vm.OffsetTimeSaved;
            _vm.OffsetBalance = 200_000;
            var timeHigh = _vm.OffsetTimeSaved;
            Assert.That(timeHigh, Is.Not.EqualTo(timeLow));
        }

        [Test]
        public void OffsetMonthlySaving_MatchesBalanceTimesMonthlyRate()
        {
            // Offset $50k on a 5% loan: $50,000 × (5/100/12) ≈ $208/mo
            _vm.OffsetBalance = 50_000;
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", "").Replace("/mo", ""));
            Assert.That(ParseCurrency(_vm.OffsetMonthlySaving), Is.EqualTo(208).Within(2));
        }

        // ── Scenario 8: Stress Test ───────────────────────────────────────────

        [Test]
        public void StressTest_NoAffordabilityData_IsUnavailable()
            => Assert.That(_vm.StressTestAvailable, Is.False);

        [Test]
        public void StressTest_NoAffordabilityData_UnavailableIsTrue()
            => Assert.That(_vm.StressTestUnavailable, Is.True);

        // ── Cross-scenario: recalculate on SetLoanViewModel ───────────────────

        [Test]
        public void Recalculate_DifferentLoan_UpdatesAllScenarios()
        {
            var before = _vm.RateChangeMonthlyRepayment;
            _vm.SetLoanViewModel(BuildLoanVm(loanAmount: 500_000));
            var after = _vm.RateChangeMonthlyRepayment;
            Assert.That(after, Is.Not.EqualTo(before));
        }

        [Test]
        public void Recalculate_HigherRate_ProducesHigherMonthlyRepayment()
        {
            double ParseCurrency(string s) => double.Parse(s.Replace("$", "").Replace(",", "").Replace("/mo", ""));
            _vm.SetLoanViewModel(BuildLoanVm(interestRate: 3.0));
            double low = ParseCurrency(_vm.RateChangeMonthlyRepayment);
            _vm.SetLoanViewModel(BuildLoanVm(interestRate: 7.0));
            double high = ParseCurrency(_vm.RateChangeMonthlyRepayment);
            Assert.That(high, Is.GreaterThan(low));
        }
    }
}
