using LoanCalculator.Core.Models.Enums;
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

        // ── Text size (Stage 3 of the font-size feature) ─────────────────────────
        //
        // A true get-after-set round-trip is NOT asserted here, and cannot be: Preferences.Set
        // throws NotImplementedInReferenceAssemblyException without a MAUI host, so both
        // GetAppTextSizeAsync and SetAppTextSizeAsync are wrapped in a try/catch that swallows it
        // — the same documented gap LoanTermBoundsTests records for the other Preferences-backed
        // settings. What IS covered is that the gap degrades gracefully: a getter with nothing
        // stored returns the default rather than throwing, and a setter never throws outward
        // regardless of whether the underlying Preferences call succeeded.

        [Test]
        public void GetAppTextSizeAsync_WithNoMauiHost_ReturnsDefaultRatherThanThrowing()
        {
            Assert.That(SharedServiceCore.GetAppTextSizeAsync().Result, Is.EqualTo(AppTextSize.Default));
        }

        [Test]
        public void SetAppTextSizeAsync_WithNoMauiHost_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(async () => await SharedServiceCore.SetAppTextSizeAsync(AppTextSize.Large));
        }
    }
}
