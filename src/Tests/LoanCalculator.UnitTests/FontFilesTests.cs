using LoanCalculator.Core.Constants;
using Pj.Library;

namespace LoanCalculator.UnitTests
{
    [TestFixture]
    public class FontFilesTests
    {
        private string _fontsFolder;

        [SetUp]
        public void Setup()
        {
            _fontsFolder = Path.Combine(
                PjUtility.Runtime.ExecutingRepositoryRootFolder,
                "src", "LoanCalculator", "Resources", "Fonts");
        }

        [Test]
        public void FontsFolder_Exists()
        {
            Assert.That(Directory.Exists(_fontsFolder), Is.True,
                $"Fonts folder not found at: {_fontsFolder}");
        }

        [TestCase("Lato-Regular.ttf")]
        [TestCase("Nunito-Regular.ttf")]
        [TestCase("Quicksand-Regular.ttf")]
        [TestCase("Raleway-Regular.ttf")]
        [TestCase("Merriweather-Regular.ttf")]
        [TestCase("SourceSerif4-Regular.ttf")]
        [TestCase("PlayfairDisplay-Regular.ttf")]
        [TestCase("Pacifico-Regular.ttf")]
        public void UiFontFile_Exists(string fileName)
        {
            var path = Path.Combine(_fontsFolder, fileName);
            Assert.That(File.Exists(path), Is.True,
                $"UI font file missing: {fileName}");
        }

        [TestCase("NotoSans-Regular.ttf")]
        [TestCase("NotoSans-Bold.ttf")]
        [TestCase("NotoSans-Italic.ttf")]
        public void PdfFontFile_Exists(string fileName)
        {
            var path = Path.Combine(_fontsFolder, fileName);
            Assert.That(File.Exists(path), Is.True,
                $"PDF font file missing: {fileName}");
        }

        [Test]
        public void CalibriFontFile_DoesNotExist()
        {
            var files = Directory.GetFiles(_fontsFolder, "*.ttf", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .ToList();
            Assert.That(files, Has.None.EqualTo("CALIBRI.TTF").IgnoreCase,
                "CALIBRI.TTF must not be shipped — it is proprietary Microsoft IP");
        }

        [Test]
        public void AllRegisteredFonts_HaveMatchingFile()
        {
            var filesOnDisk = Directory.GetFiles(_fontsFolder, "*.ttf", SearchOption.TopDirectoryOnly)
                .Select(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant())
                .ToHashSet();

            foreach (var alias in RegisteredFonts.GetFontFamilies())
            {
                var hasMatch = filesOnDisk.Any(f => f.StartsWith(alias.ToLowerInvariant()));
                Assert.That(hasMatch, Is.True,
                    $"No .ttf file found for registered font alias '{alias}'");
            }
        }
    }
}
