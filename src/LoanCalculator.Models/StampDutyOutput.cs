using LoanCalculator.Models.AdditionalExpense;
using LoanCalculator.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoanCalculator.Models
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
        public int AustraliaStateIndex => EnumHelper<AustralianStatesEnum>.ToIndex(AustraliaStateSelected);

        public AustralianStatesEnum AustraliaStateSelected { get; set; }
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

        public void AutoUpdateMortgageCharges()
        {
            MortgageCharges = ExpenseEntries.Where(f => f.Name != "Mortgage charges").Sum(f => f.Expense);
        }

        public double StampDuty { get; set; }

        public double Total { get; set; }
        public List<AdditionalExpenseEntry> ExpenseEntries { get; set; }

        public void SumUpData()
        {
            Total = StampDuty + MortgageCharges;
            //if (ExpenseEntries != null)
            //{
            //    foreach (var item in ExpenseEntries)
            //    {
            //        Total += item.Expense;
            //    }
            //}
        }
    }
}
