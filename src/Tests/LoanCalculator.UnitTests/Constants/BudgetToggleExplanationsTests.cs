using LoanCalculator.Core.Constants;

namespace LoanCalculator.UnitTests.Constants
{
    /// <summary>
    /// Guards the Budget switch copy. The four rows were hand-written markup and had already
    /// drifted — two of them describing the same deduction in different words, and one naming its
    /// implementation ("Income after new asset expense") rather than its effect.
    /// </summary>
    [TestFixture]
    public class BudgetToggleExplanationsTests
    {
        private static IEnumerable<BudgetToggle> AllToggles() => Enum.GetValues<BudgetToggle>();

        /// <summary>Driven off the enum, so a fifth switch fails here rather than rendering blank.</summary>
        [TestCaseSource(nameof(AllToggles))]
        public void EveryToggle_HasCompleteCopy(BudgetToggle toggle)
        {
            var e = BudgetToggleExplanations.For(toggle);

            Assert.Multiple(() =>
            {
                Assert.That(e.Title, Is.Not.Null.And.Not.Empty, $"{toggle}: Title");
                Assert.That(e.Subtitle, Is.Not.Null.And.Not.Empty, $"{toggle}: Subtitle");
                Assert.That(e.Detail, Is.Not.Null.And.Not.Empty, $"{toggle}: Detail");

                // A one-line detail is a subtitle, not an explanation worth a tap.
                Assert.That(e.Detail!.Length, Is.GreaterThan(80),
                    $"{toggle}: the ⓘ detail is too short to be worth revealing");
            });
        }

        [TestCaseSource(nameof(AllToggles))]
        public void EveryToggle_HasNoPlaceholderCopy(BudgetToggle toggle)
        {
            var e = BudgetToggleExplanations.For(toggle);
            var prose = $"{e.Title} {e.Subtitle} {e.Detail}";

            foreach (var placeholder in new[] { "TODO", "TBD", "Lorem", "FIXME" })
                Assert.That(prose, Does.Not.Contain(placeholder).IgnoreCase, $"{toggle}: {placeholder}");
        }

        /// <summary>
        /// The labels must describe the effect, not the internals. "Asset expense" was the old
        /// wording and meant nothing to a reader — the user could not tell what it was, what was
        /// new about it, or what it would be subtracted from.
        /// </summary>
        [TestCaseSource(nameof(AllToggles))]
        public void Titles_DoNotUseTheOldImplementationWording(BudgetToggle toggle)
        {
            var title = BudgetToggleExplanations.For(toggle).Title;

            Assert.That(title, Does.Not.Contain("new asset expense").IgnoreCase);
            Assert.That(title, Does.Not.Contain("in calculation").IgnoreCase);
        }

        /// <summary>
        /// The Expenses tab's switch changes the Net box, and the copy has to say so — it is the
        /// only feedback that the switch did anything, beyond the figure itself moving.
        /// </summary>
        /// <remarks>
        /// This replaces a test asserting the switch "does nothing on its own". That was true while
        /// it sat under a companion switch it depended on; that companion was removed, because the
        /// Net box already subtracts the expenses on this tab and so the companion could never
        /// change anything.
        /// </remarks>
        [Test]
        public void ExpenseAssetCostsToggle_SaysWhichFigureItChanges()
        {
            var e = BudgetToggleExplanations.For(BudgetToggle.ExpenseAfterLoanAndAssetCosts);

            Assert.Multiple(() =>
            {
                Assert.That(e.Subtitle, Does.Contain("Net").IgnoreCase,
                    "the sub-line should name the figure this switch moves");
                Assert.That(e.Detail, Does.Contain("Net / month").IgnoreCase,
                    "the detail must point at the box the switch changes");
                Assert.That(e.Detail, Does.Not.Contain("above is switched on"),
                    "the companion switch this used to depend on has been removed");
            });
        }

        /// <summary>
        /// The Income tab applies its pair independently — either can be used alone — which is the
        /// opposite of the Expense tab and so has to be said explicitly.
        /// </summary>
        [Test]
        public void IncomeAssetCostsToggle_SaysItWorksIndependently()
        {
            var e = BudgetToggleExplanations.For(BudgetToggle.IncomeAfterLoanAndAssetCosts);

            Assert.That(e.Detail, Does.Contain("on its own").IgnoreCase,
                "the Income tab applies its pair independently, so the copy must say this switch "
                + "can be used alone — the opposite of the Expense tab");
        }

        /// <summary>
        /// Each tab's two switches must be distinguishable from one another at a glance.
        /// </summary>
        [Test]
        public void TheTwoTogglesOnATab_HaveDifferentTitles()
        {
            Assert.Multiple(() =>
            {
                Assert.That(BudgetToggleExplanations.For(BudgetToggle.IncomeAfterExpenses).Title,
                    Is.Not.EqualTo(BudgetToggleExplanations.For(BudgetToggle.IncomeAfterLoanAndAssetCosts).Title));
                // The Expenses tab has a single switch now, so there is no pair to disambiguate
                // there — but its title must still differ from the Income tab's equivalent, which
                // changes a different figure.
                Assert.That(BudgetToggleExplanations.For(BudgetToggle.ExpenseAfterLoanAndAssetCosts).Title,
                    Is.Not.EqualTo(BudgetToggleExplanations.For(BudgetToggle.IncomeAfterExpenses).Title));
            });
        }

        /// <summary>
        /// The app calculates a loan against any asset, not only a house, so user-facing copy must
        /// not assume one. "the property you are planning to buy" was in the first draft of this
        /// table and narrowed the whole feature to mortgages.
        /// </summary>
        /// <remarks>
        /// Covers the scenario explanation cards too — the same copy-writing slip would read just
        /// as wrong there, and both tables are prose a reader sees.
        /// </remarks>
        [Test]
        public void UserFacingCopy_DoesNotAssumeTheLoanIsForAHouse()
        {
            var prose = new List<string>();

            foreach (var toggle in AllToggles())
            {
                var e = BudgetToggleExplanations.For(toggle);
                prose.Add($"{e.Title} {e.Subtitle} {e.Detail}");
            }

            foreach (var scenario in Enum.GetValues<LoanCalculator.Core.Models.Enums.WhatIfScenario>())
            {
                var e = ScenarioExplanations.For(scenario);
                prose.Add($"{e.Title} {e.Purpose} {e.WatchFor} {e.Example}");
                prose.AddRange(e.ReadingTheNumbers.Select(t => $"{t.Term} {t.Meaning}"));
            }

            foreach (var word in new[] { "property", "house", "home loan", "mortgage" })
            {
                var offenders = prose.Where(p => p.Contains(word, StringComparison.OrdinalIgnoreCase)).ToList();

                Assert.That(offenders, Is.Empty,
                    $"user-facing copy says \"{word}\", which assumes the loan is for a house. "
                    + "The app calculates against any asset — use \"asset\", or describe it as "
                    + "\"whatever you are borrowing for\".\n  "
                    + string.Join("\n  ", offenders.Select(o => o[..Math.Min(120, o.Length)])));
            }
        }

    }
}