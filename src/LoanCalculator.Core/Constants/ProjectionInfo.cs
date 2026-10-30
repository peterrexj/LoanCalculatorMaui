namespace LoanCalculator.Core.Constants
{
    /// <summary>
    /// Short, plain-English explanations for the What If ▸ Projection tab, one per thing on the
    /// screen.
    /// </summary>
    /// <remarks>
    /// <para><b>Why these are separate strings and not one card.</b> The Projection tab first got a
    /// single collapsed explanation card at the bottom, matching the nine scenarios. That was the
    /// wrong shape for this tab: the scenarios are one idea each, so one card explains the whole
    /// screen, whereas Projection is a stack of unrelated controls — and an explanation of all of
    /// them, sitting below all of them, is something most people will never scroll to. Each of
    /// these is shown inline, right beside the thing it describes, behind its own ⓘ.</para>
    ///
    /// <para><b>One per control, not one per section.</b> The first attempt at inline hints still
    /// grouped them: one hint for all of "INCOME PROJECTION" and one for all of "EXPENSE
    /// PROJECTION". That reintroduced the original problem on a smaller scale — expanding the
    /// income hint produced four paragraphs covering a stepper and two switches, so the reader had
    /// to find the paragraph matching the control they were looking at, and the explanation of the
    /// second switch sat a long way above the switch itself. Now the growth stepper and each
    /// switch own their own hint, placed directly beneath them. The rule to keep: <b>one hint
    /// explains exactly one control.</b> If a new string here needs the word "and" to list what it
    /// covers, it should be two strings.</para>
    ///
    /// <para><b>Written for someone who has never taken out a loan.</b> Short sentences, everyday
    /// words, no finance or computing vocabulary. "The rise builds on last year's rise" rather than
    /// "compounded"; "money left over" rather than "net surplus". Nothing here refers to the app
    /// itself, to tabs, or to other screens by name — a reader wants to know what the number in
    /// front of them means, not how the software is arranged.</para>
    ///
    /// <para><b>No currency symbols.</b> The symbol is user-selectable, so examples use bare
    /// figures. Live, correctly-symbolled numbers come from
    /// <c>BudgetViewModel.ProjectionPersonalLine</c>.</para>
    ///
    /// <para>Consts rather than a lookup so the markup can bind them with <c>x:Static</c>, with no
    /// view-model plumbing and no notifications to forget: the copy never changes at runtime.</para>
    /// </remarks>
    public static class ProjectionInfo
    {
        public const string Totals =
            "These two boxes add up everything over all the years shown — not one year.\n\n"
            + "The first is the money you expect to come in over that whole time. The second is "
            + "the money you expect to go out. Both start from the income and expenses you have "
            + "already entered, and both grow a little each year using the growth figures below.\n\n"
            + "Because they cover many years, they are big numbers. That is normal. The useful "
            + "thing is not either number on its own, but the difference between them — that is "
            + "what you would have left.";

        public const string IncomeGrowth =
            "How much more you expect to earn each year. Try the figure your pay normally goes up "
            + "by.\n\n"
            + "A small number here matters more than it looks. Next year's rise is worked out on "
            + "the already-higher amount, so each rise builds on the one before it. Over ten or "
            + "twenty years that adds up to a lot more than you would guess.\n\n"
            + "If you have no idea what to put, the rate prices are going up by in your country is "
            + "a sensible starting point. A rise that only matches that leaves you no better off "
            + "than you are now.";

        public const string IncomeTakeOffSpending =
            "Turn this on and we show what is left after your day-to-day costs, instead of "
            + "everything you earn.\n\n"
            + "Off shows your full earnings. On shows your spare money.\n\n"
            + "Spare money is usually the more useful one to look at, because it is what you could "
            + "actually put towards a loan.";

        public const string IncomeTakeOffLoan =
            "Turn this on and we also take away the loan repayment, plus the cost of looking after "
            + "the thing you are buying — upkeep, insurance, bills and so on.\n\n"
            + "This is the setting that answers the real question: once you are paying the loan "
            + "every month, how much would you genuinely have left?\n\n"
            + "If the answer still looks healthy years from now, the loan fits comfortably. If it "
            + "gets thin, that is worth knowing before you commit.";

        public const string ExpenseGrowth =
            "How much more you expect things to cost each year.\n\n"
            + "Prices almost always creep up, so leaving this at zero is unlikely to be realistic.\n\n"
            + "It is worth setting this at least as high as the figure you used for your income. If "
            + "your costs go up faster than your pay, the spare money you have today slowly "
            + "disappears — and this is where you would see that coming, years before it happens.";

        public const string ExpenseTakeOffLoan =
            "Turn this on to count the loan repayment and the upkeep as part of what you spend.\n\n"
            + "It is the same idea as the switch in the section above, just looked at from the "
            + "other side. There, the loan is taken off what you earn. Here, it is added to what "
            + "you spend.\n\n"
            + "Use whichever way of thinking suits you — the gap between the two totals comes out "
            + "the same.";

        public const string Years =
            "This is how far into the future to look.\n\n"
            + "A short setting is the one to trust more. Five years is mostly based on figures you "
            + "can actually guess at. Twenty-five years is mostly based on the growth percentages "
            + "you typed in, and nobody knows those that far ahead.\n\n"
            + "So use the long views to see which way things are heading, not to rely on the exact "
            + "amount at the end.";

        public const string Chart =
            "Each bar is one year. One colour is money coming in, the other is money going out.\n\n"
            + "What matters is the gap between them. If the gap gets wider as the years go on, you "
            + "are gradually better off. If it gets narrower, things are getting tighter. If the "
            + "going-out bars ever become the taller ones, that is the year the plan stops working "
            + "— and it is worth knowing about now rather than then.";

        public const string IncomeTable =
            "The same thing as the chart, written out as numbers, one row per year.\n\n"
            + "\"Period\" is which year the row is for. \"Start\" is the amount at the beginning of "
            + "that year. \"Growth\" is how much the growth percentage added. \"Adjusted\" is what "
            + "is left for that year once the switches above have taken off your everyday spending "
            + "and the loan. \"Sum\" is the running total up to and including that year.\n\n"
            + "Worth a look when you want to know which particular year something changes, rather "
            + "than just that it does.";

        public const string ExpenseTable =
            "The money going out, written out year by year, with the same columns as the table "
            + "above.\n\n"
            + "\"Period\" is the year. \"Start\" is what it cost at the beginning of that year. "
            + "\"Growth\" is how much the growth percentage added. \"Adjusted\" is that year's "
            + "spending once the loan repayment and upkeep have been added in, which happens only "
            + "when the switch above is on. \"Sum\" is the running total so far.\n\n"
            + "Reading down the \"Growth\" column is the quickest way to see how much of your "
            + "future spending is simply prices going up.";

        /// <summary>Everything above, for the completeness test to walk.</summary>
        public static IReadOnlyList<(string Name, string Text)> All() =>
        [
            (nameof(Totals), Totals),
            (nameof(IncomeGrowth), IncomeGrowth),
            (nameof(IncomeTakeOffSpending), IncomeTakeOffSpending),
            (nameof(IncomeTakeOffLoan), IncomeTakeOffLoan),
            (nameof(ExpenseGrowth), ExpenseGrowth),
            (nameof(ExpenseTakeOffLoan), ExpenseTakeOffLoan),
            (nameof(Years), Years),
            (nameof(Chart), Chart),
            (nameof(IncomeTable), IncomeTable),
            (nameof(ExpenseTable), ExpenseTable),
        ];
    }
}
