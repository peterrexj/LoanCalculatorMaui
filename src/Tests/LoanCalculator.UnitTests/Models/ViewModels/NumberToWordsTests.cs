using LoanCalculator.Core.Models.ViewModels.PrimaryModels;

namespace LoanCalculator.UnitTests.Models.ViewModels
{
    [TestFixture]
    public class NumberToWordsTests
    {
        // ── Edge cases ───────────────────────────────────────────────────────

        [Test]
        public void Zero_ReturnsEmpty()
            => Assert.That(LoanViewModel.NumberToWordsPublic(0), Is.EqualTo(string.Empty));

        [Test]
        public void Negative_ReturnsEmpty()
            => Assert.That(LoanViewModel.NumberToWordsPublic(-1), Is.EqualTo(string.Empty));

        // ── Ones ─────────────────────────────────────────────────────────────

        [TestCase(1, "One")]
        [TestCase(5, "Five")]
        [TestCase(9, "Nine")]
        [TestCase(19, "Nineteen")]
        public void SingleDigitAndTeens(long n, string expected)
            => Assert.That(LoanViewModel.NumberToWordsPublic(n), Is.EqualTo(expected));

        // ── Tens ─────────────────────────────────────────────────────────────

        [TestCase(20, "Twenty")]
        [TestCase(30, "Thirty")]
        [TestCase(99, "Ninety Nine")]
        [TestCase(21, "Twenty One")]
        [TestCase(55, "Fifty Five")]
        public void TensAndCombinations(long n, string expected)
            => Assert.That(LoanViewModel.NumberToWordsPublic(n), Is.EqualTo(expected));

        // ── Hundreds ─────────────────────────────────────────────────────────

        [Test]
        public void OneHundred()
            => Assert.That(LoanViewModel.NumberToWordsPublic(100), Is.EqualTo("One Hundred"));

        [Test]
        public void OneHundredAndOne()
            => Assert.That(LoanViewModel.NumberToWordsPublic(101), Is.EqualTo("One Hundred, One"));

        [Test]
        public void NineHundredNinetyNine()
            => Assert.That(LoanViewModel.NumberToWordsPublic(999), Is.EqualTo("Nine Hundred, Ninety Nine"));

        // ── Thousands ────────────────────────────────────────────────────────

        [Test]
        public void OneThousand()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1000), Is.EqualTo("One Thousand"));

        [Test]
        public void FiveHundredThousand()
            => Assert.That(LoanViewModel.NumberToWordsPublic(500000), Is.EqualTo("Five Hundred Thousand"));

        [Test]
        public void SevenHundredFiftyThousand()
            => Assert.That(LoanViewModel.NumberToWordsPublic(750000), Is.EqualTo("Seven Hundred, Fifty Thousand"));

        [Test]
        public void ThousandWithRemainder()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1500), Is.EqualTo("One Thousand, Five Hundred"));

        // ── Millions ─────────────────────────────────────────────────────────

        [Test]
        public void OneMillion()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1_000_000), Is.EqualTo("One Million"));

        [Test]
        public void TwoAndHalfMillion()
            => Assert.That(LoanViewModel.NumberToWordsPublic(2_500_000), Is.EqualTo("Two Million, Five Hundred Thousand"));

        [Test]
        public void OneMillionFiveHundredThousandFiveHundred()
        {
            var result = LoanViewModel.NumberToWordsPublic(1_500_500);
            Assert.That(result, Does.Contain("Million"));
            Assert.That(result, Does.Contain("Thousand"));
            Assert.That(result, Does.Contain("Five Hundred"));
        }

        // ── Billions ─────────────────────────────────────────────────────────

        [Test]
        public void OneBillion()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1_000_000_000), Is.EqualTo("One Billion"));

        [Test]
        public void TwoBillionFiveHundredMillion()
            => Assert.That(LoanViewModel.NumberToWordsPublic(2_500_000_000), Is.EqualTo("Two Billion, Five Hundred Million"));

        // ── Common mortgage amounts ───────────────────────────────────────────

        [TestCase(650000, "Six Hundred, Fifty Thousand")]
        [TestCase(1200000, "One Million, Two Hundred Thousand")]
        public void CommonMortgageAmounts(long n, string expected)
            => Assert.That(LoanViewModel.NumberToWordsPublic(n), Is.EqualTo(expected));
    }
}
