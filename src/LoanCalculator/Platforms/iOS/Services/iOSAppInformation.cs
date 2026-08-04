
using LoanCalculator.Core.Constants;
using LoanCalculatorMaui.Services;
using Pj.Library;

namespace LoanCalculatorMaui.Platforms.iOS.Services
{
    public class iOSAppInformation : IAppInformation
    {
        public string Country => "Australia";

        public bool IsAustralia => Country.EqualsIgnoreCase("Australia");

        public string ApplicationTitle => "Loan Affordability Calculator";

        public string InAppProductId => "com.pj.loan.afford.calc.premium"; // Replace with your actual product ID

        public string AppShareLink => "https://www.yoursimpleapps.com";

        public string RateAppLink => "itms-apps://itunes.apple.com/app/idYOUR_APP_ID?action=write-review"; //idYOUR_APP_ID with your App Store app ID
        public bool IsFullyPaidApplication => false; // Assuming the app is not fully paid, adjust as necessary

        public List<string> GetRegisteredFontFamilies() => RegisteredFonts.GetFontFamilies();
    }
}
