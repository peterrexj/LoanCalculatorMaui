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

        /// <summary>
        /// Creates a LoanViewModel that has a real loan amount (900k), rate (5%) and term (30yr)
        /// so WhatIf calculations have meaningful input.
        /// </summary>
        private static LoanViewModel BuildLoanVm(double propertyAmount = 1_000_000, double loanAmount = 900_000,
            double interestRate = 5.0, int termYears = 30)
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
                        TotalNumberPaymentPerYear = 12
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
        {
            Assert.That(_vm.HasLoanData, Is.True);
        }

        [Test]
        public void HasNoLoanData_WhenLoanAmountPositive_IsFalse()
        {
            Assert.That(_vm.HasNoLoanData, Is.False);
        }

        [Test]
        public void HasLoanData_WhenNoLoanVm_IsFalse()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.HasLoanData, Is.False);
        }

        [Test]
        public void HasNoLoanData_WhenNoLoanVm_IsTrue()
        {
            var vm = new WhatIfViewModel(null!);
            Assert.That(vm.HasNoLoanData, Is.True);
        }

        // ── RateChangeNewRate ─────────────────────────────────────────────────

        [Test]
        public void RateChangeNewRate_DefaultDelta_ShowsBaseRatePlusHalf()
        {
            // BaseRate=5, default delta=0.5 → 5.5%
            Assert.That(_vm.RateChangeNewRate, Is.EqualTo("5.5%"));
        }

        [Test]
        public void RateChangeNewRate_CustomDelta_ShowsCorrectRate()
        {
            _vm.RateChangeDelta = 1.0;
            Assert.That(_vm.RateChangeNewRate, Is.EqualTo("6%"));
        }

        // ── RateChangeMonthlyRepayment ────────────────────────────────────────

        [Test]
        public void RateChangeMonthlyRepayment_AfterSetup_IsNotDash()
        {
            Assert.That(_vm.RateChangeMonthlyRepayment, Is.Not.EqualTo("--"));
        }

        [Test]
        public void RateChangeMonthlyRepayment_ContainsMonthSuffix()
        {
            Assert.That(_vm.RateChangeMonthlyRepayment, Does.EndWith("/mo"));
        }

        // ── RateChangeMonthlyDiff ─────────────────────────────────────────────

        [Test]
        public void RateChangeMonthlyDiff_PositiveDelta_StartsWithPlus()
        {
            _vm.RateChangeDelta = 1.0; // rate goes up → payment goes up → diff positive
            Assert.That(_vm.RateChangeMonthlyDiff, Does.StartWith("+"));
        }

        [Test]
        public void RateChangeDiffIsPositive_PositiveDelta_IsTrue()
        {
            _vm.RateChangeDelta = 1.0;
            Assert.That(_vm.RateChangeDiffIsPositive, Is.True);
        }

        // ── ExtraRepaymentTimeSaved ────────────────────────────────────────────

        [Test]
        public void ExtraRepaymentTimeSaved_AfterSetup_IsNotDash()
        {
            Assert.That(_vm.ExtraRepaymentTimeSaved, Is.Not.EqualTo("--"));
        }

        [Test]
        public void ExtraRepaymentTimeSaved_ContainsYr()
        {
            Assert.That(_vm.ExtraRepaymentTimeSaved, Does.Contain("yr"));
        }

        [Test]
        public void ExtraRepaymentInterestSaved_IsNotDash()
        {
            Assert.That(_vm.ExtraRepaymentInterestSaved, Is.Not.EqualTo("--"));
        }

        [Test]
        public void ExtraRepaymentNewPayoff_ContainsRemaining()
        {
            Assert.That(_vm.ExtraRepaymentNewPayoff, Does.Contain("remaining"));
        }

        // ── ExtraRepaymentMonthly — higher extra = more saved ─────────────────

        [Test]
        public void ExtraRepaymentTimeSaved_HigherExtra_SavesMoreMonths()
        {
            _vm.ExtraRepaymentMonthly = 200;
            var savedLow = _vm.ExtraRepaymentTimeSaved;

            _vm.ExtraRepaymentMonthly = 2000;
            var savedHigh = _vm.ExtraRepaymentTimeSaved;

            // Both should be populated and different (higher extra saves more)
            Assert.That(savedHigh, Is.Not.EqualTo(savedLow));
        }

        // ── TermComparison ────────────────────────────────────────────────────

        [Test]
        public void TermComparison20Monthly_IsNotDash()
        {
            Assert.That(_vm.TermComparison20Monthly, Is.Not.EqualTo("--"));
        }

        [Test]
        public void TermComparison25Monthly_IsNotDash()
        {
            Assert.That(_vm.TermComparison25Monthly, Is.Not.EqualTo("--"));
        }

        [Test]
        public void TermComparison30Monthly_IsNotDash()
        {
            Assert.That(_vm.TermComparison30Monthly, Is.Not.EqualTo("--"));
        }

        [Test]
        public void TermComparison20Interest_IsNotDash()
        {
            Assert.That(_vm.TermComparison20Interest, Is.Not.EqualTo("--"));
        }

        [Test]
        public void TermComparison_ShorterTerm_HasHigherMonthlyPayment()
        {
            // 20-year monthly > 30-year monthly for same loan
            var monthly20 = double.Parse(_vm.TermComparison20Monthly.Replace("$", "").Replace(",", ""));
            var monthly30 = double.Parse(_vm.TermComparison30Monthly.Replace("$", "").Replace(",", ""));
            Assert.That(monthly20, Is.GreaterThan(monthly30));
        }

        [Test]
        public void TermComparison_ShorterTerm_HasLowerTotalInterest()
        {
            var interest20 = double.Parse(_vm.TermComparison20Interest.Replace("$", "").Replace(",", ""));
            var interest30 = double.Parse(_vm.TermComparison30Interest.Replace("$", "").Replace(",", ""));
            Assert.That(interest20, Is.LessThan(interest30));
        }

        // ── DepositScenarios ──────────────────────────────────────────────────

        [Test]
        public void Deposit10PcLoan_IsNotDash()
        {
            Assert.That(_vm.Deposit10PcLoan, Is.Not.EqualTo("--"));
        }

        [Test]
        public void Deposit20PcLoan_IsNotDash()
        {
            Assert.That(_vm.Deposit20PcLoan, Is.Not.EqualTo("--"));
        }

        [Test]
        public void Deposit30PcLoan_IsNotDash()
        {
            Assert.That(_vm.Deposit30PcLoan, Is.Not.EqualTo("--"));
        }

        [Test]
        public void Deposit10PcMonthly_ContainsMonthSuffix()
        {
            Assert.That(_vm.Deposit10PcMonthly, Does.EndWith("/mo"));
        }

        [Test]
        public void DepositScenarios_HigherDeposit_LowerLoanAmount()
        {
            var loan10 = double.Parse(_vm.Deposit10PcLoan.Replace("$", "").Replace(",", ""));
            var loan30 = double.Parse(_vm.Deposit30PcLoan.Replace("$", "").Replace(",", ""));
            Assert.That(loan30, Is.LessThan(loan10));
        }

        [Test]
        public void DepositScenarios_10Pc_LoanEqualsNinetyPctOfProperty()
        {
            var loan10 = double.Parse(_vm.Deposit10PcLoan.Replace("$", "").Replace(",", ""));
            // PropertyAmount = 1,000,000 * 90% = 900,000
            Assert.That(loan10, Is.EqualTo(900_000).Within(100));
        }
    }
}
