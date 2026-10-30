using LoanCalculator.Core.Constants;
using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// The explanations have to contain the reader's own figures, not just definitions.
    /// </summary>
    /// <remarks>
    /// A general explanation was not enough — "still not clear". Someone asking what "of income"
    /// means is far better served by seeing their own repayment measured against their own income
    /// than by a definition of the ratio. So these assert the actual numbers appear.
    /// </remarks>
    [TestFixture]
    public class LiveExplanationTests
    {
        private const double Income = 9_000;
        private const double Expenses = 2_000;
        private const double AssetCost = 300;

        [OneTimeSetUp]
        public void GrantPremium() => SharedServiceCore.EntitlementOverrideForTests = true;

        [OneTimeTearDown]
        public void ClearPremium() => SharedServiceCore.EntitlementOverrideForTests = null;

        private static LoanViewModel BuildLoanWithBudget()
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
                        TotalNumberPaymentPerYear = 12,
                    },
                    PropertyAmount = 650_000,
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] },
            };
            vm.MarkInitializationComplete();
            vm.HomeLoanInfo.DepositPercentage = 20;

            var income = new IncomeViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            income.TransactionRecords!.Add("Salary", Income, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            income.TransactionRecords.SumUpData();
            income.MarkInitializationComplete();

            var expense = new ExpenseViewModel { TransactionRecords = new Incomes { IncomeExpenseEntries = [] } };
            expense.TransactionRecords!.Add("Rent", Expenses, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expense.TransactionRecords.SumUpData();
            expense.MarkInitializationComplete();

            vm.IncomeSummary = income;
            vm.ExpenseSummary = expense;
            vm.HasIncomeExpensesRecorded = true;
            vm.EventsTriggerPriceUpdate();
            return vm;
        }

        // ── The What If Answer card ───────────────────────────────────────────

        [Test]
        public void AnswerCard_HasTheSameShapeAsAScenarioCard()
        {
            var e = BuildLoanWithBudget().AnswerPanelsExplanation;

            Assert.Multiple(() =>
            {
                Assert.That(e.Title, Is.Not.Empty);
                Assert.That(e.Purpose, Is.Not.Empty);
                Assert.That(e.WatchFor, Does.Contain("not financial advice").IgnoreCase);
                Assert.That(e.ReadingTheNumbers, Has.Count.EqualTo(3),
                    "one entry per figure beside the verdict");
            });
        }

        /// <summary>Each heading must carry the figure, not just the caption.</summary>
        [Test]
        public void AnswerCard_EachHeadingIncludesTheActualValue()
        {
            var vm = BuildLoanWithBudget();
            var terms = vm.AnswerPanelsExplanation.ReadingTheNumbers;

            Assert.Multiple(() =>
            {
                Assert.That(terms[0].Term, Does.Contain(vm.AnswerRepaymentDisplay));
                Assert.That(terms[1].Term, Does.Contain(vm.AnswerNetIncomeDisplay));
                Assert.That(terms[2].Term, Does.Contain(vm.AnswerRepaymentShareOfIncome));
            });
        }

        /// <summary>
        /// And the explanation must walk through where the figure came from, in real numbers.
        /// </summary>
        [Test]
        public void AnswerCard_ExplainsNetIncomeFromTheUsersOwnIncomeAndExpenses()
        {
            var terms = BuildLoanWithBudget().AnswerPanelsExplanation.ReadingTheNumbers;

            Assert.Multiple(() =>
            {
                Assert.That(terms[1].Meaning, Does.Contain("9,000"), "their income");
                Assert.That(terms[1].Meaning, Does.Contain("2,000"), "their expenses");
            });
        }

        [Test]
        public void AnswerCard_ExplainsTheShareAgainstTheirOwnIncome()
            => Assert.That(BuildLoanWithBudget().AnswerPanelsExplanation.ReadingTheNumbers[2].Meaning,
                Does.Contain("9,000"),
                "the ratio's denominator should be shown as the reader's own figure");

        /// <summary>With no budget the card must say so rather than quoting zeros as if real.</summary>
        [Test]
        public void AnswerCard_WithNoBudget_SaysThereIsNothingToWorkFrom()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput { InterestRate = 5, LoanTermInYears = 30, TotalNumberPaymentPerYear = 12 },
                    PropertyAmount = 650_000,
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] },
            };
            vm.MarkInitializationComplete();

            Assert.That(vm.AnswerPanelsExplanation.ReadingTheNumbers[1].Meaning,
                Does.Contain("not recorded").IgnoreCase);
        }

        // ── The Budget switch "with your numbers" sentences ───────────────────

        private static BudgetViewModel BuildBudget()
        {
            var loan = BuildLoanWithBudget();
            loan.TransactionRecords!.Add("Insurance", AssetCost, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            loan.TransactionRecords.SumUpData();

            var budget = new BudgetViewModel();
            budget.SetPeerViewModels(loan.IncomeSummary!, loan.ExpenseSummary!, loan);
            return budget;
        }

        [Test]
        public void IncomeSwitchExample_QuotesTheirIncomeAndSpending()
        {
            var text = BuildBudget().IncomeAfterExpensesLiveExample;

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("9,000"), "their income");
                Assert.That(text, Does.Contain("2,000"), "their spending");
                Assert.That(text, Does.StartWith("With your numbers"));
            });
        }

        [Test]
        public void LoanSwitchExample_QuotesTheirAssetRunningCosts()
            => Assert.That(BuildBudget().IncomeAfterLoanLiveExample, Does.Contain("300"),
                "the asset running costs they entered");

        [Test]
        public void ExpenseSwitchExample_NamesTheFigureItChanges()
            => Assert.That(BuildBudget().ExpenseAfterLoanLiveExample, Does.Contain("Net / month"),
                "the switch moves the Net box, so the sentence should name it");

        /// <summary>
        /// With nothing entered these must guide rather than quote zeros, which would read as a
        /// confident statement about figures the user has not given.
        /// </summary>
        [Test]
        public void SwitchExamples_WithNoData_TellTheUserWhatToEnter()
        {
            var empty = new BudgetViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(empty.IncomeAfterExpensesLiveExample, Does.Contain("Add some income").IgnoreCase);
                Assert.That(empty.IncomeAfterLoanLiveExample, Does.Contain("Add your income").IgnoreCase);
                Assert.That(empty.ExpenseAfterLoanLiveExample, Does.Contain("Add your income").IgnoreCase);
            });
        }

        // ── The Expenses tab's Net box, which the switch now actually moves ───

        [Test]
        public void ExpensesTabNet_RespondsToTheSwitch()
        {
            var budget = BuildBudget();
            var before = budget.ExpensesTabNetMonthly;

            budget.Expense.ShowPropertyExpense = true;

            // Cancel the 600 ms debounce that setter scheduled. Left armed it fires during a LATER
            // test, whose own expense deduction is then applied twice — which is exactly how this
            // test broke ExpenseViewModelTests' negative-net case (-9000 instead of -4000). The
            // no-op action cancels without saving; see the same guard in that test.
            budget.Expense.FlushPendingSave(() => { });

            Assert.Multiple(() =>
            {
                Assert.That(budget.ExpensesTabNetMonthly, Is.Not.EqualTo(before),
                    "the switch did nothing at all before this was wired up");
                Assert.That(budget.ExpensesTabNetCaption, Does.Contain("after loan"),
                    "and the caption must say what changed");
            });
        }

        // ── Negative-figure flags ─────────────────────────────────────────────
        //
        // The summary boxes colour a figure red when it is below zero, and the condition comes
        // from these flags. They are trivial, but a flag stuck at false renders a minus figure in
        // the ordinary colour and nothing else would notice — which is exactly the state the
        // Income tab's two boxes were in.

        [Test]
        public void IncomeNegativeFlags_FollowTheFigures()
        {
            var budget = BuildBudget();
            var income = budget.Income;

            // Nothing deducted, so income is positive.
            income.ShowIncomeAfterExpense = false;
            income.ShowIncomeAfterPropertyExpense = false;

            Assert.Multiple(() =>
            {
                Assert.That(income.TotalMonthlyIncomeValue, Is.GreaterThan(0));
                Assert.That(income.IsTotalMonthlyIncomeNegative, Is.False);
                Assert.That(income.IsTotalYearlyIncomeNegative, Is.False);
            });

            // Deduct the loan and the asset costs, which on this fixture outweighs the income.
            income.ShowIncomeAfterExpense = true;
            income.ShowIncomeAfterPropertyExpense = true;

            Assert.Multiple(() =>
            {
                Assert.That(income.IsTotalMonthlyIncomeNegative,
                    Is.EqualTo(income.TotalMonthlyIncomeValue < 0),
                    "the monthly flag disagrees with the monthly figure it colours");
                Assert.That(income.IsTotalYearlyIncomeNegative,
                    Is.EqualTo(income.TotalYearlyIncomeValue < 0),
                    "the yearly flag disagrees with the yearly figure it colours");
            });
        }

        [Test]
        public void ExpensesTabNetIsNegative_IsTheInverseOfIsPositive()
        {
            var budget = BuildBudget();

            // ExpensesTabIncludesLoan is computed from the switch, so drive the switch.
            foreach (var showLoan in new[] { false, true })
            {
                budget.Expense.ShowPropertyExpense = showLoan;

                Assert.That(budget.ExpensesTabNetIsNegative, Is.EqualTo(!budget.ExpensesTabNetIsPositive),
                    $"the two flags disagree with ShowPropertyExpense={showLoan}");
                Assert.That(budget.ExpensesTabNetIsNegative, Is.EqualTo(budget.ExpensesTabNetMonthly < 0),
                    "the flag disagrees with the figure it colours");
            }
        }

        // ── Projection settings summary ───────────────────────────────────────
        //
        // The Projection settings panel is collapsed by default, so this one line is all a reader
        // sees of what is currently applied. If it stops naming a setting, the closed panel is
        // hiding information rather than just controls — and the totals above it would be
        // unexplainable (before the loan or after it are very different numbers).

        [Test]
        public void ProjectionSettingsSummary_NamesBothGrowthRatesAndTheHorizon()
        {
            var budget = BuildBudget();
            budget.Income.AnnualGrowthRate = 3;
            budget.Expense.AnnualGrowthRate = 5;
            budget.ProjectionYears = 12;

            var summary = budget.ProjectionSettingsSummary;

            Assert.Multiple(() =>
            {
                Assert.That(summary, Does.Contain("3%"), "the income growth rate is not shown");
                Assert.That(summary, Does.Contain("5%"), "the expense growth rate is not shown");
                Assert.That(summary, Does.Contain("12 years"), "the horizon is not shown");
            });
        }

        [Test]
        public void ProjectionSettingsSummary_SaysSingularYearForOne()
        {
            var budget = BuildBudget();
            budget.ProjectionYears = 1;

            Assert.That(budget.ProjectionSettingsSummary, Does.Contain("1 year").And.Not.Contain("1 years"));
        }

        /// <summary>
        /// The deductions are the settings that change what the totals MEAN, so they are the ones
        /// that must never be silently in force behind a closed panel.
        /// </summary>
        [Test]
        public void ProjectionSettingsSummary_MentionsADeductionExactlyWhenOneIsApplied()
        {
            var budget = BuildBudget();

            budget.Income.IncludeExpenses = false;
            budget.Income.IncludePropertyExpenses = false;
            budget.Expense.ShowPropertyExpense = false;
            var none = budget.ProjectionSettingsSummary;

            budget.Income.IncludeExpenses = true;
            var withSpending = budget.ProjectionSettingsSummary;

            budget.Income.IncludePropertyExpenses = true;
            var withLoan = budget.ProjectionSettingsSummary;

            Assert.Multiple(() =>
            {
                Assert.That(none, Does.Not.Contain("after"),
                    "nothing is deducted, so the summary must not claim anything is");
                Assert.That(withSpending, Does.Contain("after everyday spending"));
                Assert.That(withLoan, Does.Contain("after the loan"));
            });
        }
    }
}
