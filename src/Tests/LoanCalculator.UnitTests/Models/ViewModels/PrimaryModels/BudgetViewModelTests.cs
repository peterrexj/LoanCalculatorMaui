using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    [TestFixture]
    public class BudgetViewModelTests
    {
        private BudgetViewModel _vm;

        private static IncomeViewModel BuildIncomeVm(double monthlyAmount = 0)
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            var vm = new IncomeViewModel();
            vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            if (monthlyAmount > 0)
                vm.TransactionRecords.Add("Salary", monthlyAmount, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            vm.TransactionRecords.SumUpData();
            vm.MarkInitializationComplete();
            return vm;
        }

        private static ExpenseViewModel BuildExpenseVm(double monthlyAmount = 0)
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            var vm = new ExpenseViewModel();
            vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            if (monthlyAmount > 0)
                vm.TransactionRecords.Add("Rent", monthlyAmount, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            vm.TransactionRecords.SumUpData();
            vm.MarkInitializationComplete();
            return vm;
        }

        [SetUp]
        public void Setup()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            _vm = new BudgetViewModel();
        }

        [TearDown]
        public void TearDown()
        {
            SharedServiceCore.LoadSafeOff();
            PageHelper.PageLoadingComplete();
        }

        // ── Income and Expense properties initialized ─────────────────────────

        [Test]
        public void BudgetViewModel_Income_IsNotNull_OnConstruction()
        {
            Assert.That(_vm.Income, Is.Not.Null);
        }

        [Test]
        public void BudgetViewModel_Expense_IsNotNull_OnConstruction()
        {
            Assert.That(_vm.Expense, Is.Not.Null);
        }

        // ── InitializeBudget ──────────────────────────────────────────────────

        [Test]
        public void InitializeBudget_PopulatesIncomeFrequencyCollection()
        {
            _vm.InitializeBudget();
            Assert.That(_vm.Income.IncomeFrequencyCollection, Is.Not.Null);
            Assert.That(_vm.Income.IncomeFrequencyCollection.Count, Is.GreaterThan(0));
        }

        [Test]
        public void InitializeBudget_PopulatesExpenseFrequencyCollection()
        {
            _vm.InitializeBudget();
            Assert.That(_vm.Expense.IncomeFrequencyCollection, Is.Not.Null);
            Assert.That(_vm.Expense.IncomeFrequencyCollection.Count, Is.GreaterThan(0));
        }

        [Test]
        public void InitializeBudget_WiresExpenseIncomeSummaryToIncome()
        {
            _vm.InitializeBudget();
            Assert.That(_vm.Expense.IncomeSummary, Is.SameAs(_vm.Income));
        }

        // ── SetPeerViewModels ─────────────────────────────────────────────────

        [Test]
        public void SetPeerViewModels_AssignsIncomeAndExpense()
        {
            var income = BuildIncomeVm(3000);
            var expense = BuildExpenseVm(1000);
            var loan = new LoanViewModel();

            _vm.SetPeerViewModels(income, expense, loan);

            Assert.That(_vm.Income, Is.SameAs(income));
            Assert.That(_vm.Expense, Is.SameAs(expense));
        }

        // ── RecalculateSummary ────────────────────────────────────────────────

        [Test]
        public void RecalculateSummary_TotalIncomeMonthly_MatchesIncomeSumUpData()
        {
            var income = BuildIncomeVm(5000);
            var expense = BuildExpenseVm(2000);
            _vm.SetPeerViewModels(income, expense, new LoanViewModel());

            _vm.RecalculateSummary();

            Assert.That(_vm.TotalIncomeMonthly, Is.EqualTo(5000).Within(1));
        }

        [Test]
        public void RecalculateSummary_TotalExpenseMonthly_MatchesExpenseSumUpData()
        {
            var income = BuildIncomeVm(5000);
            var expense = BuildExpenseVm(2000);
            _vm.SetPeerViewModels(income, expense, new LoanViewModel());

            _vm.RecalculateSummary();

            Assert.That(_vm.TotalExpenseMonthly, Is.EqualTo(2000).Within(1));
        }

        [Test]
        public void NetMonthly_IsIncomeMinus_Expense()
        {
            var income = BuildIncomeVm(5000);
            var expense = BuildExpenseVm(2000);
            _vm.SetPeerViewModels(income, expense, new LoanViewModel());

            _vm.RecalculateSummary();

            Assert.That(_vm.NetMonthly, Is.EqualTo(3000).Within(1));
        }

        [Test]
        public void NetMonthly_Negative_WhenExpenseExceedsIncome()
        {
            var income = BuildIncomeVm(1000);
            var expense = BuildExpenseVm(3000);
            _vm.SetPeerViewModels(income, expense, new LoanViewModel());

            _vm.RecalculateSummary();

            Assert.That(_vm.NetMonthly, Is.LessThan(0));
        }

        // ── HasData ───────────────────────────────────────────────────────────

        [Test]
        public void HasData_IsFalse_WhenNeitherHasEntries()
        {
            var income = BuildIncomeVm(0);
            var expense = BuildExpenseVm(0);
            _vm.SetPeerViewModels(income, expense, new LoanViewModel());

            _vm.RecalculateSummary();

            Assert.That(_vm.HasData, Is.False);
        }

        [Test]
        public void HasData_IsTrue_WhenIncomeHasEntries()
        {
            var income = BuildIncomeVm(5000);
            var expense = BuildExpenseVm(0);
            _vm.SetPeerViewModels(income, expense, new LoanViewModel());

            _vm.RecalculateSummary();

            Assert.That(_vm.HasData, Is.True);
        }

        [Test]
        public void HasData_IsTrue_WhenExpenseHasEntries()
        {
            var income = BuildIncomeVm(0);
            var expense = BuildExpenseVm(1500);
            _vm.SetPeerViewModels(income, expense, new LoanViewModel());

            _vm.RecalculateSummary();

            Assert.That(_vm.HasData, Is.True);
        }

        // ── TopExpenses ───────────────────────────────────────────────────────

        [Test]
        public void TopExpenses_ReturnsAtMostFive()
        {
            var expense = new ExpenseViewModel();
            expense.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            for (int i = 1; i <= 8; i++)
                expense.TransactionRecords.Add($"Expense{i}", i * 100, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expense.TransactionRecords.SumUpData();
            expense.MarkInitializationComplete();

            _vm.SetPeerViewModels(BuildIncomeVm(), expense, new LoanViewModel());
            _vm.RecalculateSummary();

            Assert.That(_vm.TopExpenses.Count, Is.LessThanOrEqualTo(5));
        }

        [Test]
        public void TopExpenses_OrderedByAmountDescending()
        {
            var expense = new ExpenseViewModel();
            expense.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            expense.TransactionRecords.Add("Low", 100, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expense.TransactionRecords.Add("High", 3000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expense.TransactionRecords.Add("Mid", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expense.TransactionRecords.SumUpData();
            expense.MarkInitializationComplete();

            _vm.SetPeerViewModels(BuildIncomeVm(), expense, new LoanViewModel());
            _vm.RecalculateSummary();

            var top = _vm.TopExpenses;
            Assert.That(top.Count, Is.GreaterThan(0));
            for (int i = 0; i < top.Count - 1; i++)
                Assert.That(top[i].AmountMonthly, Is.GreaterThanOrEqualTo(top[i + 1].AmountMonthly));
        }

        // ── RecalculateProjection ─────────────────────────────────────────────

        [Test]
        public void RecalculateProjection_WithNoData_ReturnsEmptyCollections()
        {
            _vm.SetPeerViewModels(BuildIncomeVm(), BuildExpenseVm(), new LoanViewModel());

            _vm.RecalculateProjection();

            Assert.That(_vm.ProjectionIncomeAxis.Count, Is.EqualTo(0));
            Assert.That(_vm.ProjectionExpenseAxis.Count, Is.EqualTo(0));
        }

        // ── SummaryDonutData ──────────────────────────────────────────────────

        [Test]
        public void RecalculateSummary_PopulatesSummaryDonutData_WithTwoEntries()
        {
            var income = BuildIncomeVm(5000);
            var expense = BuildExpenseVm(2000);
            _vm.SetPeerViewModels(income, expense, new LoanViewModel());

            _vm.RecalculateSummary();

            Assert.That(_vm.SummaryDonutData.Count, Is.EqualTo(2));
            Assert.That(_vm.SummaryDonutData[0].Name, Is.EqualTo("Monthly Income"));
            Assert.That(_vm.SummaryDonutData[1].Name, Is.EqualTo("Monthly Expenses"));
        }
    }
}
