using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LoanCalculator.Models.Income.Summary
{
    public class IncomeExpenseSummary
    {
        public double TotalMonthly { get; set; }
        public double TotalYearly { get; set; }
        [JsonIgnore]
        public string TotalMonthlyWithComma => $"{TotalMonthly:N0}";
        [JsonIgnore]
        public string TotalYearlyWithComma => $"{TotalYearly:N0}";


        public double AnnualGrowthRate { get; set; }
        [JsonIgnore]
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
