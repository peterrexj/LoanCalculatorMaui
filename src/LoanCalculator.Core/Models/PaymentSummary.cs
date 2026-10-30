using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Models
{
    public class PaymentSummary
    {
        public PaymentOutput? Payment { get; set; }

        /// <summary>
        /// One row per payment across the whole term. Built on demand — see
        /// <c>HomeLoanCalculator.BuildPaymentSchedule</c>.
        /// </summary>
        /// <remarks>
        /// <para><b>Not serialised.</b> This is derived data, and it is large: a 30-year loan paid
        /// weekly is 1,560 rows, roughly 230 KB of JSON. It was being written to
        /// <c>homeloandata.json</c> on every save — and a save is triggered (debounced) by every
        /// change to any loan figure, so dragging the deposit slider meant repeatedly serialising a
        /// schedule nothing reads back. Rebuilding it costs one pass over a list; storing it cost a
        /// file write of that size.</para>
        /// </remarks>
        [JsonIgnore]
        public List<PaymentPerTermOutput>? PaymentTerms { get; set; }

        /// <summary>Derived from <see cref="PaymentTerms"/>; not serialised, for the same reason.</summary>
        [JsonIgnore]
        public List<PaymentAmortisationOutput>? PaymentAmortizationTerms { get; set; }

        /// <summary>
        /// The principal the schedule should be built from, kept so it can be built later from this
        /// summary alone rather than needing the original inputs again.
        /// </summary>
        public double SchedulePrincipal { get; set; }

        /// <summary>Number of payments across the term, kept for the same reason.</summary>
        public int ScheduleTotalPayments { get; set; }
    }
}
