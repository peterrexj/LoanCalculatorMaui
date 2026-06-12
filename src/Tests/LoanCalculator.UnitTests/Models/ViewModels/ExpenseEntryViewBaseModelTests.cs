using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels;

namespace LoanCalculator.UnitTests.Models.ViewModels
{
    [TestFixture]
    public class ExpenseEntryViewBaseModelTests
    {
        private ExpenseEntryViewBaseModel _vm;

        [SetUp]
        public void Setup()
        {
            _vm = new ExpenseEntryViewBaseModel();
            _vm.TransactionRecords = new Incomes();
            _vm.MarkInitializationComplete();
        }

        // ── HasInitialized / IsEditMode ───────────────────────────────────────

        [Test]
        public void HasInitialized_AfterMarkInitializationComplete_IsTrue()
        {
            var vm = new ExpenseEntryViewBaseModel();
            vm.MarkInitializationComplete();
            Assert.That(vm.HasInitialized, Is.True);
        }

        [Test]
        public void IsEditMode_NewEntry_IsFalse()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense();
            Assert.That(_vm.IsEditMode, Is.False);
        }

        [Test]
        public void IsEditMode_EntryWithId_IsTrue()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Id = Guid.NewGuid() };
            Assert.That(_vm.IsEditMode, Is.True);
        }

        // ── IncomeEntryAmountText parsing ─────────────────────────────────────

        [Test]
        public void IncomeEntryAmountText_SetValidNumber_UpdatesAmount()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense();
            _vm.IncomeEntryAmountText = "1500";
            Assert.That(_vm.IncomeExpenseEntry.Amount, Is.EqualTo(1500).Within(0.01));
        }

        [Test]
        public void IncomeEntryAmountText_SetWithCommas_ParsesCorrectly()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense();
            _vm.IncomeEntryAmountText = "1,234,567";
            Assert.That(_vm.IncomeExpenseEntry.Amount, Is.EqualTo(1234567).Within(0.01));
        }

        [Test]
        public void IncomeEntryAmountText_SetEmpty_SetsAmountZero()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Amount = 500 };
            _vm.IncomeEntryAmountText = "";
            Assert.That(_vm.IncomeExpenseEntry.Amount, Is.EqualTo(0));
        }

        [Test]
        public void IncomeEntryAmountText_SetWhitespace_SetsAmountZero()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Amount = 500 };
            _vm.IncomeEntryAmountText = "   ";
            Assert.That(_vm.IncomeExpenseEntry.Amount, Is.EqualTo(0));
        }

        [Test]
        public void IncomeEntryAmountText_SetInvalidString_DoesNotChangeAmount()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Amount = 999 };
            _vm.IncomeEntryAmountText = "abc";
            // neither branch fires: amount stays 999
            Assert.That(_vm.IncomeExpenseEntry.Amount, Is.EqualTo(999));
        }

        // ── Validation flags ──────────────────────────────────────────────────

        [Test]
        public void HasErrorIncomeDescription_EmptyName_IsTrue()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "" };
            Assert.That(_vm.HasErrorIncomeDescription, Is.True);
        }

        [Test]
        public void HasErrorIncomeDescription_WithName_IsFalse()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "Rent" };
            Assert.That(_vm.HasErrorIncomeDescription, Is.False);
        }

        [Test]
        public void HasErrorIncomeAmount_ZeroAmount_IsTrue()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Amount = 0 };
            Assert.That(_vm.HasErrorIncomeAmount, Is.True);
        }

        [Test]
        public void HasErrorIncomeAmount_PositiveAmount_IsFalse()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Amount = 500 };
            Assert.That(_vm.HasErrorIncomeAmount, Is.False);
        }

        [Test]
        public void ShowErrorIncomeDescription_FalseUntilValidationEnabled()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "" };
            _vm.ShowValidationErrors = false;
            Assert.That(_vm.ShowErrorIncomeDescription, Is.False);

            _vm.ShowValidationErrors = true;
            Assert.That(_vm.ShowErrorIncomeDescription, Is.True);
        }

        [Test]
        public void ShowErrorIncomeAmount_FalseUntilValidationEnabled()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Amount = 0 };
            _vm.ShowValidationErrors = false;
            Assert.That(_vm.ShowErrorIncomeAmount, Is.False);

            _vm.ShowValidationErrors = true;
            Assert.That(_vm.ShowErrorIncomeAmount, Is.True);
        }

        [Test]
        public void IsExpenseDataFormReadyToSubmit_ValidNameAndAmount_IsTrue()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "Mortgage", Amount = 2000 };
            Assert.That(_vm.IsExpenseDataFormReadyToSubmit, Is.True);
        }

        [Test]
        public void IsExpenseDataFormReadyToSubmit_MissingName_IsFalse()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "", Amount = 2000 };
            Assert.That(_vm.IsExpenseDataFormReadyToSubmit, Is.False);
        }

        [Test]
        public void IsExpenseDataFormReadyToSubmit_ZeroAmount_IsFalse()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "Mortgage", Amount = 0 };
            Assert.That(_vm.IsExpenseDataFormReadyToSubmit, Is.False);
        }

        // ── AddOrUpdateEntryFromView ───────────────────────────────────────────

        [Test]
        public void AddOrUpdateEntryFromView_NullRecords_ReturnsFalse()
        {
            var vm = new ExpenseEntryViewBaseModel();
            vm.TransactionRecords = null;
            vm.IncomeExpenseEntry = new IncomeExpense { Name = "Rent", Amount = 500, Frequency = TimeFrequencyEnum.Monthly };
            Assert.That(vm.AddOrUpdateEntryFromView(), Is.False);
        }

        [Test]
        public void AddOrUpdateEntryFromView_NewEntry_AddsToRecords()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "Rent", Amount = 1500, Frequency = TimeFrequencyEnum.Monthly };
            var result = _vm.AddOrUpdateEntryFromView();
            Assert.That(result, Is.True);
            Assert.That(_vm.TransactionRecords!.Exists("Rent"), Is.True);
        }

        [Test]
        public void AddOrUpdateEntryFromView_ResetsEntryAfterAdd()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "Rent", Amount = 1500, Frequency = TimeFrequencyEnum.Monthly };
            _vm.AddOrUpdateEntryFromView();
            Assert.That(_vm.IncomeExpenseEntry.Id, Is.EqualTo(Guid.Empty).Or.Null);
        }

        [Test]
        public void AddOrUpdateEntryFromView_HidesFormAfterAdd()
        {
            _vm.IsAddFormVisible = true;
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "Groceries", Amount = 400, Frequency = TimeFrequencyEnum.Weekly };
            _vm.AddOrUpdateEntryFromView();
            Assert.That(_vm.IsAddFormVisible, Is.False);
        }

        [Test]
        public void AddOrUpdateEntryFromView_UpdateExistingById_ReplaceEntry()
        {
            _vm.TransactionRecords!.Add("Rent", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            var entry = _vm.TransactionRecords.Get("Rent");
            Assert.That(entry, Is.Not.Null);

            _vm.IncomeExpenseEntry = new IncomeExpense { Id = entry!.Id, Name = "Rent", Amount = 1500, Frequency = TimeFrequencyEnum.Monthly };
            _vm.AddOrUpdateEntryFromView();

            var updated = _vm.TransactionRecords.Get("Rent");
            Assert.That(updated!.Amount, Is.EqualTo(1500));
        }

        [Test]
        public void AddOrUpdateEntryFromView_UpdateExistingByName_ReplaceEntry()
        {
            _vm.TransactionRecords!.Add("Utilities", 200, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);

            // new entry — no Id set, but name matches
            _vm.IncomeExpenseEntry = new IncomeExpense { Name = "Utilities", Amount = 300, Frequency = TimeFrequencyEnum.Monthly };
            _vm.AddOrUpdateEntryFromView();

            var updated = _vm.TransactionRecords.Get("Utilities");
            Assert.That(updated!.Amount, Is.EqualTo(300));
        }

        // ── FilteredTransactions ──────────────────────────────────────────────

        [Test]
        public void FilteredTransactions_NoSearch_ReturnsSortedAll()
        {
            _vm.TransactionRecords!.Add("Rent", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            _vm.TransactionRecords.Add("Groceries", 500, TimeFrequencyEnum.Weekly, isCheckForExistingRequired: true);
            _vm.SearchExpenseIncomeName = null;

            var results = _vm.FilteredTransactions;
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Name, Is.EqualTo("Groceries"));
        }

        [Test]
        public void FilteredTransactions_WithSearch_FiltersMatching()
        {
            _vm.TransactionRecords!.Add("Rent", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            _vm.TransactionRecords.Add("Groceries", 500, TimeFrequencyEnum.Weekly, isCheckForExistingRequired: true);
            _vm.SearchExpenseIncomeName = "rent";

            var results = _vm.FilteredTransactions;
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Name, Is.EqualTo("Rent"));
        }

        [Test]
        public void FilteredTransactions_SearchCaseInsensitive()
        {
            _vm.TransactionRecords!.Add("Mortgage", 2000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            _vm.SearchExpenseIncomeName = "MORT";
            Assert.That(_vm.FilteredTransactions.Count, Is.EqualTo(1));
        }

        [Test]
        public void FilteredTransactions_NullRecords_ReturnsEmpty()
        {
            var vm = new ExpenseEntryViewBaseModel();
            vm.TransactionRecords = null;
            Assert.That(vm.FilteredTransactions, Is.Empty);
        }

        // ── AutocompleteNameList ──────────────────────────────────────────────

        [Test]
        public void AutocompleteNameList_ReturnsAllNames()
        {
            _vm.TransactionRecords!.Add("Rent", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            _vm.TransactionRecords.Add("Groceries", 500, TimeFrequencyEnum.Weekly, isCheckForExistingRequired: true);

            var names = _vm.AutocompleteNameList;
            Assert.That(names, Contains.Item("Rent"));
            Assert.That(names, Contains.Item("Groceries"));
        }

        [Test]
        public void AutocompleteNameList_IsSorted()
        {
            _vm.TransactionRecords!.Add("Utilities", 200, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            _vm.TransactionRecords.Add("Rent", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);

            var names = _vm.AutocompleteNameList;
            Assert.That(names, Is.Ordered);
        }

        // ── ResetTransactionEntryData ─────────────────────────────────────────

        [Test]
        public void ResetTransactionEntryData_ClearsEntryAndHidesForm()
        {
            _vm.IncomeExpenseEntry = new IncomeExpense { Id = Guid.NewGuid(), Name = "Rent", Amount = 500 };
            _vm.ShowValidationErrors = true;
            _vm.IsAddFormVisible = true;

            _vm.ResetTransactionEntryData();

            Assert.That(_vm.IsEditMode, Is.False);
            Assert.That(_vm.ShowValidationErrors, Is.False);
            Assert.That(_vm.IsAddFormVisible, Is.False);
        }
    }
}
