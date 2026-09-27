using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Tier-2 integration tests: verify that the configurable settings flow end-to-end
    /// from SharedServiceCore defaults into the ViewModels that consume them.
    ///
    /// Because Preferences is unavailable in the test environment, every Get*() helper
    /// returns its hardcoded fallback.  These tests confirm the full chain:
    ///   SharedServiceCore.Get*() default  →  ViewModel field initializer / property  →  correct value
    ///
    /// If any ViewModel is accidentally wired to a different constant, or a default in
    /// SharedServiceCore is changed, one of these tests will catch it.
    /// </summary>
    [TestFixture]
    public class CalculatorSettingsIntegrationTests
    {
        [SetUp]
        public void SetUp()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
        }

        [TearDown]
        public void TearDown()
        {
            SharedServiceCore.LoadSafeOff();
            PageHelper.PageLoadingComplete();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static LoanViewModel BuildLoanVm(
            double loanAmount = 900_000,
            double interestRate = 5.0,
            int termYears = 30)
        {
            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = interestRate,
                        LoanTermInYears = termYears,
                        TotalNumberPaymentPerYear = 12
                    },
                    PropertyAmount = 1_000_000
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.HomeLoanInfo.LoanAmountDirectInput = loanAmount;
            vm.MarkInitializationComplete();
            return vm;
        }

        // ── WhatIfViewModel: field initializers use SharedServiceCore defaults ─

        [Test]
        public void WhatIfViewModel_RateChangeDelta_MatchesSettingsDefault()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.RateChangeDelta, Is.EqualTo(SharedServiceCore.GetWhatIfRateDelta()));
        }

        [Test]
        public void WhatIfViewModel_ExtraRepaymentMonthly_MatchesSettingsDefault()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.ExtraRepaymentMonthly, Is.EqualTo(SharedServiceCore.GetWhatIfExtraRepayment()));
        }

        [Test]
        public void WhatIfViewModel_LumpSumAmount_MatchesSettingsDefault()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.LumpSumAmount, Is.EqualTo(SharedServiceCore.GetWhatIfLumpSum()));
        }

        [Test]
        public void WhatIfViewModel_OffsetBalance_MatchesSettingsDefault()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.OffsetBalance, Is.EqualTo(SharedServiceCore.GetWhatIfOffsetBalance()));
        }

        [Test]
        public void WhatIfViewModel_CombinedExtraMonthly_MatchesSettingsDefault()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.CombinedExtraMonthly, Is.EqualTo(SharedServiceCore.GetWhatIfExtraRepayment()));
        }

        [Test]
        public void WhatIfViewModel_CombinedLumpSum_MatchesSettingsDefault()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.CombinedLumpSum, Is.EqualTo(SharedServiceCore.GetWhatIfLumpSum()));
        }

        [Test]
        public void WhatIfViewModel_CombinedOffset_MatchesSettingsDefault()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.CombinedOffset, Is.EqualTo(SharedServiceCore.GetWhatIfOffsetBalance()));
        }

        // ── LoanViewModel: getter null-coalesce fallback uses settings default ─

        [Test]
        public void LoanViewModel_InterestRate_FallsBackToSettingsDefault_WhenHomeLoanInfoNull()
        {
            var vm = new LoanViewModel(); // HomeLoanInfo is null
            Assert.That(vm.InterestRate, Is.EqualTo(SharedServiceCore.GetDefaultInterestRate()));
        }

        [Test]
        public void LoanViewModel_LoanTermInYears_FallsBackToSettingsDefault_WhenHomeLoanInfoNull()
        {
            var vm = new LoanViewModel();
            Assert.That(vm.LoanTermInYears, Is.EqualTo(SharedServiceCore.GetDefaultLoanTermYears()));
        }

        // ── LoanViewModel: AddDefaultValues() writes settings defaults ─────────

        [Test]
        public void LoanViewModel_AddDefaultValues_SetsInterestRateFromSettings()
        {
            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = 0,
                        LoanTermInYears = 0,
                        TotalNumberPaymentPerYear = 12
                    },
                    PropertyAmount = 500_000
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.MarkInitializationComplete();

            vm.AddDefaultValues();

            Assert.That(vm.InterestRate, Is.EqualTo(SharedServiceCore.GetDefaultInterestRate()));
        }

        [Test]
        public void LoanViewModel_AddDefaultValues_SetsLoanTermFromSettings()
        {
            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = 0,
                        LoanTermInYears = 0,
                        TotalNumberPaymentPerYear = 12
                    },
                    PropertyAmount = 500_000
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.MarkInitializationComplete();

            vm.AddDefaultValues();

            Assert.That(vm.LoanTermInYears, Is.EqualTo(SharedServiceCore.GetDefaultLoanTermYears()));
        }

        // ── BudgetViewModel: projection year defaults ─────────────────────────

        [Test]
        public void BudgetViewModel_MaxProjectionYears_MatchesSettingsDefault()
        {
            var vm = new BudgetViewModel();
            Assert.That(vm.MaxProjectionYears, Is.EqualTo(SharedServiceCore.GetMaxProjectionYears()));
        }

        [Test]
        public void BudgetViewModel_ProjectionYears_StartsAtSettingsDefault()
        {
            var vm = new BudgetViewModel();
            Assert.That(vm.ProjectionYears, Is.EqualTo(SharedServiceCore.GetDefaultProjectionYears()));
        }

        [Test]
        public void BudgetViewModel_DefaultProjectionYears_DoesNotExceedMaxProjectionYears()
        {
            var vm = new BudgetViewModel();
            Assert.That(vm.ProjectionYears, Is.LessThanOrEqualTo(vm.MaxProjectionYears));
        }

        // ── IncomeViewModel + ExpenseViewModel: MaxProjectionYears ───────────

        [Test]
        public void IncomeViewModel_MaxProjectionYears_MatchesSettingsDefault()
        {
            var vm = new IncomeViewModel(null!, null!);
            Assert.That(vm.MaxProjectionYears, Is.EqualTo(SharedServiceCore.GetMaxProjectionYears()));
        }

        [Test]
        public void ExpenseViewModel_MaxProjectionYears_MatchesSettingsDefault()
        {
            var vm = new ExpenseViewModel(null!, null!);
            Assert.That(vm.MaxProjectionYears, Is.EqualTo(SharedServiceCore.GetMaxProjectionYears()));
        }

        // ── WhatIfViewModel: property value clamping ─────────────────────────

        [Test]
        public void WhatIfViewModel_LumpSumAmount_ClampedToZero_WhenSetNegative()
        {
            var vm = new WhatIfViewModel(null!);
            vm.LumpSumAmount = -500;
            Assert.That(vm.LumpSumAmount, Is.EqualTo(0));
        }

        [Test]
        public void WhatIfViewModel_OffsetBalance_ClampedToZero_WhenSetNegative()
        {
            var vm = new WhatIfViewModel(null!);
            vm.OffsetBalance = -1000;
            Assert.That(vm.OffsetBalance, Is.EqualTo(0));
        }

        [Test]
        public void WhatIfViewModel_CombinedExtraMonthly_ClampedToZero_WhenSetNegative()
        {
            var vm = new WhatIfViewModel(null!);
            vm.CombinedExtraMonthly = -200;
            Assert.That(vm.CombinedExtraMonthly, Is.EqualTo(0));
        }

        [Test]
        public void WhatIfViewModel_CombinedLumpSum_ClampedToZero_WhenSetNegative()
        {
            var vm = new WhatIfViewModel(null!);
            vm.CombinedLumpSum = -9999;
            Assert.That(vm.CombinedLumpSum, Is.EqualTo(0));
        }

        [Test]
        public void WhatIfViewModel_CombinedOffset_ClampedToZero_WhenSetNegative()
        {
            var vm = new WhatIfViewModel(null!);
            vm.CombinedOffset = -1;
            Assert.That(vm.CombinedOffset, Is.EqualTo(0));
        }

        [Test]
        public void WhatIfViewModel_RateChangeDelta_RoundedToTwoDecimalPlaces()
        {
            var vm = new WhatIfViewModel(null!);
            vm.RateChangeDelta = 0.12345;
            Assert.That(vm.RateChangeDelta, Is.EqualTo(0.12));
        }

        [Test]
        public void WhatIfViewModel_RateChangeDelta_PositiveValue_StoredCorrectly()
        {
            var vm = new WhatIfViewModel(null!);
            vm.RateChangeDelta = 1.5;
            Assert.That(vm.RateChangeDelta, Is.EqualTo(1.5));
        }

        // ── WhatIfViewModel: initial values stay within expected bounds ───────

        [Test]
        public void WhatIfViewModel_AllInitialValues_AreNonNegative()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.RateChangeDelta, Is.GreaterThanOrEqualTo(0), "RateChangeDelta");
            Assert.That(vm.ExtraRepaymentMonthly, Is.GreaterThanOrEqualTo(0), "ExtraRepaymentMonthly");
            Assert.That(vm.LumpSumAmount, Is.GreaterThanOrEqualTo(0), "LumpSumAmount");
            Assert.That(vm.OffsetBalance, Is.GreaterThanOrEqualTo(0), "OffsetBalance");
            Assert.That(vm.CombinedExtraMonthly, Is.GreaterThanOrEqualTo(0), "CombinedExtraMonthly");
            Assert.That(vm.CombinedLumpSum, Is.GreaterThanOrEqualTo(0), "CombinedLumpSum");
            Assert.That(vm.CombinedOffset, Is.GreaterThanOrEqualTo(0), "CombinedOffset");
        }

        // ── WhatIfViewModel: RateChangeNewRate reflects initial RateChangeDelta ─

        [Test]
        public void WhatIfViewModel_RateChangeNewRate_ReflectsInitialDelta_WhenLoanSet()
        {
            var vm = new WhatIfViewModel(null!);
            vm.SetLoanViewModel(BuildLoanVm(interestRate: 5.0));
            // Default delta is 0.5, base rate is 5.0 → new rate should be 5.5%
            Assert.That(vm.RateChangeNewRate, Is.EqualTo("5.5%"));
        }
    }
}
