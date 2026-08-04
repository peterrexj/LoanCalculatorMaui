using LoanCalculator.Core.Constants;

namespace LoanCalculator.UnitTests.Constants
{
    [TestFixture]
    public class RegisteredFontsTests
    {
        [Test]
        public void GetFontFamilies_IsNotEmpty()
        {
            Assert.That(RegisteredFonts.GetFontFamilies(), Is.Not.Empty);
        }

        [Test]
        public void GetFontFamilies_ContainsNoDuplicates()
        {
            var fonts = RegisteredFonts.GetFontFamilies();
            Assert.That(fonts.Distinct().Count(), Is.EqualTo(fonts.Count));
        }

        [Test]
        public void GetFontFamilies_ContainsNoNullOrEmpty()
        {
            var fonts = RegisteredFonts.GetFontFamilies();
            Assert.That(fonts, Has.None.Null.Or.Empty);
        }

        [Test]
        public void DefaultFontFamily_IsLato()
        {
            Assert.That(RegisteredFonts.DefaultFontFamily, Is.EqualTo("Lato"));
        }

        [Test]
        public void DefaultFontFamily_IsInFontList()
        {
            Assert.That(RegisteredFonts.GetFontFamilies(), Contains.Item(RegisteredFonts.DefaultFontFamily));
        }

        [Test]
        public void GetFontFamilies_DoesNotContainPdfFonts()
        {
            var fonts = RegisteredFonts.GetFontFamilies();
            Assert.That(fonts, Has.None.StartWith("NotoSans"));
            Assert.That(fonts, Has.None.StartWith("OpenSans"));
        }

        [Test]
        public void GetFontFamilies_DoesNotContainCalbri()
        {
            var fonts = RegisteredFonts.GetFontFamilies();
            Assert.That(fonts, Has.None.EqualTo("Calibri").IgnoreCase);
        }

        [TestCase("Lato")]
        [TestCase("Nunito")]
        [TestCase("Quicksand")]
        [TestCase("Raleway")]
        [TestCase("Merriweather")]
        [TestCase("SourceSerif4")]
        [TestCase("PlayfairDisplay")]
        [TestCase("Pacifico")]
        public void GetFontFamilies_ContainsExpectedFont(string fontName)
        {
            Assert.That(RegisteredFonts.GetFontFamilies(), Contains.Item(fontName));
        }
    }
}
