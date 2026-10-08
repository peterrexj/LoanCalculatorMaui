#if IOS || MACCATALYST
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.Expander;
using Syncfusion.Maui.TabView;

namespace LoanCalculatorMaui.Extensions;

/// <summary>
/// Workarounds for iOS-only defects in Syncfusion 33.2.6 where a control renders correctly but
/// silently ignores taps, because a pure drawing view is stacked on top of the interactive views
/// and is itself touch-enabled while owning no gesture recognizers. iOS hit-testing returns the
/// topmost touch-enabled view and stops, so the touch is accepted and then dropped. Android
/// dispatches touches differently and is unaffected.
/// </summary>
internal static class SyncfusionIosTouchFix
{
    /// <summary>
    /// <see cref="SfTabView"/>: tapping the header strip does nothing, so tabs cannot be changed.
    /// </summary>
    /// <remarks>
    /// <code>
    /// LayoutView      {0, 0, W x 48}   header slot
    /// | ContentView   {0, 0, W x 48}
    /// | | MauiScrollView {0,0,W x 48}  real tab bar   gestures = 7  ← should get the tap
    /// LayoutViewExt   {0, 0, W x 48}   drawing layer  gestures = 0  ← LAST sibling = topmost
    /// | NativePlatformGraphicsView     UserInteractionEnabled = false
    /// </code>
    /// </remarks>
    internal static void ApplyToTabView(SfTabView? tabView)
    {
        if (tabView?.Handler?.PlatformView is not UIKit.UIView root)
            return;

        DisableHeaderDrawingOverlay(root, depth: 0);
    }

    // The overlay sits shallowly (depth 2) under the tab view, so recursion is capped to stay
    // well clear of tab *content* — charts and grids also draw into graphics views and must
    // keep receiving touches.
    private static void DisableHeaderDrawingOverlay(UIKit.UIView view, int depth)
    {
        const int maxDepth = 3;
        const int headerHeightLimit = 120;

        if (view.GetType().Name == "LayoutViewExt"
            && view.UserInteractionEnabled
            && (view.GestureRecognizers?.Length ?? 0) == 0
            && view.Frame.Height <= headerHeightLimit
            && view.Subviews.Length == 1
            && view.Subviews[0].GetType().Name == "NativePlatformGraphicsView")
        {
            view.UserInteractionEnabled = false;
        }

        if (depth >= maxDepth)
            return;

        foreach (var child in view.Subviews)
            DisableHeaderDrawingOverlay(child, depth + 1);
    }

    /// <summary>
    /// <see cref="SfSegmentedControl"/>: tapping a segment does nothing, so the selection is stuck.
    /// </summary>
    /// <remarks>
    /// Here two drawing views blanket the whole control as the last siblings:
    /// <code>
    /// MauiScrollView            {0,0, 341x40}      gestures = 7   segment host
    /// | LayoutViewExt           {1,0, 111x40}      gestures = 4   ← each segment, should get the tap
    /// | LayoutViewExt           {114,0, 111x40}    gestures = 4
    /// | LayoutViewExt           {228,0, 111x40}    gestures = 4
    /// PlatformGraphicsViewExt   {3,3, 343x44}      gestures = 0   ← topmost, covers everything
    /// PlatformGraphicsViewExt   {0,0, 349x50}      gestures = 0   ← topmost, covers everything
    /// </code>
    /// Scoped to this control's own subtree, so graphics views elsewhere (charts, which do need
    /// touches for tooltips and selection) are never altered. Syncfusion already ships the
    /// per-segment graphics views with interaction disabled, which confirms drawing layers here
    /// have no need for touches.
    /// </remarks>
    internal static void ApplyToSegmentedControl(SfSegmentedControl? control)
    {
        if (control?.Handler?.PlatformView is not UIKit.UIView root)
            return;

        DisableGraphicsOverlays(root);
    }

    private static void DisableGraphicsOverlays(UIKit.UIView view)
    {
        var typeName = view.GetType().Name;

        if (view.UserInteractionEnabled
            && (view.GestureRecognizers?.Length ?? 0) == 0
            && (typeName == "PlatformGraphicsViewExt" || typeName == "NativePlatformGraphicsView"))
        {
            view.UserInteractionEnabled = false;
        }

        foreach (var child in view.Subviews)
            DisableGraphicsOverlays(child);
    }

    /// <summary>
    /// <see cref="SfExpander"/>: tapping the header does not expand or collapse it.
    /// </summary>
    /// <remarks>
    /// <para>The third control in this family, after <see cref="SfTabView"/> and
    /// <see cref="SfSegmentedControl"/>. None of this app's ~39 expanders carry an
    /// <c>x:Name</c> — they are repeated rows inside Settings and the Loan tabs — so unlike the
    /// other two fixes there is no control to hand in. This walks the page's visual tree to find
    /// them.</para>
    /// <para>Only the <b>header</b> subtree is touched. Expander content holds charts and data
    /// grids, which draw into graphics views of their own and must keep receiving touches for
    /// tooltips and selection.</para>
    /// </remarks>
    internal static void ApplyToExpanders(Element? root)
    {
        if (root is not IVisualTreeElement tree) return;

        foreach (var descendant in tree.GetVisualTreeDescendants())
        {
            if (descendant is SfExpander expander)
                ApplyToExpanderHeader(expander);
        }
    }

    private static void ApplyToExpanderHeader(SfExpander expander)
    {
        if (expander.Handler?.PlatformView is not UIKit.UIView root) return;

        // Only the expander's own DIRECT subviews are considered, so content — charts, data grids,
        // which need their graphics-view touches — is never reached.
        //
        // Measured tree, collapsed expander on the Loan page:
        //   LayoutViewExt (402x30)                     expander root
        //   | LayoutViewExt (402x30) gestures=4        header, the real tap target
        //   | LayoutViewExt (402x0)  gestures=0        content, zero-height while collapsed
        //   | LayoutViewExt (402x30) gestures=5        ← drawing layer, LAST sibling = topmost
        //   | | NativePlatformGraphicsView  touch=False
        //
        // A window hit-test at the header's centre returned that third view, confirming it
        // swallows the tap. Note it reports FIVE gesture recognizers, so the "gestures == 0 means
        // decorative" test used for SfTabView and SfSegmentedControl does not identify it — that
        // assumption is why earlier attempts missed this entirely. The reliable signature is
        // structural: a layout wrapper whose only child is a graphics view.
        foreach (var child in root.Subviews)
        {
            if (child.Subviews.Length == 1
                && child.Subviews[0].GetType().Name == "NativePlatformGraphicsView"
                && child.UserInteractionEnabled)
            {
                child.UserInteractionEnabled = false;
            }
        }
    }


}
#endif
