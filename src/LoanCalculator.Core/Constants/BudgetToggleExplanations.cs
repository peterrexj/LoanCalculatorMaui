namespace LoanCalculator.Core.Constants
{
    /// <summary>The four "show the figure net of…" switches on the Budget tabs.</summary>
    public enum BudgetToggle
    {
        IncomeAfterExpenses,
        IncomeAfterLoanAndAssetCosts,

        // There is deliberately no ExpenseAfterExpenses. The Expenses tab's Net box already
        // subtracts the expenses listed on that tab, so a switch to "take off my everyday
        // spending" there could never change anything — and did not. It was removed rather
        // than wired.
        ExpenseAfterLoanAndAssetCosts,
    }

    /// <summary>Label, one-line summary and tap-to-read detail for a Budget switch.</summary>
    public sealed record BudgetToggleExplanation(string Title, string Subtitle, string Detail);

    /// <summary>
    /// What each Budget switch does, in plain language.
    /// </summary>
    /// <remarks>
    /// <para>The old labels described their implementation rather than their effect — "Income after
    /// new asset expense" with the sub-line "Include property loan costs in calculation" left the
    /// reader to guess what an asset expense was, which was new about it, and what it would be
    /// subtracted from. These name the action and the consequence instead.</para>
    ///
    /// <para><b>The dependency is now stated.</b> On the Expense tab the loan-and-asset switch only
    /// takes effect when the expenses switch is also on — <c>ExpenseViewModel</c>'s
    /// <c>TotalIncomeMonthlyValue</c> applies it inside that branch — so on its own it silently does
    /// nothing. That was invisible, and it is also why the two sit in this order on that tab. The
    /// Income tab applies its pair independently, and its copy says so.</para>
    ///
    /// <para>In Core so it can be unit-tested; the test project cannot reference the MAUI app.</para>
    /// </remarks>
    public static class BudgetToggleExplanations
    {
        public static BudgetToggleExplanation For(BudgetToggle toggle) => toggle switch
        {
            BudgetToggle.IncomeAfterExpenses => new(
                Title: "Take off my everyday spending",
                Subtitle: "See what is left once the bills are paid",
                Detail:
                    "The three figures at the top of this tab normally show all the money coming "
                    + "in, before any of it goes out again.\n\n"
                    + "Switch this on and they show what is left instead — what remains after the "
                    + "everyday spending you have listed on the Expenses tab, things like rent, "
                    + "food, bills and travel.\n\n"
                    + "Nothing you have entered is changed or deleted by this. It only changes "
                    + "which number is shown at the top, and you can switch it off again whenever "
                    + "you want."),

            BudgetToggle.IncomeAfterLoanAndAssetCosts => new(
                Title: "Take off the loan and its running costs",
                Subtitle: "See what is left once you are paying for it",
                Detail:
                    "This takes two more things off the figures at the top.\n\n"
                    + "The first is the repayment on the loan you are working out here. The second "
                    + "is the running costs you have listed on the Asset costs tab — the bills that "
                    + "come with whatever you are borrowing for, which might be insurance, "
                    + "maintenance, fees or anything else you have to keep paying.\n\n"
                    + "So it answers the question: once I am making this repayment and covering "
                    + "what comes with it, how much do I have left?\n\n"
                    + "You can use this switch on its own — you do not have to turn on the one "
                    + "above first. Turning both on is what gives you the fullest picture, with "
                    + "your everyday spending taken off as well."),

            BudgetToggle.ExpenseAfterLoanAndAssetCosts => new(
                Title: "Also take off the loan and its running costs",
                Subtitle: "Changes the Net figure in the box above",
                Detail:
                    "The \u201cNet / month\u201d box at the top of this tab normally shows your income "
                    + "with the spending listed on this tab taken off.\n\n"
                    + "Switch this on and it takes two more things off as well: the repayment on the "
                    + "loan you are working out, and the running costs you have listed on the Asset "
                    + "costs tab \u2014 the bills that come with whatever you are borrowing for, which "
                    + "might be insurance, maintenance, fees or anything else you have to keep "
                    + "paying.\n\n"
                    + "The box\u2019s label changes to say so, which is how you can tell at a glance "
                    + "which of the two figures you are looking at.\n\n"
                    + "So it answers: once I am making this repayment and covering what comes with "
                    + "it, how much is actually left over each month?"),

            _ => throw new ArgumentOutOfRangeException(nameof(toggle)),
        };
    }
}
