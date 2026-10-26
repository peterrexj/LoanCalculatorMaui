using LoanCalculator.Core.Models.AdditionalExpense;
using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.Core.Models
{
    public class StampDutyOutput
    {

        public StampDutyOutput()
        {
            ExpenseEntries = new List<AdditionalExpenseEntry> { new AdditionalExpenseEntry { Name = "Mortgage charges", Expense = 0 } };
        }

        public static int AustraliaStateToIndex(AustralianStatesEnum ausState) => EnumHelper<AustralianStatesEnum>.ToIndex(ausState);
        public static AustralianStatesEnum AustraliaStateFromIndex(int index) => EnumHelper<AustralianStatesEnum>.FromIndex(index);
        public static List<string> AustralianStates => EnumHelper<AustralianStatesEnum>.List;
        public int? AustraliaStateIndex => AustraliaStateSelected.HasValue
            ? EnumHelper<AustralianStatesEnum>.ToIndex(AustraliaStateSelected.Value)
            : null;

        public AustralianStatesEnum? AustraliaStateSelected { get; set; }
        public StampDutyOutput SetState(AustralianStatesEnum state)
        {
            AustraliaStateSelected = state;
            return this;
        }

        public double MortgageCharges
        {
            get => ExpenseEntries.First(f => f.Name == "Mortgage charges").Expense;
            set => ExpenseEntries.First(f => f.Name == "Mortgage charges").Expense = value;
        }

        /// <summary>
        /// Rolls the state-calculated fee entries (e.g. "Mortgage Registration", "Transfer Fee")
        /// up into <see cref="MortgageCharges"/>.
        /// </summary>
        /// <remarks>
        /// Returns early when there is nothing to roll up. Outside Australian mode nothing ever
        /// adds those extra entries, so the unguarded sum was 0 and overwrote whatever the user
        /// had typed into the Mortgage Charges field — and because <see cref="SumUpData"/> is
        /// called from <c>HomeLoanInformation.OtherExpenseTotalAmount</c>'s getter, merely
        /// displaying the upfront total was enough to wipe it.
        /// </remarks>
        public void AutoUpdateMortgageCharges()
        {
            var calculatedFees = ExpenseEntries.Where(f => f.Name != "Mortgage charges").ToList();
            if (calculatedFees.Count == 0) return;

            MortgageCharges = calculatedFees.Sum(f => f.Expense);
        }

        public double StampDuty { get; set; }

        public double Total { get; set; }
        public List<AdditionalExpenseEntry> ExpenseEntries { get; set; }

        public void SumUpData()
        {
            AutoUpdateMortgageCharges();
            Total = StampDuty + MortgageCharges;
        }
    }
}
