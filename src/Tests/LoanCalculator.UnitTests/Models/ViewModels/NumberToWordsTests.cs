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

        // ── Above a billion ──────────────────────────────────────────────────
        //
        // The tests above stopped at billions, which is exactly where the bug lived: the largest
        // scale was Billion and the method recursed into itself for n / 1_000_000_000, so any
        // quotient that was itself a billion or more appended a SECOND "Billion". A user typing
        // into the deposit field saw 173.7 trillion rendered as
        // "One Hundred, Seventy Three Thousand, Seven Hundred, One Billion".

        [Test]
        public void OneTrillion()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1_000_000_000_000), Is.EqualTo("One Trillion"));

        [Test]
        public void OneQuadrillion()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1_000_000_000_000_000), Is.EqualTo("One Quadrillion"));

        /// <summary>
        /// The regression guard: no scale name may appear twice, at any magnitude. Asserting the
        /// exact wording of a seventeen-word string would be unreadable and brittle; "Billion
        /// cannot appear twice" is the actual invariant that was broken.
        /// </summary>
        [TestCase(173_701_000_000_000L)]
        [TestCase(999_999_999_999_999L)]
        [TestCase(long.MaxValue)]
        public void NoScaleNameIsRepeated(long n)
        {
            var words = LoanViewModel.NumberToWordsPublic(n);

            foreach (var scale in new[] { "Quintillion", "Quadrillion", "Trillion", "Billion", "Million", "Thousand" })
            {
                var occurrences = words.Split(scale).Length - 1;
                Assert.That(occurrences, Is.LessThanOrEqualTo(1),
                    $"\"{scale}\" appears {occurrences} times for {n:N0}: {words}");
            }
        }

        // ── AmountToWords: the overflow-safe entry point ─────────────────────

        /// <summary>
        /// Casting an out-of-range double to long is unchecked and differs by CPU — ARM64 saturates
        /// to long.MaxValue, x64 gives long.MinValue. The call sites all did that cast themselves,
        /// so the same typed amount produced a long nonsense string on a phone and an empty one on
        /// a desktop. AmountToWords clamps first, so neither can happen.
        /// </summary>
        [TestCase(1e20)]
        [TestCase(1.1111111111111111e20)]
        [TestCase(double.MaxValue)]
        public void AmountToWords_AbsurdlyLargeValues_ClampRatherThanOverflow(double value)
            => Assert.That(LoanViewModel.AmountToWords(value),
                Is.EqualTo(LoanViewModel.NumberToWordsPublic((long)LoanViewModel.MaxEnterableAmount)));

        [TestCase(0d)]
        [TestCase(-1d)]
        public void AmountToWords_NonPositive_ReturnsEmpty(double value)
            => Assert.That(LoanViewModel.AmountToWords(value), Is.Empty);

        [Test]
        public void AmountToWords_WithinRange_MatchesTheLongOverload()
            => Assert.That(LoanViewModel.AmountToWords(650_000), Is.EqualTo("Six Hundred, Fifty Thousand"));

        /// <summary>
        /// The ceiling has to stay inside the range a double represents integers exactly (2^53),
        /// or the figure displayed stops being the figure typed regardless of the word form.
        /// </summary>
        [Test]
        public void MaxEnterableAmount_IsExactlyRepresentableAsADouble()
        {
            Assert.That(LoanViewModel.MaxEnterableAmount, Is.LessThan(9007199254740992d));
            Assert.That((double)(long)LoanViewModel.MaxEnterableAmount,
                Is.EqualTo(LoanViewModel.MaxEnterableAmount));
        }
    }
}
