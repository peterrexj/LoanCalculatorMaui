using LoanCalculator.Core.Models;
using LoanCalculator.Core.Exts;

namespace LoanCalculator.UnitTests.Models
{
    [TestFixture]
    public class HelperAndExtensionsTests
    {
        // ── Helper.WithComma ──────────────────────────────────────────────────

        [Test]
        public void WithComma_LargeNumber_IncludesComma()
        {
            Assert.That(1234567.0.WithComma(), Does.Contain(","));
        }

        [Test]
        public void WithComma_SmallNumber_NoDecimal()
        {
            Assert.That(999.0.WithComma(), Is.EqualTo("999"));
        }

        [Test]
        public void WithComma_Zero_IsZeroString()
        {
            Assert.That(0.0.WithComma(), Is.EqualTo("0"));
        }

        [Test]
        public void WithComma_StripsDecimalPart()
        {
            // N0 format truncates decimal
            Assert.That(1234.56.WithComma(), Is.EqualTo("1,235"));
        }

        // ── Helper.Round2 ─────────────────────────────────────────────────────

        [Test]
        public void Round2_RoundsToTwoDecimalPlaces()
        {
            Assert.That(1.23456.Round2(), Is.EqualTo(1.23).Within(0.0001));
        }

        [Test]
        public void Round2_MidpointRoundsUp()
        {
            Assert.That(1.235.Round2(), Is.EqualTo(1.24).Within(0.0001));
        }

        [Test]
        public void Round2_Zero_StaysZero()
        {
            Assert.That(0.0.Round2(), Is.EqualTo(0.0));
        }

        // ── Helper.Round0 ─────────────────────────────────────────────────────

        [Test]
        public void Round0_RoundsToWholeNumber()
        {
            Assert.That(1234.567.Round0(), Is.EqualTo(1235).Within(0.0001));
        }

        [Test]
        public void Round0_LessThanHalf_RoundsDown()
        {
            Assert.That(1234.4.Round0(), Is.EqualTo(1234).Within(0.0001));
        }

        // ── Helper.CurrencySymbol event ───────────────────────────────────────

        [Test]
        public void CurrencySymbol_Set_FiresChangedEvent()
        {
            var original = Helper.CurrencySymbol;
            bool fired = false;
            Helper.CurrencySymbolChanged += (s, e) => fired = true;

            Helper.CurrencySymbol = "€";
            Assert.That(fired, Is.True);

            // restore
            Helper.CurrencySymbol = original;
        }

        [Test]
        public void CurrencySymbol_SetSameValue_DoesNotFireEvent()
        {
            Helper.CurrencySymbol = "$";
            bool fired = false;
            Helper.CurrencySymbolChanged += (s, e) => fired = true;

            Helper.CurrencySymbol = "$";
            Assert.That(fired, Is.False);
        }

        // ── UsefulExtensions.ToCurrency ───────────────────────────────────────

        [Test]
        public void ToCurrency_IncludesCurrencySymbol()
        {
            Helper.CurrencySymbol = "$";
            var result = 5000.0.ToCurrency();
            Assert.That(result, Does.StartWith("$"));
        }

        [Test]
        public void ToCurrency_IncludesFormattedNumber()
        {
            Helper.CurrencySymbol = "$";
            var result = 1234.56.ToCurrency();
            Assert.That(result, Does.Contain("1,234"));
        }

        // ── UsefulExtensions.ToCustomCurrencyRounded ──────────────────────────

        [Test]
        public void ToCustomCurrencyRounded_IncludesCurrencySymbol()
        {
            Helper.CurrencySymbol = "$";
            var result = 9876.54.ToCustomCurrencyRounded();
            Assert.That(result, Does.StartWith("$"));
        }

        [Test]
        public void ToCustomCurrencyRounded_StripsFractional()
        {
            Helper.CurrencySymbol = "$";
            var result = 1234.99.ToCustomCurrencyRounded();
            Assert.That(result, Does.Contain("1,235"));
            Assert.That(result, Does.Not.Contain("."));
        }

        [Test]
        public void ToCustomCurrencyRounded_Zero_ReturnsSymbolWithZero()
        {
            Helper.CurrencySymbol = "$";
            var result = 0.0.ToCustomCurrencyRounded();
            Assert.That(result, Is.EqualTo("$0"));
        }

        // ── JsonExts.DeepCloneObject ──────────────────────────────────────────

        private record SimpleRecord(string Name, double Amount);

        [Test]
        public void DeepCloneObject_ReturnsEqual()
        {
            var obj = new SimpleRecord("Test", 12345.67);
            var clone = obj.DeepCloneObject();
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone!.Name, Is.EqualTo("Test"));
            Assert.That(clone.Amount, Is.EqualTo(12345.67).Within(0.001));
        }

        [Test]
        public void DeepCloneObject_IsNotSameReference()
        {
            var list = new List<int> { 1, 2, 3 };
            var clone = list.DeepCloneObject();
            Assert.That(clone, Is.Not.SameAs(list));
        }

        [Test]
        public void DeepCloneObject_MutatingCloneDoesNotAffectOriginal()
        {
            var original = new List<string> { "a", "b" };
            var clone = original.DeepCloneObject()!;
            clone.Add("c");
            Assert.That(original.Count, Is.EqualTo(2));
        }
    }
}
