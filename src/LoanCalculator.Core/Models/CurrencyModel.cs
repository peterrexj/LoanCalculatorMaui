namespace LoanCalculator.Core.Models;

public class CurrencyModel(string name, string symbol, string isoCode)
{
    public string Name { get; set; } = name;
    public string Symbol { get; set; } = symbol;
    public string IsoCode { get; set; } = isoCode;

    // Omit the trailing symbol when it is just the ISO code again (no real glyph exists)
    // so the entry reads "Romanian Leu (RON)" rather than "Romanian Leu (RON) RON".
    public string Display =>
        string.IsNullOrWhiteSpace(Symbol) || Symbol == IsoCode
            ? $"{Name} ({IsoCode})"
            : $"{Name} ({IsoCode}) {Symbol}";
}
