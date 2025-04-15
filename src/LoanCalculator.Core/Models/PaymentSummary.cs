namespace LoanCalculator.Core.Models
{
    public class PaymentSummary
    {
        public PaymentOutput? Payment { get; set; }
        //[JsonIgnore]
        public List<PaymentPerTermOutput>? PaymentTerms { get; set; }
        //JsonIgnore]
        public List<PaymentAmortisationOutput>? PaymentAmortizationTerms { get; set; }
    }
}
