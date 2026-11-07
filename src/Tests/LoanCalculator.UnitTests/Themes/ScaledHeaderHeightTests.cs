using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Themes;

namespace LoanCalculator.UnitTests.Themes
{
    /// <summary>
    /// Guards <see cref="FontScaleHandler.ScaledHeaderHeight"/>, which feeds
    /// <c>SfPopup.HeaderHeight</c> on the Disclaimer and Privacy Policy popups via
    /// <c>SettingsViewModel.PopupHeaderHeight</c>.
    /// </summary>
    /// <remarks>
    /// Those header titles scale with the Text size setting while SfPopup.HeaderHeight is a hard
    /// height that CLIPS its template rather than growing to fit it — a header height that does
    /// not track the text size is the original bug, where the title scaled up inside a box that
    /// did not and SfPopup clipped the overflow away.
    ///
    /// The arithmetic is tested here rather than on SettingsViewModel.PopupHeaderHeight itself
    /// because that view model cannot be constructed from a unit test at all: its parameterless
    /// constructor reaches ServiceLocator for AppInformation, which throws
    /// ArgumentNullException("provider") with no DI container behind it. The platform choice the
    /// view model makes on top of this (which padding constant to pass) is device-only.
    /// </remarks>
    [TestFixture]
    public class ScaledHeaderHeightTests
    {
        [TearDown]
        public void ResetCurrentSize() => FontScaleHandler.ApplyTextSize(AppTextSize.Default).GetAwaiter().GetResult();

        [Test]
        public async Task ScaledHeaderHeight_GrowsWithTheTextSizeSetting()
        {
            await FontScaleHandler.ApplyTextSize(AppTextSize.Small);
            var small = FontScaleHandler.ScaledHeaderHeight(40);

            await FontScaleHandler.ApplyTextSize(AppTextSize.ExtraLarge);
            var extraLarge = FontScaleHandler.ScaledHeaderHeight(40);

            Assert.That(extraLarge, Is.GreaterThan(small));
        }

        [Test]
        public void ScaledHeaderHeight_LeavesRoomForTwoLinesOfTitleBeyondThePadding()
        {
            // Two lines, not one: "Privacy Policy" is the longer of the two titles and those
            // labels are WordWrap, so a wrap at a large size must not clip either.
            var titleLine = FontScaleHandler.BaseSizeFor("FontSizeXL") * FontScaleHandler.CurrentFactor;

            Assert.That(FontScaleHandler.ScaledHeaderHeight(40), Is.EqualTo(40 + (titleLine * 2)));
        }

        [Test]
        public void ScaledHeaderHeight_AddsThePaddingItIsGiven()
        {
            // The two callers differ only by padding — 70 where a status bar / notch has to be
            // cleared, 40 elsewhere — so the difference between them must be exactly that.
            Assert.That(
                FontScaleHandler.ScaledHeaderHeight(70) - FontScaleHandler.ScaledHeaderHeight(40),
                Is.EqualTo(30));
        }
    }
}
