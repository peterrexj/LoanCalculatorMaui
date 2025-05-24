using LoanCalculator.Core.Services;
using Pj.Library;

namespace LoanCalculator.Playground
{
    internal class FileSystemFontProvider : IFontUnicodeProvider
    {
        public Stream LoadFont(string fileName)
        {
            var path = Path.Combine(PjUtility.Runtime.ExecutingRepositoryRootFolder, "src", "LoanCalculator", "Resources", "Fonts", fileName);
            return File.OpenRead(path);

        }
    }
}
