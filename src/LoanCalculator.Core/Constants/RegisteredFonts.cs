namespace LoanCalculator.Core.Constants
{
    public static class RegisteredFonts
    {
        public static List<string> GetFontFamilies() => new List<string>
        {
            "Lato",
            "Nunito",
            "Quicksand",
            "Raleway",
            "Merriweather",
            "SourceSerif4",
            "PlayfairDisplay",
            "Pacifico",
        };

        public static string DefaultFontFamily => "Lato";
    }
}
