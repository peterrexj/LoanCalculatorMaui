using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Income.Summary;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    [TestFixture]
    public class IncomeViewModelTests
    {
        private IncomeViewModel _vm;

        private static IncomeViewModel BuildInitialized()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            var vm = new IncomeViewModel();
            vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            vm.MarkInitializationComplete();
            return vm;
        }

        [SetUp]
        public void Setup()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            _vm = BuildInitialized();
        }

        [TearDown]
        public void TearDown()
        {
            SharedServiceCore.LoadSafeOff();
            PageHelper.PageLoadingComplete();
        }

        // ── WizardIncomeHasValue ──────────────────────────────────────────────

        [Test]
        public void WizardIncomeHasValue_NoEntries_IsFalse()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.WizardIncomeHasValue, Is.False);
        }

        [Test]
        public void WizardIncomeHasValue_WithPositiveEntry_IsTrue()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.WizardIncomeHasValue, Is.True);
        }

        // ── WizardIncomeSummary ───────────────────────────────────────────────

        [Test]
        public void WizardIncomeSummary_ContainsRecorded()
        {
            Assert.That(_vm.WizardIncomeSummary, Does.Contain("Recorded"));
        }

        [Test]
        public void WizardIncomeSummary_ContainsYrSuffix()
        {
            Assert.That(_vm.WizardIncomeSummary, Does.Contain("/yr"));
        }

        // ── TotalMonthlyExpense branches ──────────────────────────────────────

        [Test]
        public void TotalMonthlyExpense_NeitherFlagSet_ReturnsZero()
        {
            _vm.ShowIncomeAfterExpense = false;
            _vm.ShowIncomeAfterPropertyExpense = false;
            Assert.That(_vm.TotalMonthlyExpense, Is.EqualTo(0));
        }

        [Test]
        public void TotalMonthlyExpense_ShowIncomeAfterExpense_ReturnsExpenseSummaryMonthly()
        {
            var expenseVm = new ExpenseViewModel();
            expenseVm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            expenseVm.TransactionRecords.Add("Rent", 1500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expenseVm.TransactionRecords.SumUpData();

            _vm.ExpenseSummary = expenseVm;
            _vm.ShowIncomeAfterExpense = true;
            _vm.ShowIncomeAfterPropertyExpense = false;

            Assert.That(_vm.TotalMonthlyExpense, Is.EqualTo(1500).Within(1));
        }

        [Test]
        public void TotalMonthlyExpense_ShowIncomeAfterPropertyExpense_AddsPropertyExpenseAndPayment()
        {
            _vm.ShowIncomeAfterExpense = false;
            _vm.ShowIncomeAfterPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalMonthly = 300 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 2000, TotalNumberPaymentPerYear = 12 };

            Assert.That(_vm.TotalMonthlyExpense, Is.EqualTo(2300).Within(1));
        }

        [Test]
        public void TotalMonthlyExpense_BothFlagsSet_SumsAll()
        {
            var expenseVm = new ExpenseViewModel();
            expenseVm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            expenseVm.TransactionRecords.Add("Groceries", 500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expenseVm.TransactionRecords.SumUpData();

            _vm.ExpenseSummary = expenseVm;
            _vm.ShowIncomeAfterExpense = true;
            _vm.ShowIncomeAfterPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalMonthly = 200 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 1000, TotalNumberPaymentPerYear = 12 };

            Assert.That(_vm.TotalMonthlyExpense, Is.EqualTo(1700).Within(1));
        }

        // ── TotalYearlyExpense branches ───────────────────────────────────────

        [Test]
        public void TotalYearlyExpense_NeitherFlagSet_ReturnsZero()
        {
            _vm.ShowIncomeAfterExpense = false;
            _vm.ShowIncomeAfterPropertyExpense = false;
            Assert.That(_vm.TotalYearlyExpense, Is.EqualTo(0));
        }

        [Test]
        public void TotalYearlyExpense_ShowIncomeAfterExpense_ReturnsYearlyExpense()
        {
            var expenseVm = new ExpenseViewModel();
            expenseVm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            expenseVm.TransactionRecords.Add("Rent", 1200, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            expenseVm.TransactionRecords.SumUpData();

            _vm.ExpenseSummary = expenseVm;
            _vm.ShowIncomeAfterExpense = true;
            _vm.ShowIncomeAfterPropertyExpense = false;

            Assert.That(_vm.TotalYearlyExpense, Is.EqualTo(14400).Within(1));
        }

        // ── TotalMonthlySumExpenseWithComma ────────────────────────────────────

        [Test]
        public void TotalMonthlySumExpenseWithComma_NoFlags_ReturnsZero()
        {
            _vm.ShowIncomeAfterPropertyExpense = false;
            Assert.That(_vm.TotalMonthlySumExpenseWithComma, Is.EqualTo("0"));
        }

        [Test]
        public void TotalMonthlySumExpenseWithComma_WithPropertyExpense_IncludesProperty()
        {
            _vm.ShowIncomeAfterPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalMonthly = 300 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 1000, TotalNumberPaymentPerYear = 12 };
            // No base expense summary
            _vm.ExpenseSummary = null;

            var result = _vm.TotalMonthlySumExpenseWithComma;
            // PropertyExpenseSummary(300) + PropertyPayment.TermPaymentMonthly(1000) = 1300
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(1300).Within(1));
        }

        // ── StringMonthlyTextOnTopBox ──────────────────────────────────────────

        [Test]
        public void StringMonthlyTextOnTopBox_NoExpense_SaysMonthlyIncome()
        {
            _vm.ShowIncomeAfterExpense = false;
            Assert.That(_vm.StringMonthlyTextOnTopBox, Is.EqualTo("Monthly Income"));
        }

        [Test]
        public void StringMonthlyTextOnTopBox_WithExpense_SaysAfterExpenses()
        {
            _vm.ShowIncomeAfterExpense = true;
            Assert.That(_vm.StringMonthlyTextOnTopBox, Is.EqualTo("Monthly Income (after expenses)"));
        }

        // ── StringChartTitleText ───────────────────────────────────────────────

        [Test]
        public void StringChartTitleText_NoExpense_SaysIncomeGrowthProjection()
        {
            _vm.ShowIncomeAfterExpense = false;
            Assert.That(_vm.StringChartTitleText, Is.EqualTo("Income Growth Projection"));
        }

        [Test]
        public void StringChartTitleText_WithExpense_SaysAfterExpense()
        {
            _vm.ShowIncomeAfterExpense = true;
            Assert.That(_vm.StringChartTitleText, Is.EqualTo("Income Growth Projection (after expense)"));
        }

        // ── StringProjectionInfoText ───────────────────────────────────────────

        [Test]
        public void StringProjectionInfoText_NoExpense_IsEmpty()
        {
            _vm.ShowIncomeAfterExpense = false;
            Assert.That(_vm.StringProjectionInfoText, Is.Empty);
        }

        [Test]
        public void StringProjectionInfoText_WithExpense_ContainsAfterExpense()
        {
            _vm.ShowIncomeAfterExpense = true;
            Assert.That(_vm.StringProjectionInfoText, Does.Contain("after expense"));
        }

        // ── ChartProjection axes with no data ────────────────────────────────

        [Test]
        public void ChartProjectionTermStartAmountAxis_NullProjectionTerms_ReturnsEmptyCollection()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            Assert.That(_vm.ChartProjectionTermStartAmountAxis, Is.Empty);
        }

        [Test]
        public void ChartProjectionIncomeExpenseAmountAxis_NullProjectionTerms_ReturnsEmptyCollection()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            Assert.That(_vm.ChartProjectionIncomeExpenseAmountAxis, Is.Empty);
        }

        [Test]
        public void ChartProjectionDeductionAmountAxis_NullProjectionTerms_ReturnsEmptyCollection()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            Assert.That(_vm.ChartProjectionDeductionAmountAxis, Is.Empty);
        }

        // ── AnnualGrowthRate ───────────────────────────────────────────────────

        [Test]
        public void AnnualGrowthRate_NoSummary_ReturnsZero()
        {
            _vm.TransactionRecords = null;
            Assert.That(_vm.AnnualGrowthRate, Is.EqualTo(0));
        }

        [Test]
        public void AnnualGrowthRatePercentage_NoSummary_ReturnsZero()
        {
            _vm.TransactionRecords = null;
            Assert.That(_vm.AnnualGrowthRatePercentage, Is.EqualTo(0));
        }

        // ── AddDefaultToExpenses resets records ────────────────────────────────

        [Test]
        public void AddDefaultToExpenses_SetsEmptyRecords()
        {
            _vm.TransactionRecords!.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.AddDefaultToExpenses();
            Assert.That(_vm.TransactionRecords!.IncomeExpenseEntries, Is.Empty);
        }

        // ── CopyPropertiesFrom ─────────────────────────────────────────────────

        [Test]
        public void CopyPropertiesFrom_Null_ThrowsArgumentNullException()
        {
            var vm = new IncomeViewModel();
            Assert.Throws<ArgumentNullException>(() => vm.CopyPropertiesFrom(null!));
        }

        [Test]
        public void CopyPropertiesFrom_CopiesShowIncomeAfterExpense()
        {
            var source = BuildInitialized();
            source.ShowIncomeAfterExpense = true;

            var target = new IncomeViewModel();
            target.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            target.MarkInitializationComplete();
            target.CopyPropertiesFrom(source);

            Assert.That(target.ShowIncomeAfterExpense, Is.True);
        }

        // ── TotalMonthlyIncomeWithComma / TotalYearlyIncomeWithComma ──────────

        [Test]
        public void TotalMonthlyIncomeWithComma_NoEntries_ReturnsZeroOrEmpty()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            // IncomeExpenseSummary.TotalMonthlyWithComma returns "0" when no entries exist
            Assert.That(_vm.TotalMonthlyIncomeWithComma, Is.EqualTo("0").Or.Empty);
        }

        [Test]
        public void TotalMonthlyIncomeWithComma_WithEntry_ContainsAmount()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Salary", 4000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.TotalMonthlyIncomeWithComma, Does.Contain("4,000").Or.Contain("4000"));
        }

        [Test]
        public void TotalYearlyIncomeWithComma_NoEntries_ReturnsZeroOrEmpty()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            // IncomeExpenseSummary.TotalYearlyWithComma returns "0" when no entries exist
            Assert.That(_vm.TotalYearlyIncomeWithComma, Is.EqualTo("0").Or.Empty);
        }

        [Test]
        public void TotalYearlyIncomeWithComma_WithMonthlyEntry_IsAnnualised()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            var yearly = double.Parse(_vm.TotalYearlyIncomeWithComma.Replace(",", ""));
            Assert.That(yearly, Is.EqualTo(60000).Within(1));
        }

        // ── WizardIncomeEditable mirrors WizardIncomeHasValue ────────────────

        [Test]
        public void WizardIncomeEditable_NoEntries_IsTrue()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.WizardIncomeEditable, Is.True);
        }

        [Test]
        public void WizardIncomeEditable_WithPositiveEntry_IsFalse()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.WizardIncomeEditable, Is.False);
        }

        // ── StringYearlyTextOnTopBox ──────────────────────────────────────────

        [Test]
        public void StringYearlyTextOnTopBox_NoExpense_SaysYearlyIncome()
        {
            _vm.ShowIncomeAfterExpense = false;
            Assert.That(_vm.StringYearlyTextOnTopBox, Is.EqualTo("Yearly Income"));
        }

        [Test]
        public void StringYearlyTextOnTopBox_WithExpense_SaysAfterExpenses()
        {
            _vm.ShowIncomeAfterExpense = true;
            Assert.That(_vm.StringYearlyTextOnTopBox, Is.EqualTo("Yearly Income (after expenses)"));
        }

        // ── TotalYearlyExpense branches ───────────────────────────────────────

        [Test]
        public void TotalYearlyExpense_ShowIncomeAfterPropertyExpense_AddsPropertyYearly()
        {
            _vm.ShowIncomeAfterExpense = false;
            _vm.ShowIncomeAfterPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalYearly = 3600 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 2000, TotalNumberPaymentPerYear = 12 };

            Assert.That(_vm.TotalYearlyExpense, Is.EqualTo(27600).Within(1));
        }

        // ── TotalProjectedYearlyIncomeWithComma ───────────────────────────────

        [Test]
        public void TotalProjectedYearlyIncomeWithComma_NullSummary_ReturnsEmpty()
        {
            _vm.TransactionRecords = null;
            Assert.That(_vm.TotalProjectedYearlyIncomeWithComma, Is.Empty);
        }

        // ── IncomeProjectList ─────────────────────────────────────────────────

        [Test]
        public void IncomeProjectList_NoProjectionData_ReturnsEmptyList()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            Assert.That(_vm.IncomeProjectList, Is.Empty);
        }

        // ── StringMonthlyExpenseOnTopBox ──────────────────────────────────────

        [Test]
        public void StringMonthlyExpenseOnTopBox_NoPropertyExpense_SaysMonthlyExpenses()
        {
            _vm.ShowIncomeAfterPropertyExpense = false;
            Assert.That(_vm.StringMonthlyExpenseOnTopBox, Is.EqualTo("Monthly expenses"));
        }

        [Test]
        public void StringMonthlyExpenseOnTopBox_WithPropertyExpense_SaysMonthlyAndProperty()
        {
            _vm.ShowIncomeAfterPropertyExpense = true;
            Assert.That(_vm.StringMonthlyExpenseOnTopBox, Does.Contain("Property loan"));
        }
    }
}
