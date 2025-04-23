namespace LoanCalculator.Core.Models.Enums
{
    public static class EnumHelper<T> where T : Enum
    {
        public static int ToIndex(T ausState) => Array.IndexOf(Enum.GetValues(typeof(T)), ausState);
        public static T FromIndex(int index) => (T)Enum.ToObject(typeof(T), index);
        public static T? FromString(string? value)
            => value is null ? default : (T)Enum.Parse(typeof(T), value);

        public static List<string> List => Enum.GetNames(typeof(T)).ToList();
    }
}
