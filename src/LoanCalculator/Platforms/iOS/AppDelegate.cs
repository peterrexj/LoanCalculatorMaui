using Foundation;
using UIKit;

namespace LoanCalculatorMaui
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var result = base.FinishedLaunching(application, launchOptions);

            // Paint the root UIWindow with the brand dark color so there is no black
            // flash before MAUI renders, nor any flash during the SplashPage → AppShell swap.
            var brandDark = UIColor.FromRGB(0x0A, 0x1A, 0x20);
            if (Window != null)
                Window.BackgroundColor = brandDark;

            return result;
        }
    }
}
