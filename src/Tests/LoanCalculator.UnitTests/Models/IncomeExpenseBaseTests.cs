using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;

namespace LoanCalculator.UnitTests.Models
{
    [TestFixture]
    public class IncomeExpenseBaseTests
    {
        private IncomeExpenseBase _base;

        [SetUp]
        public void Setup()
        {
            _base = new IncomeExpenseBase();
        }

        // ── Add ──────────────────────────────────────────────────────────────

        [Test]
        public void Add_NewEntry_AppearsInCollection()
        {
            _base.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(1));
            Assert.That(_base.IncomeExpenseEntries[0].Name, Is.EqualTo("Salary"));
        }

        [Test]
        public void Add_WithoutCheckForExisting_AllowsDuplicateNames()
        {
            _base.Add("Rent", 2000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("Rent", 3000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(2));
        }

        [Test]
        public void Add_WithCheckForExisting_UpdatesInsteadOfDuplicate()
        {
            _base.Add("Rent", 2000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            _base.Add("Rent", 3000, TimeFrequencyEnum.Fortnightly, isCheckForExistingRequired: true);

            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(1));
            Assert.That(_base.IncomeExpenseEntries[0].Amount, Is.EqualTo(3000));
            Assert.That(_base.IncomeExpenseEntries[0].Frequency, Is.EqualTo(TimeFrequencyEnum.Fortnightly));
        }

        [Test]
        public void Add_WithCheckForExisting_NewNameAddsEntry()
        {
            _base.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            _base.Add("Bonus", 1000, TimeFrequencyEnum.Yearly, isCheckForExistingRequired: true);
            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(2));
        }

        [Test]
        public void Add_AssignsUniqueGuids()
        {
            _base.Add("A", 100, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("B", 200, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            var ids = _base.IncomeExpenseEntries.Select(e => e.Id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(2));
        }

        // ── Update ───────────────────────────────────────────────────────────

        [Test]
        public void Update_ExistingId_UpdatesFields()
        {
            _base.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            var id = _base.IncomeExpenseEntries[0].Id;

            _base.Update(id, "New Salary", 6000, TimeFrequencyEnum.Weekly);

            Assert.That(_base.IncomeExpenseEntries[0].Name, Is.EqualTo("New Salary"));
            Assert.That(_base.IncomeExpenseEntries[0].Amount, Is.EqualTo(6000));
            Assert.That(_base.IncomeExpenseEntries[0].Frequency, Is.EqualTo(TimeFrequencyEnum.Weekly));
        }

        [Test]
        public void Update_NonExistentId_AddsNewEntry()
        {
            _base.Update(Guid.NewGuid(), "Ghost", 999, TimeFrequencyEnum.Yearly);
            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(1));
            Assert.That(_base.IncomeExpenseEntries[0].Name, Is.EqualTo("Ghost"));
        }

        // ── Delete ───────────────────────────────────────────────────────────

        [Test]
        public void Delete_ExistingId_RemovesEntry()
        {
            _base.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            var id = _base.IncomeExpenseEntries[0].Id;

            _base.Delete(id);

            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(0));
        }

