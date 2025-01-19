namespace LoanCalculatorMaui.Controls;

public partial class DisclaimerBannerView : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(DisclaimerBannerView), default(string));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public DisclaimerBannerView()
	{
		InitializeComponent();
	}
}