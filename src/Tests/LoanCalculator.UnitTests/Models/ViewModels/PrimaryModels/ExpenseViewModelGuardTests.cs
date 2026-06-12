using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Tests that verify LoadSafe and PageHelper.IsFormLoading guards in ExpenseViewModel,
    /// and that RefreshIncomePropertyChanged and TriggerPropertyChangedOnProjectionTab
    /// notify all expected binding properties.
    /// </summary>
    [TestFixture]
    public class ExpenseViewModelGuardTests
    {
        private static ExpenseViewModel BuildInitialised()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            var vm = new ExpenseViewModel();
            vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
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
        // LoadSafe guard
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void RefreshIncomePropertyChanged_WhileLoadSafe_FiresNoEvents()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshIncomePropertyChanged();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void UpdateProjectionData_WhileLoadSafe_DoesNotThrow()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            Assert.DoesNotThrow(() => vm.UpdateProjectionData());
        }

        [Test]
        public void TriggerPropertyChangedOnProjectionTab_WhileLoadSafe_FiresNoEvents()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnProjectionTab();

            Assert.That(changed, Is.Empty);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PageHelper guard
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void RefreshIncomePropertyChanged_WhilePageLoading_FiresNoEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshIncomePropertyChanged();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void TriggerPropertyChangedOnProjectionTab_WhilePageLoading_FiresNoEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnProjectionTab();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void RefreshTransactionEntry_WhilePageLoading_FiresNoEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshTransactionEntry();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void RefreshTransactionEntry_WhileLoadSafe_FiresNoEvents()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshTransactionEntry();

            Assert.That(changed, Is.Empty);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PropertyChanged completeness
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void RefreshIncomePropertyChanged_NotifiesAllExpectedProperties()
        {
            var vm = BuildInitialised();
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshIncomePropertyChanged();

            var required = new[]
            {
                nameof(vm.TotalMonthlyExpense),
                nameof(vm.TotalYearlyExpense),
                nameof(vm.TotalMonthlyIncomeWithComma),
                nameof(vm.TotalMonthlySumExpenseWithComma),
                nameof(vm.TotalYearlyIncomeWithComma),
                nameof(vm.TotalIncomeMonthlyWithComma),
                nameof(vm.Transactions),
                nameof(vm.FilteredTransactions),
                nameof(vm.IncomeEntryAmountText),
                nameof(vm.AutocompleteNameList),
                nameof(vm.IsEditMode),
                nameof(vm.ShowIncomeAfterExpense),
                nameof(vm.StringIncomeTextOnTopBox),
            };
            Assert.That(changed, Is.SupersetOf(required),
                $"Missing: {string.Join(", ", required.Except(changed))}");
        }

        [Test]
        public void TriggerPropertyChangedOnProjectionTab_NotifiesProjectionProperties()
        {
            var vm = BuildInitialised();
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnProjectionTab();

            var required = new[]
            {
                nameof(vm.ChartProjectionTermStartAmountAxis),
                nameof(vm.ChartProjectionIncomeExpenseAmountAxis),
                nameof(vm.ChartProjectionDeductionAmountAxis),
                nameof(vm.TotalYearsToProject),
                nameof(vm.TotalProjectedYearlyIncomeWithComma),
                nameof(vm.AnnualGrowthRatePercentage),
                nameof(vm.AnnualGrowthRate),
                nameof(vm.IncomeProjectList),
            };
            Assert.That(changed, Is.SupersetOf(required),
                $"Missing: {string.Join(", ", required.Except(changed))}");
        }

        [Test]
        public void RefreshTransactionEntry_NotifiesFormFields()
        {
            var vm = BuildInitialised();
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshTransactionEntry();

            var required = new[]
            {
                nameof(vm.IncomeEntryName),
                nameof(vm.HasErrorIncomeDescription),
                nameof(vm.IncomeEntryAmount),
                nameof(vm.IncomeEntryAmountText),
                nameof(vm.HasErrorIncomeAmount),
                nameof(vm.IsExpenseDataFormReadyToSubmit),
            };
            Assert.That(changed, Is.SupersetOf(required),
                $"Missing: {string.Join(", ", required.Except(changed))}");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Guard lifting
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void RefreshIncomePropertyChanged_AfterLoadSafeLifted_FiresEvents()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshIncomePropertyChanged();
            Assert.That(changed, Is.Empty, "should be empty while LoadSafe is on");

            SharedServiceCore.LoadSafeOff();
            vm.RefreshIncomePropertyChanged();
            Assert.That(changed, Is.Not.Empty, "should fire after LoadSafe is lifted");
        }

        [Test]
        public void RefreshIncomePropertyChanged_NullTransactionRecords_DoesNotThrow()
        {
            var vm = BuildInitialised();
            vm.TransactionRecords = null;

            Assert.DoesNotThrow(() => vm.RefreshIncomePropertyChanged());
        }
    }
}
