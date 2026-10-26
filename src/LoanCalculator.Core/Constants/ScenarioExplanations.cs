using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.Core.Constants
{
    /// <summary>One figure shown by a scenario, and what it means in plain language.</summary>
    /// <remarks>
    /// A record, not a <c>(string Term, string Meaning)</c> tuple. <c>ValueTuple</c> exposes
    /// <i>fields</i> named <c>Item1</c>/<c>Item2</c> — the friendly names exist only in compiler
    /// metadata — so <c>{Binding Term}</c> inside a <c>DataTemplate</c> would resolve to nothing,
    /// MAUI would log no warning, and the Label would render empty at zero height. That is the
    /// unresolved-binding trap CLAUDE.md describes, where an invisible Label reads as a missing
    /// <c>AutomationId</c>.
    /// </remarks>
    public sealed record ExplanationTerm(string Term, string Meaning);

    /// <summary>The plain-language explanation card for one What If scenario.</summary>
    public sealed record ScenarioExplanation(
        string Title,
        string Purpose,
        IReadOnlyList<ExplanationTerm> ReadingTheNumbers,
        string WatchFor,
        string? Example = null);

    /// <summary>
    /// Static copy explaining each of the nine What If scenarios: what it is for, what every
    /// figure on it means, and what to be careful of.
    /// </summary>
    /// <remarks>
    /// <para>In Core rather than in the views so that it can be unit-tested — the test project
    /// references Core only — and so the nine read as one set of copy rather than nine
    /// independently-drifting blocks of markup.</para>
    ///
    /// <para>Three of these (Offset, RepaymentFrequency, StressTest) are migrated from the
    /// <c>IAlertService</c> popups those views used to show behind a muted ⓘ button. The wording
    /// was already good; what was wrong was the container.</para>
    ///
    /// <para><b>No currency symbols in the copy.</b> The symbol is user-selectable, so examples are
    /// written as bare figures ("a 600,000 loan") rather than inventing a currency the reader does
    /// not use. The personalised line on the card carries live, correctly-symbolled figures.</para>
    ///
    /// <para><b>Every entry hedges.</b> The app presents estimates, not financial advice, and this
    /// is the most prose-heavy surface in it — so each <c>WatchFor</c> closes by saying so.</para>
    /// </remarks>
    public static class ScenarioExplanations
    {
        public static ScenarioExplanation For(WhatIfScenario scenario) => scenario switch
        {
            WhatIfScenario.RateChange => new(
                Title: "Rate change",
                Purpose:
                    "Your interest rate is the one number on a mortgage you do not control. This "
                    + "shows what a rise or a fall does to your repayment and to the total interest "
                    + "over the life of the loan, so a rate move becomes something you have already "
                    + "seen rather than something that surprises you.",
                ReadingTheNumbers:
                [
                    new("New rate",
                        "Your current rate plus the change you dialled in. The − and + buttons move "
                        + "it by the step size set in Settings."),
                    new("Monthly change",
                        "The difference between what you pay now and what you would pay at the new "
                        + "rate. This is the figure that lands on your budget each month."),
                    new("New monthly",
                        "What the repayment itself becomes. Worth comparing against your income "
                        + "rather than against the old repayment."),
                    new("Total interest",
                        "Interest paid across the whole term at the new rate. A small rate move "
                        + "looks trivial month to month and substantial over 25 to 30 years — this "
                        + "is the figure where that difference shows up."),
                    new("The chart",
                        "Each bar is one year, split into the part that pays down what you owe and "
                        + "the part that is interest. The line is the balance still outstanding. "
                        + "Early years are mostly interest; where the split crosses over is where "
                        + "the loan starts moving."),
                    new("The banner",
                        "Compares the new repayment against the income and expenses you entered, "
                        + "and says how much room is left over."),
                ],
                WatchFor:
                    "Unless you have fixed your rate, a lender can move it at any time — and a fixed "
                    + "period ends. Trying a rise of two or three percent is closer to what has "
                    + "happened historically than half a percent. These are estimates from the "
                    + "figures you entered, not financial advice."),

            WhatIfScenario.ExtraRepayment => new(
                Title: "Extra repayment",
                Purpose:
                    "Paying a little more than you have to each month is the simplest way to shorten "
                    + "a loan, because the extra is not spread across interest — all of it comes "
                    + "straight off what you owe. This shows what a given monthly extra is worth in "
                    + "time and in money.",
                ReadingTheNumbers:
                [
                    new("Extra per month",
                        "The amount you add on top of your required repayment. Adjust it with the − "
                        + "and + buttons."),
                    new("Time saved",
                        "How much sooner the loan would be paid off."),
                    new("Interest saved",
                        "Total interest you would never be charged, across the rest of the loan."),
                    new("The chart",
                        "Two lines: what you owe on your current repayment, and the same loan with "
                        + "the extra added. The widening gap between them is the extra payment "
                        + "compounding."),
                ],
                WatchFor:
                    "Check your lender accepts extra repayments without a fee — some fixed-rate loans "
                    + "cap or charge for them. Money paid onto a loan is usually much harder to get "
                    + "back than money left in savings, so having funds set aside for emergencies "
                    + "normally comes first. Estimates only, not financial advice."),

            WhatIfScenario.TermComparison => new(
                Title: "Loan term",
                Purpose:
                    "The term you choose sets a trade-off: a longer loan makes every month easier "
                    + "and the loan as a whole more expensive. This puts three terms beside each "
                    + "other so the trade is explicit rather than implied.",
                ReadingTheNumbers:
                [
                    new("The three columns",
                        "The same loan repaid over a different number of years. The column marked "
                        + "YOUR TERM is the term you have set now."),
                    new("Monthly",
                        "What you would hand over each month on that term."),
                    new("Interest",
                        "Total interest across the whole of that term — the real price of the extra "
                        + "years."),
                ],
                WatchFor:
                    "The cheapest total is not automatically the right answer: a repayment you cannot "
                    + "sustain is a worse outcome than a longer loan. Many lenders let you overpay a "
                    + "long loan, which can give you a short loan's cost with a long loan's safety "
                    + "net — see the extra repayment scenario. Estimates only, not financial advice."),

            WhatIfScenario.DepositScenarios => new(
                Title: "Deposit",
                Purpose:
                    "Your deposit decides how much you actually borrow, and therefore both the "
                    + "repayment and the total interest. This shows what saving a little more — or "
                    + "putting down a little less — would change.",
                ReadingTheNumbers:
                [
                    new("The three columns",
                        "Different deposits, as a share of the asset price. YOUR DEPOSIT marks where "
                        + "you are now. MAX DEPOSIT appears on a column that would need more than "
                        + "the asset is worth."),
                    new("Loan",
                        "What you would borrow with that deposit."),
                    new("Monthly",
                        "The repayment that loan implies."),
                ],
                WatchFor:
                    "Many lenders price by loan-to-value band, so crossing a threshold — often eighty "
                    + "or ninety percent — can cut your interest rate as well as your balance. This "
                    + "screen cannot model that, because the bands and the discount depend on your "
                    + "lender. Estimates only, not financial advice."),

            WhatIfScenario.LumpSum => new(
                Title: "Lump sum",
                Purpose:
                    "A single one-off payment onto the loan — a bonus, an inheritance, the proceeds "
                    + "of a sale. Because it reduces the balance that interest is charged on for "
                    + "every remaining year, the same amount paid early is worth far more than paid "
                    + "late.",
                ReadingTheNumbers:
                [
                    new("Amount",
                        "The one-off payment you are modelling. Adjust it with the − and + buttons."),
                    new("Time saved",
                        "How much sooner the loan would finish."),
                    new("Interest saved",
                        "Interest you would never be charged as a result."),
                    new("New loan balance",
                        "What you would owe immediately after making the payment."),
                    new("The chart",
                        "What you owe with and without the lump sum. The two lines never meet again, "
                        + "which is precisely why the timing of a lump sum matters so much."),
                ],
                WatchFor:
                    "Check for early-repayment charges, which are common on fixed-rate deals and can "
                    + "undo part of the saving. It is also worth comparing against what the money "
                    + "would earn elsewhere, and against clearing any higher-interest debt first. "
                    + "Estimates only, not financial advice."),

            WhatIfScenario.RepaymentFrequency => new(
                Title: "Repayment frequency",
                Purpose:
                    "Paying fortnightly or weekly instead of monthly shortens the loan without "
                    + "feeling like paying more. A year holds twenty-six fortnights but only twelve "
                    + "months, so handing over half your monthly amount every fortnight quietly "
                    + "makes thirteen monthly-equivalent payments a year instead of twelve — one "
                    + "extra payment, which goes entirely onto what you owe.",
                Example:
                    "Weekly works the same way: fifty-two payments of a quarter of the monthly "
                    + "amount is also thirteen monthly equivalents a year, so the saving is almost "
                    + "identical to fortnightly.",
                ReadingTheNumbers:
                [
                    new("The three columns",
                        "Monthly, fortnightly and weekly. YOUR FREQUENCY marks how you pay now."),
                    new("Payment",
                        "What you hand over each time at that frequency — not per month."),
                    new("Time saved and interest saved",
                        "Both measured against paying monthly. The saving comes from that one extra "
                        + "annual payment reducing the balance sooner."),
                    new("The chart",
                        "One line per frequency, showing what you owe over time."),
                ],
                WatchFor:
                    "The saving comes from the extra payment, not from the frequency itself. A lender "
                    + "that simply divides your monthly payment into fortnightly halves taken "
                    + "twenty-four times a year gives you no benefit at all — confirm yours takes "
                    + "half the monthly amount twenty-six times. Results vary with your rate, "
                    + "balance and repayments; these are estimates, not financial advice."),

            WhatIfScenario.Offset => new(
                Title: "Offset account",
                Purpose:
                    "An offset account is a savings or transaction account linked to your loan. The "
                    + "lender charges interest only on the difference between your loan balance and "
                    + "your offset balance, so savings you were keeping anyway reduce your interest "
                    + "without being locked away where you cannot reach them.",
                Example:
                    "A 600,000 loan with 20,000 sitting in the offset account is charged interest on "
                    + "580,000.",
                ReadingTheNumbers:
                [
                    new("Balance",
                        "How much you keep in the offset account. Adjust it with the − and + "
                        + "buttons."),
                    new("Rate",
                        "Models a lender applying the offset at a different effective rate from your "
                        + "loan rate. Leave it matching your loan rate for a true, full offset."),
                    new("Time saved",
                        "How much sooner the loan ends. Your repayment does not change, so more of "
                        + "each one reaches the balance instead of the interest — and that is what "
                        + "shortens the term. The time saving is real, not just an interest "
                        + "reduction."),
                    new("Interest saved",
                        "Interest never charged, across the life of the loan."),
                    new("Monthly interest saving",
                        "What the offset is worth this month at the balance shown."),
                    new("The chart",
                        "What you owe over time, with and without the offset."),
                ],
                WatchFor:
                    "An offset only helps while the money stays in the account — spending it removes "
                    + "the benefit for that period. Weigh any account or package fee against the "
                    + "saving shown, and check whether the same money would earn more in a "
                    + "higher-interest savings account after tax. Estimates only, not financial "
                    + "advice."),

            WhatIfScenario.StressTest => new(
                Title: "Stress test",
                Purpose:
                    "Instead of asking what a particular rate rise would cost, this asks the question "
                    + "in reverse: how far could the rate rise before this loan stops working on the "
                    + "income and expenses you entered? If you are still deciding how much to "
                    + "borrow, it is the most useful number on this page.",
                ReadingTheNumbers:
                [
                    new("Breaks even at",
                        "The estimated rate at which your repayment would equal your income minus "
                        + "your expenses, leaving nothing over. Above this rate, the repayment would "
                        + "exceed the income you entered."),
                    new("Your buffer",
                        "The gap between that break-even rate and your current rate. A larger buffer "
                        + "means you could absorb more rate rises before it starts to hurt."),
                    new("Current monthly surplus",
                        "Your income, minus your expenses, minus the repayment you have now. This is "
                        + "your breathing room as things stand today."),
                    new("The chart",
                        "Your current rate set beside the break-even rate, so the size of the gap is "
                        + "visible rather than arithmetic."),
                    new("The banner",
                        "Grades that buffer as comfortable, moderate, or at risk."),
                ],
                WatchFor:
                    "This is only as good as the income and expenses you entered — understate your "
                    + "spending and the buffer will look larger than it really is. It also assumes "
                    + "your income holds steady, which is often not what happens at the same time as "
                    + "rates rise. Estimates only, not financial advice."),

            WhatIfScenario.Combined => new(
                Title: "Combined strategy",
                Purpose:
                    "Every other scenario changes one thing at a time. Real repayment plans usually "
                    + "stack several — paying fortnightly, adding a little extra, keeping savings in "
                    + "offset. This lets you switch them on together, which matters because their "
                    + "effects compound rather than simply adding up.",
                ReadingTheNumbers:
                [
                    new("The switches",
                        "Turn each strategy on or off. Everything below updates for whatever "
                        + "combination you have selected."),
                    new("Time saved",
                        "How much sooner the loan finishes with that combination."),
                    new("Interest saved",
                        "Total interest avoided."),
                    new("Paid off in",
                        "The term the combination produces outright, rather than the saving measured "
                        + "against your current one."),
                    new("The chart",
                        "Your loan as it stands, against the combined strategy."),
                ],
                WatchFor:
                    "Check that each piece is something you could keep up for years rather than for "
                    + "this month — an ambitious plan abandoned after six months saves less than a "
                    + "modest one you maintain. Each lever also carries its own caveats, which the "
                    + "individual scenarios set out. Estimates only, not financial advice."),

            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
    }
}
