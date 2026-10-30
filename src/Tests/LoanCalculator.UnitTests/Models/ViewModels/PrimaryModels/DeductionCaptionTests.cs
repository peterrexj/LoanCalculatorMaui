using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Income.Summary;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// The Budget summary captions must name every deduction actually applied to the figure
    /// beside them.
    /// </summary>
    /// <remarks>
    /// The Income tab has two independent "show the figure net of…" toggles, so four states — but
    /// the captions read only the first and so described two. Both failure directions were live:
    /// with just the property toggle on, a figure that had already had the loan repayment and the
    /// asset running costs subtracted was captioned as plain "Monthly Income"; with both on, the
    /// caption said "after expenses" and named none of the rest.
    /// </remarks>
    [TestFixture]
    public class DeductionCaptionTests
    {
        private const double MonthlyIncome = 9_000;
        private const double MonthlyExpenses = 2_000;
        private const double AssetCosts = 300;
        private const double LoanRepayment = 2_500;

        private static IncomeViewModel BuildIncomeVm()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var income = new IncomeViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            income.TransactionRecords!.Add("Salary", MonthlyIncome, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            income.TransactionRecords.SumUpData();

            var expense = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            expense.TransactionRecords!.Add("Rent", MonthlyExpenses, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expense.TransactionRecords.SumUpData();
            expense.MarkInitializationComplete();

            income.ExpenseSummary = expense;

            // A loan and asset running costs, so the property toggle has something real to
            // subtract. Without these both are null, the deduction silently comes to zero, and a
            // test of the property-only state would prove nothing.
            income.PropertyExpenseSummary = new IncomeExpenseSummary { TotalMonthly = AssetCosts };
            income.PropertyPayment = new PaymentOutput
            {
                TermPayment = LoanRepayment,
                TotalNumberPaymentPerYear = 12,
            };

            income.MarkInitializationComplete();
            return income;
        }

        // ── All four states of the Income tab ─────────────────────────────────

        [Test]
        public void NeitherToggle_CaptionNamesNoDeduction()
        {
            var vm = BuildIncomeVm();

            Assert.Multiple(() =>
            {
                Assert.That(vm.StringMonthlyTextOnTopBox, Is.EqualTo("Monthly Income"));
                Assert.That(vm.StringYearlyTextOnTopBox, Is.EqualTo("Yearly Income"));
            });
        }

        [Test]
        public void AfterExpensesOnly_CaptionNamesExpenses()
        {
            var vm = BuildIncomeVm();
            vm.ShowIncomeAfterExpense = true;

            Assert.That(vm.StringMonthlyTextOnTopBox, Is.EqualTo("Monthly Income (after expenses)"));
        }

        /// <summary>
        /// The silent misstatement: this figure has had the loan repayment and the asset running
        /// costs taken off, and the caption used to claim it was plain income.
        /// </summary>
        [Test]
        public void PropertyToggleOnly_CaptionNamesTheLoanAndAssetCosts()
        {
            var vm = BuildIncomeVm();
            vm.ShowIncomeAfterPropertyExpense = true;

            Assert.Multiple(() =>
            {
                Assert.That(vm.StringMonthlyTextOnTopBox,
                    Is.EqualTo("Monthly Income (after loan & asset costs)"));
                Assert.That(vm.StringMonthlyTextOnTopBox, Is.Not.EqualTo("Monthly Income"),
                    "the figure is net of the loan repayment, so the caption cannot say otherwise");
            });
        }

        [Test]
        public void BothToggles_CaptionNamesEverythingDeducted()
        {
            var vm = BuildIncomeVm();
            vm.ShowIncomeAfterExpense = true;
            vm.ShowIncomeAfterPropertyExpense = true;

            Assert.Multiple(() =>
            {
                Assert.That(vm.StringMonthlyTextOnTopBox,
                    Is.EqualTo("Monthly Income (after expenses, loan & asset costs)"));
                Assert.That(vm.StringYearlyTextOnTopBox,
                    Is.EqualTo("Yearly Income (after expenses, loan & asset costs)"));
            });
        }

        /// <summary>
        /// The caption and the arithmetic must agree: whenever the figure drops below gross income,
        /// the caption has to say something was taken off.
        /// </summary>
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void CaptionAnnouncesADeductionExactlyWhenOneWasApplied(bool afterExpenses, bool afterProperty)
        {
            var vm = BuildIncomeVm();
            vm.ShowIncomeAfterExpense = afterExpenses;
            vm.ShowIncomeAfterPropertyExpense = afterProperty;

            var caption = vm.StringMonthlyTextOnTopBox;

            Assert.Multiple(() =>
            {
                Assert.That(caption.Contains("expenses"), Is.EqualTo(afterExpenses),
                    $"expenses deduction mis-stated in \"{caption}\"");
                Assert.That(caption.Contains("loan & asset costs"), Is.EqualTo(afterProperty),
                    $"loan deduction mis-stated in \"{caption}\"");

                // And the arithmetic agrees: something really was taken off whenever a toggle is on.
                Assert.That(vm.TotalMonthlyExpense > 0, Is.EqualTo(afterExpenses || afterProperty),
                    "the caption and the figure must describe the same deductions");
            });
        }

        [Test]
        public void ChartTitle_FollowsTheSameDeductions()
        {
            var vm = BuildIncomeVm();
            vm.ShowIncomeAfterExpense = true;
            vm.ShowIncomeAfterPropertyExpense = true;

            Assert.That(vm.StringChartTitleText,
                Is.EqualTo("Income Growth Projection (after expenses, loan & asset costs)"));
        }

        // ── The Expense tab applies them differently ──────────────────────────

        /// <summary>
        /// On the Expense tab the property deduction only applies inside the after-expenses branch,
        /// so the property toggle alone subtracts nothing — and the caption must not claim it did.
        /// </summary>
        [Test]
        public void ExpenseTab_PropertyToggleAloneAnnouncesNothing()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var vm = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            vm.MarkInitializationComplete();
            vm.ShowPropertyExpense = true;

            Assert.That(vm.StringIncomeTextOnTopBox, Is.EqualTo("Monthly Income"),
                "nothing was subtracted, so nothing should be announced");
        }

        [Test]
        public void ExpenseTab_BothTogglesAnnounceEverything()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var vm = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            vm.MarkInitializationComplete();
            vm.ShowIncomeAfterExpense = true;
            vm.ShowPropertyExpense = true;

            Assert.That(vm.StringIncomeTextOnTopBox,
                Is.EqualTo("Monthly Income (after expenses, loan & asset costs)"));
        }
    }
}
