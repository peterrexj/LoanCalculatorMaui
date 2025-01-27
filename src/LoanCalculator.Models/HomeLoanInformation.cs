using System.Text.Json.Serialization;
using LoanCalculator.Models.AdditionalExpense;
using LoanCalculator.Models.BaseExtensions;
using LoanCalculator.Models.Income;

namespace LoanCalculator.Models
{
    public class HomeLoanInformation : BaseViewModel
    {
        StampDutyOutput _stampDuty;
        public StampDutyOutput StampDuty
        {
            get => _stampDuty;
            set
            {
                _stampDuty = value;
                OnPropertyChanged(nameof(StampDuty));
            }
        }
        public HomeLoanRepaymentInput HomeLoanRepaymentRequest { get; set; }

        PaymentSummary _paymentSummary;
        public PaymentSummary PaymentSummary
        {
            get => _paymentSummary;
            set
            {
                _paymentSummary = value;
                OnPropertyChanged(nameof(PaymentSummary));
            }
        }

        public OtherExpense OtherExpense { get; set; }
        public ConveyanceExpense ConveyanceExpense { get; set; }
        public BankExpense BankExpense { get; set; }
        public Incomes Incomes { get; set; }
        public HomeDailyExpense HomeDailyExpense { get; set; }
        public IncomeExpense RentalIncome { get; set; }
        public IncomeExpense PropertyManagementExpense { get; set; }

        public HomeLoanInformation()
        {
            StampDuty = new StampDutyOutput();
            PaymentSummary = new PaymentSummary();
            ConveyanceExpense = new ConveyanceExpense();
            BankExpense = new BankExpense();
            OtherExpense = new OtherExpense();
            Incomes = new Incomes();
            HomeDailyExpense = new HomeDailyExpense();
            RentalIncome = new IncomeExpense();
            PropertyManagementExpense = new IncomeExpense();
        }

        public double PropertyAmount { get; set; }

        #region Loan and Property amount distributions

        public double LoanAmount
        {
            get
            {
                if (PropertyTotalAmount <= 0) return 0;

                if (LoanAmountDirectInput > 0) return LoanAmountDirectInput;

                if (DepositAmountDirectInput > 0) return PropertyTotalAmount - DepositAmountDirectInput;

                if (LoanAmountPercentage == 0 && DepositPercentage == 0) return PropertyTotalAmount;

                if (LoanAmountPercentage > 0) return (PropertyTotalAmount * LoanAmountPercentage) / 100;

                if (DepositPercentage > 0) return PropertyTotalAmount - ((PropertyTotalAmount * DepositPercentage) / 100);

                return PropertyTotalAmount;
            }
        }

