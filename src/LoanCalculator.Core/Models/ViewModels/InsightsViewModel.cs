using LoanCalculator.Core.Models.BaseExtensions;

namespace LoanCalculator.Core.Models.ViewModels
{
    public class InsightsViewModel : BasePropertyChangeModel
    {
        public string Name { get; set; }
        public string Description { get; set; }

        // Concise one-line summary shown by default; the full Description expands on tapping ⓘ.
        public string ShortDescription { get; set; }
        public string Value { get; set; }
        public int Order { get; set; }
    }

    public class InsightsDetailsViewModel : BasePropertyChangeModel
    {
        #region Key Insights

        public InsightsViewModel AffordabilityMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Affordability (monthly)",
            ShortDescription = "left each month after all expenses and the loan repayment",
            Description = "is the amount of money you have left each month after covering all your expenses, including the new loan repayment. It shows how much you can comfortably manage on a monthly basis while still meeting your financial needs."
        };
        public InsightsViewModel AffordabilityYearly { get; set; } = new InsightsViewModel
        {
            Name = "Affordability (yearly)",
            ShortDescription = "left each year after all expenses and loan repayments",
            Description = "is the amount of money you have left each year after paying for all your annual expenses, including the total of your loan repayments. It reflects how much you can sustainably manage over the course of a year while maintaining your financial stability."
        };


        #endregion

        #region Property
        public InsightsViewModel PropertyAmount { get; set; } = new InsightsViewModel
        {
            Name = "Cost of the property",
            ShortDescription = "the estimated purchase price of the property",
            Description = "is the estimated cost for proposed property"
        };
        public InsightsViewModel PropertyEstimatedUpfront { get; set; } = new InsightsViewModel
        {
            Name = "Additional upfront costs (estimated)",
            ShortDescription = "extra upfront fees: stamp duty, bank, conveyancer, inspection",
            Description = "is the additional upfront costs for a property can include various fees and expenses beyond the purchase price, from stamp duty, bank fees, conveyancer fees, inspection fees and more"
        };
        public InsightsViewModel PropertyTotalAmount { get; set; } = new InsightsViewModel
        {
            Name = "Total property cost (estimated)",
            ShortDescription = "purchase price plus all upfront costs",
            Description = "is the total investment in the property, combining both the actual purchase price and the additional upfront costs, forms the comprehensive sum that needs to be considered when planning the financial commitment"
        };
        #endregion

