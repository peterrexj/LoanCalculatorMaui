using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace LoanCalculator.Models.Enums
{
    public static class EnumHelper<T> where T : Enum
    {
        public static int ToIndex(T ausState) => Array.IndexOf(Enum.GetValues(typeof(T)), ausState);
        public static T FromIndex(int index) => (T)Enum.ToObject(typeof(T), index);
        public static T FromString(string value) => (T)Enum.Parse(typeof(T), value);
        public static List<string> List => Enum.GetNames(typeof(T)).ToList();
    }
}