        [Test]
        public void Delete_NonExistentId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _base.Delete(Guid.NewGuid()));
        }

        [Test]
        public void DeleteAll_RemovesAllEntries()
        {
            _base.Add("A", 100, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("B", 200, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("C", 300, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            _base.DeleteAll();

            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(0));
        }

        [Test]
        public void DeleteAll_EmptyCollection_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _base.DeleteAll());
        }

        // ── Exists ───────────────────────────────────────────────────────────

        [Test]
        public void Exists_ById_TrueWhenPresent()
        {
            _base.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            var id = _base.IncomeExpenseEntries[0].Id;
            Assert.That(_base.Exists(id), Is.True);
        }

        [Test]
        public void Exists_ById_FalseWhenAbsent()
        {
            Assert.That(_base.Exists(Guid.NewGuid()), Is.False);
        }

        [Test]
        public void Exists_ByName_TrueWhenPresent()
        {
            _base.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            Assert.That(_base.Exists("Salary"), Is.True);
        }

        [Test]
        public void Exists_ByName_CaseInsensitive()
        {
            _base.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            Assert.That(_base.Exists("salary"), Is.True);
            Assert.That(_base.Exists("SALARY"), Is.True);
        }

        [Test]
        public void Exists_ByName_FalseWhenAbsent()
        {
            Assert.That(_base.Exists("Ghost"), Is.False);
        }

        // ── Get ──────────────────────────────────────────────────────────────

        [Test]
        public void Get_ById_ReturnsCorrectEntry()
        {
            _base.Add("Salary", 5000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            var id = _base.IncomeExpenseEntries[0].Id;

            var entry = _base.Get(id);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Name, Is.EqualTo("Salary"));
        }

        [Test]
        public void Get_ById_ReturnsNullWhenAbsent()
        {
            Assert.That(_base.Get(Guid.NewGuid()), Is.Null);
        }

        [Test]
        public void Get_ByName_ReturnsCorrectEntry()
        {
            _base.Add("Bonus", 1000, TimeFrequencyEnum.Yearly, isCheckForExistingRequired: false);
            var entry = _base.Get("Bonus");
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Amount, Is.EqualTo(1000));
        }

        [Test]
        public void GetIndex_ReturnsCorrectIndex()
        {
            _base.Add("First", 100, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("Second", 200, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);

            var id = _base.IncomeExpenseEntries[1].Id;
            Assert.That(_base.GetIndex(id), Is.EqualTo(1));
        }

        // ── GetEntry ─────────────────────────────────────────────────────────

        [Test]
        public void GetEntry_NonExistentName_CreatesAndReturns()
        {
            var entry = _base.GetEntry("NewItem");
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Name, Is.EqualTo("NewItem"));
            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetEntry_ExistingName_ReturnsExisting()
        {
            _base.Add("ExistingItem", 500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            var entry = _base.GetEntry("ExistingItem");
            Assert.That(entry, Is.Not.Null);
            Assert.That(_base.IncomeExpenseEntries.Count, Is.EqualTo(1));
        }

        // ── SumUpData ────────────────────────────────────────────────────────

        [Test]
        public void SumUpData_EmptyCollection_BothTotalsAreZero()
        {
            _base.SumUpData();
            Assert.That(_base.IncomeExpenseSummary.TotalMonthly, Is.EqualTo(0));
            Assert.That(_base.IncomeExpenseSummary.TotalYearly, Is.EqualTo(0));
        }

        [Test]
        public void SumUpData_MultipleMonthlyEntries_SumsTotals()
        {
            _base.Add("A", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("B", 2000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.SumUpData();

            Assert.That(_base.IncomeExpenseSummary.TotalMonthly, Is.EqualTo(3000));
            Assert.That(_base.IncomeExpenseSummary.TotalYearly, Is.EqualTo(36000));
        }

        [Test]
        public void SumUpData_MixedFrequencies_ConvertsCorrectly()
        {
            _base.Add("Monthly", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("Yearly", 12000, TimeFrequencyEnum.Yearly, isCheckForExistingRequired: false);
            _base.SumUpData();

            // Monthly 1000 → yearly 12000; Yearly 12000 → yearly 12000
            Assert.That(_base.IncomeExpenseSummary.TotalYearly, Is.EqualTo(24000));
        }

        [Test]
        public void SumUpData_WithDeductions_SubtractsFromTotals()
        {
            _base.Add("Salary", 3000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.SumUpData(monthlyValue: 500, yearlyValue: 6000);

            Assert.That(_base.IncomeExpenseSummary.TotalMonthly, Is.EqualTo(2500));
            Assert.That(_base.IncomeExpenseSummary.TotalYearly, Is.EqualTo(30000)); // 36000 - 6000
        }

        [Test]
        public void SumUpData_CalledTwice_DoesNotDoubleCount()
        {
            _base.Add("Salary", 2000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.SumUpData();
            _base.SumUpData();

            Assert.That(_base.IncomeExpenseSummary.TotalMonthly, Is.EqualTo(2000));
        }

        // ── CalculatePercentages ─────────────────────────────────────────────

        [Test]
        public void CalculatePercentages_EmptyCollection_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _base.CalculatePercentages());
        }

        [Test]
        public void CalculatePercentages_TwoEqualEntries_EachIs50Percent()
        {
            _base.Add("A", 500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("B", 500, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.CalculatePercentages();

            Assert.That(_base.IncomeExpenseEntries[0].Percentage, Is.EqualTo(50));
            Assert.That(_base.IncomeExpenseEntries[1].Percentage, Is.EqualTo(50));
        }

        [Test]
        public void CalculatePercentages_UnequalEntries_SumsTo100()
        {
            _base.Add("A", 300, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.Add("B", 700, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.CalculatePercentages();

            double total = _base.IncomeExpenseEntries.Sum(e => e.Percentage);
            Assert.That(total, Is.EqualTo(100).Within(0.001));
        }

        [Test]
        public void CalculatePercentages_SingleEntry_Is100Percent()
        {
            _base.Add("Only", 1000, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.CalculatePercentages();

            Assert.That(_base.IncomeExpenseEntries[0].Percentage, Is.EqualTo(100));
        }

        [Test]
        public void CalculatePercentages_ZeroAmounts_PercentagesAreZero()
        {
            _base.Add("Zero", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            _base.CalculatePercentages();

            Assert.That(_base.IncomeExpenseEntries[0].Percentage, Is.EqualTo(0));
        }
    }
}
