namespace LoanCalculator.Core.Constants
{
    /// <summary>The three figures under the verdict on the What If "Answer" tab.</summary>
    public enum AnswerPanel
    {
        Repayment,
        NetIncome,
        ShareOfIncome,
    }

    /// <summary>A figure's caption and what it actually means.</summary>
    public sealed record AnswerPanelExplanation(string Caption, string Meaning);

    /// <summary>
    /// Plain-language explanations for the three figures beside the affordability verdict.
    /// </summary>
    /// <remarks>
    /// <para>The captions alone — "repayment", "net income", "of income" — do not say what the
    /// figures are net of, which period they cover, or what they are a share OF. "of income" was
    /// the worst: a bare percentage with no stated denominator, which is unreadable without being
    /// told it is the monthly repayment measured against gross monthly income.</para>
    ///
    /// <para>In Core so the copy is unit-tested, and so these read as one set rather than three
    /// labels written in three places.</para>
    /// </remarks>
    public static class AnswerPanelExplanations
    {
        public static AnswerPanelExplanation For(AnswerPanel panel) => panel switch
        {
            AnswerPanel.Repayment => new(
                Caption: "repayment",
                Meaning:
                    "What you would hand over to the lender each time, for the loan exactly as you "
                    + "have it set up now. If you have chosen to pay fortnightly or weekly, this is "
                    + "that amount rather than a monthly one."),

            AnswerPanel.NetIncome => new(
                Caption: "net income",
                Meaning:
                    "Your income with your everyday spending taken off, but before the loan "
                    + "repayment. It is what you have to put towards the loan — so if this is "
                    + "smaller than the repayment beside it, the loan does not fit."),

            AnswerPanel.ShareOfIncome => new(
                Caption: "of income",
                Meaning:
                    "How much of your income the repayment would use up, before anything else is "
                    + "taken off. Lenders look at this figure closely: under about a third is "
                    + "usually considered comfortable, and the higher it climbs the less room you "
                    + "have for anything going wrong. Above 999% it simply shows >999%, because "
                    + "at that point the repayment dwarfs the income and the exact number tells "
                    + "you nothing more."),

            _ => throw new ArgumentOutOfRangeException(nameof(panel)),
        };

        /// <summary>All three, in the order they appear on screen.</summary>
        public static IReadOnlyList<AnswerPanelExplanation> All() =>
            [For(AnswerPanel.Repayment), For(AnswerPanel.NetIncome), For(AnswerPanel.ShareOfIncome)];
    }
}
