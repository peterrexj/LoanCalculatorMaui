using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.Services
{
    internal class MauiFontUnicodeProvider : IFontUnicodeProvider
    {
        public Stream LoadFont(string fileName)
        {
            return FileSystem.OpenAppPackageFileAsync(fileName).GetAwaiter().GetResult();
        }
    }
}
