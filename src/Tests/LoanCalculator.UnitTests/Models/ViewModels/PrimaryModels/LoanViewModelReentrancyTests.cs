using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Verifies the isUpdating reentrancy guard in LoanViewModel.
    ///
    /// Each guarded setter sets isUpdating=true before calling trigger methods, which in turn
    /// fire OnPropertyChanged notifications that could re-enter the setter. The guard prevents
    /// StackOverflowException and ensures the final model value is always the one passed in.
    ///
    /// Also verifies the guard is always cleared (IsUpdating == false) after every setter returns,
    /// and that manually setting IsUpdating=true from outside makes the setter a no-op.
    /// </summary>
    [TestFixture]
    public class LoanViewModelReentrancyTests
    {
        private static LoanViewModel BuildInitialised(
            double property = 1_000_000,
            double deposit = 100_000,
            double rate = 5.0,
            int term = 30)
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    PropertyAmount = property,
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = rate,
                        LoanTermInYears = term,
                        TotalNumberPaymentPerYear = 12
                    }
                },
                TransactionRecords = new LoanCalculator.Core.Models.Income.Incomes { IncomeExpenseEntries = [] }
            };
            vm.HomeLoanInfo.DepositAmountDirectInput = deposit;
            vm.MarkInitializationComplete();
            return vm;
        }

        [SetUp]
        public void SetUp()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
        }

        [TearDown]
        public void TearDown()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Guard prevents re-entry — value written once, final value is correct
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void PropertyAmount_SetterDoesNotThrow_AndValueIsCorrect()
        {
            var vm = BuildInitialised(property: 1_000_000);
            Assert.DoesNotThrow(() => vm.PropertyAmount = 750_000);
            Assert.That(vm.HomeLoanInfo.PropertyAmount, Is.EqualTo(750_000));
        }

        [Test]
        public void LoanTermInYears_SetterDoesNotThrow_AndValueIsCorrect()
        {
            var vm = BuildInitialised(term: 30);
            Assert.DoesNotThrow(() => vm.LoanTermInYears = 25);
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears, Is.EqualTo(25));
        }

        [Test]
        public void InterestRate_SetterDoesNotThrow_AndValueIsCorrect()
        {
            var vm = BuildInitialised(rate: 5.0);
            Assert.DoesNotThrow(() => vm.InterestRate = 6.5);
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate, Is.EqualTo(6.5));
        }

        [Test]
        public void DepositAmountDirectInput_SetterDoesNotThrow_AndValueIsCorrect()
        {
            var vm = BuildInitialised(deposit: 100_000);
            Assert.DoesNotThrow(() => vm.DepositAmountDirectInput = 200_000);
            Assert.That(vm.HomeLoanInfo.DepositAmountDirectInput, Is.EqualTo(200_000));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Guard is always released — IsUpdating is false after every setter
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void PropertyAmount_AfterSet_IsUpdatingIsFalse()
        {
            var vm = BuildInitialised();
            vm.PropertyAmount = 800_000;
            Assert.That(vm.IsUpdating, Is.False);
        }

        [Test]
        public void LoanTermInYears_AfterSet_IsUpdatingIsFalse()
        {
            var vm = BuildInitialised();
            vm.LoanTermInYears = 20;
            Assert.That(vm.IsUpdating, Is.False);
        }

        [Test]
        public void InterestRate_AfterSet_IsUpdatingIsFalse()
        {
            var vm = BuildInitialised();
            vm.InterestRate = 7.0;
            Assert.That(vm.IsUpdating, Is.False);
        }

        [Test]
        public void DepositAmountDirectInput_AfterSet_IsUpdatingIsFalse()
        {
            var vm = BuildInitialised();
            vm.DepositAmountDirectInput = 150_000;
            Assert.That(vm.IsUpdating, Is.False);
        }

        // ══════════════════════════════════════════════════════════════════════
        // External IsUpdating=true makes each setter a no-op
        // (simulates a caller mid-cascade setting the guard)
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void PropertyAmount_WhileIsUpdatingTrue_SetterIsNoOp()
        {
            var vm = BuildInitialised(property: 1_000_000);
            vm.IsUpdating = true;
            vm.PropertyAmount = 999_999;
            // Value should be unchanged since the guard bailed out
            Assert.That(vm.HomeLoanInfo.PropertyAmount, Is.EqualTo(1_000_000));
        }

        [Test]
        public void InterestRate_WhileIsUpdatingTrue_SetterIsNoOp()
        {
            var vm = BuildInitialised(rate: 5.0);
            vm.IsUpdating = true;
            vm.InterestRate = 9.99;
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate, Is.EqualTo(5.0));
        }

        [Test]
        public void LoanTermInYears_WhileIsUpdatingTrue_SetterIsNoOp()
        {
            var vm = BuildInitialised(term: 30);
            vm.IsUpdating = true;
            vm.LoanTermInYears = 10;
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears, Is.EqualTo(30));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Cascade — setting one property triggers recalculation without re-entering
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void PropertyAmount_ThenInterestRate_BothValuesCorrect()
        {
            var vm = BuildInitialised(property: 1_000_000, rate: 5.0);
            vm.PropertyAmount = 600_000;
            vm.InterestRate = 4.5;
            Assert.That(vm.HomeLoanInfo.PropertyAmount, Is.EqualTo(600_000));
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate, Is.EqualTo(4.5));
            Assert.That(vm.IsUpdating, Is.False);
        }

        [Test]
        public void MultipleSettersInSequence_NoThrow_AllValuesCorrect()
        {
            var vm = BuildInitialised(property: 1_000_000, deposit: 100_000, rate: 5.0, term: 30);

            Assert.DoesNotThrow(() =>
            {
                vm.PropertyAmount = 500_000;
                vm.DepositAmountDirectInput = 50_000;
                vm.InterestRate = 3.5;
                vm.LoanTermInYears = 20;
            });

            Assert.That(vm.HomeLoanInfo.PropertyAmount, Is.EqualTo(500_000));
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate, Is.EqualTo(3.5));
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears, Is.EqualTo(20));
            Assert.That(vm.IsUpdating, Is.False);
        }
    }
}
