using LoanCalculator.Models.Income;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoanCalculator.Models.AdditionalExpense
{
    public class HomeDailyExpense : IncomeExpenseBase
    {
        public IncomeExpense Repair
        {
            get => GetEntry("Repair");
            set
            {
                Add("Repair", value.Amount, value.Frequency, isCheckForExistingRequired: false);
            }
        }

        public IncomeExpense Water
        {
            get => GetEntry("Water");
            set
            {
                Add("Water", value.Amount, value.Frequency, isCheckForExistingRequired: false);
            }
        }
        public IncomeExpense CouncilRates
        {
            get => GetEntry("CouncilRates");
            set
            {
                Add("CouncilRates", value.Amount, value.Frequency, isCheckForExistingRequired: false);
            }
        }
        public IncomeExpense Strata
        {
            get => GetEntry("Strata");
            set
            {
                Add("Strata", value.Amount, value.Frequency, isCheckForExistingRequired: false);
            }
        }
    }
}
