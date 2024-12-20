using System;
using System.Collections.Generic;
using System.Text;

namespace LoanCalculator.Models
{
    public class PaymentPerTermOutput
    {
        public int TermNumber { get; set; }
        public double PaymentAmount { get; set; }
        public double InterestAmount { get; set; }
        public double PrincipalAmount { get; set; }
        public double TotalPayments { get; set; }
    }
}
