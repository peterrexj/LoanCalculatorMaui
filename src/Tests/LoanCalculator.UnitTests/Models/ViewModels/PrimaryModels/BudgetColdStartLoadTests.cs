using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using Moq;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Cold-start load: the affordability box must be available on the first Loan page appearance,
    /// without the user having to visit Budget first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>SplashPage</c> pre-warms through <see cref="BudgetViewModel.EnsureSubVmsLoadedAsync"/>,
    /// which loads via <c>LoadDataFile</c> + <c>CopyPropertiesFrom</c> + <c>InitializeViewData</c>.
    /// None of those sums the entries, and
    /// <c>IncomeExpenseSummary.TotalMonthly</c>/<c>TotalYearly</c> are
    /// <c>[JsonIgnore]</c> — so they arrive as 0 regardless of what the saved file contained.
    /// </para>
    /// <para>
    /// <c>LoanViewModel.HasIncomeExpensesRecorded</c> gates on <c>TotalYearly &gt; 0</c>, so
    /// the box stayed hidden for the whole session until something called
    /// <c>RecalculateSummary</c>. These tests simulate the deserialized shape directly: entries
    /// present, summary totals zero.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class BudgetColdStartLoadTests
    {
        private Mock<ILocalStorage> _storageMock = null!;

        [SetUp]
        public void SetUp()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var income = new IncomeViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            income.TransactionRecords.Add("Salary", 7_000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            var expense = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            expense.TransactionRecords.Add("Rent", 2_500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            // Deliberately NOT summed — this is the state a file on disk produces.
            Assume.That(income.TransactionRecords.IncomeExpenseSummary.TotalYearly, Is.EqualTo(0),
                "precondition: a deserialized summary has no gross totals");

            _storageMock = new Mock<ILocalStorage>(MockBehavior.Loose);
            _storageMock.Setup(s => s.GetData<IncomeViewModel>()).ReturnsAsync(income);
            _storageMock.Setup(s => s.GetData<ExpenseViewModel>()).ReturnsAsync(expense);
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

        [Test]
        public async Task EnsureSubVmsLoadedAsync_PopulatesGrossTotals_WithoutNeedingRecalculateSummary()
        {
            var budget = new BudgetViewModel();

            await budget.EnsureSubVmsLoadedAsync();

            Assert.Multiple(() =>
            {
                Assert.That(budget.Income.TransactionRecords!.IncomeExpenseSummary.TotalYearly,
                    Is.EqualTo(84_000).Within(1),
                    "income gross must be summed on load, not left for RecalculateSummary");
                Assert.That(budget.Expense.TransactionRecords!.IncomeExpenseSummary.TotalYearly,
                    Is.EqualTo(30_000).Within(1),
                    "expense gross must be summed on load");
            });
        }

        /// <summary>
        /// The user-visible symptom, asserted the way the Loan page actually computes it.
        /// </summary>
        [Test]
        public async Task AfterColdStart_AffordabilityIsAvailableWithoutVisitingBudget()
        {
            var budget = new BudgetViewModel();
            await budget.EnsureSubVmsLoadedAsync();

            // Exactly what LoanViewModel.RefreshIncomeExpenseSummariesAsync computes.
            var hasIncomeExpenses =
                budget.Expense.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 &&
                budget.Income.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;

            Assert.That(hasIncomeExpenses, Is.True,
                "HasIncomeExpensesRecorded gates the affordability box on the gross yearly totals; "
                + "they must be populated by the splash pre-warm, not by a later Budget visit");
        }
    }
}
