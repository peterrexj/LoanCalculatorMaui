using LoanCalculator.Models.BaseExtensions;
using System;

namespace LoanCalculator.Models
{
    public class HomeLoanRepaymentInput : BaseViewModel
    {

        private double _interestRate;
        public double InterestRate
        {
            get
            {
                return _interestRate;
            }
            set
            {
                _interestRate = value;
                OnPropertyChanged("InterestRate");
                OnPropertyChanged("TotalNumberOfPaymentsWithInTermPeriod");
            }
        }



        private int _loanTermInYears;
        public int LoanTermInYears
        {
            get
            {
                return _loanTermInYears;
            }
            set
            {
                _loanTermInYears = value;
                OnPropertyChanged("LoanTermInYears");
                OnPropertyChanged("TotalNumberOfPaymentsWithInTermPeriod");
            }
        }



        private int _totalNumberPaymentPerYear;
        public int TotalNumberPaymentPerYear
        {
            get
            {
                return _totalNumberPaymentPerYear;
            }
            set
            {
                _totalNumberPaymentPerYear = value;
                OnPropertyChanged("TotalNumberPaymentPerYear");
                OnPropertyChanged("TotalNumberOfPaymentsWithInTermPeriod");
            }
        }
        public int TotalNumberOfPaymentsWithInTermPeriod => LoanTermInYears * TotalNumberPaymentPerYear;
    }
}
