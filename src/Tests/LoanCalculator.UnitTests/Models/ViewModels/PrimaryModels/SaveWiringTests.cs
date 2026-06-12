using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using Moq;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Verifies that trigger methods schedule a save to ILocalStorage after their work.
    ///
    /// Strategy: inject a Mock{ILocalStorage} via SharedServiceCore.SetLocalStorage, call the
    /// method under test, then immediately flush the 600 ms debounce with FlushPendingSave so
    /// the mock can be verified synchronously without waiting for a timer.
    /// </summary>
    [TestFixture]
    public class SaveWiringTests
    {
        private Mock<ILocalStorage> _storageMock = null!;

        [SetUp]
        public void SetUp()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            _storageMock = new Mock<ILocalStorage>(MockBehavior.Loose);
            _storageMock.Setup(s => s.SaveData(It.IsAny<object>())).Returns(Task.CompletedTask);
            SharedServiceCore.SetLocalStorage(_storageMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            SharedServiceCore.ResetLocalStorage();
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static IncomeViewModel BuildIncomeVm()
        {
            var vm = new IncomeViewModel
            {
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.MarkInitializationComplete();
            return vm;
        }

        private static ExpenseViewModel BuildExpenseVm()
        {
            var vm = new ExpenseViewModel
            {
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.MarkInitializationComplete();
            return vm;
        }

        private static LoanViewModel BuildLoanVm()
        {
            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    PropertyAmount = 1_000_000,
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = 5.0,
                        LoanTermInYears = 30,
                        TotalNumberPaymentPerYear = 12
                    }
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.HomeLoanInfo.DepositAmountDirectInput = 100_000;
            vm.MarkInitializationComplete();
            return vm;
        }

        // ══════════════════════════════════════════════════════════════════════
        // IncomeViewModel — RefreshIncomePropertyChanged schedules a save
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void IncomeViewModel_RefreshIncomePropertyChanged_SavesViaStorage()
        {
            var vm = BuildIncomeVm();

            vm.RefreshIncomePropertyChanged();
            vm.FlushPendingSave(() => SharedServiceCore.SaveData(vm));

            _storageMock.Verify(s => s.SaveData(vm), Times.AtLeastOnce());
        }

        [Test]
        public void IncomeViewModel_TriggerPropertyChangedOnProjectionTab_SavesViaStorage()
        {
            var vm = BuildIncomeVm();

            vm.TriggerPropertyChangedOnProjectionTab();
            vm.FlushPendingSave(() => SharedServiceCore.SaveData(vm));

            _storageMock.Verify(s => s.SaveData(vm), Times.AtLeastOnce());
        }

        // ══════════════════════════════════════════════════════════════════════
        // ExpenseViewModel — RefreshIncomePropertyChanged schedules a save
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void ExpenseViewModel_RefreshIncomePropertyChanged_SavesViaStorage()
        {
            var vm = BuildExpenseVm();

            vm.RefreshIncomePropertyChanged();
            vm.FlushPendingSave(() => SharedServiceCore.SaveData(vm));

            _storageMock.Verify(s => s.SaveData(vm), Times.AtLeastOnce());
        }

        [Test]
        public void ExpenseViewModel_TriggerPropertyChangedOnProjectionTab_SavesViaStorage()
        {
            var vm = BuildExpenseVm();

            vm.TriggerPropertyChangedOnProjectionTab();
            vm.FlushPendingSave(() => SharedServiceCore.SaveData(vm));

            _storageMock.Verify(s => s.SaveData(vm), Times.AtLeastOnce());
        }

        // ══════════════════════════════════════════════════════════════════════
        // LoanViewModel — trigger methods schedule a save
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void LoanViewModel_TriggerPropertyChangedOnPropertyTab_SavesViaStorage()
        {
            var vm = BuildLoanVm();

            vm.TriggerPropertyChangedOnPropertyTab();
            vm.FlushPendingSave(() => SharedServiceCore.SaveData(vm));

            _storageMock.Verify(s => s.SaveData(vm), Times.AtLeastOnce());
        }

        [Test]
        public void LoanViewModel_TriggerPropertyChangedOnAmortizationTab_SavesViaStorage()
        {
            var vm = BuildLoanVm();

            vm.TriggerPropertyChangedOnAmortizationTab();
            vm.FlushPendingSave(() => SharedServiceCore.SaveData(vm));

            _storageMock.Verify(s => s.SaveData(vm), Times.AtLeastOnce());
        }

        [Test]
        public void LoanViewModel_RefreshExpenseTabPropertyChanged_SavesViaStorage()
        {
            var vm = BuildLoanVm();

            vm.RefreshExpenseTabPropertyChanged();
            vm.FlushPendingSave(() => SharedServiceCore.SaveData(vm));

            _storageMock.Verify(s => s.SaveData(vm), Times.AtLeastOnce());
        }

        // ══════════════════════════════════════════════════════════════════════
        // Guard: no save when LoadSafe is active
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void IncomeViewModel_RefreshIncomePropertyChanged_WhileLoadSafe_DoesNotSave()
        {
            var vm = BuildIncomeVm();
            SharedServiceCore.LoadSafeOn();

            vm.RefreshIncomePropertyChanged();
            // No FlushPendingSave needed — ScheduleSave was never called

            _storageMock.Verify(s => s.SaveData(It.IsAny<IncomeViewModel>()), Times.Never());
        }

        [Test]
        public void LoanViewModel_TriggerPropertyChangedOnPropertyTab_WhileLoadSafe_DoesNotSave()
        {
            var vm = BuildLoanVm();
            SharedServiceCore.LoadSafeOn();

            vm.TriggerPropertyChangedOnPropertyTab();

            _storageMock.Verify(s => s.SaveData(It.IsAny<LoanViewModel>()), Times.Never());
        }

        // ══════════════════════════════════════════════════════════════════════
        // Debounce — rapid calls collapse to a single save
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void IncomeViewModel_RapidRefreshCalls_FlushProducesExactlyOneSave()
        {
            var vm = BuildIncomeVm();

            // Schedule three saves in quick succession
            vm.RefreshIncomePropertyChanged();
            vm.RefreshIncomePropertyChanged();
            vm.RefreshIncomePropertyChanged();

            // FlushPendingSave cancels the pending debounce and fires synchronously once
            vm.FlushPendingSave(() => SharedServiceCore.SaveData(vm));

            _storageMock.Verify(s => s.SaveData(vm), Times.Exactly(1));
        }
    }
}
