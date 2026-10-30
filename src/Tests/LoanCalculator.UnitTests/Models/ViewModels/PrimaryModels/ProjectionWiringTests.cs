using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Income.Summary;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Every What If ▸ Projection switch must actually move the projected totals.
    /// </summary>
    /// <remarks>
    /// Two separate faults made these switches do nothing, and both were silent — the switch
    /// animated, the page redrew, and not one number changed:
    ///
    /// <list type="number">
    /// <item>The expense-side "take off the loan and its running costs" handler wrote
    /// <c>ShowPropertyExpense</c> (the Budget Expenses tab's display toggle) while
    /// <c>ExpenseViewModel.UpdateProjectionData</c> reads <c>IncludePropertyExpenses</c>. Two
    /// real, similarly-named properties on the same class, so nothing failed.</item>
    /// <item><c>PropertyExpenseSummary</c> and <c>PropertyPayment</c> — the figures those switches
    /// deduct — were populated only by visiting the Budget tab. Reaching Projection without
    /// having been there left them null, and <c>UpdateProjectionData</c> adds them up with
    /// <c>?? 0</c>, so the deduction was zero.</item>
    /// </list>
    ///
    /// These tests assert the figures MOVE, which is the only thing that would have caught either.
    /// Asserting that a flag was set would have passed throughout.
    /// </remarks>
    [TestFixture]
    public class ProjectionWiringTests
    {
        private const double IncomeYearly = 60_000;
        private const double ExpenseYearly = 24_000;
        private const double AssetCostMonthly = 200;
        private const double RepaymentMonthly = 1_500;

        [SetUp]
        public void Setup()
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

        private static IncomeViewModel BuildIncome(ExpenseViewModel expense)
        {
            var income = new IncomeViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            income.TransactionRecords!.Add("Salary", IncomeYearly / 12, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            income.TransactionRecords.SumUpData();
            income.MarkInitializationComplete();

            income.ExpenseSummary = expense;
            income.TotalYearsToProject = 10;
            return income;
        }

        private static ExpenseViewModel BuildExpense()
        {
            var expense = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            expense.TransactionRecords!.Add("Living", ExpenseYearly / 12, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expense.TransactionRecords.SumUpData();
            expense.MarkInitializationComplete();

            expense.TotalYearsToProject = 10;
            return expense;
        }

        /// <summary>What BudgetViewModel.WireLoanFiguresAsync hands the two sub-view-models.</summary>
        private static void WireLoanFigures(params ExpenseEntryViewBaseModel[] models)
        {
            foreach (var m in models)
            {
                if (m is IncomeViewModel income)
                {
                    income.PropertyExpenseSummary = new IncomeExpenseSummary { TotalYearly = AssetCostMonthly * 12 };
                    income.PropertyPayment = new PaymentOutput { TermPayment = RepaymentMonthly, TotalNumberPaymentPerYear = 12 };
                }
                else if (m is ExpenseViewModel expense)
                {
                    expense.PropertyExpenseSummary = new IncomeExpenseSummary { TotalYearly = AssetCostMonthly * 12 };
                    expense.PropertyPayment = new PaymentOutput { TermPayment = RepaymentMonthly, TotalNumberPaymentPerYear = 12 };
                }
            }
        }

        private static double ProjectedTotal(ExpenseEntryViewBaseModel vm) =>
            vm.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;

        // ── the switch that did nothing ───────────────────────────────────────

        [Test]
        public void ExpenseTakeOffLoan_ChangesTheProjectedExpenseTotal()
        {
            var expense = BuildExpense();
            WireLoanFigures(expense);

            expense.IncludePropertyExpenses = false;
            expense.UpdateProjectionData();
            var without = ProjectedTotal(expense);

            expense.IncludePropertyExpenses = true;
            expense.UpdateProjectionData();
            var with = ProjectedTotal(expense);

            Assert.That(with, Is.GreaterThan(without),
                "the expense-side \"take off the loan\" switch did not change the projected total — "
                + "this is the fault where the handler wrote ShowPropertyExpense instead");
        }

        /// <summary>
        /// The two flags are genuinely different things, so writing the wrong one must not happen
        /// to work by accident.
        /// </summary>
        [Test]
        public void ExpenseShowPropertyExpense_DoesNotDriveTheProjection()
        {
            var expense = BuildExpense();
            WireLoanFigures(expense);

            expense.IncludePropertyExpenses = false;
            expense.UpdateProjectionData();
            var baseline = ProjectedTotal(expense);

            expense.ShowPropertyExpense = true;      // the Budget tab's display toggle
            expense.UpdateProjectionData();

            Assert.That(ProjectedTotal(expense), Is.EqualTo(baseline).Within(0.01),
                "ShowPropertyExpense must not affect the projection — if it does, the two flags "
                + "have been conflated and the Budget tab and What If will fight over one switch");
        }

        // ── the switches that worked, but had nothing to deduct ───────────────

        [Test]
        public void IncomeTakeOffSpending_ChangesTheProjectedIncomeTotal()
        {
            var expense = BuildExpense();
            var income = BuildIncome(expense);

            income.IncludeExpenses = false;
            income.UpdateProjectionData();
            var without = ProjectedTotal(income);

            income.IncludeExpenses = true;
            income.UpdateProjectionData();

            Assert.That(ProjectedTotal(income), Is.LessThan(without),
                "taking off everyday spending must reduce the projected income total");
        }

        [Test]
        public void IncomeTakeOffLoan_ChangesTheProjectedIncomeTotal()
        {
            var expense = BuildExpense();
            var income = BuildIncome(expense);
            WireLoanFigures(income);

            income.IncludePropertyExpenses = false;
            income.UpdateProjectionData();
            var without = ProjectedTotal(income);

            income.IncludePropertyExpenses = true;
            income.UpdateProjectionData();

            Assert.That(ProjectedTotal(income), Is.LessThan(without),
                "taking off the loan must reduce the projected income total");
        }

        /// <summary>
        /// Pins the second fault directly: with the loan figures left unwired — which is the state
        /// Projection was reached in whenever the Budget tab had not been visited — the switch has
        /// nothing to subtract and silently does nothing.
        /// </summary>
        [Test]
        public void WithoutTheLoanFiguresWired_TheTakeOffLoanSwitchCannotDoAnything()
        {
            var expense = BuildExpense();
            var income = BuildIncome(expense);
            // deliberately NOT calling WireLoanFigures

            income.IncludePropertyExpenses = false;
            income.UpdateProjectionData();
            var without = ProjectedTotal(income);

            income.IncludePropertyExpenses = true;
            income.UpdateProjectionData();

            Assert.That(ProjectedTotal(income), Is.EqualTo(without).Within(0.01),
                "this documents WHY WireLoanFiguresAsync has to run before the projection: with "
                + "PropertyExpenseSummary and PropertyPayment null there is nothing to deduct, and "
                + "the switch is indistinguishable from a dead control");
        }

        /// <summary>
        /// The third switch had the same fault as the two loan ones, for its own reason:
        /// <c>Income.ExpenseSummary</c> was assigned only in <c>BudgetViewModel.InitializeBudget</c>,
        /// which runs from the Budget tab. Reaching Projection without going there left it null,
        /// and <c>UpdateProjectionData</c> skips the deduction entirely when it is
        /// (<c>if (IncludeExpenses &amp;&amp; ExpenseSummary != null)</c>) — so the switch moved and
        /// nothing happened. <c>WireLoanFiguresAsync</c> now assigns it.
        /// </summary>
        [Test]
        public void WithoutExpenseSummaryWired_TheTakeOffSpendingSwitchCannotDoAnything()
        {
            var income = BuildIncome(BuildExpense());
            income.ExpenseSummary = null;   // the state Projection was reached in

            income.IncludeExpenses = false;
            income.UpdateProjectionData();
            var without = ProjectedTotal(income);

            income.IncludeExpenses = true;
            income.UpdateProjectionData();

            Assert.That(ProjectedTotal(income), Is.EqualTo(without).Within(0.01),
                "documents WHY Income.ExpenseSummary must be wired before the projection runs");
        }

        /// <summary>
        /// The income deduction reads a total that only the EXPENSE view model refreshes, so the
        /// expense projection has to be computed first. RefreshProjection ran them the other way
        /// round, which left the income side deducting the previous pass's expense figure.
        /// </summary>
        [Test]
        public void IncomeDeduction_UsesTheCurrentExpenseTotalNotThePreviousOne()
        {
            var expense = BuildExpense();
            var income = BuildIncome(expense);
            income.IncludeExpenses = true;

            // Same order RefreshProjection now uses.
            expense.UpdateProjectionData();
            income.UpdateProjectionData();
            var before = ProjectedTotal(income);

            // Spending doubles. The income projection must follow it on the very next pass.
            expense.TransactionRecords!.Add("More living", ExpenseYearly / 12, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            expense.UpdateProjectionData();
            income.UpdateProjectionData();

            Assert.That(ProjectedTotal(income), Is.LessThan(before),
                "the income projection did not pick up the increased spending on the same pass");
        }

        // ── growth rates and horizon ─────────────────────────────────────────

        [Test]
        public void GrowthRate_ChangesTheProjectedTotal()
        {
            var expense = BuildExpense();
            var income = BuildIncome(expense);

            income.AnnualGrowthRate = 0;
            income.UpdateProjectionData();
            var flat = ProjectedTotal(income);

            income.AnnualGrowthRate = 5;
            income.UpdateProjectionData();

            Assert.That(ProjectedTotal(income), Is.GreaterThan(flat),
                "a positive growth rate must raise the projected total");
        }

        [Test]
        public void MoreYears_RaisesTheProjectedTotal()
        {
            var expense = BuildExpense();
            var income = BuildIncome(expense);

            income.TotalYearsToProject = 5;
            income.UpdateProjectionData();
            var five = ProjectedTotal(income);

            income.TotalYearsToProject = 15;
            income.UpdateProjectionData();

            Assert.That(ProjectedTotal(income), Is.GreaterThan(five),
                "the totals are cumulative, so a longer horizon must produce a bigger number");
        }
    }
}
