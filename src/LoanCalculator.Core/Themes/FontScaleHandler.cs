using LoanCalculator.Core.Models.Enums;

namespace LoanCalculator.Core.Themes
{
    /// <summary>
    /// Stage 3 of the font-size feature: applies the Settings ▸ Text size choice to every
    /// scalable font-size resource in the app.
    /// </summary>
    /// <remarks>
    /// <para><b>What "every" means.</b> Stage 2 converted all 8 original FontSizeXxx tokens and
    /// introduced 13 new semantic/hero/chart tokens — 21 keys in total — and routed every markup
    /// consumer in the app onto one of them via <c>DynamicResource</c>. This class is the thing
    /// that actually rewrites those 21 resources when the user changes the setting; nothing before
    /// this class made any of that work do anything visible.</para>
    ///
    /// <para><b>Why the per-idiom base table is duplicated here rather than read back from
    /// Resources.</b> Each key's XAML definition is an <c>OnIdiom</c> value — Phone/Tablet/Desktop/
    /// TV/Watch/Default — resolved to one fixed double the moment the resource dictionary loads,
    /// for whichever idiom the device actually is. Reading that resolved double back out of
    /// <see cref="Application.Current"/>.Resources to use as "the base to scale from" is fragile:
    /// it depends on nothing having scaled it already this session, which is exactly the condition
    /// that stops holding the second time a scale is applied. A fixed table, keyed by idiom, keyed
    /// again by which scale is active, removes that ordering dependency entirely — applying a scale
    /// is always "idiom's base value for this key, times this factor", never "whatever happens to
    /// be in Resources right now, times this factor".</para>
    ///
    /// <para><b>The table is a manually-kept mirror of Theme.CommonStyles.xaml, and that is a real
    /// duplication risk</b> — a number changed in one place and not the other silently drifts.
    /// <c>FontScaleHandlerTests.BaseSizeTable_MatchesTheXamlFile</c> parses the live XAML file and
    /// asserts every key and every idiom value here matches it exactly, so drift is a build-time
    /// test failure rather than a silent runtime mismatch.</para>
    ///
    /// <para><b>Why this has to re-run after every theme load, not just once at startup.</b>
    /// <c>ThemeHandler.LoadDefaultStyle</c> calls <c>ClearAllResources("LoanApp")</c> on every
    /// theme switch, which removes any top-level override on the 13 "LoanApp"-prefixed keys this
    /// class has written — their un-scaled XAML value then comes back as soon as the freshly
    /// re-added Theme.CommonStyles.xaml dictionary is merged in. The other 8 keys are not
    /// "LoanApp"-prefixed, so they are never swept and a stale scaled value on them would
    /// technically survive a theme change on its own — but this re-applies all 21 every time
    /// regardless, for one predictable behaviour instead of two.</para>
    /// </remarks>
    public static class FontScaleHandler
    {
        /// <summary>
        /// One row per scalable key: Phone, Tablet, Desktop, TV, Watch, Default — in that order,
        /// matching the attribute order every OnIdiom in Theme.CommonStyles.xaml is written in.
        /// </summary>
        private static readonly Dictionary<string, (double Phone, double Tablet, double Desktop, double TV, double Watch, double Default)> BaseSizes = new()
        {
            { "FontSizeXS", (10, 15, 15, 15, 8, 10) },
            { "FontSizeSmall", (11, 16, 16, 16, 9, 11) },
            { "FontSizeBody", (12, 18, 18, 18, 10, 12) },
            { "FontSizeMedium", (14, 20, 20, 20, 12, 14) },
            { "FontSizeLarge", (15, 22, 22, 22, 13, 15) },
            { "FontSizeXL", (18, 26, 26, 26, 15, 18) },
            { "FontSizeHeading", (20, 28, 28, 28, 16, 20) },
            { "FontSizeFigure", (22, 34, 34, 34, 18, 22) },
            { "LoanAppTextCaption", (12, 20, 16, 16, 10, 12) },
            { "LoanAppTextBody", (12, 18, 18, 18, 10, 12) },
            { "LoanAppTextCaptionCompact", (11, 16, 16, 16, 9, 11) },
            { "LoanAppTextChartAxis", (12, 18, 18, 18, 10, 12) },
            { "LoanAppTextChartLegend", (10, 15, 15, 15, 8, 10) },
            { "LoanAppTextChartMarker", (9, 16, 16, 16, 7, 9) },
            { "LoanAppTextValue", (18, 32, 32, 32, 14, 18) },
            { "LoanAppTextLabel", (11, 20, 20, 20, 9, 11) },
            { "LoanAppTextCell", (11, 18, 18, 18, 9, 11) },
            { "LoanAppTextHeroNet", (32, 56, 56, 56, 25, 32) },
            { "LoanAppTextHeroVerdict", (28, 50, 50, 50, 22, 28) },
            { "LoanAppTextHeroAnswer", (30, 53, 53, 53, 23, 30) },
            { "LoanAppTextHeroAnswerTail", (15, 27, 27, 27, 12, 15) },
        };

        /// <summary>
        /// Every key this handler knows how to scale — exposed so a test can assert the table
        /// above is complete against the live theme file, and so nothing silently falls outside
        /// the feature by being added to the XAML but forgotten here.
        /// </summary>
        public static IReadOnlyCollection<string> KnownKeys => BaseSizes.Keys;

