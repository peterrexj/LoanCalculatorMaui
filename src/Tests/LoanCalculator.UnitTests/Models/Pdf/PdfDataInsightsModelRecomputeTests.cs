using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Pdf;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.Pdf
{
    /// <summary>
    /// The PDF path must recompute all three record sets itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The report is built from data loaded with <c>SharedServiceCore.LoadDataFile</c>, which never
    /// sums. Before TECH-DEBT D1a only the income records were corrected (and only as a side effect
    /// of a reset), so the <b>expense</b> totals and the <b>loan running cost</b> totals were
    /// whatever was last written to disk — stale, or zero on a fresh install.
    /// </para>
    /// <para>
    /// These assertions are made on the model rather than on extracted PDF text on purpose: the
    /// running-cost figures also reach the page from the per-entry table rows, which bypass the
    /// summary, so a text assertion passes whether or not the recompute happened.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class PdfDataInsightsModelRecomputeTests
    {
        private const double RunningCost = 417;
        private const double MonthlyIncome = 9_137;
        private const double MonthlyExpense = 2_513;

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

        /// <summary>Entries present, summary totals left at zero — the shape a load produces.</summary>
        private static Incomes Unsummed(string name, double monthly)
        {
            var records = new Incomes { IncomeExpenseEntries = [] };
            records.Add(name, monthly, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            records.IncomeExpenseSummary.TotalMonthly = 0;
            records.IncomeExpenseSummary.TotalYearly = 0;
            return records;
        }

        private static PdfDataInsightsModel BuildModel()
        {
            var loan = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    { InterestRate = 5.0, LoanTermInYears = 30, TotalNumberPaymentPerYear = 12 },
                    PropertyAmount = 700_000
                },
                TransactionRecords = Unsummed("Maintenance", RunningCost)
            };
            loan.HomeLoanInfo.LoanAmountDirectInput = 560_000;

            var income = new IncomeViewModel { TransactionRecords = Unsummed("Salary", MonthlyIncome) };
            var expense = new ExpenseViewModel { TransactionRecords = Unsummed("Rent", MonthlyExpense) };

            var model = new PdfDataInsightsModel(loan, income, expense);
            model.InitializeLocalDataSet();
            return model;
        }

        [Test]
        public void InitializeLocalDataSet_RecomputesTheLoanRunningCosts()
        {
            var model = BuildModel();

            Assert.Multiple(() =>
            {
                Assert.That(model.Loan.TotalMonthlyRunningExpense, Is.EqualTo(RunningCost).Within(0.01),
                    "the loan's running-cost summary must be recomputed, not read off disk");
                Assert.That(model.Loan.TotalYearlyRunningExpense, Is.EqualTo(RunningCost * 12).Within(0.01));
            });
        }

        [Test]
        public void InitializeLocalDataSet_RecomputesTheExpenseTotals()
        {
            var model = BuildModel();

            Assert.Multiple(() =>
            {
                Assert.That(model.Expense.TotalMonthly, Is.EqualTo(MonthlyExpense).Within(0.01));
                Assert.That(model.Expense.TotalYearly, Is.EqualTo(MonthlyExpense * 12).Within(0.01));
            });
        }

        [Test]
        public void InitializeLocalDataSet_RecomputesTheIncomeTotals()
        {
            var model = BuildModel();

            Assert.Multiple(() =>
            {
                Assert.That(model.Income.TotalMonthly, Is.EqualTo(MonthlyIncome).Within(0.01));
                Assert.That(model.Income.TotalYearly, Is.EqualTo(MonthlyIncome * 12).Within(0.01));
            });
        }

        /// <summary>
        /// The running cost must reach the figure the user is judged on: the "including property"
        /// deduction is expenses + repayment + running costs, so a zero running-cost summary makes
        /// the report overstate affordability.
        /// </summary>
        [Test]
        public void IncludingPropertyDeduction_CarriesTheRunningCost()
        {
            var model = BuildModel();

            Assert.That(model.Income.TotalExpenseIncludingPropertyMonthly,
                Is.EqualTo(model.Expense.TotalMonthly
                           + model.Loan.MonthlyRepayment
                           + RunningCost).Within(0.01));

            Assert.That(model.Income.TotalAfterExpenseIncludingPropertyMonthly,
                Is.EqualTo(model.Income.TotalMonthly - model.Income.TotalExpenseIncludingPropertyMonthly)
                    .Within(0.01));
        }
    }
}
