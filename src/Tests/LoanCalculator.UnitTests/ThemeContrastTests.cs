using System.Globalization;
using System.Text.RegularExpressions;
using Pj.Library;

namespace LoanCalculator.UnitTests
{
    /// <summary>
    /// Asserts that text colours actually contrast with the surface they are drawn on, in every
    /// theme.
    /// </summary>
    /// <remarks>
    /// <para>Written after the Budget search boxes turned out to be unreadable: the Forest theme set
    /// the autocomplete's text to <c>#183d3d</c>, a dark green, over its dark green background —
    /// 1.37:1, effectively invisible — and the Dark theme's placeholder was <c>#222831</c>, the
    /// exact hex of the background behind it, so 1.00:1. Both were authored as if for a light
    /// theme.</para>
    ///
    /// <para>The existing <c>ThemesTests</c> guards that all four themes define the same KEYS. It
    /// cannot catch this, because every key was present and correctly spelled — only the values were
    /// wrong. Nothing else in the suite looks at what a colour actually is.</para>
    ///
    /// <para>4.5:1 is the WCAG AA threshold for body text. Placeholders are held to 3:1, the
    /// large-text/non-essential threshold, since they are meant to recede — but 1.00:1 is not
    /// receding, it is absent.</para>
    /// </remarks>
    [TestFixture]
    public class ThemeContrastTests
    {
        private static readonly string[] Themes = ["Dark", "Light", "Forest", "Warm"];

        private const double BodyTextMinimum = 4.5;
        private const double RecedingTextMinimum = 3.0;

        /// <summary>
        /// The floor for the big bold figures in the summary boxes.
        /// </summary>
        /// <remarks>
        /// 3:1, not 4.5:1, and that is WCAG's own number rather than a concession to make these
        /// pass. SC 1.4.3 sets the large-scale text threshold at 18pt, or 14pt when bold;
        /// SpanThreeBoxAmountStyle is 18pt Bold on a phone and 32pt on a tablet, so it clears that
        /// comfortably and 3:1 is the applicable minimum.
        ///
        /// Worth recording why this constant exists at all: when these pairs were first asserted
        /// at BodyTextMinimum, the ORDINARY figure colour failed too (LoanAppHighlightFgColor came
        /// out at 3.85:1 in Light and 4.02:1 in Forest) — and that colour predates the negative one
        /// by a long way. Two independent colours failing the same way is a sign the floor is wrong
        /// for the element, not that both colours are wrong.
        /// </remarks>
        private const double LargeBoldFigureMinimum = 3.0;

        private static string ThemeFolder => Path.Combine(
            PjUtility.Runtime.ExecutingRepositoryRootFolder,
            "src", "LoanCalculator", "Extensions", "Data");

