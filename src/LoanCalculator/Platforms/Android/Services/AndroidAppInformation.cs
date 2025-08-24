using LoanCalculatorMaui.Services;
using Pj.Library;

namespace LoanCalculatorMaui.Platforms.Android.Services
{
    public class AndroidAppInformation : IAppInformation
    {
        public string Country => "Australia";

        public bool IsAustralia => Country.EqualsIgnoreCase("Australia");
        public string ApplicationTitle => "Loan Affordability Calculator"; 

        public string InAppProductId => "com.pj.loan.afford.calc.premium"; // Replace with your actual product ID

        public string AppShareLink => "https://www.yoursimpleapps.com";

        public string RateAppLink => "market://details?id=com.pj.loan.afford.calc"; //com.companyname.yourapp with your Android package ID

        public bool IsFullyPaidApplication => false; // Assuming the app is not fully paid, adjust as necessary
    }
}