using Microsoft.Maui.Controls;

namespace LoanCalculatorMaui.Controls
{
    public partial class ThreeColumnBorderedContentView : ContentView
    {
        public ThreeColumnBorderedContentView()
        {
            InitializeComponent();
        }

        public static readonly BindableProperty FormattedText1Property =
            BindableProperty.Create(nameof(FormattedText1), typeof(FormattedString), typeof(ThreeColumnBorderedContentView), new FormattedString(), propertyChanged: OnFormattedText1Changed);

        public static readonly BindableProperty FormattedText2Property =
            BindableProperty.Create(nameof(FormattedText2), typeof(FormattedString), typeof(ThreeColumnBorderedContentView), new FormattedString(), propertyChanged: OnFormattedText2Changed);

        public static readonly BindableProperty FormattedText3Property =
            BindableProperty.Create(nameof(FormattedText3), typeof(FormattedString), typeof(ThreeColumnBorderedContentView), new FormattedString(), propertyChanged: OnFormattedText3Changed);

        /// <summary>
        /// Makes box 3 tappable. Null by default, so every existing use of this control keeps
        /// three inert boxes — a <c>TapGestureRecognizer</c> with no Command does nothing.
        /// </summary>
        /// <remarks>
        /// Only box 3 is wired. The Loan page uses it to link the affordability verdict through to
        /// What If, which is the one place in the design where a hero box is also a link.
        /// </remarks>
        public static readonly BindableProperty Box3CommandProperty =
            BindableProperty.Create(nameof(Box3Command), typeof(System.Windows.Input.ICommand), typeof(ThreeColumnBorderedContentView));

        /// <summary>
        /// Automation id for box 3 specifically. The control's own AutomationId lands on the whole
        /// three-box row, whose centre is box 2 — so a test tapping that would hit the wrong box.
        /// </summary>
        public static readonly BindableProperty Box3AutomationIdProperty =
            BindableProperty.Create(nameof(Box3AutomationId), typeof(string), typeof(ThreeColumnBorderedContentView), propertyChanged: OnBox3AutomationIdChanged);

        public System.Windows.Input.ICommand? Box3Command
        {
            get => (System.Windows.Input.ICommand?)GetValue(Box3CommandProperty);
            set => SetValue(Box3CommandProperty, value);
        }

        public string? Box3AutomationId
        {
            get => (string?)GetValue(Box3AutomationIdProperty);
            set => SetValue(Box3AutomationIdProperty, value);
        }

        private static void OnBox3AutomationIdChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ThreeColumnBorderedContentView control && newValue is string id)
                control.Box3Border.AutomationId = id;
        }

        /// <summary>
        /// Shows or hides the third box. When hidden, its column collapses so the two remaining
        /// boxes take half the width each.
        /// </summary>
        /// <remarks>
        /// Added for the Projection summary, where the third box held a two-digit year count next
        /// to two currency totals. At a third of the width an eight-digit figure wrapped
        /// mid-number ("$19,336,98 / 9"), so the box that needed the least space was costing the
        /// two that needed the most. Defaults to true, so every existing three-box use is
        /// untouched.
        /// </remarks>
        public static readonly BindableProperty Box3VisibleProperty =
            BindableProperty.Create(
                nameof(Box3Visible),
                typeof(bool),
                typeof(ThreeColumnBorderedContentView),
                true,
                propertyChanged: OnBox3VisibleChanged);

        public bool Box3Visible
        {
            get => (bool)GetValue(Box3VisibleProperty);
            set => SetValue(Box3VisibleProperty, value);
        }

        private static void OnBox3VisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not ThreeColumnBorderedContentView control || newValue is not bool visible)
                return;

            control.Box3Border.IsVisible = visible;

            // Collapsing the column as well as the Border is what redistributes the width —
            // hiding the Border alone leaves its third of the grid empty.
            control.Box3Column.Width = visible ? new GridLength(33, GridUnitType.Star) : new GridLength(0);
        }

        public FormattedString FormattedText1
        {
            get => (FormattedString)GetValue(FormattedText1Property);
            set => SetValue(FormattedText1Property, value);
        }

        public FormattedString FormattedText2
        {
            get => (FormattedString)GetValue(FormattedText2Property);
            set => SetValue(FormattedText2Property, value);
        }

        public FormattedString FormattedText3
        {
            get => (FormattedString)GetValue(FormattedText3Property);
            set => SetValue(FormattedText3Property, value);
        }

        private static void OnFormattedText1Changed(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ThreeColumnBorderedContentView control && newValue is FormattedString newFormattedText)
            {
                control.Label1.FormattedText = newFormattedText;
            }
        }

        private static void OnFormattedText2Changed(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ThreeColumnBorderedContentView control && newValue is FormattedString newFormattedText)
            {
                control.Label2.FormattedText = newFormattedText;
            }
        }

        private static void OnFormattedText3Changed(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ThreeColumnBorderedContentView control && newValue is FormattedString newFormattedText)
            {
                control.Label3.FormattedText = newFormattedText;
            }
        }

        public static readonly BindableProperty IsBox3DangerProperty =
            BindableProperty.Create(nameof(IsBox3Danger), typeof(bool), typeof(ThreeColumnBorderedContentView), false, propertyChanged: OnIsBox3DangerChanged);

        public bool IsBox3Danger
        {
            get => (bool)GetValue(IsBox3DangerProperty);
            set => SetValue(IsBox3DangerProperty, value);
        }

        private static void OnIsBox3DangerChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not ThreeColumnBorderedContentView control) return;
            if ((bool)newValue)
            {
                control.Label3.SetDynamicResource(Label.TextColorProperty, "LoanAppBorderTopDangerFgColor");
                // Walk up to the parent Border (Label → StackLayout → Grid → ContentView → Border)
                if (control.Label3.Parent?.Parent?.Parent?.Parent is Border border3)
                    border3.Style = (Style)Application.Current.Resources["BorderTopDangerHighlightBox"];
            }
            else
            {
                control.Label3.SetDynamicResource(Label.TextColorProperty, "LoanAppHighlightFgColor");
                if (control.Label3.Parent?.Parent?.Parent?.Parent is Border border3)
                    border3.Style = (Style)Application.Current.Resources["BorderTopHighlightBox"];
            }
        }

        public static readonly BindableProperty LineHeightBox1Property =
            BindableProperty.Create(nameof(LineHeightBox1), typeof(double), typeof(ThreeColumnBorderedContentView), 1.0);

        public double LineHeightBox1
        {
            get => (double)GetValue(LineHeightBox1Property);
            set => SetValue(LineHeightBox1Property, value);
        }

        public static readonly BindableProperty LineHeightBox2Property =
            BindableProperty.Create(nameof(LineHeightBox2), typeof(double), typeof(ThreeColumnBorderedContentView), 1.0);

        public double LineHeightBox2
        {
            get => (double)GetValue(LineHeightBox2Property);
            set => SetValue(LineHeightBox2Property, value);
        }

        public static readonly BindableProperty LineHeightBox3Property =
            BindableProperty.Create(nameof(LineHeightBox3), typeof(double), typeof(ThreeColumnBorderedContentView), 1.0);

        public double LineHeightBox3
        {
            get => (double)GetValue(LineHeightBox3Property);
            set => SetValue(LineHeightBox3Property, value);
        }
    }
}
