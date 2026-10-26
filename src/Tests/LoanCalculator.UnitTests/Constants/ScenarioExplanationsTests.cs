using System.Text.RegularExpressions;
using System.Xml.Linq;
using LoanCalculator.Core.Constants;
using LoanCalculator.Core.Models.Enums;
using Pj.Library;

namespace LoanCalculator.UnitTests.Constants
{
    /// <summary>
    /// Guards the nine scenario explanation cards against the way this feature can fail silently:
    /// someone adds a tenth scenario, or renames one, and a card renders blank or a scenario row
    /// stops opening. Nothing in the app would complain in either case.
    /// </summary>
    [TestFixture]
    public class ScenarioExplanationsTests
    {
        private static IEnumerable<WhatIfScenario> AllScenarios() => Enum.GetValues<WhatIfScenario>();

        /// <summary>
        /// Total by construction: driven off the enum rather than a hardcoded list of nine, so a
        /// new scenario fails here instead of shipping an empty card.
        /// </summary>
        [TestCaseSource(nameof(AllScenarios))]
        public void EveryScenario_HasCompleteExplanationContent(WhatIfScenario scenario)
        {
            var e = ScenarioExplanations.For(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(e.Title, Is.Not.Null.And.Not.Empty, $"{scenario}: Title");
                Assert.That(e.Purpose, Is.Not.Null.And.Not.Empty, $"{scenario}: Purpose");
                Assert.That(e.WatchFor, Is.Not.Null.And.Not.Empty, $"{scenario}: WatchFor");

                // Two is the floor for a "breakdown": one entry is a caption, not a breakdown.
                Assert.That(e.ReadingTheNumbers, Has.Count.GreaterThanOrEqualTo(2),
                    $"{scenario}: needs at least two figures explained");

                foreach (var term in e.ReadingTheNumbers)
                {
                    Assert.That(term.Term, Is.Not.Null.And.Not.Empty, $"{scenario}: a Term is empty");
                    Assert.That(term.Meaning, Is.Not.Null.And.Not.Empty,
                        $"{scenario}: '{term.Term}' has no Meaning");
                }
            });
        }

        /// <summary>
        /// Duplicate terms mean two figures were explained under one name, which reads as a
        /// copy-paste slip rather than a breakdown.
        /// </summary>
        [TestCaseSource(nameof(AllScenarios))]
        public void EveryScenario_ExplainsEachFigureOnlyOnce(WhatIfScenario scenario)
        {
            var terms = ScenarioExplanations.For(scenario).ReadingTheNumbers
                .Select(t => t.Term)
                .ToList();

            Assert.That(terms, Is.Unique, $"{scenario}: a figure is explained twice");
        }

        [TestCaseSource(nameof(AllScenarios))]
        public void EveryScenario_HasNoPlaceholderCopy(WhatIfScenario scenario)
        {
            var e = ScenarioExplanations.For(scenario);
            var allProse = string.Join(" ",
                new[] { e.Title, e.Purpose, e.WatchFor, e.Example ?? string.Empty }
                    .Concat(e.ReadingTheNumbers.SelectMany(t => new[] { t.Term, t.Meaning })));

            foreach (var placeholder in new[] { "TODO", "TBD", "Lorem", "FIXME", "XXX" })
            {
                Assert.That(allProse, Does.Not.Contain(placeholder).IgnoreCase,
                    $"{scenario}: placeholder copy '{placeholder}' left in");
            }
        }

        /// <summary>
        /// This app shows estimates, not financial advice, and the explanation cards are by far its
        /// most prose-heavy surface — the one place where an explanation could start sounding like a
        /// recommendation. Every card has to say what it is.
        /// </summary>
        [TestCaseSource(nameof(AllScenarios))]
        public void EveryScenario_DisclaimsThatItIsNotAdvice(WhatIfScenario scenario)
        {
            var watchFor = ScenarioExplanations.For(scenario).WatchFor;

            Assert.That(watchFor, Does.Contain("not financial advice").IgnoreCase,
                $"{scenario}: WatchFor must state that these are estimates, not advice");
        }

