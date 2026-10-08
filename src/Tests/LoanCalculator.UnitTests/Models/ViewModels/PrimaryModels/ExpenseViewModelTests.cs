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
    public class ExpenseViewModelTests
    {
        private ExpenseViewModel _vm;

        private static ExpenseViewModel BuildInitialized()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            var vm = new ExpenseViewModel();
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

        // ── WizardExpenseHasValue ─────────────────────────────────────────────

        [Test]
        public void WizardExpenseHasValue_NoEntries_IsFalse()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.WizardExpenseHasValue, Is.False);
        }

        [Test]
        public void WizardExpenseHasValue_WithPositiveEntry_IsTrue()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Rent", 1500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.WizardExpenseHasValue, Is.True);
        }

        // ── WizardExpenseSummary ──────────────────────────────────────────────

        [Test]
        public void WizardExpenseSummary_ContainsRecorded()
        {
            Assert.That(_vm.WizardExpenseSummary, Does.Contain("Recorded"));
        }

        [Test]
        public void WizardExpenseSummary_ContainsMoSuffix()
        {
            Assert.That(_vm.WizardExpenseSummary, Does.Contain("/mo"));
        }

        // ── TotalMonthlyExpense ───────────────────────────────────────────────

        [Test]
        public void TotalMonthlyExpense_NoEntries_ReturnsZero()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            _vm.ShowPropertyExpense = false;
            Assert.That(_vm.TotalMonthlyExpense, Is.EqualTo(0));
        }

        [Test]
        public void TotalMonthlyExpense_WithBaseEntries_ReturnsBaseMonthly()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Groceries", 800, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            _vm.ShowPropertyExpense = false;

            Assert.That(_vm.TotalMonthlyExpense, Is.EqualTo(800).Within(1));
        }

        [Test]
        public void TotalMonthlyExpense_WithPropertyExpense_AddsPropertyAndPayment()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Groceries", 500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();

            _vm.ShowPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalMonthly = 300 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 2000, TotalNumberPaymentPerYear = 12 };

            Assert.That(_vm.TotalMonthlyExpense, Is.EqualTo(2800).Within(1));
        }

        // ── TotalYearlyExpense ────────────────────────────────────────────────

        [Test]
        public void TotalYearlyExpense_NoEntries_ReturnsZero()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            _vm.ShowPropertyExpense = false;
            Assert.That(_vm.TotalYearlyExpense, Is.EqualTo(0));
        }

        [Test]
        public void TotalYearlyExpense_WithBaseEntries_ReturnsBaseYearly()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Rent", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            _vm.ShowPropertyExpense = false;

            Assert.That(_vm.TotalYearlyExpense, Is.EqualTo(12000).Within(1));
        }

        [Test]
        public void TotalYearlyExpense_WithPropertyExpense_AddsPropertyYearly()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            _vm.ShowPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalYearly = 3600 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 2000, TotalNumberPaymentPerYear = 12 };

            Assert.That(_vm.TotalYearlyExpense, Is.EqualTo(27600).Within(1));
        }

        // ── TotalYearlyIncomeWithComma (returns expense, not income) ──────────

        [Test]
        public void TotalYearlyIncomeWithComma_NoProperty_ReturnsBaseExpenseFormatted()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Rent", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            _vm.ShowPropertyExpense = false;

            var result = _vm.TotalYearlyIncomeWithComma;
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(12000).Within(1));
        }

        // ── TotalIncomeMonthlyWithComma ────────────────────────────────────────

        [Test]
        public void TotalIncomeMonthlyWithComma_NoFlags_ReturnsIncomeMonthly()
        {
            var incomeVm = new IncomeViewModel();
            incomeVm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            incomeVm.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            incomeVm.TransactionRecords.SumUpData();

            _vm.IncomeSummary = incomeVm;
            _vm.ShowIncomeAfterExpense = false;

            var result = _vm.TotalIncomeMonthlyWithComma;
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(5000).Within(1));
        }

        [Test]
        public void TotalIncomeMonthlyWithComma_ShowIncomeAfterExpense_SubtractsExpenses()
        {
            var incomeVm = new IncomeViewModel();
            incomeVm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            incomeVm.TransactionRecords.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            incomeVm.TransactionRecords.SumUpData();

            _vm.IncomeSummary = incomeVm;
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Rent", 1500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            _vm.ShowIncomeAfterExpense = true;
            _vm.ShowPropertyExpense = false;

            var result = _vm.TotalIncomeMonthlyWithComma;
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(3500).Within(1));
        }

        [Test]
        public void TotalIncomeMonthlyWithComma_ShowPropertyExpense_SubtractsPropertyCosts()
        {
            var incomeVm = new IncomeViewModel();
            incomeVm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            incomeVm.TransactionRecords.Add("Salary", 8000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            incomeVm.TransactionRecords.SumUpData();

            _vm.IncomeSummary = incomeVm;
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Groceries", 500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            _vm.ShowIncomeAfterExpense = true;
            _vm.ShowPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalMonthly = 200 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 2000, TotalNumberPaymentPerYear = 12 };

            var result = _vm.TotalIncomeMonthlyWithComma;
            // 8000 - 500 - 200 - 2000 = 5300
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(5300).Within(1));
        }

        [Test]
        public void TotalIncomeMonthlyWithComma_AfterIncomeTabDeductedItsOwnTotals_StillShowsGrossIncome()
        {
            // Regression: navigating Budget tabs made this box show a different (negative) value.
            // The Income tab refreshing with its own "income after expenses" toggles on used to
            // call SumUpData(deduction), overwriting TotalMonthly on the SHARED summary with a net
            // figure. This box means "the income the user entered", so it must show the gross.
            //
            // Driven through the real trigger rather than a synthetic SumUpData(deduction) call,
            // which no longer exists: asserting against the plain SumUpData() would make this
            // test tautological. The loading guard keeps the setter's fire-and-forget async
            // refresh from racing the synchronous one — SumUpData is not atomic.
            var incomeVm = new IncomeViewModel();
            incomeVm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            incomeVm.TransactionRecords.Add("Salary", 14000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            incomeVm.TransactionRecords.SumUpData();
            incomeVm.MarkInitializationComplete();
            incomeVm.ExpenseSummary = _vm;
            PageHelper.PageIsLoading();
            incomeVm.ShowIncomeAfterExpense = true;
            PageHelper.PageLoadingComplete();
            incomeVm.RefreshIncomePropertyChanged();
            incomeVm.FlushPendingSave(() => { });

            _vm.IncomeSummary = incomeVm;
            // This tab's own toggles are off, so no deduction should be applied here.
            _vm.ShowIncomeAfterExpense = false;
            _vm.ShowPropertyExpense = false;

            var result = _vm.TotalIncomeMonthlyWithComma;
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(14000).Within(1),
                "must not inherit the Income tab's after-expenses deduction");
        }

        [Test]
        public void TotalIncomeMonthly_NegativeNet_KeepsSignOnSymbolAndAmountAbsolute()
        {
            // The box renders symbol + amount separately so a negative net reads "-$4,000"
            // rather than "$-4,000".
            var incomeVm = new IncomeViewModel();
            incomeVm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            incomeVm.TransactionRecords.Add("Salary", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            incomeVm.TransactionRecords.SumUpData();

            _vm.IncomeSummary = incomeVm;
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Rent", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            _vm.ShowIncomeAfterExpense = true;
            _vm.ShowPropertyExpense = false;

            // Cancel the 600 ms debounce the property sets above scheduled. If that timer fires
            // between here and the asserts, its pass leaves IncomeExpenseSummary.TotalMonthly
            // already carrying the expense deduction, and the getter below subtracts it a second
            // time — giving -9000 instead of -4000. The no-op action cancels without saving.
            // (The underlying order-dependence of SumUpData(deduction) is a production issue; see
            // the defensive comment in LoanViewModel.AffordabilityRawValue.)
            _vm.FlushPendingSave(() => { });

            Assert.Multiple(() =>
            {
                Assert.That(_vm.TotalIncomeMonthlyValue, Is.EqualTo(-4000).Within(1));
                Assert.That(_vm.TotalIncomeMonthlyCurrencySymbol, Does.StartWith("-"));
                Assert.That(double.Parse(_vm.TotalIncomeMonthlyWithComma.Replace(",", "")),
                    Is.EqualTo(4000).Within(1), "amount is absolute; the sign lives on the symbol");
            });
        }

        // ── TotalMonthlySumExpenseWithComma ────────────────────────────────────

        [Test]
        public void TotalMonthlySumExpenseWithComma_NoPropertyFlag_ReturnsBaseExpense()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Groceries", 600, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            _vm.ShowPropertyExpense = false;

            var result = _vm.TotalMonthlySumExpenseWithComma;
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(600).Within(1));
        }

        [Test]
        public void TotalMonthlySumExpenseWithComma_WithPropertyFlag_AddsPropertyCosts()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            _vm.ShowPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalMonthly = 300 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 1500, TotalNumberPaymentPerYear = 12 };

            var result = _vm.TotalMonthlySumExpenseWithComma;
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(1800).Within(1));
        }

        // ── AddDefaultToExpenses ──────────────────────────────────────────────

        [Test]
        public void AddDefaultToExpenses_Adds22Entries()
        {
            _vm.AddDefaultToExpenses();
            Assert.That(_vm.TransactionRecords!.IncomeExpenseEntries!.Count, Is.EqualTo(22));
        }

        [Test]
        public void AddDefaultToExpenses_ContainsFoodAndGroceries()
        {
            _vm.AddDefaultToExpenses();
            Assert.That(_vm.TransactionRecords!.Exists("Food and Groceries"), Is.True);
        }

        [Test]
        public void AddDefaultToExpenses_AllDefaultEntriesHaveZeroAmount()
        {
            _vm.AddDefaultToExpenses();
            var allZero = _vm.TransactionRecords!.IncomeExpenseEntries!.All(e => e.Amount == 0);
            Assert.That(allZero, Is.True);
        }

        // ── ChartProjection axes ──────────────────────────────────────────────

        [Test]
        public void ChartProjectionTermStartAmountAxis_NoProjectionData_ReturnsEmpty()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            Assert.That(_vm.ChartProjectionTermStartAmountAxis, Is.Empty);
        }

        [Test]
        public void ChartProjectionIncomeExpenseAmountAxis_NoProjectionData_ReturnsEmpty()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            Assert.That(_vm.ChartProjectionIncomeExpenseAmountAxis, Is.Empty);
        }

        // ── CopyPropertiesFrom ─────────────────────────────────────────────────

        [Test]
        public void CopyPropertiesFrom_Null_ThrowsArgumentNullException()
        {
            var vm = new ExpenseViewModel();
            Assert.Throws<ArgumentNullException>(() => vm.CopyPropertiesFrom(null!));
        }

        [Test]
        public void CopyPropertiesFrom_CopiesShowPropertyExpense()
        {
            var source = BuildInitialized();
            source.ShowPropertyExpense = true;

            var target = new ExpenseViewModel();
            target.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            target.MarkInitializationComplete();
            target.CopyPropertiesFrom(source);

            Assert.That(target.ShowPropertyExpense, Is.True);
        }

        // ── WizardExpenseEditable mirrors WizardExpenseHasValue ──────────────

        [Test]
        public void WizardExpenseEditable_NoEntries_IsTrue()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.WizardExpenseEditable, Is.True);
        }

        [Test]
        public void WizardExpenseEditable_WithPositiveEntry_IsFalse()
        {
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.Add("Rent", 1500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _vm.TransactionRecords.SumUpData();
            Assert.That(_vm.WizardExpenseEditable, Is.False);
        }

        // ── StringMonthlyExpenseOnTopBox ──────────────────────────────────────

        [Test]
        public void StringMonthlyExpenseOnTopBox_NoPropertyFlag_SaysMonthlyExpenses()
        {
            _vm.ShowPropertyExpense = false;
            Assert.That(_vm.StringMonthlyExpenseOnTopBox, Is.EqualTo("Monthly expenses"));
        }

        [Test]
        public void StringMonthlyExpenseOnTopBox_WithPropertyFlag_SaysMonthlyAndProperty()
        {
            _vm.ShowPropertyExpense = true;
            Assert.That(_vm.StringMonthlyExpenseOnTopBox, Does.Contain("Property loan"));
        }

        // ── StringIncomeTextOnTopBox ──────────────────────────────────────────

        [Test]
        public void StringIncomeTextOnTopBox_NoExpense_SaysMonthlyIncome()
        {
            _vm.ShowIncomeAfterExpense = false;
            Assert.That(_vm.StringIncomeTextOnTopBox, Is.EqualTo("Monthly Income"));
        }

        [Test]
        public void StringIncomeTextOnTopBox_WithExpense_SaysAfterExpenses()
        {
            _vm.ShowIncomeAfterExpense = true;
            Assert.That(_vm.StringIncomeTextOnTopBox, Is.EqualTo("Monthly Income (after expenses)"));
        }

        // ── TotalMonthlyExpenseBreakdownWithComma ──────────────────────────────

        [Test]
        public void TotalMonthlyExpenseBreakdownWithComma_NoPropertyFlag_ReturnsEmpty()
        {
            _vm.ShowPropertyExpense = false;
            Assert.That(_vm.TotalMonthlyExpenseBreakdownWithComma, Is.Empty);
        }

        [Test]
        public void TotalMonthlyExpenseBreakdownWithComma_WithPropertyFlag_ContainsPlusSigns()
        {
            _vm.ShowPropertyExpense = true;
            _vm.PropertyExpenseSummary = new IncomeExpenseSummary { TotalMonthly = 300 };
            _vm.PropertyPayment = new PaymentOutput { TermPayment = 1500, TotalNumberPaymentPerYear = 12 };
            Assert.That(_vm.TotalMonthlyExpenseBreakdownWithComma, Does.Contain("+"));
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

        // ── AnnualGrowthRate / AnnualGrowthRatePercentage ─────────────────────

        [Test]
        public void AnnualGrowthRate_NullRecords_ReturnsZero()
        {
            _vm.TransactionRecords = null;
            Assert.That(_vm.AnnualGrowthRate, Is.EqualTo(0));
        }

        [Test]
        public void AnnualGrowthRatePercentage_NullRecords_ReturnsZero()
        {
            _vm.TransactionRecords = null;
            Assert.That(_vm.AnnualGrowthRatePercentage, Is.EqualTo(0));
        }

        // ── TotalMonthlyIncomeWithComma ────────────────────────────────────────

        [Test]
        public void TotalMonthlyIncomeWithComma_NoIncomeSummary_ReturnsZero()
        {
            _vm.IncomeSummary = null;
            _vm.ShowIncomeAfterExpense = false;
            var result = _vm.TotalIncomeMonthlyWithComma;
            Assert.That(double.Parse(result.Replace(",", "")), Is.EqualTo(0));
        }
    }
}
