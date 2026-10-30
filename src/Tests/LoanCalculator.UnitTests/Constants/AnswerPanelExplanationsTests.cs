using LoanCalculator.Core.Constants;

namespace LoanCalculator.UnitTests.Constants
{
    /// <summary>
    /// Guards the explanations for the three figures beside the affordability verdict.
    /// </summary>
    /// <remarks>
    /// "of income" shipped as a bare percentage with no stated denominator, which is unreadable
    /// without being told it is the monthly repayment measured against gross monthly income —
    /// and it is the figure a reader is most likely to misinterpret, because a number over 100%
    /// looks like a defect rather than a measurement.
    /// </remarks>
    [TestFixture]
    public class AnswerPanelExplanationsTests
    {
        private static IEnumerable<AnswerPanel> AllPanels() => Enum.GetValues<AnswerPanel>();

        [TestCaseSource(nameof(AllPanels))]
        public void EveryPanel_HasACaptionAndAMeaning(AnswerPanel panel)
        {
            var e = AnswerPanelExplanations.For(panel);

            Assert.Multiple(() =>
            {
                Assert.That(e.Caption, Is.Not.Null.And.Not.Empty, $"{panel}: Caption");
                Assert.That(e.Meaning, Is.Not.Null.And.Not.Empty, $"{panel}: Meaning");
                Assert.That(e.Meaning!.Length, Is.GreaterThan(60),
                    $"{panel}: the meaning is too short to explain anything");
            });
        }

        /// <summary>The captions must be the ones actually printed under the figures.</summary>
        [TestCase(AnswerPanel.Repayment, "repayment")]
        [TestCase(AnswerPanel.NetIncome, "net income")]
        [TestCase(AnswerPanel.ShareOfIncome, "of income")]
        public void Caption_MatchesTheLabelOnScreen(AnswerPanel panel, string expected)
            => Assert.That(AnswerPanelExplanations.For(panel).Caption, Is.EqualTo(expected));

        /// <summary>All three are revealed together, in the order the boxes appear.</summary>
        [Test]
        public void All_ReturnsTheThreeInScreenOrder()
        {
            var captions = AnswerPanelExplanations.All().Select(p => p.Caption).ToList();

            Assert.That(captions, Is.EqualTo(new[] { "repayment", "net income", "of income" }));
        }

        /// <summary>
        /// The repayment box shows the payment at the user's chosen frequency, not a monthly one.
        /// A reader comparing it with "net income" — which IS monthly — would otherwise draw the
        /// wrong conclusion, so the explanation has to say which period it covers.
        /// </summary>
        [Test]
        public void RepaymentMeaning_SaysItFollowsTheChosenFrequency()
            => Assert.That(AnswerPanelExplanations.For(AnswerPanel.Repayment).Meaning,
                Does.Contain("fortnightly").IgnoreCase.Or.Contain("frequency").IgnoreCase);

        /// <summary>Net income is before the loan — the whole point of comparing it with one.</summary>
        [Test]
        public void NetIncomeMeaning_SaysItIsBeforeTheLoan()
            => Assert.That(AnswerPanelExplanations.For(AnswerPanel.NetIncome).Meaning,
                Does.Contain("before the loan").IgnoreCase);

        /// <summary>
        /// The percentage needs its denominator stated, and the >999% cap explained — otherwise a
        /// capped value reads as a bug.
        /// </summary>
        [Test]
        public void ShareOfIncomeMeaning_StatesWhatItIsAShareOfAndExplainsTheCap()
        {
            var meaning = AnswerPanelExplanations.For(AnswerPanel.ShareOfIncome).Meaning;

            Assert.Multiple(() =>
            {
                Assert.That(meaning, Does.Contain("income").IgnoreCase, "the denominator");
                Assert.That(meaning, Does.Contain("999"), "the cap, or a capped figure reads as a defect");
            });
        }
    }
}
