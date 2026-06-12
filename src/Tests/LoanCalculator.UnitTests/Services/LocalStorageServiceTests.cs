using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Services
{
    /// <summary>
    /// Concrete subclass that avoids MAUI-only APIs (FileSystem, Launcher).
    /// </summary>
    internal class TestLocalStorage : LocalStorageService
    {
        public TestLocalStorage(string rootFolder) : base(rootFolder) { }
    }

    [TestFixture]
    public class LocalStorageServiceTests
    {
        private string _tempDir;
        private TestLocalStorage _storage;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "LoanCalcTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
            _storage = new TestLocalStorage(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── IsInitialized ─────────────────────────────────────────────────────

        [Test]
        public void IsInitialized_WithValidFolder_IsTrue()
        {
            Assert.That(_storage.IsInitialized, Is.True);
        }

        [Test]
        public void IsInitialized_EmptyFolder_IsFalse()
        {
            var s = new TestLocalStorage("");
            Assert.That(s.IsInitialized, Is.False);
        }

        // ── FilePathBasedOnType — all 6 known types ───────────────────────────

        [Test]
        public void FilePathBasedOnType_IncomeViewModel_ReturnsIncomeFile()
        {
            var path = _storage.FilePathBasedOnType<IncomeViewModel>();
            Assert.That(path, Does.EndWith("incomedata.json"));
        }

        [Test]
        public void FilePathBasedOnType_LoanViewModel_ReturnsHomeLoanFile()
        {
            var path = _storage.FilePathBasedOnType<LoanViewModel>();
            Assert.That(path, Does.EndWith("homeloandata.json"));
        }

        [Test]
        public void FilePathBasedOnType_ExpenseViewModel_ReturnsExpenseFile()
        {
            var path = _storage.FilePathBasedOnType<ExpenseViewModel>();
            Assert.That(path, Does.EndWith("expensedata.json"));
        }

        [Test]
        public void FilePathBasedOnType_SettingsViewModel_ReturnsSettingsFile()
        {
            var path = _storage.FilePathBasedOnType<SettingsViewModel>();
            Assert.That(path, Does.EndWith("settingsdata.json"));
        }

        [Test]
        public void FilePathBasedOnType_NameValueDataModel_ReturnsNameValueFile()
        {
            var path = _storage.FilePathBasedOnType<NameValueDataModel>();
            Assert.That(path, Does.EndWith("namevaluedata.json"));
        }

        [Test]
        public void FilePathBasedOnType_ThemeSelect_ReturnsThemeFile()
        {
            var path = _storage.FilePathBasedOnType<ThemeSelect>();
            Assert.That(path, Does.EndWith("themeselectdata.json"));
        }

        [Test]
        public void FilePathBasedOnType_Unknown_ReturnsDefaultFile()
        {
            var path = _storage.FilePathBasedOnType<object>();
            Assert.That(path, Does.EndWith("defaultdata.json"));
        }

        // ── File paths are under RootFolder ──────────────────────────────────

        [Test]
        public void HomeLoanDataFilePath_IsUnderRootFolder()
        {
            Assert.That(_storage.HomeLoanDataFilePath, Does.StartWith(_tempDir));
        }

        [Test]
        public void IncomeDataFilePath_IsUnderRootFolder()
        {
            Assert.That(_storage.IncomeDataFilePath, Does.StartWith(_tempDir));
        }

        // ── GetData / SaveData round-trips ────────────────────────────────────

        private record Loan(string Title, double Amount);

        [Test]
        public async Task SaveData_ThenGetData_ReturnsOriginal()
        {
            var loan = new Loan("My Loan", 500000.0);
            await _storage.SaveData(loan);
            var back = await _storage.GetData<Loan>();
            Assert.That(back, Is.Not.Null);
            Assert.That(back!.Title, Is.EqualTo("My Loan"));
            Assert.That(back.Amount, Is.EqualTo(500000.0).Within(0.01));
        }

        [Test]
        public async Task GetData_MissingFile_ReturnsDefault()
        {
            var result = await _storage.GetData<Loan>();
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ClearData_RemovesFile()
        {
            await _storage.SaveData(new Loan("Test", 100));
            await _storage.ClearData<Loan>();
            var result = await _storage.GetData<Loan>();
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ClearData_NonExistentFile_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(() => _storage.ClearData<Loan>());
        }
    }
}
