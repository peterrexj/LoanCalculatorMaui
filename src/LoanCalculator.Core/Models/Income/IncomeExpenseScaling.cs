using LoanCalculator.Core.Exts;
using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.Core.Models.Income
{
    public static class IncomeExpenseScaling
    {
        /// <summary>
        /// Returns a <b>copy</b> of <paramref name="source"/> whose entries are re-apportioned
        /// across <paramref name="netYearly"/> in proportion to their share of the original, with
        /// the copy's summary carrying the supplied net totals. The source is not modified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the "income after expenses" breakdown the PDF draws. It exists as a pure
        /// function because the previous shape could not be tested: the generator read
        /// <c>PdfDataInsightsModel.IncomeModel.TransactionRecordsWithExpense</c>, a property whose
        /// getter called <c>SumUpData(deduction)</c> on the <b>live, shared</b> records and
        /// returned them — so rendering a PDF mutated view-model state, and the clone was taken
        /// only after that mutation.
        /// </para>
        /// <para>
        /// Setting the summary totals explicitly is load-bearing, not tidiness.
        /// <c>DrawTransactionRecordsTable</c> builds its "Total" row from the <b>summary</b>, not
        /// by adding up the rows, so a copy with re-scaled rows and un-scaled totals prints a
        /// table whose rows are net and whose total is gross. <c>TotalWeekly</c> and
        /// <c>TotalFortnightly</c> derive from <c>TotalYearly</c> and follow from this too.
        /// </para>
        /// <para>
        /// The totals are assigned rather than recomputed with <c>SumUpData</c> on purpose:
        /// <c>SumUpData</c> re-sums the rounded per-entry amounts, which yields
        /// <c>netYearly / 12</c> for the monthly figure instead of the caller's
        /// <paramref name="netMonthly"/> and drifts the printed numbers.
        /// </para>
        /// </remarks>
        public static IncomeExpenseBase? ScaleToNet(
            this IncomeExpenseBase? source, double netMonthly, double netYearly)
        {
            var copy = source.DeepCloneObject();
            if (copy?.IncomeExpenseEntries == null) return copy;

            // Percentages come from the ORIGINAL amounts — each entry's share of the gross.
            copy.CalculatePercentages();

            foreach (var entry in copy.IncomeExpenseEntries)
            {
                entry.Amount = (entry.Percentage / 100) * netYearly;
                entry.Frequency = TimeFrequencyEnum.Yearly;
            }

            // Re-derive percentages from the re-scaled amounts, so a negative net produces the
            // same shares the table's conditional shading keys off.
            copy.CalculatePercentages();

            copy.IncomeExpenseSummary.TotalMonthly = netMonthly;
            copy.IncomeExpenseSummary.TotalYearly = netYearly;

            return copy;
        }
    }
}
