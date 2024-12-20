using LoanCalculator.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoanCalculator.Models.Income
{
    public static class IncomeExpenseHelper
    {
        public static int TimeFrequencyToIndex(TimeFrequencyEnum timeFrequency) => Array.IndexOf(Enum.GetValues(typeof(TimeFrequencyEnum)), timeFrequency);
        public static TimeFrequencyEnum TimeFrequencyFromIndex(int index) => (TimeFrequencyEnum)Enum.ToObject(typeof(TimeFrequencyEnum), index);
        public static TimeFrequencyEnum TimeFrequencyFromString(string name) => (TimeFrequencyEnum)Enum.Parse(typeof(TimeFrequencyEnum), name);
        public static List<string> TimeFrequencies => Enum.GetNames(typeof(TimeFrequencyEnum)).ToList();
    }
}
