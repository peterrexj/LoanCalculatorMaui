using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models
{
    /// <summary>
    /// Guards the Budget tab-navigation bug (TECH-DEBT D1): one screen's "income after expenses"
    /// display toggle must not change what any other screen computes.
    /// </summary>
    /// <remarks>
    /// This fixture used to assert the gross/net split on <c>SumUpData(deduction)</c> directly.
    /// That API is gone — the deduction is applied where it is displayed — so the premise is now
    /// tested one level up, through the screens that were actually wrong. Asserting on
    /// <c>SumUpData()</c> alone would be tautological: with no deduction parameter there is
    /// nothing left for it to get wrong.
    /// </remarks>
    [TestFixture]
    public class IncomeExpenseGrossTotalsTests
    {
        private IncomeViewModel _income = null!;
        private ExpenseViewModel _expense = null!;

        [SetUp]
        public void SetUp()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            _income = new IncomeViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            _income.TransactionRecords.Add("Salary", 14_000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _income.TransactionRecords.SumUpData();
            _income.MarkInitializationComplete();

            _expense = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            _expense.TransactionRecords.Add("Rent", 3_000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _expense.TransactionRecords.SumUpData();
            _expense.MarkInitializationComplete();
        }

        [TearDown]
        public void TearDown()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
        }

        /// <summary>
        /// The Income tab refreshing with its "after expenses" toggle on — the real trigger, not a
        /// synthetic call. Set under the loading guard so the setter's fire-and-forget async
        /// refresh cannot race the synchronous one (SumUpData is not atomic).
        /// </summary>
        private void IncomeTabShowsAfterExpenses()
        {
            _income.ExpenseSummary = _expense;
            PageHelper.PageIsLoading();
            _income.ShowIncomeAfterExpense = true;
            PageHelper.PageLoadingComplete();
            _income.RefreshIncomePropertyChanged();
            _income.FlushPendingSave(() => { });
        }

        [Test]
        public void IncomeTabToggle_DoesNotChangeTheSharedSummary()
        {
            IncomeTabShowsAfterExpenses();

            Assert.Multiple(() =>
            {
                Assert.That(_income.TransactionRecords!.IncomeExpenseSummary.TotalMonthly,
                    Is.EqualTo(14_000).Within(0.01),
                    "the shared summary must stay the plain sum of the entries");
                Assert.That(_income.TransactionRecords.IncomeExpenseSummary.TotalYearly,
                    Is.EqualTo(168_000).Within(0.01));
            });
        }

        [Test]
        public void IncomeTabToggle_StillShowsTheNetFigureOnItsOwnTab()
        {
            IncomeTabShowsAfterExpenses();

            // The feature still works — it is just computed at the point of display now.
            Assert.That(_income.TotalMonthlyIncomeValue, Is.EqualTo(11_000).Within(0.01),
                "14,000 income less 3,000 expenses");
        }

        [Test]
        public void IncomeTabToggle_DoesNotLeakOntoTheExpensesTab()
        {
            _expense.IncomeSummary = _income;
            _expense.ShowIncomeAfterExpense = false;
            _expense.ShowPropertyExpense = false;

            IncomeTabShowsAfterExpenses();

            Assert.That(_expense.TotalIncomeMonthlyValue, Is.EqualTo(14_000).Within(0.01),
                "this box means 'the income the user entered' and must not inherit the Income "
                + "tab's after-expenses deduction — it used to render as a negative figure");
        }

        [Test]
        public void IncomeTabToggle_DoesNotDoubleSubtractOnBudgetsSummary()
        {
            var budget = new BudgetViewModel();
            budget.SetPeerViewModels(_income, _expense, new LoanViewModel());
            budget.RecalculateSummary();

            IncomeTabShowsAfterExpenses();

            Assert.That(budget.NetMonthly, Is.EqualTo(11_000).Within(0.01),
                "Net is income minus expenses once; the Income tab's toggle must not make it twice");
        }

        [Test]
        public void SumUpData_RepeatedCalls_DoNotAccumulate()
        {
            _income.TransactionRecords!.SumUpData();
            _income.TransactionRecords.SumUpData();
            _income.TransactionRecords.SumUpData();

            Assert.That(_income.TransactionRecords.IncomeExpenseSummary.TotalMonthly,
                Is.EqualTo(14_000).Within(0.01),
                "totals must be recomputed from the entries each time, never accumulated");
        }
    }
}
