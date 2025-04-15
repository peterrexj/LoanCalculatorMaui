using LoanCalculator.Core.Models.BaseExtensions;

namespace LoanCalculator.Core.Models.ViewModels
{
    public class InsightsViewModel : BasePropertyChangeModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public int Order { get; set; }
    }

    public class InsightsDetailsViewModel : BasePropertyChangeModel 
    {
        #region Property
        public InsightsViewModel PropertyAmount { get; set; }
        public InsightsViewModel PropertyEstimatedUpfront { get; set; }
        public InsightsViewModel PropertyTotalAmount { get; set; }
        #endregion

        #region Loan Amount
        public InsightsViewModel LoanAmount { get; set; }
        public InsightsViewModel DepositAmount { get; set; }
        public InsightsViewModel TotalRepaymentToBank { get; set; }
        public InsightsViewModel TotalInterestToBank { get; set; }
        public InsightsViewModel LoanTerm { get; set; }
        public InsightsViewModel InterestRate { get; set; }
        public InsightsViewModel RepaymentDetailSelectedFrequency { get; set; }
        public InsightsViewModel RepaymentFrequency { get; set; }
        public InsightsViewModel RepaymentDetailYearly { get; set; }

        #endregion

        #region Expense Income
        public InsightsViewModel ExpenseExistingMonthly { get; set; }
        public InsightsViewModel ExpenseExistingYearly { get; set; }
        public InsightsViewModel ExpenseThisPropertyMonthly { get; set; }
        public InsightsViewModel ExpenseThisPropertyYearly { get; set; }
        public InsightsViewModel ExpenseTotalMonthly { get; set; }
        public InsightsViewModel ExpenseTotalYearly { get; set; }
        public InsightsViewModel IncomeTotalMonthly { get; set; }
        public InsightsViewModel IncomeTotalYearly { get; set; }
        public InsightsViewModel SavingMonthly { get; set; }
        public InsightsViewModel SavingYearly { get; set; }

        #endregion

        public InsightsDetailsViewModel()
        {
            #region Property
            PropertyAmount = new InsightsViewModel
            {
                Name = "Cost of the Property",
                Description = "The estimated cost for proposed property"
            };
            PropertyEstimatedUpfront = new InsightsViewModel
            {
                Name = "Additional Upfront Costs (Estimated)",
                Description = "Additional upfront costs for a property can include various fees and expenses beyond the purchase price, from stamp duty, bank fees, conveyancer fees, inspection fees and more"
            };
            PropertyTotalAmount = new InsightsViewModel
            {
                Name = "Total Property Cost (Estimated)",
                Description = "The total investment in the property, combining both the actual purchase price and the additional upfront costs, forms the comprehensive sum that need to bed considered when planning the financial commitment"
            };
            #endregion

            #region Loan Amount
            LoanAmount = new InsightsViewModel
            {
                Name = "Total Loan Amount",
                Description = "The aggregate loan amount to be procured from the bank"
            };
            DepositAmount = new InsightsViewModel
            {
                Name = "Total Deposit Amount",
                Description = "The full sum you must have available upfront as a deposit before applying for a loan or when settling the loan"
            };
            TotalRepaymentToBank = new InsightsViewModel
            {
                Name = "Total Loan Repayment Amount",
                Description = "The total repayment to the bank comprises two main components: the principal and the interest. The principal is the original amount borrowed, while the interest is the additional cost incurred for the privilege of borrowing that amount. Together, these two elements constitute the overall repayment amount, representing the combined sum of the borrowed principal and the interest accrued over the loan term."
            };
            TotalInterestToBank = new InsightsViewModel
            {
                Name = "Total Interest Cost",
                Description = "Overall amount of interest that is required to repay to the bank over the course of a loan. This includes the interest accrued on the principal amount borrowed. It's an important factor to be aware of, as it represents the cost of borrowing and is a significant component of the total repayment."
            };
            LoanTerm = new InsightsViewModel
            {
                Name = "Loan Duration",
                Description = "Period over which a loan is scheduled to be repaid. It is the duration during which is obligated to make regular payments toward the loan, including both principal and interest."
            };
            InterestRate = new InsightsViewModel
            {
                Name = "Interest Rate",
                Description = "Percentage of the loan amount that a borrower pays to the lender as a cost of borrowing"
            };
            RepaymentDetailSelectedFrequency = new InsightsViewModel
            {
                Name = "Loan pay back details",
                Description = "Percentage of the loan amount that a borrower pays to the lender as a cost of borrowing"
            };
            RepaymentFrequency = new InsightsViewModel
            {
                Name = "Repayment Frequency",
                Description = "Percentage of the loan amount that a borrower pays to the lender as a cost of borrowing"
            };
            RepaymentDetailYearly = new InsightsViewModel
            {
                Name = "Annual Repayment",
                Description = "Percentage of the loan amount that a borrower pays to the lender as a cost of borrowing"
            };
            #endregion

            #region Expense Income
            ExpenseExistingMonthly = new InsightsViewModel
            {
                Name = "Expense Current (Monthly)",
                Description = "Cumulative sum of all your monthly expenditures or costs expect the new property"
            };
            ExpenseExistingYearly = new InsightsViewModel
            {
                Name = "Expense Current (Yearly)",
                Description = "The total amount of all your monthly expenses except the new property aggregated over the course of a year"
            };
            ExpenseThisPropertyMonthly = new InsightsViewModel
            {
                Name = "Expense New Property (Monthly)",
                Description = "Expenses related to this recently acquired property pertain solely to costs incurred directly on the property itself, excluding mortgage or bank repayment. These include items such as periodic utility bills, council payments, phone or internet, maintenance, upgrades and so on"
            };
            ExpenseThisPropertyYearly = new InsightsViewModel
            {
                Name = "Expense New Property (Yearly)",
                Description = "Expenses related to this recently acquired property pertain solely to costs incurred directly on the property itself, excluding mortgage or bank repayment aggregated over the course of a year"
            };
            ExpenseTotalMonthly = new InsightsViewModel
            {
                Name = "Expense Total (Monthly)",
                Description = "The combined total of the current monthly expenses and the additional costs incurred after acquiring the new property for a given month"
            };
            ExpenseTotalYearly = new InsightsViewModel
            {
                Name = "Expense Total (Yearly)",
                Description = "The aggregate of both the existing monthly expenses and the additional costs incurred after acquiring the new property, calculated for the entirety of a year"
            };
            IncomeTotalMonthly = new InsightsViewModel
            {
                Name = "Income (Monthly)",
                Description = "Recorded monthly income"
            };
            IncomeTotalYearly = new InsightsViewModel
            {
                Name = "Income (Yearly)",
                Description = "Recorded income calculated for the entirety of a year"
            };
            SavingMonthly = new InsightsViewModel
            {
                Name = "Remaining Budget (Monthly)",
                Description = "The surplus funds that remain after deducting your total expenses from your income. It signifies the amount of money available for saving, investing, or discretionary spending. Monitoring your remaining budget is crucial for financial planning as it allows you to make informed decisions about savings goals, debt repayment, and lifestyle choices based on your financial resources"
            };
            SavingYearly = new InsightsViewModel
            {
                Name = "Remaining Budget (Yearly)",
                Description = "The surplus funds that remain after deducting your total expenses from your income aggregated over the course of a year"
            };
            #endregion
        }

    }
}
