using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Pdf;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models
{
    /// <summary>
    /// Characterisation tests written BEFORE the TECH-DEBT D1 refactor, which removes the
    /// deduction parameters from <c>IncomeExpenseBase.SumUpData</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// D1: <c>SumUpData(monthly, yearly)</c> subtracts a caller-supplied deduction and leaves it
    /// in <c>IncomeExpenseSummary.TotalMonthly</c>/<c>TotalYearly</c> — which is shared by the
    /// Income tab, Expense tab, Budget page, Loan/affordability box, Wizard and the PDF. So those
    /// fields mean net-or-gross depending on which tab refreshed last.
    /// </para>
    /// <para>
    /// The affordability figure is the thing that must not move, and before this file it had
    /// <b>zero</b> assertions anywhere in the suite. These tests are deliberately written as
    /// relationships and invariants rather than hand-computed magic numbers: the point is that
    /// the refactor changes HOW the number is produced (mutate-then-read becomes arithmetic)
    /// without changing WHAT it is.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class AffordabilityOrderDependenceTests
    {
        [SetUp]
        public void Setup()
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

        private static LoanViewModel BuildLoanVm(
            double propertyAmount = 500_000,
            double loanAmount = 400_000,
            double otherExpense = 15_000)
        {
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
            vm.HomeLoanInfo.LoanAmountDirectInput = loanAmount;
            vm.HomeLoanInfo.OtherExpense.OtherExpenses = otherExpense;
            vm.MarkInitializationComplete();
            return vm;
        }

        private static IncomeViewModel BuildIncomeVm(double monthly = 6_000)
        {
            var vm = new IncomeViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            if (monthly > 0)
                vm.TransactionRecords.Add("Salary", monthly, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            vm.TransactionRecords.SumUpData();
            vm.MarkInitializationComplete();
            return vm;
        }

        private static ExpenseViewModel BuildExpenseVm(double monthly = 2_000)
        {
            var vm = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            if (monthly > 0)
                vm.TransactionRecords.Add("Rent", monthly, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            vm.TransactionRecords.SumUpData();
            vm.MarkInitializationComplete();
            return vm;
        }

        private static PdfDataInsightsModel BuildModel(
            LoanViewModel loan, IncomeViewModel income, ExpenseViewModel expense)
        {
            var model = new PdfDataInsightsModel(loan, income, expense);
            model.InitializeLocalDataSet();
            return model;
        }

        // ── The arithmetic the refactor will substitute in ───────────────────
        //
        // Today these figures are produced by SumUpData(deduction) + read-back. Step 5 replaces
        // that with plain subtraction. Pinning the relationship first is what licenses that swap.

        [Test]
        public void TotalAfterExpense_EqualsIncomeMinusExpense()
        {
            var model = BuildModel(BuildLoanVm(), BuildIncomeVm(6_000), BuildExpenseVm(2_000));

            Assert.Multiple(() =>
            {
                Assert.That(model.Income.TotalAfterExpenseMonthly,
                    Is.EqualTo(model.Income.TotalMonthly - model.Expense.TotalMonthly).Within(0.01));
                Assert.That(model.Income.TotalAfterExpenseYearly,
                    Is.EqualTo(model.Income.TotalYearly - model.Expense.TotalYearly).Within(0.01));
            });
        }

        [Test]
        public void TotalAfterExpenseIncludingProperty_EqualsIncomeMinusTheIncludingPropertyDeduction()
        {
            var model = BuildModel(BuildLoanVm(), BuildIncomeVm(6_000), BuildExpenseVm(2_000));

            Assert.Multiple(() =>
            {
                Assert.That(model.Income.TotalAfterExpenseIncludingPropertyMonthly,
                    Is.EqualTo(model.Income.TotalMonthly - model.Income.TotalExpenseIncludingPropertyMonthly)
                        .Within(0.01));
                Assert.That(model.Income.TotalAfterExpenseIncludingPropertyYearly,
                    Is.EqualTo(model.Income.TotalYearly - model.Income.TotalExpenseIncludingPropertyYearly)
                        .Within(0.01));
            });
        }

        /// <summary>
        /// The gross figures must be the plain sum of the entries, not whatever the last caller
        /// left behind.
        /// </summary>
        [Test]
        public void GrossFigures_AreThePlainSumOfTheEntries()
        {
            var model = BuildModel(BuildLoanVm(), BuildIncomeVm(6_000), BuildExpenseVm(2_000));

            Assert.Multiple(() =>
            {
                Assert.That(model.Income.TotalMonthly, Is.EqualTo(6_000).Within(0.01));
                Assert.That(model.Income.TotalYearly, Is.EqualTo(72_000).Within(0.01));
                Assert.That(model.Expense.TotalMonthly, Is.EqualTo(2_000).Within(0.01));
                Assert.That(model.Expense.TotalYearly, Is.EqualTo(24_000).Within(0.01));
            });
        }

        // ── The D1 invariant: THIS is the oracle ─────────────────────────────

        /// <summary>
        /// Every affordability figure must be identical whether or not some other screen has
        /// already left a deduction sitting in the shared summary. Must pass before AND after the
        /// refactor — if it ever fails, affordability has become order-dependent again.
        /// </summary>
        [Test]
        public void AffordabilityFigures_AreInvariantUnderAPriorDeductedSumUpData()
        {
            var loan = BuildLoanVm();
            var income = BuildIncomeVm(6_000);
            var expense = BuildExpenseVm(2_000);

            var before = BuildModel(loan, income, expense);
            var baseline = (
                before.Income.TotalMonthly,
                before.Income.TotalYearly,
                before.Income.TotalAfterExpenseMonthly,
                before.Income.TotalAfterExpenseYearly,
                before.Income.TotalAfterExpenseIncludingPropertyMonthly,
                before.Income.TotalAfterExpenseIncludingPropertyYearly);

            // The Income tab refreshing with its "after expenses" toggle on — the real trigger.
            // SumUpData no longer accepts a deduction, so this is the only way the contamination
            // could still occur. Loading guard prevents the async refresh racing the sync one.
            income.ExpenseSummary = expense;
            PageHelper.PageIsLoading();
            income.ShowIncomeAfterExpense = true;
            PageHelper.PageLoadingComplete();
            income.RefreshIncomePropertyChanged();
            income.FlushPendingSave(() => { });

            var after = BuildModel(loan, income, expense);

            Assert.That((
                after.Income.TotalMonthly,
                after.Income.TotalYearly,
                after.Income.TotalAfterExpenseMonthly,
                after.Income.TotalAfterExpenseYearly,
                after.Income.TotalAfterExpenseIncludingPropertyMonthly,
                after.Income.TotalAfterExpenseIncludingPropertyYearly),
                Is.EqualTo(baseline),
                "affordability must not depend on what another screen left in the shared summary");
        }

        /// <summary>
        /// The same invariant one level up, through the public surplus the What If page consumes.
        /// </summary>
        /// <remarks>
        /// Depends on <c>SharedServiceCore.IsTrialUser</c> being false, which today it is because
        /// <c>TESTING_PREMIUM_OVERRIDE</c> is on. That flag is a release blocker; when it is
        /// removed this test needs a seam for the premium check rather than a silent skip.
        /// </remarks>
        [Test]
        public void MonthlySurplus_IsInvariantUnderAPriorDeductedSumUpData()
        {
            var loan = BuildLoanVm();
            var income = BuildIncomeVm(6_000);
            var expense = BuildExpenseVm(2_000);

            loan.IncomeSummary = income;
            loan.ExpenseSummary = expense;
            loan.HasIncomeExpensesRecorded = true;

            Assume.That(loan.IsAffordabilityAvailable, Is.True,
                "affordability must be available for this test to mean anything");

            var baseline = loan.MonthlySurplus;

            // The Income tab refreshing with its "after expenses" toggle on — the real trigger.
            // SumUpData no longer accepts a deduction, so this is the only way the contamination
            // could still occur. Loading guard prevents the async refresh racing the sync one.
            income.ExpenseSummary = expense;
            PageHelper.PageIsLoading();
            income.ShowIncomeAfterExpense = true;
            PageHelper.PageLoadingComplete();
            income.RefreshIncomePropertyChanged();
            income.FlushPendingSave(() => { });

            Assert.That(loan.MonthlySurplus, Is.EqualTo(baseline).Within(0.01),
                "MonthlySurplus must not change because another screen deducted from the summary");
        }

        /// <summary>
        /// Budget's Net double-subtracts the expenses after the Income tab refreshes with "income
        /// after expenses" on — the concrete, reproducible instance of D1.
        /// </summary>
        /// <remarks>
        /// RED before the D1 fix, GREEN after. <c>BudgetViewModel.TotalIncomeMonthly</c> reads
        /// <c>TotalMonthly</c> directly, so once the Income tab has written its net figure into the
        /// shared summary, Net subtracts the expense a second time: 5000 − 2000 = 3000 becomes
        /// (5000 − 2000) − 2000 = 1000, and stays wrong until the next RecalculateSummary.
        /// Un-ignore this in Step 5 of the D1 plan.
        /// </remarks>
        [Test]
        public void BudgetNetMonthly_AfterTheIncomeTabDeducts_DoesNotDoubleSubtractTheExpense()
        {
            var income = BuildIncomeVm(5_000);
            var expense = BuildExpenseVm(2_000);
            var budget = new BudgetViewModel();
            budget.SetPeerViewModels(income, expense, BuildLoanVm());
            budget.RecalculateSummary();

            Assume.That(budget.NetMonthly, Is.EqualTo(3_000).Within(0.01), "precondition");

            // The user flips "income after expenses" on Budget's Income tab.
            //
            // Set the toggle under the loading guard, then drive ONE synchronous refresh. The
            // setter otherwise launches RefreshIncomePropertyChangedAsync, which cannot be
            // awaited, and that async pass races the synchronous one INSIDE SumUpData — which is
            // not atomic (TotalMonthly = 0, then += per entry, then -= deduction). Interleaved,
            // two passes yield 5000 - 2000 - 2000 = 1000 on the income summary and a Net of
            // -1000. This test failed with exactly that value before the guard was added, and it
            // is a third manifestation of D1's root cause rather than a test artefact.
            income.ExpenseSummary = expense;
            PageHelper.PageIsLoading();
            income.ShowIncomeAfterExpense = true;
            PageHelper.PageLoadingComplete();
            income.RefreshIncomePropertyChanged();
            income.FlushPendingSave(() => { });

            Assert.That(budget.NetMonthly, Is.EqualTo(3_000).Within(0.01),
                "Budget's Net must stay income − expense; the Income tab's display toggle must not "
                + "make it subtract the expense twice");
        }
    }
}