        #region Loan Amount
        public InsightsViewModel LoanAmount { get; set; } = new InsightsViewModel
        {
            Name = "Total loan amount",
            ShortDescription = "the amount borrowed from the bank",
            Description = "is the aggregate loan amount to be procured from the bank"
        };
        public InsightsViewModel DepositAmount { get; set; } = new InsightsViewModel
        {
            Name = "Total deposit amount",
            ShortDescription = "the upfront amount you must provide yourself",
            Description = "is the full sum you must have available upfront as a deposit before applying for a loan or when settling the loan"
        };
        public InsightsViewModel TotalRepaymentToBank { get; set; } = new InsightsViewModel
        {
            Name = "Total loan repayment amount",
            ShortDescription = "principal plus interest paid over the whole loan term",
            Description = "is the total repayment to the bank comprises two main components: the principal and the interest. The principal is the original amount borrowed, while the interest is the additional cost incurred for the privilege of borrowing that amount. Together, these two elements constitute the overall repayment amount, representing the combined sum of the borrowed principal and the interest accrued over the loan term."
        };
        public InsightsViewModel TotalInterestToBank { get; set; } = new InsightsViewModel
        {
            Name = "Total interest cost",
            ShortDescription = "the total interest paid to the bank over the loan term",
            Description = "is the overall amount of interest that is required to repay to the bank over the course of a loan. This includes the interest accrued on the principal amount borrowed. It's an important factor to be aware of, as it represents the cost of borrowing and is a significant component of the total repayment."
        };
        public InsightsViewModel LoanTerm { get; set; } = new InsightsViewModel
        {
            Name = "Loan duration",
            ShortDescription = "the period over which the loan is repaid",
            Description = "is the period over which a loan is scheduled to be repaid. It is the duration during which is obligated to make regular payments toward the loan, including both principal and interest."
        };
        public InsightsViewModel InterestRate { get; set; } = new InsightsViewModel
        {
            Name = "Interest rate",
            ShortDescription = "the annual percentage charged on the loan",
            Description = "is the percentage charged annually on the loan amount"
        };
        public InsightsViewModel RepaymentDetailSelectedFrequency { get; set; } = new InsightsViewModel
        {
            Name = "Loan pay back details",
            ShortDescription = "amount repaid each time, for your chosen frequency",
            Description = "is the amount you need to repay each time based on your selected repayment frequency"
        };
        public InsightsViewModel RepaymentFrequency { get; set; } = new InsightsViewModel
        {
            Name = "Repayment frequency",
            ShortDescription = "how often you make repayments",
            Description = "is how often you make loan repayments"
        };
        public InsightsViewModel RepaymentDetailMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Monthly repayment",
            ShortDescription = "repaid each month, including interest and principal",
            Description = "is the total amount you repay every month toward your loan, including interest and principal"
        };
        public InsightsViewModel RepaymentDetailYearly { get; set; } = new InsightsViewModel
        {
            Name = "Annual repayment",
            ShortDescription = "repaid each year, including interest and principal",
            Description = "is the total amount you repay every year toward your loan, including interest and principal"
        };

        #endregion

        #region Expenses
        public InsightsViewModel ExpenseOverallTotalMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Total expenses (monthly)",
            ShortDescription = "all monthly outflows: loan repayment plus every expense",
            Description = "" // on runtime
        };
        public InsightsViewModel ExpenseOverallTotalYearly { get; set; } = new InsightsViewModel
        {
            Name = "Total expenses (yearly)",
            ShortDescription = "all yearly outflows: loan repayments plus every expense",
            Description = "" // on runtime
        };

        public InsightsViewModel ExpenseCostOfNewPropertyOwnershipMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Additional expense for this property (monthly)",
            ShortDescription = "recurring monthly costs of owning this property",
            Description = "refers to the recurring monthly costs associated with the property, which might include maintenance charges, utility bills, and other expenses directly tied to its ownership. These expenses are a crucial consideration for budgeting after the initial purchase."
        };
        public InsightsViewModel ExpenseCostOfNewPropertyOwnershipYearly { get; set; } = new InsightsViewModel
        {
            Name = "Additional expense for this property (yearly)",
            ShortDescription = "recurring yearly costs of owning this property",
            Description = "is the yearly additional expense for this property encompasses total utility costs, maintenance fees, insurance premiums, and other recurring annual charges."
        };
        public InsightsViewModel ExpenseLoanFinancialCommitmentsMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Loan financial commitments (monthly)",
            ShortDescription = "monthly repayment plus property running costs",
            Description = "includes the mortgage payment and recurring costs such as utilities, maintenance, and insurance, giving a comprehensive view of monthly financial commitments."
        };
        public InsightsViewModel ExpenseLoanFinancialCommitmentsYearly { get; set; } = new InsightsViewModel
        {
            Name = "Loan financial commitments (yearly)",
            ShortDescription = "yearly repayment plus property running costs",
            Description = "combines the yearly mortgage repayment with all recurring annual costs such as utilities, maintenance, and insurance, to provide a comprehensive summary of total yearly obligations"
        };
        public InsightsViewModel ExpenseCurrentFinancialOutflowsMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Expenses recorded (monthly)",
            ShortDescription = "your recorded monthly expenses",
            Description = "represents the total recurring costs incurred on a monthly basis, including utility bills, maintenance charges, and other recurring expenses."
        };
        public InsightsViewModel ExpenseCurrentFinancialOutflowsYearly { get; set; } = new InsightsViewModel
        {
            Name = "Expenses recorded (yearly)",
            ShortDescription = "your recorded yearly expenses",
            Description = "represents the cumulative recurring costs incurred over a year, including utility bills, maintenance charges, and other recurring annual expenses."
        };

        #endregion

        #region Income

        public InsightsViewModel IncomeTotalMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Income earned (monthly)",
            ShortDescription = "your total recorded monthly income",
            Description = "represents the total income generated on a monthly basis, reflecting the earnings recorded"
        };
        public InsightsViewModel IncomeTotalYearly { get; set; } = new InsightsViewModel
        {
            Name = "Income earned (yearly)",
            ShortDescription = "your total recorded yearly income",
            Description = "represents the cumulative income earned over a year, giving a comprehensive view of annual financial inflow"
        };

        public InsightsViewModel IncomeAfterExpenseMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Net income (monthly)",
            ShortDescription = "monthly income left after expenses (before the loan)",
            Description = ""
        };
        public InsightsViewModel IncomeAfterExpenseYearly { get; set; } = new InsightsViewModel
        {
            Name = "Net income (yearly)",
            ShortDescription = "yearly income left after expenses (before the loan)",
            Description = ""
        };

        public InsightsViewModel IncomeAfterExpenseWithLoanMonthly { get; set; } = new InsightsViewModel
        {
            Name = "Net income (monthly)",
            ShortDescription = "monthly income left after expenses and the loan",
            Description = ""
        };
        public InsightsViewModel IncomeAfterExpenseWithLoanYearly { get; set; } = new InsightsViewModel
        {
            Name = "Net income (yearly)",
            ShortDescription = "yearly income left after expenses and the loan",
            Description = ""
        };

        #endregion

    }
}
