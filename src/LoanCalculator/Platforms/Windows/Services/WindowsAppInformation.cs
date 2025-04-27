using LoanCalculatorMaui.Services;
using Pj.Library;

namespace LoanCalculatorMaui.Platforms.Windows.Services
{
    public class WindowsAppInformation : IAppInformation
    {
        public string AppCentreAppKeyDroid => "51ab5ec9-0037-4983-93a4-e67ec23950b8";

        public string AdsBannerId => "ca-app-pub-4219645367584712/9833451000";

        public string AdsInterstitialId => "ca-app-pub-4219645367584712/3235891676";

        public int ShowFirstInterstitialAdOnClickLimit => 3;

        public int ShowLaterInterstitialAdOnClickLimit => 8;

        public string Country => "Australia";

        public bool IsAustralia => Country.EqualsIgnoreCase("Australia");

        public string InAppProductId => "com.pj.loan.calculator.pro"; // Replace with your actual product ID

    }
}
