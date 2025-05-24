namespace LoanCalculator.Core.Models;

public class CurrencyModel(string name, string symbol, string isoCode)
{
    public string Name { get; set; } = name;
    public string Symbol { get; set; } = symbol;
    public string IsoCode { get; set; } = isoCode;

    public string Display => $"{Name} ({IsoCode}) {Symbol}";
}
