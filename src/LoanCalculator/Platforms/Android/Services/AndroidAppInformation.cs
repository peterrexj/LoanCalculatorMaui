using LoanCalculatorMaui.Services;
using Pj.Library;

namespace LoanCalculatorMaui.Platforms.Android.Services
{
    public class AndroidAppInformation : IAppInformation
    {
        public string AppCentreAppKeyDroid => "51ab5ec9-0037-4983-93a4-e67ec23950b8";

        public string AdsBannerId => "ca-app-pub-4219645367584712/7002050051";

        public string AdsInterstitialId => "ca-app-pub-4219645367584712/2337467619";

        public int ShowFirstInterstitialAdOnClickLimit => 3;

        public int ShowLaterInterstitialAdOnClickLimit => 8;

        public string Country => "Australia";

        public bool IsAustralia => Country.EqualsIgnoreCase("Australia");

        public string InAppProductId => "com.pj.loan.calculator.pro"; // Replace with your actual product ID
    }
}