        private static string? ColourOf(string theme, string key)
        {
            var path = Path.Combine(ThemeFolder, $"Theme.{theme}.xaml");
            if (!File.Exists(path)) return null;

            var match = Regex.Match(File.ReadAllText(path), $"x:Key=\"{Regex.Escape(key)}\">([^<]+)<");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        /// <summary>Relative luminance per WCAG 2.1.</summary>
        private static double Luminance(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 8) hex = hex[2..];   // ARGB — drop the alpha

            double Channel(int offset)
            {
                var c = int.Parse(hex.Substring(offset, 2), NumberStyles.HexNumber) / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(0) + 0.7152 * Channel(2) + 0.0722 * Channel(4);
        }

        private static double ContrastRatio(string foreground, string background)
        {
            var a = Luminance(foreground);
            var b = Luminance(background);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        private static bool IsHex(string? value) => value is not null && value.StartsWith('#');

        /// <summary>
        /// The pairs worth asserting: a foreground key, the surface it is drawn on, and the floor it
        /// has to clear. Deliberately a short list of places where a reader has to read something,
        /// rather than every colour in the file.
        /// </summary>
        private static IEnumerable<TestCaseData> ReadabilityPairs()
        {
            (string Fg, string Bg, double Min, string What)[] pairs =
            [
                ("LoanAppAutoCompleteTextColor", "LoanAppAppShellBgColor", BodyTextMinimum,
                    "text typed into a search box"),
                ("LoanAppAutoCompleteWaterColor", "LoanAppAppShellBgColor", RecedingTextMinimum,
                    "a search box's placeholder"),
                ("LoanAppAutoCompleteDropdownTextColor", "LoanAppAutoCompleteDropdownBackgroundColor",
                    BodyTextMinimum, "a search suggestion"),
                ("LoanAppDefaultForegroundColor", "LoanAppDefaultBackgroundColor", BodyTextMinimum,
                    "ordinary body text"),
                ("LoanAppInputTextFgColor", "LoanAppAppShellBgColor", BodyTextMinimum,
                    "text typed into an input"),

                // The summary boxes. Their background is a gradient, so both ends are checked —
                // a colour that clears one stop can fail the other, and the figure sits across
                // the whole card.
                //
                // These matter because the negative colour is the one place a reader is told
                // something is wrong, and it is theme-specific: the highlight card is LIGHT in
                // all four themes (Dark included, #9DB2BF to #DDE6ED), which is why
                // LoanAppNegativeValueOnHighlightColor is a DARK red while the plain
                // LoanAppNegativeValueColor is a light one. Getting those two the wrong way round
                // would produce red-on-red, and nothing else would catch it.
                ("LoanAppNegativeValueOnHighlightColor", "LoanAppBorderTopGradientBackgroundStart",
                    LargeBoldFigureMinimum, "a negative figure in a summary box"),
                ("LoanAppNegativeValueOnHighlightColor", "LoanAppBorderTopGradientBackgroundEnd",
                    LargeBoldFigureMinimum, "a negative figure in a summary box, far edge"),
                ("LoanAppHighlightFgColor", "LoanAppBorderTopGradientBackgroundStart",
                    LargeBoldFigureMinimum, "an ordinary figure in a summary box"),
                ("LoanAppHighlightFgColor", "LoanAppBorderTopGradientBackgroundEnd",
                    LargeBoldFigureMinimum, "an ordinary figure in a summary box, far edge"),
            ];

            foreach (var theme in Themes)
                foreach (var (fg, bg, min, what) in pairs)
                    yield return new TestCaseData(theme, fg, bg, min, what)
                        .SetName($"{what} is readable in {theme} ({fg} on {bg})");
        }

        [TestCaseSource(nameof(ReadabilityPairs))]
        public void ColourPair_MeetsItsContrastFloor(
            string theme, string foregroundKey, string backgroundKey, double minimum, string what)
        {
            var fg = ColourOf(theme, foregroundKey);
            var bg = ColourOf(theme, backgroundKey);

            Assume.That(IsHex(fg) && IsHex(bg),
                $"{theme}: {foregroundKey} or {backgroundKey} is not a plain hex value "
                + $"({fg} / {bg}) — named colours and brushes are out of scope here");

            var ratio = ContrastRatio(fg!, bg!);

            Assert.That(ratio, Is.GreaterThanOrEqualTo(minimum),
                $"In the {theme} theme, {what} is {fg} on {bg} — a contrast ratio of "
                + $"{ratio:F2}:1, below the {minimum:F1}:1 floor. This is how the Budget search "
                + "boxes ended up unreadable: the key existed and was spelled correctly, so the "
                + "key-parity test passed while the value was wrong for the theme.");
        }

        /// <summary>
        /// A foreground identical to its background is the specific mistake the Dark theme's
        /// placeholder made, and it is worth naming separately — a ratio assertion reports it as a
        /// number, this reports it as the copy-paste it was.
        /// </summary>
        [TestCaseSource(nameof(ReadabilityPairs))]
        public void ColourPair_IsNotTheSameColourTwice(
            string theme, string foregroundKey, string backgroundKey, double minimum, string what)
        {
            var fg = ColourOf(theme, foregroundKey);
            var bg = ColourOf(theme, backgroundKey);

            Assume.That(IsHex(fg) && IsHex(bg));

            Assert.That(fg, Is.Not.EqualTo(bg).IgnoreCase,
                $"In the {theme} theme, {what} uses the very colour it is drawn on ({fg}).");
        }
    }
}
