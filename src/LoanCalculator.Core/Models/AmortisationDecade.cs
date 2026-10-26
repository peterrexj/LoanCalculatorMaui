using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Models
{
    /// <summary>
    /// One decade of the repayment schedule: a collapsed summary row plus the year rows inside it.
    /// </summary>
    /// <remarks>
    /// Replaces the five-rows-at-a-time pager on the schedule page. A 30-year loan was six pages of
    /// five rows with no way to see the shape of the thing, and no way to compare the start of the
    /// loan against the end without paging between them. Three decades, each one row until opened,
    /// fits the whole loan on one screen.
    ///
    /// The totals are sums over the decade's own years, and <see cref="EndBalance"/> is the balance
    /// after its last year — not an average or a midpoint, so an opened decade's final row and its
    /// collapsed header agree.
    /// </remarks>
    public class AmortisationDecade
    {
        public AmortisationDecade(
            string periodLabel,
            string repaymentTotal,
            string interestTotal,
            string principalTotal,
            string endBalance,
            List<PaymentAmortisationOutput> years,
            bool isExpanded)
        {
            PeriodLabel = periodLabel;
            RepaymentTotal = repaymentTotal;
            InterestTotal = interestTotal;
            PrincipalTotal = principalTotal;
            EndBalance = endBalance;
            Years = years;
            IsExpanded = isExpanded;
        }

        /// <summary>e.g. "2026–2035", or just "2026" for a decade holding a single year.</summary>
        [JsonIgnore] public string PeriodLabel { get; }

        [JsonIgnore] public string RepaymentTotal { get; }
        [JsonIgnore] public string InterestTotal { get; }
        [JsonIgnore] public string PrincipalTotal { get; }
        [JsonIgnore] public string EndBalance { get; }

        [JsonIgnore] public List<PaymentAmortisationOutput> Years { get; }

        /// <summary>
        /// True for the first decade only. The prototype opens it to show what a decade contains —
        /// a page of identical collapsed rows does not communicate that they open at all.
        /// </summary>
        [JsonIgnore] public bool IsExpanded { get; }

        /// <summary>Automation id for the decade's expander header, derived from its label.</summary>
        [JsonIgnore] public string AutomationId => $"LoanScheduleDecade_{PeriodLabel.Replace("–", "_")}";
    }
}
