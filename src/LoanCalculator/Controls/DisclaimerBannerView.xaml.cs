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

    public static readonly BindableProperty HeaderTextProperty =
        BindableProperty.Create(nameof(HeaderText), typeof(string), typeof(DisclaimerBannerView), default(string));

    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }


    public static readonly BindableProperty BannerTypeProperty =
        BindableProperty.Create(nameof(BannerType), typeof(string), typeof(DisclaimerBannerView), default(string));

    public string BannerType
    {
        get => (string)GetValue(BannerTypeProperty);
        set => SetValue(BannerTypeProperty, value);
    }

    public DisclaimerBannerView()
	{
		InitializeComponent();
	}
}