        /// <summary>
        /// The multiplier for each step. Small text people actually use day to day is the anchor
        /// (1.0, unchanged from today); the other three are even steps either side of it, close to
        /// what iOS/Android's own system text-size settings use, so nothing here invents an
        /// unfamiliar scale.
        /// </summary>
        /// <summary>
        /// The scale <see cref="ApplyTextSize"/> was last asked to apply this session. Defaults to
        /// <see cref="AppTextSize.Default"/> (factor 1.0, today's sizes) before it has ever run —
        /// i.e. before the saved preference has loaded, which happens once per launch via
        /// ThemeHandler.LoadSavedTextSizeAsync.
        /// </summary>
        /// <remarks>
        /// Exists for consumers that CANNOT be a markup DynamicResource — <c>BoolToDisclaimerFontSizeConverter</c>
        /// is the one example today. A value converter only re-runs when its OWN bound source
        /// property changes, never because some unrelated resource changed elsewhere, so it has
        /// no way to react live to a text-size change while already on screen; reading this lets
        /// it at least pick up the CURRENT setting the next time it is asked to convert (e.g. the
        /// next time that popup opens), rather than being permanently stuck at whatever the app's
        /// built-in literal was.
        /// </remarks>
        public static AppTextSize CurrentSize { get; private set; } = AppTextSize.Default;

        /// <summary>Convenience for <c>FactorFor(CurrentSize)</c> — see <see cref="CurrentSize"/>.</summary>
        public static double CurrentFactor => FactorFor(CurrentSize);

        public static double FactorFor(AppTextSize size) => size switch
        {
            AppTextSize.Small => 0.9,
            AppTextSize.Default => 1.0,
            AppTextSize.Large => 1.15,
            AppTextSize.ExtraLarge => 1.3,
            _ => 1.0,
        };

        /// <summary>
        /// The base (Default-scale) size for one key on the current device's idiom. Exposed for
        /// tests; production code should call <see cref="ApplyTextSize"/> instead.
        /// </summary>
        public static double BaseSizeFor(string key) => BaseSizeFor(key, DeviceInfo.Idiom);

        /// <summary>
        /// The base size for one key on an EXPLICIT idiom — the testable form.
        /// <see cref="DeviceInfo"/>.Idiom cannot be substituted from a unit test (it is a sealed,
        /// static MAUI API), so a test asserting the Tablet/Desktop/TV/Watch columns — not just
        /// whichever idiom the test host happens to report — needs this overload rather than the
        /// one callers actually use.
        /// </summary>
        internal static double BaseSizeFor(string key, DeviceIdiom idiom)
        {
            if (!BaseSizes.TryGetValue(key, out var sizes)) return 0;

            return idiom switch
            {
                var i when i == DeviceIdiom.Phone => sizes.Phone,
                var i when i == DeviceIdiom.Tablet => sizes.Tablet,
                var i when i == DeviceIdiom.Desktop => sizes.Desktop,
                var i when i == DeviceIdiom.TV => sizes.TV,
                var i when i == DeviceIdiom.Watch => sizes.Watch,
                _ => sizes.Default,
            };
        }

        /// <summary>
        /// Writes every known key's scaled size onto <c>Application.Current.Resources</c>, so every
        /// <c>DynamicResource</c> consumer in the app picks it up immediately.
        /// </summary>
        /// <remarks>
        /// <para>Rounded to the nearest whole point and floored at 1 — a fractional or
        /// zero/negative font size is not a case any consumer here is written to expect, and
        /// floating-point multiplication of an int-looking base (e.g. 11 * 0.9 = 9.9) would
        /// otherwise hand one out by accident.</para>
        ///
        /// <para><b>Async, and yields between every single write, for a reason that is not
        /// obvious from the loop itself.</b> Each <c>resources[key] = value</c> assignment fires
        /// DynamicResource's change notification SYNCHRONOUSLY and IMMEDIATELY — every bound
        /// element across the whole app that consumes that one key re-measures and re-lays-out
        /// before the next line of this method runs. 21 keys means 21 of those cascades back to
        /// back with nothing letting the UI thread render a frame in between, which is long enough
        /// to read as the main thread freezing solid: the busy spinner stops animating (it cannot
        /// progress without a frame either), and whatever the semi-transparent overlay let show
        /// through behind it renders in some mid-cascade, half-updated state — which is what a
        /// screenshot of this bug showed as "flickering with blank and appearing".</para>
        ///
        /// <para><c>await Task.Yield()</c> is the right tool specifically because the resource
        /// write MUST stay on the UI thread — this is not work that can move to a background
        /// thread. Yield does not move it; it posts the continuation back through the same
        /// SynchronizationContext after giving the message loop a chance to process pending work,
        /// which includes painting a frame. 21 yields is cheap enough to not meaningfully lengthen
        /// the total time, and turns one unbroken freeze into 21 points where the UI can actually
        /// repaint — including the spinner itself.</para>
        /// </remarks>
        public static async Task ApplyTextSize(AppTextSize size)
        {
            CurrentSize = size;

            var resources = Application.Current?.Resources;
            if (resources == null) return;

            var factor = FactorFor(size);

            foreach (var key in BaseSizes.Keys)
            {
                var scaled = Math.Max(1, Math.Round(BaseSizeFor(key) * factor));
                resources[key] = scaled;
                await Task.Yield();
            }
        }
    }
}
