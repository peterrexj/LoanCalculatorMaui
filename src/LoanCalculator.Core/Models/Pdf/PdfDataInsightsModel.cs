using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;

namespace LoanCalculator.Core.Models.Pdf
{
    public class PdfDataInsightsModel(
        LoanViewModel loanViewModel,
        IncomeViewModel incomeModel,
        ExpenseViewModel expenseModel)
    {
        public LoanModel Loan { get; set; } = new LoanModel();
        public IncomeModel Income { get; set; } = new IncomeModel();
        public ExpenseModel Expense { get; set; } = new ExpenseModel();

        public class LoanModel
        {
            public double PropertyAmount { get; set; }
            public double TotalPropertyAmount { get; set; } // PropertyAmount + OtherExpenseTotalAmount
            public double TotalRepayment { get; set; }
            public double TotalInterest { get; set; }

            public double LoanAmount { get; set; }
            public double DepositAmount { get; set; }


            public int LoanTermInYears { get; set; }
            public double InterestRate { get; set; }
            public double TermPayment { get; set; }
            public string RepaymentFrequency { get; set; } = "monthly"; //RepaymentFrequencySelectedIndex == 0 ? " monthly" :


            public double OtherExpenseTotalAmount { get; set; }
            public double StampDuty { get; set; }
            public double MortgageCharges { get; set; }
            public double BankSettlementFee { get; set; }
            public double ConveyancerFee { get; set; }
            public double InspectionFee { get; set; }
            public double OtherExpenses { get; set; }

            public double WeeklyRepayment { get; set; }
            public double FortnightlyRepayment { get; set; }
            public double MonthlyRepayment { get; set; }
            public double YearlyRepayment { get; set; }

            public double MonthlyRepaymentWithExpenses { get; set; }
            public double YearlyRepaymentWithExpenses { get; set; }

            public double TotalMonthlyRunningExpense { get; set; }
            public double TotalYearlyRunningExpense { get; set; }

            public IncomeExpenseBase? Transactions { get; set; }

            public List<PaymentAmortisationOutput> PaymentAmortization { get; set; }
        }

        public class IncomeModel
        {
            public double TotalMonthly { get; set; }
            public double TotalYearly { get; set; }

            public double TotalAfterExpenseMonthly { get; set; }
            public double TotalAfterExpenseYearly { get; set; }

            public double TotalAfterExpenseIncludingPropertyMonthly { get; set; }
            public double TotalAfterExpenseIncludingPropertyYearly { get; set; }

            public IncomeExpenseBase? Transactions { get; set; }

            public void ResetTransactions()
            {
                Transactions?.SumUpData();
            }

            public IncomeExpenseBase? TransactionRecordsWithExpense
            {
                get
                {
                    Transactions?.SumUpData(TotalExpenseMonthly, TotalExpenseYearly);
                    return Transactions;
                }
            }

            public IncomeExpenseBase? TransactionRecordsWithExpenseIncludingProperty
            {
                get
                {
                    Transactions?.SumUpData(TotalExpenseIncludingPropertyMonthly, TotalExpenseIncludingPropertyYearly);
                    return Transactions;
                }
            }

            public double TotalExpenseMonthly { get; set; }
            public double TotalExpenseYearly { get; set; }

            public double TotalExpenseIncludingPropertyMonthly { get; set; }
            public double TotalExpenseIncludingPropertyYearly { get; set; }

        }

        public class ExpenseModel
        {
            public double TotalMonthly { get; set; }
            public double TotalYearly { get; set; }
            public IncomeExpenseBase? Transactions { get; set; }
        }

        public void InitializeLocalDataSet()
        {
            Loan.PropertyAmount = loanViewModel.HomeLoanInfo.PropertyAmount;
            Loan.TotalRepayment = loanViewModel.HomeLoanInfo.PaymentSummary?.Payment?.TotalPayment ?? 0;
            Loan.DepositAmount = loanViewModel.HomeLoanInfo.DepositAmountDirectInput;
            Loan.LoanAmount = loanViewModel.HomeLoanInfo.LoanAmountDirectInput;

            Loan.LoanTermInYears = loanViewModel.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears;
            Loan.InterestRate = loanViewModel.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate;
            Loan.TermPayment = loanViewModel.HomeLoanInfo.PaymentSummary?.Payment?.TermPayment ?? 0;
            Loan.RepaymentFrequency = loanViewModel.RepaymentFrequencySelectedIndex == 0 ? "Monthly" :
                loanViewModel.RepaymentFrequencySelectedIndex == 1 ? "Fortnightly" :
                loanViewModel.RepaymentFrequencySelectedIndex == 2 ? "Weekly" : "Monthly";

            Loan.TotalInterest = loanViewModel.HomeLoanInfo.PaymentSummary?.Payment?.TotalInterestPayment ?? 0;

            Loan.OtherExpenseTotalAmount = loanViewModel.HomeLoanInfo.OtherExpenseTotalAmount;
            Loan.StampDuty = loanViewModel.HomeLoanInfo.StampDuty.StampDuty;
            Loan.MortgageCharges = loanViewModel.HomeLoanInfo.StampDuty.MortgageCharges;
            Loan.BankSettlementFee = loanViewModel.HomeLoanInfo.BankExpense.BankSettlementFee;
            Loan.ConveyancerFee = loanViewModel.HomeLoanInfo.ConveyanceExpense.ConveyancerFee;
            Loan.InspectionFee = loanViewModel.HomeLoanInfo.OtherExpense.InspectionFee;
            Loan.OtherExpenses = loanViewModel.HomeLoanInfo.OtherExpense.OtherExpenses;

            Loan.TotalPropertyAmount = Loan.PropertyAmount + Loan.OtherExpenseTotalAmount;

            Loan.TotalMonthlyRunningExpense = loanViewModel.TransactionRecords.IncomeExpenseSummary.TotalMonthly;
            Loan.TotalYearlyRunningExpense = loanViewModel.TransactionRecords.IncomeExpenseSummary.TotalYearly;

            Loan.Transactions = loanViewModel.TransactionRecords;


            Loan.WeeklyRepayment = loanViewModel.HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentWeekly ?? 0;
            Loan.FortnightlyRepayment = loanViewModel.HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentFortnightly ?? 0;
            Loan.MonthlyRepayment = loanViewModel.HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0;
            Loan.YearlyRepayment = loanViewModel.HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentYearly ?? 0;

            Loan.MonthlyRepaymentWithExpenses = Math.Round(Loan.TotalMonthlyRunningExpense + Loan.MonthlyRepayment);
            Loan.YearlyRepaymentWithExpenses = Math.Round(Loan.TotalYearlyRunningExpense + Loan.YearlyRepayment);


            Expense.TotalMonthly = expenseModel.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;
            Expense.TotalYearly = expenseModel.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;
            Expense.Transactions = expenseModel.TransactionRecords;

            Income.Transactions = incomeModel.TransactionRecords;

            Income.TotalExpenseMonthly = Expense.TotalMonthly;
            Income.TotalExpenseYearly = Expense.TotalYearly;

            Income.ResetTransactions();

            Income.TotalMonthly = incomeModel.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;
            Income.TotalYearly = incomeModel.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;
            Income.Transactions = incomeModel.TransactionRecords;

            incomeModel.TransactionRecords?.SumUpData(Expense.TotalMonthly, Expense.TotalYearly);

            Income.TotalAfterExpenseMonthly = incomeModel.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;
            Income.TotalAfterExpenseYearly = incomeModel.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;

            Income.ResetTransactions();

            Income.TotalExpenseIncludingPropertyMonthly = Expense.TotalMonthly + Loan.MonthlyRepayment + Loan.TotalMonthlyRunningExpense;
            Income.TotalExpenseIncludingPropertyYearly = Expense.TotalYearly + Loan.YearlyRepayment + Loan.TotalYearlyRunningExpense;

            incomeModel.TransactionRecords?.SumUpData(Income.TotalExpenseIncludingPropertyMonthly, Income.TotalExpenseIncludingPropertyYearly);

            Income.TotalAfterExpenseIncludingPropertyMonthly = incomeModel.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;
            Income.TotalAfterExpenseIncludingPropertyYearly = incomeModel.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;

            loanViewModel.UpdateAmortizationData();
            Loan.PaymentAmortization = loanViewModel.PaymentAmortization ?? new List<PaymentAmortisationOutput>();
        }
    }
}
