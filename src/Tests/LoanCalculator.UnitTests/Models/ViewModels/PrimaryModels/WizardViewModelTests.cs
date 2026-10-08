using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using Moq;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Covers WizardViewModel, which took over the Quick Setup state that used to live on
    /// LoanViewModel. Tests for the three IsWizardStepNVisible flags are gone with them — the
    /// wizard is a single page now, so there are no steps to show or hide.
    /// </summary>
    [TestFixture]
    public class WizardViewModelTests
    {
        private LoanViewModel _loan;
        private IncomeViewModel _income;
        private ExpenseViewModel _expense;
        private WizardViewModel _vm;

        private static LoanViewModel BuildLoan(
            double propertyAmount = 1_000_000, double depositDirect = 100_000)
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = 5.0,
                        LoanTermInYears = 30,
                        TotalNumberPaymentPerYear = 12
                    },
                    PropertyAmount = propertyAmount
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.HomeLoanInfo.DepositAmountDirectInput = depositDirect;
            vm.MarkInitializationComplete();
            return vm;
        }

        private static IncomeViewModel BuildIncome()
        {
            var vm = new IncomeViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            vm.MarkInitializationComplete();
            return vm;
        }

        private static ExpenseViewModel BuildExpense()
        {
            var vm = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            vm.MarkInitializationComplete();
            return vm;
        }

        [SetUp]
        public void SetUp()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            _loan = BuildLoan();
            _income = BuildIncome();
            _expense = BuildExpense();
            _vm = new WizardViewModel(_loan, _income, _expense,
                new Mock<IErrorHandlingService>(MockBehavior.Loose).Object);
        }

        [TearDown]
        public void TearDown()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
        }

        // ── HasValue / Editable ──────────────────────────────────────────────

        [Test]
        public void WizardRunningCostHasValue_NoEntries_IsFalse()
        {
            Assert.That(_vm.WizardRunningCostHasValue, Is.False);
        }

        [Test]
        public void WizardRunningCostHasValue_WithPositiveEntry_IsTrue()
        {
            _loan.TransactionRecords.Add("Rates", 150, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            Assert.That(_vm.WizardRunningCostHasValue, Is.True);
        }

        [Test]
        public void WizardIncomeHasValue_PeerWithEntry_IsTrue()
        {
            _income.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            Assert.That(_vm.WizardIncomeHasValue, Is.True);
        }

        [Test]
        public void WizardIncomeHasValue_PeerWithNoEntries_IsFalse() =>
            Assert.That(_vm.WizardIncomeHasValue, Is.False);

        [Test]
        public void WizardExpenseHasValue_PeerWithNoEntries_IsFalse() =>
            Assert.That(_vm.WizardExpenseHasValue, Is.False);

        [Test]
        public void WizardExpenseHasValue_PeerWithEntry_IsTrue()
        {
            _expense.TransactionRecords.Add("Rent", 2000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            Assert.That(_vm.WizardExpenseHasValue, Is.True);
        }

        [Test]
        public void WizardUpfrontHasValue_NoUpfrontCosts_IsFalse()
        {
            Assert.That(_vm.WizardUpfrontHasValue, Is.False);
        }

        // ── Section B visibility ─────────────────────────────────────────────
        //
        // Upfront, running cost, income and expenses each have their own editing screen, so the
        // page shows them only while empty. *NeedsInput is what IsVisible binds to, so these pin
        // the rule: a figure that exists is not offered here.

        [Test]
        public void RunningCost_NeedsInputOnlyWhileEmpty()
        {
            Assert.That(_vm.WizardRunningCostNeedsInput, Is.True, "nothing recorded — offer the field");

            _loan.TransactionRecords.Add("Rates", 150, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            Assert.That(_vm.WizardRunningCostNeedsInput, Is.False, "recorded elsewhere — hide the field");
        }

        [Test]
        public void Income_NeedsInputOnlyWhileEmpty()
        {
            Assert.That(_vm.WizardIncomeNeedsInput, Is.True);

            _income.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            Assert.That(_vm.WizardIncomeNeedsInput, Is.False);
        }

        [Test]
        public void Expense_NeedsInputOnlyWhileEmpty()
        {
            Assert.That(_vm.WizardExpenseNeedsInput, Is.True);

            _expense.TransactionRecords.Add("Rent", 2000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            Assert.That(_vm.WizardExpenseNeedsInput, Is.False);
        }

        [Test]
        public void Upfront_NeedsInputOnlyWhileEmpty()
        {
            Assert.That(_vm.WizardUpfrontNeedsInput, Is.True);

            _loan.OtherExpenses = 15_000;

            Assert.That(_vm.WizardUpfrontNeedsInput, Is.False);
        }

        /// <summary>
        /// The note is the only thing telling the user that hidden figures still feed the
        /// calculation, so it must name exactly what was taken from elsewhere.
        /// </summary>
        [Test]
        public void InheritedNote_NamesOnlyTheFiguresTakenFromElsewhere()
        {
            Assert.That(_vm.WizardShowInheritedNote, Is.False, "nothing inherited yet");

            _income.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _expense.TransactionRecords.Add("Rent", 2000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            Assert.Multiple(() =>
            {
                Assert.That(_vm.WizardShowInheritedNote, Is.True);
                Assert.That(_vm.WizardInheritedNote, Does.Contain("income").And.Contain("expenses"));
                Assert.That(_vm.WizardInheritedNote, Does.Not.Contain("running costs"),
                    "running costs were not recorded, so they must not be claimed");
            });
        }

        // ── Summary labels ───────────────────────────────────────────────────

        [Test]
        public void WizardRunningCostSummary_ContainsMoSuffix() =>
            Assert.That(_vm.WizardRunningCostSummary, Does.Contain("/mo"));

        [Test]
        public void WizardRunningCostSummary_WithEntry_ContainsAmount()
        {
            _loan.TransactionRecords.Add("Rates", 150, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.Refresh();

            Assert.That(_vm.WizardRunningCostSummary, Does.Contain("150"));
        }

        [Test]
        public void WizardIncomeSummary_WithEntry_ContainsAmount()
        {
            _income.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.Refresh();

            Assert.That(_vm.WizardIncomeSummary, Does.Contain("5,000").Or.Contain("5000"));
        }

        [Test]
        public void WizardAssetTotalLabel_ContainsAssetCostText() =>
            Assert.That(_vm.WizardAssetTotalLabel, Does.Contain("asset cost").IgnoreCase);

        [Test]
        public void WizardLoanAmountLabel_ContainsLoanAmountText() =>
            Assert.That(_vm.WizardLoanAmountLabel, Does.Contain("Loan amount").IgnoreCase);

        [Test]
        public void WizardShowAssetTotal_WithPropertyAmount_IsTrue() =>
            Assert.That(_vm.WizardShowAssetTotal, Is.True);

        [Test]
        public void WizardShowLoanAmount_WithLoanAmount_IsTrue() =>
            Assert.That(_vm.WizardShowLoanAmount, Is.True);



        // ── Transient text inputs ────────────────────────────────────────────

        [Test]
        public void WizardAssetText_SetValue_FiresPropertyChanged()
        {
            var changed = new List<string?>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _vm.WizardAssetText = "500000";

            Assert.That(changed, Does.Contain(nameof(_vm.WizardAssetText)));
            Assert.That(_vm.WizardAssetText, Is.EqualTo("500000"));
        }

        [Test]
        public void WizardDepositText_SetValue_FiresPropertyChanged()
        {
            var changed = new List<string?>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _vm.WizardDepositText = "50000";

            Assert.That(changed, Does.Contain(nameof(_vm.WizardDepositText)));
            Assert.That(_vm.WizardDepositText, Is.EqualTo("50000"));
        }

        /// <summary>
        /// The unchanged-value guard is load-bearing, not an optimisation: without it the property
        /// notification can push a value back into the Entry mid-keystroke.
        /// </summary>
        [Test]
        public void WizardAssetText_SetSameValueTwice_FiresPropertyChangedOnce()
        {
            _vm.WizardAssetText = "500000";

            var changed = new List<string?>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            _vm.WizardAssetText = "500000";

            Assert.That(changed, Is.Empty, "setting the same value must not notify");
        }

        // ── Prepopulate / Commit ─────────────────────────────────────────────

        [Test]
        public void Prepopulate_FillsTextFromTheLoanViewModel()
        {
            _vm.Prepopulate();

            Assert.Multiple(() =>
            {
                Assert.That(_vm.WizardAssetText, Does.Contain("1,000,000"));
                Assert.That(_vm.WizardDepositText, Does.Contain("100,000"));
            });
        }

        [Test]
        public void Commit_WritesAssetAndDeposit()
        {
            _vm.WizardAssetText = "850,000";
            _vm.WizardDepositText = "200,000";

            Assert.That(_vm.Commit(), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(_loan.PropertyAmount, Is.EqualTo(850_000).Within(1));
                Assert.That(_loan.DepositAmountDirectInput, Is.EqualTo(200_000).Within(1));
            });
        }

        /// <summary>
        /// Regression: entering an asset price while no deposit is given must not leave the deposit
        /// holding the whole asset total.
        /// </summary>
        /// <remarks>
        /// LoanViewModel.PropertyAmount's setter self-assigns HomeLoanInfo.LoanAmountDirectInput to
        /// force a recompute. With no loan amount recorded that takes ProcessDepositCalc's
        /// byLoanDirectOnZero branch, which sets deposit = PropertyTotalAmount - 0. The wizard used
        /// to then prepopulate its deposit field from that value, so reopening it showed the asset
        /// total (e.g. 875,000) sitting in the deposit box. Commit writes the deposit
        /// unconditionally after the asset, which corrects the clobber.
        /// </remarks>
        [Test]
        public void Commit_AssetWithNoDeposit_DoesNotLeaveDepositEqualToAssetTotal()
        {
            var loan = BuildLoan(propertyAmount: 0, depositDirect: 0);
            var vm = new WizardViewModel(loan, BuildIncome(), BuildExpense(),
                new Mock<IErrorHandlingService>(MockBehavior.Loose).Object);

            vm.WizardAssetText = "850,000";
            vm.WizardDepositText = string.Empty;

            vm.Commit();

            Assert.That(loan.DepositAmountDirectInput, Is.Not.EqualTo(loan.HomeLoanInfo.PropertyTotalAmount),
                "the deposit must not absorb the entire asset total when none was entered");
            Assert.That(loan.DepositAmountDirectInput, Is.EqualTo(0).Within(1));
        }

        /// <summary>
        /// Regression: entering a deposit and upfront costs together must not inflate the deposit
        /// by the upfront amount.
        /// </summary>
        /// <remarks>
        /// The six expense setters preserve the loan and re-derive the deposit, so writing upfront
        /// costs AFTER the deposit gives deposit_new = deposit_old + upfront. Commit() originally
        /// applied asset+deposit first and upfront second, so every user who filled both fields got
        /// a deposit silently larger than the one they typed. Commit() now writes expenses first and
        /// the deposit last.
        /// </remarks>
        [Test]
        public void Commit_DepositAndUpfrontTogether_DoesNotInflateTheDeposit()
        {
            var loan = BuildLoan(propertyAmount: 0, depositDirect: 0);
            var vm = new WizardViewModel(loan, BuildIncome(), BuildExpense(),
                new Mock<IErrorHandlingService>(MockBehavior.Loose).Object);

            vm.WizardAssetText = "800,000";
            vm.WizardDepositText = "150,000";
            vm.WizardUpfrontText = "40,000";

            vm.Commit();

            Assert.That(loan.DepositAmountDirectInput, Is.EqualTo(150_000).Within(1),
                "the deposit must be exactly what was typed, not the typed value plus upfront costs");
        }

        /// <summary>
        /// The model holds a total plus ONE split point, so a loan amount and a deposit cannot both
        /// be honoured. Whichever the user edited last must win, and the other must derive from it.
        /// </summary>
        [Test]
        public void Commit_LastEditedSplitIsLoan_WritesTheLoanAndDerivesTheDeposit()
        {
            var loan = BuildLoan(propertyAmount: 0, depositDirect: 0);
            var vm = new WizardViewModel(loan, BuildIncome(), BuildExpense(),
                new Mock<IErrorHandlingService>(MockBehavior.Loose).Object);

            vm.WizardAssetText = "600,000";
            vm.WizardDepositText = "100,000";   // stale — the user then typed a loan amount
            vm.WizardLoanText = "450,000";
            vm.LastEditedSplit = LoanSplit.Loan;

            vm.Commit();

            Assert.Multiple(() =>
            {
                Assert.That(loan.HomeLoanInfo.LoanAmountDirectInput, Is.EqualTo(450_000).Within(1),
                    "the loan amount the user typed last must be the one that survives");
                Assert.That(loan.DepositAmountDirectInput,
                    Is.EqualTo(loan.HomeLoanInfo.PropertyTotalAmount - 450_000).Within(1),
                    "the deposit must be derived from the loan, not left at the stale typed value");
            });
        }

        [Test]
        public void Commit_LastEditedSplitIsDeposit_WritesTheDepositAndDerivesTheLoan()
        {
            var loan = BuildLoan(propertyAmount: 0, depositDirect: 0);
            var vm = new WizardViewModel(loan, BuildIncome(), BuildExpense(),
                new Mock<IErrorHandlingService>(MockBehavior.Loose).Object);

            vm.WizardAssetText = "600,000";
            vm.WizardDepositText = "100,000";
            vm.WizardLoanText = "450,000";      // stale — the user then typed a deposit
            vm.LastEditedSplit = LoanSplit.Deposit;

            vm.Commit();

            Assert.Multiple(() =>
            {
                Assert.That(loan.DepositAmountDirectInput, Is.EqualTo(100_000).Within(1));
                Assert.That(loan.HomeLoanInfo.LoanAmountDirectInput,
                    Is.EqualTo(loan.HomeLoanInfo.PropertyTotalAmount - 100_000).Within(1));
            });
        }

        [Test]
        public void Commit_NothingEntered_ReturnsFalse()
        {
            var loan = BuildLoan(propertyAmount: 0, depositDirect: 0);
            var vm = new WizardViewModel(loan, BuildIncome(), BuildExpense(),
                new Mock<IErrorHandlingService>(MockBehavior.Loose).Object);

            Assert.That(vm.Commit(), Is.False);
        }

        [Test]
        public void Commit_AddsIncomeAndExpenseWhenNoneRecorded()
        {
            _vm.WizardIncomeText = "6,000";
            _vm.WizardExpenseText = "1,500";

            Assert.That(_vm.Commit(), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(_income.TransactionRecords.IncomeExpenseEntries, Is.Not.Empty);
                Assert.That(_expense.TransactionRecords.IncomeExpenseEntries, Is.Not.Empty);
            });
        }

        [Test]
        public void Commit_DoesNotOverwriteIncomeThatAlreadyExists()
        {
            _income.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.WizardIncomeText = "9,999";

            _vm.Commit();

            Assert.That(_income.TransactionRecords.IncomeExpenseEntries.Count, Is.EqualTo(1),
                "an existing income record must not be added to from the wizard");
        }
    }
}
