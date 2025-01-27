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
                OnPropertyChanged(nameof(InterestRate));
                OnPropertyChanged(nameof(TotalNumberOfPaymentsWithInTermPeriod));
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
                OnPropertyChanged(nameof(LoanTermInYears));
                OnPropertyChanged(nameof(TotalNumberOfPaymentsWithInTermPeriod));
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
                OnPropertyChanged(nameof(TotalNumberPaymentPerYear));
                OnPropertyChanged(nameof(TotalNumberOfPaymentsWithInTermPeriod));
            }
        }
        public int TotalNumberOfPaymentsWithInTermPeriod => LoanTermInYears * TotalNumberPaymentPerYear;
    }
}
