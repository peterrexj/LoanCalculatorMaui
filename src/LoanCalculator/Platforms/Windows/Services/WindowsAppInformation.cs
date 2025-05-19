using LoanCalculatorMaui.Services;
using Pj.Library;

namespace LoanCalculatorMaui.Platforms.Windows.Services
{
    public class WindowsAppInformation : IAppInformation
    {
        public string Country => "Australia";

        public bool IsAustralia => Country.EqualsIgnoreCase("Australia");

        public string InAppProductId => "com.pj.loan.calculator.pro"; // Replace with your actual product ID

        public string AppShareLink => "https://www.yoursimpleapps.com";
    }
}
