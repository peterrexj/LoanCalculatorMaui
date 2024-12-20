using System;
using System.Collections.Generic;
using System.Text;

namespace LoanCalculator.Models
{
    public class PaymentSummary
    {
        public PaymentOutput Payment { get; set; }
        public List<PaymentPerTermOutput> PaymentTerms { get; set; }
        public List<PaymentAmortisationOutput> PaymentAmortizationTerms { get; set; }
    }
}
