using Microsoft.Maui.Controls;
using Syncfusion.Maui.Sliders;

namespace LoanCalculatorMaui.Extensions
{
    /// <summary>
    /// Syncfusion sliders resolve the colors inside their ThumbStyle / TrackStyle / LabelStyle
    /// sub-objects from DynamicResource ONCE and then cache them — they do not pick up a live
    /// theme change the way a plain {DynamicResource} on a control property does. This walks a
    /// page's visual tree, finds every SfSlider, and re-applies the standard theme colors from
    /// the current Application.Current.Resources so a theme switch is reflected on return.
    ///
    /// All sliders in this app use the same standard resource keys (see the *View.xaml files),
    /// so we re-apply those known keys rather than trying to recover each slider's original key.
    /// </summary>
    public static class SliderThemeRefresher
    {
        public static void Refresh(Element? root)
        {
            if (root == null) return;
            try
            {
                foreach (var slider in Descendants<SfSlider>(root))
                    ApplyTheme(slider);
            }
            catch
            {
                // Cosmetic refresh only — never let it throw into OnAppearing.
            }
        }

        private static void ApplyTheme(SfSlider slider)
        {
            var res = Application.Current?.Resources;
            if (res == null) return;

            Color? C(string key) => res.TryGetValue(key, out var v) && v is Color c ? c : null;

            // Thumb/Track Fill/Stroke properties are Brush; Label text colors are Color.
            var knobFill = C("LoanAppSliderKnobFillColor");
            var knobBorder = C("LoanAppSliderKnobBorderColor");
            var knobOverlap = C("LoanAppSliderKnobOverlapColor");
            var trackActive = C("LoanAppRangeSliderTrackActiveFillColor");
            var trackInactive = C("LoanAppRangeSliderTrackInActiveFillColor");
            var labelText = C("LoanAppRangeSliderLabelTextColor");

            if (knobFill != null || knobBorder != null || knobOverlap != null)
            {
                slider.ThumbStyle ??= new SliderThumbStyle();
                if (knobFill != null) slider.ThumbStyle.Fill = new SolidColorBrush(knobFill);
                if (knobBorder != null) slider.ThumbStyle.Stroke = new SolidColorBrush(knobBorder);
                if (knobOverlap != null) slider.ThumbStyle.OverlapStroke = new SolidColorBrush(knobOverlap);
            }

            if (trackActive != null || trackInactive != null)
            {
                slider.TrackStyle ??= new SliderTrackStyle();
                if (trackActive != null) slider.TrackStyle.ActiveFill = new SolidColorBrush(trackActive);
                if (trackInactive != null) slider.TrackStyle.InactiveFill = new SolidColorBrush(trackInactive);
            }

            if (labelText != null)
            {
                slider.LabelStyle ??= new SliderLabelStyle();
                slider.LabelStyle.ActiveTextColor = labelText;
                slider.LabelStyle.InactiveTextColor = labelText;
            }
        }

        private static IEnumerable<T> Descendants<T>(Element root) where T : Element
        {
            foreach (var child in GetChildren(root))
            {
                if (child is T match) yield return match;
                foreach (var nested in Descendants<T>(child))
                    yield return nested;
            }
        }

        private static IEnumerable<Element> GetChildren(Element element)
        {
            // Covers the container types used in these pages.
            switch (element)
            {
                case IVisualTreeElement vte:
                    foreach (var c in vte.GetVisualChildren())
                        if (c is Element e) yield return e;
                    yield break;
            }
        }
    }
}
