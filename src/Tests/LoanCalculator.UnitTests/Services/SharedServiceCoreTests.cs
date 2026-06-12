using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Services
{
    [TestFixture]
    public class SharedServiceCoreTests
    {
        // ── LoadSafe flag ──────────────────────────────────────────────────────

        [Test]
        public void LoadSafe_DefaultIsFalse()
        {
            SharedServiceCore.LoadSafeOff();
            Assert.That(SharedServiceCore.LoadSafe, Is.False);
        }

        [Test]
        public void LoadSafeOn_SetsTrue()
        {
            SharedServiceCore.LoadSafeOn();
            Assert.That(SharedServiceCore.LoadSafe, Is.True);
            SharedServiceCore.LoadSafeOff(); // restore
        }

        [Test]
        public void LoadSafeOff_SetsFalse()
        {
            SharedServiceCore.LoadSafeOn();
            SharedServiceCore.LoadSafeOff();
            Assert.That(SharedServiceCore.LoadSafe, Is.False);
        }

        // ── IsIncomeDirty ─────────────────────────────────────────────────────

        [TearDown]
        public void TearDown()
        {
            SharedServiceCore.ClearIncomeDirty();
            SharedServiceCore.ClearExpenseDirty();
            SharedServiceCore.ClearLoanDirty();
        }

        [Test]
        public void IsIncomeDirty_DefaultFalse()
        {
            SharedServiceCore.ClearIncomeDirty();
            Assert.That(SharedServiceCore.IsIncomeDirty, Is.False);
        }

        [Test]
        public void MarkIncomeDirty_SetsTrue()
        {
            SharedServiceCore.MarkIncomeDirty();
            Assert.That(SharedServiceCore.IsIncomeDirty, Is.True);
        }

        [Test]
        public void ClearIncomeDirty_SetsFalse()
        {
            SharedServiceCore.MarkIncomeDirty();
            SharedServiceCore.ClearIncomeDirty();
            Assert.That(SharedServiceCore.IsIncomeDirty, Is.False);
        }

        // ── IsExpenseDirty ────────────────────────────────────────────────────

        [Test]
        public void IsExpenseDirty_DefaultFalse()
        {
            SharedServiceCore.ClearExpenseDirty();
            Assert.That(SharedServiceCore.IsExpenseDirty, Is.False);
        }

        [Test]
        public void MarkExpenseDirty_SetsTrue()
        {
            SharedServiceCore.MarkExpenseDirty();
            Assert.That(SharedServiceCore.IsExpenseDirty, Is.True);
        }

        [Test]
        public void ClearExpenseDirty_SetsFalse()
        {
            SharedServiceCore.MarkExpenseDirty();
            SharedServiceCore.ClearExpenseDirty();
            Assert.That(SharedServiceCore.IsExpenseDirty, Is.False);
        }

        // ── IsLoanDirty ───────────────────────────────────────────────────────

        [Test]
        public void IsLoanDirty_DefaultFalse()
        {
            SharedServiceCore.ClearLoanDirty();
            Assert.That(SharedServiceCore.IsLoanDirty, Is.False);
        }

        [Test]
        public void MarkLoanDirty_SetsTrue()
        {
            SharedServiceCore.MarkLoanDirty();
            Assert.That(SharedServiceCore.IsLoanDirty, Is.True);
        }

        [Test]
        public void ClearLoanDirty_SetsFalse()
        {
            SharedServiceCore.MarkLoanDirty();
            SharedServiceCore.ClearLoanDirty();
            Assert.That(SharedServiceCore.IsLoanDirty, Is.False);
        }

        // ── Dirty flags are independent ───────────────────────────────────────

        [Test]
        public void DirtyFlags_AreIndependent()
        {
            SharedServiceCore.MarkIncomeDirty();
            Assert.That(SharedServiceCore.IsExpenseDirty, Is.False);
            Assert.That(SharedServiceCore.IsLoanDirty, Is.False);
        }

        // ── GetCurrencySymbol ─────────────────────────────────────────────────

        [Test]
        public void GetCurrencySymbol_NullCode_ReturnsDollar()
        {
            Assert.That(SharedServiceCore.GetCurrencySymbol(null), Is.EqualTo("$"));
        }

        [Test]
        public void GetCurrencySymbol_EmptyCode_ReturnsDollar()
        {
            Assert.That(SharedServiceCore.GetCurrencySymbol(""), Is.EqualTo("$"));
        }

        [Test]
        public void GetCurrencySymbol_WhitespaceCode_ReturnsDollar()
        {
            Assert.That(SharedServiceCore.GetCurrencySymbol("   "), Is.EqualTo("$"));
        }

        [Test]
        public void GetCurrencySymbol_UnknownCode_ReturnsDollar()
        {
            Assert.That(SharedServiceCore.GetCurrencySymbol("ZZZ"), Is.EqualTo("$"));
        }

        [Test]
        public void GetCurrencySymbol_AUD_ReturnsAustralianDollarSymbol()
        {
            var symbol = SharedServiceCore.GetCurrencySymbol("AUD");
            Assert.That(symbol, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void GetCurrencySymbol_USD_ReturnsSymbol()
        {
            var symbol = SharedServiceCore.GetCurrencySymbol("USD");
            Assert.That(symbol, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void GetCurrencySymbol_EUR_ReturnsSymbol()
        {
            var symbol = SharedServiceCore.GetCurrencySymbol("EUR");
            Assert.That(symbol, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void Currencies_NotNull()
        {
            Assert.That(SharedServiceCore.Currencies, Is.Not.Null);
        }

        [Test]
        public void Currencies_ContainsAUD()
        {
            var auds = SharedServiceCore.Currencies!.Where(c => c?.IsoCode == "AUD");
            Assert.That(auds, Is.Not.Empty);
        }
    }
}
