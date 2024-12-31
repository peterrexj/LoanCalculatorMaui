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
    }
}
