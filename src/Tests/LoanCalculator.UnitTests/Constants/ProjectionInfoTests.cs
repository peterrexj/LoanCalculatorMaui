using LoanCalculator.Core.Constants;

namespace LoanCalculator.UnitTests.Constants
{
    /// <summary>
    /// Guards the inline explanations on the What If ▸ Projection tab.
    /// </summary>
    /// <remarks>
    /// These are seven separate strings rather than one card, so there is no single place a
    /// reviewer would notice one of them going blank, losing its disclaimer, or drifting back into
    /// jargon. The tests walk <see cref="ProjectionInfo.All"/>, so a new hint is covered the moment
    /// it is added to that list — and a hint added without being listed there fails
    /// <see cref="EveryPublicConstant_IsCoveredByAll"/>.
    /// </remarks>
    [TestFixture]
    public class ProjectionInfoTests
    {
        private static IEnumerable<string> Names() => ProjectionInfo.All().Select(h => h.Name);

        private static string TextFor(string name) =>
            ProjectionInfo.All().Single(h => h.Name == name).Text;

        [TestCaseSource(nameof(Names))]
        public void EveryHint_HasRealContent(string name)
        {
            var text = TextFor(name);

            Assert.Multiple(() =>
            {
                Assert.That(text, Is.Not.Null.And.Not.Empty);
                Assert.That(text.Trim(), Has.Length.GreaterThan(80),
                    $"{name}: too short to explain anything");
                foreach (var placeholder in new[] { "TODO", "TBD", "Lorem", "XXX" })
                    Assert.That(text, Does.Not.Contain(placeholder).IgnoreCase);
            });
        }

        /// <summary>
        /// The app is global and the loan is not assumed to be for a house — the same rule the
        /// scenario copy is held to.
        /// </summary>
        [TestCaseSource(nameof(Names))]
        public void EveryHint_DoesNotAssumeTheLoanIsForAProperty(string name)
        {
            var text = TextFor(name);

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Not.Contain("property").IgnoreCase);
                Assert.That(text, Does.Not.Contain("mortgage").IgnoreCase);
                Assert.That(text, Does.Not.Contain("house").IgnoreCase);
            });
        }

        /// <summary>
        /// Written for someone with no finance or computing background. This is a blunt instrument
        /// — a word list — but it is the only automatic check on the one quality that matters most
        /// here, and it is what stops the copy sliding back towards the register it was rewritten
        /// out of.
        /// </summary>
        [TestCaseSource(nameof(Names))]
        public void EveryHint_AvoidsJargon(string name)
        {
            var text = TextFor(name);

            // Whole words. Substring matching is not good enough here and quietly produces a false
            // failure: "tab" is inside "table", which these hints legitimately say a lot.
            string[] jargonWords =
            [
                "compounded", "compounding", "principal", "aggregate", "cumulative",
                "nominal", "tab", "parameter", "parameters",
            ];

            // Phrases, matched as written.
            string[] jargonPhrases =
            [
                "net surplus", "in absolute terms", "real terms", "view model",
            ];

            Assert.Multiple(() =>
            {
                foreach (var word in jargonWords)
                    Assert.That(text, Does.Not.Match($@"(?i)\b{word}\b"),
                        $"{name}: \"{word}\" is not plain English");

                foreach (var phrase in jargonPhrases)
                    Assert.That(text, Does.Not.Contain(phrase).IgnoreCase,
                        $"{name}: \"{phrase}\" is not plain English");

                foreach (var stem in new[] { "amortis", "amortiz" })
                    Assert.That(text, Does.Not.Contain(stem).IgnoreCase,
                        $"{name}: \"{stem}...\" is not plain English");
            });
        }

        /// <summary>
        /// No currency symbols in the static copy: the symbol is user-selectable, so a hard-coded
        /// one would show the wrong currency to most readers. Live figures come from the view
        /// model's own formatted properties instead.
        /// </summary>
        [TestCaseSource(nameof(Names))]
        public void EveryHint_UsesNoCurrencySymbol(string name)
        {
            Assert.That(TextFor(name), Does.Not.Match(@"[$£€¥₹]"));
        }

        [Test]
        public void HintsAreAllDistinct()
        {
            Assert.That(ProjectionInfo.All().Select(h => h.Text), Is.Unique);
        }

        /// <summary>
        /// Catches a hint added as a constant but forgotten in <see cref="ProjectionInfo.All"/>,
        /// which would silently escape every test above.
        /// </summary>
        [Test]
        public void EveryPublicConstant_IsCoveredByAll()
        {
            var declared = typeof(ProjectionInfo)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => f.Name)
                .OrderBy(n => n);

            Assert.That(ProjectionInfo.All().Select(h => h.Name).OrderBy(n => n),
                Is.EqualTo(declared),
                "a hint constant is missing from All(), so nothing tests it");
        }

        /// <summary>
        /// The table hints name the grid's actual column headers, so a renamed column has to be
        /// mirrored here.
        /// </summary>
        [TestCase("IncomeTable")]
        [TestCase("ExpenseTable")]
        public void TableHints_ExplainEveryColumn(string name)
        {
            var text = TextFor(name);

            Assert.Multiple(() =>
            {
                foreach (var column in new[] { "Period", "Start", "Growth", "Adjusted", "Sum" })
                    Assert.That(text, Does.Contain(column),
                        $"{name}: the \"{column}\" column is not explained");
            });
        }
    }
}
