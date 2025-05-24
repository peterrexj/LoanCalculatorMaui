namespace LoanCalculator.Core.Services;

public interface IFontUnicodeProvider
{
    Stream LoadFont(string fileName);
}