        /// <summary>
        /// Pins the one coupling that moving <see cref="WhatIfScenario"/> into Core exposed: the
        /// nine scenario rows in WhatIfView.xaml carry enum member names as <c>CommandParameter</c>
        /// string literals, which <c>OnScenarioTapped</c> resolves with <c>Enum.TryParse</c>.
        /// Renaming a member compiles perfectly and silently stops that row opening.
        /// </summary>
        /// <remarks>
        /// Reads the app project's XAML from a Core-only test project the same way
        /// <c>ThemesTests</c> and <c>FontFilesTests</c> already do.
        /// </remarks>
        [Test]
        public void EveryScenarioRowInTheIndex_NamesARealScenario()
        {
            var xamlPath = Path.Combine(
                PjUtility.Runtime.ExecutingRepositoryRootFolder,
                "src", "LoanCalculator", "View", "WhatIfView.xaml");

            Assume.That(File.Exists(xamlPath), $"WhatIfView.xaml not found at {xamlPath}");

            var doc = XDocument.Load(xamlPath);
            var ns = doc.Root!.GetDefaultNamespace();

            var parameters = doc.Descendants(ns + "TapGestureRecognizer")
                .Where(t => (string?)t.Attribute("Tapped") == "OnScenarioTapped")
                .Select(t => (string?)t.Attribute("CommandParameter"))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!)
                .ToList();

            Assert.That(parameters, Has.Count.EqualTo(Enum.GetValues<WhatIfScenario>().Length),
                "the index should carry exactly one row per scenario");

            foreach (var parameter in parameters)
            {
                Assert.That(Enum.TryParse<WhatIfScenario>(parameter, out _), Is.True,
                    $"CommandParameter \"{parameter}\" in WhatIfView.xaml does not match any "
                    + "WhatIfScenario member, so tapping that row does nothing");
            }

            Assert.That(parameters, Is.Unique, "two index rows open the same scenario");
        }

        /// <summary>
        /// The page header comes from the explanation record now that ScenarioPage's own TitleOf
        /// switch is gone, so a missing Title would leave the header blank.
        /// </summary>
        [Test]
        public void ScenarioTitles_AreAllDistinct()
        {
            var titles = AllScenarios().Select(s => ScenarioExplanations.For(s).Title).ToList();

            Assert.That(titles, Is.Unique);
        }

        /// <summary>
        /// The nine titles are also the scenario page headers, taken over from the switch that
        /// used to live in <c>ScenarioPage</c>. Pinned so a reword here is a deliberate act rather
        /// than a side effect of editing the explanation copy around it.
        /// </summary>
        [TestCase(WhatIfScenario.RateChange, "Rate change")]
        [TestCase(WhatIfScenario.ExtraRepayment, "Extra repayment")]
        [TestCase(WhatIfScenario.TermComparison, "Loan term")]
        [TestCase(WhatIfScenario.DepositScenarios, "Deposit")]
        [TestCase(WhatIfScenario.LumpSum, "Lump sum")]
        [TestCase(WhatIfScenario.RepaymentFrequency, "Repayment frequency")]
        [TestCase(WhatIfScenario.Offset, "Offset account")]
        [TestCase(WhatIfScenario.StressTest, "Stress test")]
        [TestCase(WhatIfScenario.Combined, "Combined strategy")]
        public void ScenarioTitle_IsTheHeaderThePageUsedBefore(WhatIfScenario scenario, string expected)
            => Assert.That(ScenarioExplanations.For(scenario).Title, Is.EqualTo(expected));

        /// <summary>
        /// Three explanations were migrated out of <c>IAlertService</c> popups the Offset,
        /// Repayment frequency and Stress test views used to show. The wording was already good;
        /// only the container was wrong. These assert the substance survived the move, since the
        /// popups and their handlers are now deleted and nothing else would notice if it had not.
        /// </summary>
        [TestCase(WhatIfScenario.Offset, "linked to your loan")]
        [TestCase(WhatIfScenario.Offset, "580,000")]
        [TestCase(WhatIfScenario.RepaymentFrequency, "thirteen monthly-equivalent")]
        [TestCase(WhatIfScenario.StressTest, "break-even")]
        [TestCase(WhatIfScenario.StressTest, "breathing room")]
        public void MigratedPopupCopy_SurvivedTheMoveIntoTheCard(WhatIfScenario scenario, string fragment)
        {
            var e = ScenarioExplanations.For(scenario);
            var allProse = string.Join(" ",
                new[] { e.Purpose, e.WatchFor, e.Example ?? string.Empty }
                    .Concat(e.ReadingTheNumbers.Select(t => t.Meaning)));

            Assert.That(allProse, Does.Contain(fragment).IgnoreCase,
                $"{scenario}: the migrated explanation no longer says \"{fragment}\"");
        }
    }
}
