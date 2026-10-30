using System.Text.RegularExpressions;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Themes;
using Pj.Library;

namespace LoanCalculator.UnitTests.Themes
{
    /// <summary>
    /// Guards Stage 3 of the font-size feature — the part that actually moves pixels.
    /// </summary>
    /// <remarks>
    /// The one risk worth a real test here is the base-size table in FontScaleHandler drifting
    /// from Theme.CommonStyles.xaml, since it is a manually-kept mirror rather than something read
    /// back from the file at runtime (see FontScaleHandler's own remarks for why that duplication
    /// was chosen anyway). <see cref="BaseSizeTable_MatchesTheXamlFile"/> is what turns a drifted
    /// number into a build-time failure instead of a silent mismatch nobody notices until a device
    /// shows the wrong size.
    /// </remarks>
    [TestFixture]
    public class FontScaleHandlerTests
    {
        // CurrentSize is static, process-wide state — the two tests that set it must not leak
        // their value into whatever test NUnit happens to run next, the same class of bug
        // CLAUDE.md records for SharedServiceCore.LoadSafe elsewhere in this suite.
        [TearDown]
        public void ResetCurrentSize() => FontScaleHandler.ApplyTextSize(AppTextSize.Default).GetAwaiter().GetResult();

        private static Dictionary<string, (int Phone, int Tablet, int Desktop, int TV, int Watch, int Default)> ParseXamlTable()
        {
            var path = Path.Combine(
                PjUtility.Runtime.ExecutingRepositoryRootFolder,
                "src", "LoanCalculator", "Extensions", "Data", "Theme.CommonStyles.xaml");

            var xml = File.ReadAllText(path);
            var pattern = new Regex(
                "x:Key=\"((?:FontSize|LoanAppText)[A-Za-z]*)\"\\s*\\n\\s*x:TypeArguments=\"x:Double\"\\s*\\n\\s*" +
                "Phone=\"(\\d+)\" Tablet=\"(\\d+)\" Desktop=\"(\\d+)\" TV=\"(\\d+)\" Watch=\"(\\d+)\" Default=\"(\\d+)\"");

            var table = new Dictionary<string, (int, int, int, int, int, int)>();
            foreach (Match m in pattern.Matches(xml))
            {
                table[m.Groups[1].Value] = (
                    int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value),
                    int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value));
            }
            return table;
        }

        [Test]
        public void BaseSizeTable_MatchesTheXamlFile()
        {
            var fromXaml = ParseXamlTable();

            Assert.That(fromXaml, Is.Not.Empty, "the XAML parser found nothing — the regex or the file path has drifted");

            Assert.That(FontScaleHandler.KnownKeys, Is.EquivalentTo(fromXaml.Keys),
                "FontScaleHandler's table and the XAML file disagree on WHICH keys exist — " +
                "a key was added to one and not the other");

            // All six idiom columns, via the internal overload that takes an explicit idiom —
            // DeviceInfo.Idiom itself cannot be substituted from here, so without this overload
            // only whichever one idiom the test host happens to report would ever be checked.
            (DeviceIdiom Idiom, string Column)[] idioms =
            [
                (DeviceIdiom.Phone, "Phone"), (DeviceIdiom.Tablet, "Tablet"), (DeviceIdiom.Desktop, "Desktop"),
                (DeviceIdiom.TV, "TV"), (DeviceIdiom.Watch, "Watch"), (DeviceIdiom.Unknown, "Default"),
            ];

            Assert.Multiple(() =>
            {
                foreach (var (key, xamlValues) in fromXaml)
                {
                    foreach (var (idiom, column) in idioms)
                    {
                        var expected = column switch
                        {
                            "Phone" => xamlValues.Phone, "Tablet" => xamlValues.Tablet,
                            "Desktop" => xamlValues.Desktop, "TV" => xamlValues.TV,
                            "Watch" => xamlValues.Watch, _ => xamlValues.Default,
                        };

                        Assert.That(FontScaleHandler.BaseSizeFor(key, idiom), Is.EqualTo(expected),
                            $"{key}.{column}: FontScaleHandler disagrees with Theme.CommonStyles.xaml");
                    }
                }
            });
        }

        [Test]
        public void FactorFor_DefaultIsUnity()
        {
            Assert.That(FontScaleHandler.FactorFor(AppTextSize.Default), Is.EqualTo(1.0),
                "Default must be the unscaled, today's-sizes baseline — anyone who has never " +
                "touched the setting must see no change");
        }

        [Test]
        public void FactorFor_StepsAreMonotonicallyIncreasing()
        {
            var small = FontScaleHandler.FactorFor(AppTextSize.Small);
            var @default = FontScaleHandler.FactorFor(AppTextSize.Default);
            var large = FontScaleHandler.FactorFor(AppTextSize.Large);
            var extraLarge = FontScaleHandler.FactorFor(AppTextSize.ExtraLarge);

            Assert.That(new[] { small, @default, large, extraLarge }, Is.Ordered.Ascending,
                "Small < Default < Large < Extra Large must hold, or the Settings combo's order " +
                "would not match what moving down the list actually does");
        }

        [TestCase("LoanAppTextCaption")]
        [TestCase("FontSizeBody")]
        [TestCase("LoanAppTextHeroNet")]
        public void BaseSizeFor_UnknownKey_ReturnsZeroNotAnException(string knownKey)
        {
            // Sanity: known keys resolve to a positive number...
            Assert.That(FontScaleHandler.BaseSizeFor(knownKey), Is.GreaterThan(0));

            // ...and a typo'd or since-removed key fails safely rather than throwing, which matters
            // because ApplyTextSize loops over its OWN known-key list, not caller input — but a
            // future caller probing an arbitrary string must not crash the Settings page over it.
            Assert.That(FontScaleHandler.BaseSizeFor("NotARealKey"), Is.Zero);
        }

        /// <summary>
        /// Without a MAUI host, <c>Application.Current</c> is null, so ApplyTextSize cannot
        /// actually write anywhere — this pins that it degrades to a no-op rather than throwing,
        /// which is what lets every other test in this fixture call FontScaleHandler at all. The
        /// real write path (Application.Current.Resources[key] = value) can only be verified on
        /// device, same limitation CLAUDE.md records for Preferences-backed settings generally.
        /// </summary>
        [Test]
        public void ApplyTextSize_WithNoMauiHost_DoesNotThrow()
        {
            Assert.DoesNotThrowAsync(async () => await FontScaleHandler.ApplyTextSize(AppTextSize.Large));
        }

        // ── CurrentSize / CurrentFactor ───────────────────────────────────────
        //
        // BoolToDisclaimerFontSizeConverter (and anything else that cannot be a markup
        // DynamicResource) reads these to approximate the live scale. Covered separately from the
        // no-host test above because what matters here is specifically that the tracked VALUE is
        // right, not just that nothing throws.

        [Test]
        public async Task ApplyTextSize_UpdatesCurrentSize_EvenWithNoMauiHost()
        {
            await FontScaleHandler.ApplyTextSize(AppTextSize.ExtraLarge);

            Assert.That(FontScaleHandler.CurrentSize, Is.EqualTo(AppTextSize.ExtraLarge),
                "CurrentSize must track what was requested even when there is no Application.Current " +
                "to actually write to — a converter reading it should see the latest request " +
                "regardless of whether the resource write itself could complete");
        }

        [Test]
        public async Task CurrentFactor_MatchesFactorForCurrentSize()
        {
            await FontScaleHandler.ApplyTextSize(AppTextSize.Small);

            Assert.That(FontScaleHandler.CurrentFactor, Is.EqualTo(FontScaleHandler.FactorFor(AppTextSize.Small)));
        }
    }
}
