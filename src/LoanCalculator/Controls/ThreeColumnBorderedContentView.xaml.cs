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