        private double _loanAmountDirectInput;
        public double LoanAmountDirectInput
        {
            get { return _loanAmountDirectInput; }
            set
            {
                if (value <= 0)
                {
                    _loanAmountDirectInput = 0;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 0, depositDirect: 0, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: true, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
                else if (value > PropertyTotalAmount)
                {
                    _loanAmountDirectInput = PropertyTotalAmount;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 0, depositDirect: 0, loanDirect: _loanAmountDirectInput,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
                else
                {
                    _loanAmountDirectInput = value;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 0, depositDirect: 0, loanDirect: _loanAmountDirectInput,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
            }
        }

        private double _depositAmountDirectInput;
        public double DepositAmountDirectInput
        {
            get { return _depositAmountDirectInput; }
            set
            {
                if (value <= 0)
                {
                    _depositAmountDirectInput = 0;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 0, depositDirect: 0, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: true, byLoanPercentOnZero: false);
                }
                else if (value > PropertyTotalAmount)
                {
                    _depositAmountDirectInput = PropertyTotalAmount;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 0, depositDirect: _depositAmountDirectInput, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
                else
                {
                    _depositAmountDirectInput = value;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 0, depositDirect: _depositAmountDirectInput, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
            }
        }

        [JsonIgnore]
        public double LoanPercentage
        {
            get
            {
                if (LoanAmount == 0 || PropertyTotalAmount == 0) return 0;
                return Math.Round(LoanAmount / PropertyTotalAmount * 100, 2);
            }
        }

        public double Deposit { get; set; }

        private double _loanAmountPercentage;
        public double LoanAmountPercentage
        {
            get
            {
                return _loanAmountPercentage;
            }
            set
            {
                if (value <= 0)
                {
                    _loanAmountPercentage = 0;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 0, depositDirect: 0, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: true);
                }
                else if (value > 100)
                {
                    _loanAmountPercentage = 100;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 100, depositDirect: 0, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
                else
                {
                    _loanAmountPercentage = value;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: value, depositDirect: 0, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
            }
        }

        private double _depositPercentage;
        public double DepositPercentage
        {
            get
            {
                return _depositPercentage;
            }
            set
            {
                if (value <= 0)
                {
                    _depositPercentage = 0;
                    ProcessDepositCalc(depositPercent: 0, loanPercent: 0, depositDirect: 0, loanDirect: 0,
                        byDepositPercentOnZero: true, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
                else if (value > 100)
                {
                    _depositPercentage = 100;
                    ProcessDepositCalc(depositPercent: 100, loanPercent: 0, depositDirect: 0, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
                else
                {
                    _depositPercentage = value;
                    ProcessDepositCalc(depositPercent: value, loanPercent: 0, depositDirect: 0, loanDirect: 0,
                        byDepositPercentOnZero: false, byLoanDirectOnZero: false, byDepositDirectOnZero: false, byLoanPercentOnZero: false);
                }
            }
        }

        [JsonIgnore]
        public bool IsLoanAmountByPercentage => LoanAmountPercentage > 0;
        [JsonIgnore]
        public bool IsDepositByPercentage => DepositPercentage > 0;

        private void ProcessDepositCalc(double depositPercent, double loanPercent, double depositDirect, double loanDirect,
            bool byDepositPercentOnZero, bool byLoanDirectOnZero, bool byDepositDirectOnZero, bool byLoanPercentOnZero)
        {
            if (PropertyTotalAmount == 0)
            {
                _depositPercentage = 0;
                _loanAmountPercentage = 0;
                _loanAmountDirectInput = 0;
                _depositAmountDirectInput = 0;
                return;
            }

            if (depositPercent > 0 || byDepositPercentOnZero)
            {
                _depositPercentage = depositPercent;
                _loanAmountPercentage = 100 - _depositPercentage;

                _loanAmountDirectInput = PropertyTotalAmount - ((PropertyTotalAmount * _depositPercentage) / 100);
                _depositAmountDirectInput = PropertyTotalAmount - _loanAmountDirectInput;
            }
            else if (loanPercent > 0 || byLoanPercentOnZero)
            {
                _loanAmountPercentage = loanPercent;
                _depositPercentage = 100 - loanPercent;

                _loanAmountDirectInput = PropertyTotalAmount - ((PropertyTotalAmount * _depositPercentage) / 100);
                _depositAmountDirectInput = PropertyTotalAmount - _loanAmountDirectInput;
            }
            else if (loanDirect > 0 || byLoanDirectOnZero)
            {
                _loanAmountDirectInput = loanDirect;
                _depositAmountDirectInput = PropertyTotalAmount - _loanAmountDirectInput;

                _depositPercentage = (_depositAmountDirectInput / PropertyTotalAmount) * 100;
                _loanAmountPercentage = 100 - _depositPercentage;
            }
            else if (depositDirect > 0 || byDepositDirectOnZero)
            {
                _depositAmountDirectInput = depositDirect;
                _loanAmountDirectInput = PropertyTotalAmount - _depositAmountDirectInput;

                _depositPercentage = (_depositAmountDirectInput / PropertyTotalAmount) * 100;
                _loanAmountPercentage = 100 - _depositPercentage;
            }
        }

        #endregion

        public double OtherExpenseTotalAmount
        {
            get
            {
                StampDuty.SumUpData();
                BankExpense.SumUpData();
                ConveyanceExpense.SumUpData();
                OtherExpense.SumUpData();
                return StampDuty.Total + BankExpense.Total + ConveyanceExpense.Total + OtherExpense.Total;
            }
        }
        [JsonIgnore]
        public double PropertyTotalAmount => PropertyAmount + OtherExpenseTotalAmount;

        public double MonthlyTotalRepayment { get; set; }
    }
}
