
using LoanCalculatorMaui.Services;
using Pj.Library;

namespace LoanCalculatorMaui.Platforms.iOS.Services
{
    public class iOSAppInformation : IAppInformation
    {
        public string Country => "Australia";

        public bool IsAustralia => Country.EqualsIgnoreCase("Australia");

        public string InAppProductId => "com.pj.loan.calculator.pro"; // Replace with your actual product ID

        public string AppShareLink => "https://www.yoursimpleapps.com";

    }
}
