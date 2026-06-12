using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Pdf;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.Pdf
{
    [TestFixture]
    public class PdfDataInsightsModelTests
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
            double otherExpense = 15_000,
            double interestRate = 5.0,
            int termYears = 30,
            int paymentsPerYear = 12)
        {
            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = interestRate,
                        LoanTermInYears = termYears,
                        TotalNumberPaymentPerYear = paymentsPerYear
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

        private static IncomeViewModel BuildIncomeVm(double monthlyIncome = 6_000)
        {
            var vm = new IncomeViewModel();
            vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            if (monthlyIncome > 0)
            {
                vm.TransactionRecords.Add("Salary", monthlyIncome, LoanCalculator.Core.Models.Enums.TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
                vm.TransactionRecords.SumUpData();
            }
            vm.MarkInitializationComplete();
            return vm;
        }

        private static ExpenseViewModel BuildExpenseVm(double monthlyExpense = 2_000)
        {
            var vm = new ExpenseViewModel();
            vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            if (monthlyExpense > 0)
            {
                vm.TransactionRecords.Add("Rent", monthlyExpense, LoanCalculator.Core.Models.Enums.TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
                vm.TransactionRecords.SumUpData();
            }
            vm.MarkInitializationComplete();
            return vm;
        }

        // ── TotalPropertyAmount = PropertyAmount + OtherExpenseTotalAmount ────

        [Test]
        public void TotalPropertyAmount_EqualsPropertyPlusOtherExpenses()
        {
            var loanVm = BuildLoanVm(propertyAmount: 500_000, otherExpense: 15_000);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.TotalPropertyAmount,
                Is.EqualTo(model.Loan.PropertyAmount + model.Loan.OtherExpenseTotalAmount).Within(1));
        }

        [Test]
        public void TotalPropertyAmount_ZeroOtherExpenses_EqualsPropertyAmount()
        {
            var loanVm = BuildLoanVm(propertyAmount: 600_000, otherExpense: 0);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.TotalPropertyAmount, Is.EqualTo(600_000).Within(1));
        }

        // ── PropertyAmount, LoanAmount, DepositAmount populated ──────────────

        [Test]
        public void PropertyAmount_MapsFromLoanViewModel()
        {
            var loanVm = BuildLoanVm(propertyAmount: 750_000);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.PropertyAmount, Is.EqualTo(750_000).Within(1));
        }

        [Test]
        public void LoanAmount_MapsFromHomeLoanInfo()
        {
            var loanVm = BuildLoanVm(loanAmount: 450_000);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.LoanAmount, Is.EqualTo(450_000).Within(1));
        }

        // ── MonthlyRepaymentWithExpenses = TotalMonthlyRunningExpense + MonthlyRepayment ──

        [Test]
        public void MonthlyRepaymentWithExpenses_EqualsRunningExpensePlusRepayment()
        {
            var loanVm = BuildLoanVm();
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            var expected = Math.Round(model.Loan.TotalMonthlyRunningExpense + model.Loan.MonthlyRepayment);
            Assert.That(model.Loan.MonthlyRepaymentWithExpenses, Is.EqualTo(expected).Within(1));
        }

        // ── YearlyRepaymentWithExpenses ────────────────────────────────────────

        [Test]
        public void YearlyRepaymentWithExpenses_EqualsRunningExpensePlusRepayment()
        {
            var loanVm = BuildLoanVm();
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            var expected = Math.Round(model.Loan.TotalYearlyRunningExpense + model.Loan.YearlyRepayment);
            Assert.That(model.Loan.YearlyRepaymentWithExpenses, Is.EqualTo(expected).Within(1));
        }

        // ── RepaymentFrequency string mapping ─────────────────────────────────

        [Test]
        public void RepaymentFrequency_Monthly_ReturnsMonthlyString()
        {
            var loanVm = BuildLoanVm(paymentsPerYear: 12);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.RepaymentFrequency, Is.EqualTo("Monthly"));
        }

        [Test]
        public void RepaymentFrequency_Fortnightly_ReturnsFortnightlyString()
        {
            var loanVm = BuildLoanVm(paymentsPerYear: 24);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.RepaymentFrequency, Is.EqualTo("Fortnightly"));
        }

        [Test]
        public void RepaymentFrequency_Weekly_ReturnsWeeklyString()
        {
            var loanVm = BuildLoanVm(paymentsPerYear: 52);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.RepaymentFrequency, Is.EqualTo("Weekly"));
        }

        // ── Expense model populated ───────────────────────────────────────────

        [Test]
        public void ExpenseTotalMonthly_MapsFromExpenseViewModel()
        {
            var expenseVm = BuildExpenseVm(monthlyExpense: 1_500);
            var model = new PdfDataInsightsModel(BuildLoanVm(), BuildIncomeVm(), expenseVm);
            model.InitializeLocalDataSet();

            Assert.That(model.Expense.TotalMonthly, Is.EqualTo(1_500).Within(1));
        }

        [Test]
        public void ExpenseTotalYearly_MapsFromExpenseViewModel()
        {
            var expenseVm = BuildExpenseVm(monthlyExpense: 1_000);
            var model = new PdfDataInsightsModel(BuildLoanVm(), BuildIncomeVm(), expenseVm);
            model.InitializeLocalDataSet();

            Assert.That(model.Expense.TotalYearly, Is.EqualTo(12_000).Within(1));
        }

        // ── Income.TotalExpenseIncludingPropertyMonthly ───────────────────────

        [Test]
        public void TotalExpenseIncludingPropertyMonthly_IsThreeWaySum()
        {
            var expenseVm = BuildExpenseVm(monthlyExpense: 1_000);
            var model = new PdfDataInsightsModel(BuildLoanVm(), BuildIncomeVm(), expenseVm);
            model.InitializeLocalDataSet();

            var expected = model.Expense.TotalMonthly
                + model.Loan.MonthlyRepayment
                + model.Loan.TotalMonthlyRunningExpense;
            Assert.That(model.Income.TotalExpenseIncludingPropertyMonthly, Is.EqualTo(expected).Within(1));
        }

        // ── InterestRate and LoanTermInYears ──────────────────────────────────

        [Test]
        public void LoanInterestRate_MapsCorrectly()
        {
            var loanVm = BuildLoanVm(interestRate: 6.5);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.InterestRate, Is.EqualTo(6.5).Within(0.01));
        }

        [Test]
        public void LoanTermInYears_MapsCorrectly()
        {
            var loanVm = BuildLoanVm(termYears: 25);
            var model = new PdfDataInsightsModel(loanVm, BuildIncomeVm(), BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Loan.LoanTermInYears, Is.EqualTo(25));
        }

        // ── IncomeTotalMonthly ────────────────────────────────────────────────

        [Test]
        public void IncomeTotalMonthly_MapsFromIncomeViewModel()
        {
            var incomeVm = BuildIncomeVm(monthlyIncome: 7_000);
            var model = new PdfDataInsightsModel(BuildLoanVm(), incomeVm, BuildExpenseVm());
            model.InitializeLocalDataSet();

            Assert.That(model.Income.TotalMonthly, Is.EqualTo(7_000).Within(1));
        }
    }
}
