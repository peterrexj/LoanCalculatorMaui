using System.Text.Json.Serialization;
using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.Core.Models.Income.Summary
{
    public class IncomeExpenseSummary
    {
        /// <summary>
        /// Plain total of the entries. Written only by <see cref="IncomeExpenseBase.SumUpData"/>.
        /// </summary>
        /// <remarks>
        /// This used to mean net-or-gross depending on which tab refreshed last, because
        /// <c>SumUpData</c> took an "after expenses" deduction and left it here — so the Budget
        /// Expenses tab showed a negative "Monthly Income" and a negative yearly total made the
        /// Loan page claim no income was recorded. A parallel <c>TotalMonthly</c>/
        /// <c>TotalYearly</c> pair existed to work around that and is gone: these fields are
        /// now unambiguous, so there is nothing for a second pair to mean. An "after expenses"
        /// figure is computed where it is displayed. See TECH-DEBT D1.
        /// </remarks>
        public double TotalMonthly { get; set; }
        /// <inheritdoc cref="TotalMonthly"/>
        public double TotalYearly { get; set; }
        public double TotalWeekly => ModelHelper.ConvertAmountToWeeklyFrequency(TotalYearly, TimeFrequencyEnum.Yearly);
        public double TotalFortnightly => ModelHelper.ConvertAmountToFortnightlyFrequency(TotalYearly, TimeFrequencyEnum.Yearly);
        [JsonIgnore]
        public string TotalMonthlyWithComma => $"{TotalMonthly:N0}";
        [JsonIgnore]
        public string TotalYearlyWithComma => $"{TotalYearly:N0}";


        public double AnnualGrowthRate { get; set; }
        [JsonIgnore]
        /// <summary>
        /// The growth rate as a <b>fraction</b> for calculations (AnnualGrowthRate 5 => 0.05).
        /// </summary>
        /// <remarks>
        /// Do NOT display this with a "%" suffix. <see cref="AnnualGrowthRate"/> is already the
        /// percentage number, and rounding this fraction to 2 decimals collapses the 0.5 steps
        /// used by the +/- buttons (2.0 and 2.5 both render 0.02), which makes every other tap
        /// look like it did nothing. Show <see cref="AnnualGrowthRate"/> instead.
        /// </remarks>
        public double AnnualGrowthRatePercentage => Math.Round(AnnualGrowthRate / 100, 2);
        public int NumberOfYearsProjection { get; set; }

        [JsonIgnore]
        public double ProjectTotalYearly => ProjectionTerms?.Last()?.IncomeExpenseAmount ?? 0;
        [JsonIgnore]
        public string ProjectTotalYearlyWithComma => $"{Math.Round(ProjectTotalYearly, 0):N0}";

        [JsonIgnore]
        public List<IncomeExpenseProjectionOutput> ProjectionTerms { get; set; }
    }
}
