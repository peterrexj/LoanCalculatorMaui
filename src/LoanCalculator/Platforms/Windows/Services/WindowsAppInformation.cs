using LoanCalculator.Core.Constants;
using LoanCalculatorMaui.Services;
using Pj.Library;

namespace LoanCalculatorMaui.Platforms.Windows.Services
{
    public class WindowsAppInformation : IAppInformation
    {
        public string Country => "Australia";

        public bool IsAustralia => Country.EqualsIgnoreCase("Australia");

        public string ApplicationTitle => "Loan Affordability Calculator";

        public string InAppProductId => "com.pj.loan.calculator.pro"; // Replace with your actual product ID

        public string AppShareLink => "https://apps.microsoft.com/detail/9PG3PLHBBVLQ";

        public string RateAppLink => "ms-windows-store://pdp/?productid=9PG3PLHBBVLQ"; // YOUR_PRODUCT_ID with your Windows Store app ID
        public bool IsFullyPaidApplication => true; // Assuming the app is fully paid, adjust as necessary

        public List<string> GetRegisteredFontFamilies() => RegisteredFonts.GetFontFamilies();
    }
}
