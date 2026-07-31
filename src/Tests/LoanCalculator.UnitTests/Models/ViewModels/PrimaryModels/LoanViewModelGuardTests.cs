using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Tests that verify the three critical guards in LoanViewModel setters and trigger methods:
    ///   1. HasInitialized guard  — setters must be no-ops before MarkInitializationComplete()
    ///   2. LoadSafe guard        — trigger methods must be no-ops while SharedServiceCore.LoadSafe is true
    ///   3. PageHelper guard      — trigger methods must be no-ops while PageHelper.IsFormLoading is true
    /// </summary>
    [TestFixture]
    public class LoanViewModelGuardTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static LoanViewModel BuildUninitialised(double property = 500_000, double deposit = 50_000,
            double rate = 5.0, int term = 30)
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
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.HomeLoanInfo.DepositAmountDirectInput = deposit;
            // HasInitialized is intentionally NOT set
            return vm;
        }

        private static LoanViewModel BuildInitialised(double property = 1_000_000, double deposit = 100_000,
            double rate = 5.0, int term = 30)
        {
            var vm = BuildUninitialised(property, deposit, rate, term);
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
        // 1. HasInitialized guard — setters must be no-ops before init
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void PropertyAmount_BeforeInit_SetterIsNoOp()
        {
            var vm = BuildUninitialised(property: 500_000);
            vm.PropertyAmount = 999_999;
            Assert.That(vm.HomeLoanInfo.PropertyAmount, Is.EqualTo(500_000));
        }

        [Test]
        public void LoanTermInYears_BeforeInit_SetterIsNoOp()
        {
            var vm = BuildUninitialised(term: 30);
            vm.LoanTermInYears = 15;
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears, Is.EqualTo(30));
        }

        [Test]
        public void InterestRate_BeforeInit_SetterIsNoOp()
        {
            var vm = BuildUninitialised(rate: 5.0);
            vm.InterestRate = 9.99;
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate, Is.EqualTo(5.0));
        }

        [Test]
        public void DepositPercentage_BeforeInit_SetterIsNoOp()
        {
            var vm = BuildUninitialised();
            var before = vm.HomeLoanInfo.DepositPercentage;
            vm.DepositPercentage = 50;
            Assert.That(vm.HomeLoanInfo.DepositPercentage, Is.EqualTo(before));
        }

        [Test]
        public void DepositAmountDirectInput_BeforeInit_SetterIsNoOp()
        {
            var vm = BuildUninitialised(deposit: 50_000);
            vm.DepositAmountDirectInput = 999_000;
            Assert.That(vm.HomeLoanInfo.DepositAmountDirectInput, Is.EqualTo(50_000));
        }

        [Test]
        public void LoanAmountDirectInput_BeforeInit_SetterIsNoOp()
        {
            var vm = BuildUninitialised();
            var before = vm.HomeLoanInfo.LoanAmountDirectInput;
            vm.LoanAmountDirectInput = 999_000;
            Assert.That(vm.HomeLoanInfo.LoanAmountDirectInput, Is.EqualTo(before));
        }

        [Test]
        public void LoanAmountPercentage_BeforeInit_SetterIsNoOp()
        {
            var vm = BuildUninitialised();
            var before = vm.HomeLoanInfo.LoanAmountPercentage;
            vm.LoanAmountPercentage = 99;
            Assert.That(vm.HomeLoanInfo.LoanAmountPercentage, Is.EqualTo(before));
        }

        // Confirm the same setters DO work after init (proves the guard, not the setter)
        [Test]
        public void PropertyAmount_AfterInit_SetterWrites()
        {
            var vm = BuildInitialised(property: 500_000);
            vm.PropertyAmount = 800_000;
            Assert.That(vm.HomeLoanInfo.PropertyAmount, Is.EqualTo(800_000));
        }

        [Test]
        public void LoanTermInYears_AfterInit_SetterWrites()
        {
            var vm = BuildInitialised(term: 30);
            vm.LoanTermInYears = 15;
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears, Is.EqualTo(15));
        }

        [Test]
        public void InterestRate_AfterInit_SetterWrites()
        {
            var vm = BuildInitialised(rate: 5.0);
            vm.InterestRate = 6.5;
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate, Is.EqualTo(6.5));
        }

        // Setters before init must also fire NO PropertyChanged events
        [Test]
        public void PropertyAmount_BeforeInit_FiresNoPropertyChanged()
        {
            var vm = BuildUninitialised();
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.PropertyAmount = 999_999;

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void InterestRate_BeforeInit_FiresNoPropertyChanged()
        {
            var vm = BuildUninitialised();
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.InterestRate = 9.0;

            Assert.That(changed, Is.Empty);
        }

        // ══════════════════════════════════════════════════════════════════════
        // 2. LoadSafe guard — trigger methods must be no-ops while LoadSafe=true
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void TriggerPropertyChangedOnPropertyTab_WhileLoadSafe_FiresNoEvents()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnPropertyTab();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void TriggerPropertyChangedOnAmortizationTab_WhileLoadSafe_FiresNoEvents()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnAmortizationTab();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void RefreshExpenseTabPropertyChanged_WhileLoadSafe_FiresNoEvents()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshExpenseTabPropertyChanged();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void RefreshInsightsTabPropertyChanged_WhileLoadSafe_FiresNoEvents()
        {
            var vm = BuildInitialised();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshInsightsTabPropertyChanged();

            Assert.That(changed, Is.Empty);
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3. PageHelper guard — trigger methods must be no-ops while loading
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void TriggerPropertyChangedOnPropertyTab_WhilePageLoading_FiresNoEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnPropertyTab();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void TriggerPropertyChangedOnAmortizationTab_WhilePageLoading_FiresNoEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnAmortizationTab();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void RefreshExpenseTabPropertyChanged_WhilePageLoading_FiresNoEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshExpenseTabPropertyChanged();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void RefreshInsightsTabPropertyChanged_WhilePageLoading_FiresNoEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshInsightsTabPropertyChanged();

            Assert.That(changed, Is.Empty);
        }

        // ══════════════════════════════════════════════════════════════════════
        // 4. PropertyChanged completeness — trigger methods notify all bindings
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void TriggerPropertyChangedOnPropertyTab_NotifiesAllExpectedProperties()
        {
            var vm = BuildInitialised();
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnPropertyTab();

            var required = new[]
            {
                nameof(vm.PropertyAmount),
                nameof(vm.LoanTermInYears),
                nameof(vm.InterestRate),
                nameof(vm.DepositAmountDirectInput),
                nameof(vm.LoanAmountDirectInput),
                nameof(vm.LoanAmountStrFormatted),
                nameof(vm.DepositPercentage),
                nameof(vm.LoanAmount),
                nameof(vm.PropertyTotalAmount),
                nameof(vm.InterestRateFormatted),
                nameof(vm.DepositAmountStrFormatted),
                nameof(vm.RepaymentFrequencySelected),
                nameof(vm.HomeLoanInfo),
                nameof(vm.AffordabilityTextDescription),
                nameof(vm.WizardShowAssetTotal),
                nameof(vm.WizardAssetTotalLabel),
                nameof(vm.WizardShowLoanAmount),
                nameof(vm.WizardLoanAmountLabel),
            };
            Assert.That(changed, Is.SupersetOf(required),
                $"Missing: {string.Join(", ", required.Except(changed))}");
        }

        [Test]
        public void TriggerPropertyChangedOnAmortizationTab_NotifiesAmortizationProperties()
        {
            var vm = BuildInitialised();
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnAmortizationTab();

            var required = new[]
            {
                nameof(vm.PaymentAmortization),
                nameof(vm.AmortizationBalanceSubtitle),
                nameof(vm.AffordabilityTextDescription),
            };
            Assert.That(changed, Is.SupersetOf(required),
                $"Missing: {string.Join(", ", required.Except(changed))}");
        }

        [Test]
        public void RefreshExpenseTabPropertyChanged_NotifiesExpenseTabProperties()
        {
            var vm = BuildInitialised();
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.RefreshExpenseTabPropertyChanged();

            var required = new[]
            {
                nameof(vm.TotalMonthlyExpenseWithComma),
                nameof(vm.TotalYearlyExpenseWithComma),
                nameof(vm.Transactions),
                nameof(vm.FilteredTransactions),
                nameof(vm.TotalMonthlyOverallExpense),
                nameof(vm.AffordabilityTextDescription),
            };
            Assert.That(changed, Is.SupersetOf(required),
                $"Missing: {string.Join(", ", required.Except(changed))}");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 5. Guard interaction — guards are independent, both can be active
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void TriggerPropertyChangedOnPropertyTab_BothGuardsActive_FiresNoEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.TriggerPropertyChangedOnPropertyTab();

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void TriggerPropertyChangedOnPropertyTab_AfterGuardsLifted_FiresEvents()
        {
            var vm = BuildInitialised();
            PageHelper.PageIsLoading();
            SharedServiceCore.LoadSafeOn();

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            // While guarded — nothing fires
            vm.TriggerPropertyChangedOnPropertyTab();
            Assert.That(changed, Is.Empty);

            // Lift guards — now it fires
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            vm.TriggerPropertyChangedOnPropertyTab();
            Assert.That(changed, Is.Not.Empty);
        }
    }
}
