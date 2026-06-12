using LoanCalculator.Core.Models;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.View;

namespace LoanCalculatorMaui
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider, IThemeHandler themeHandler, SplashPage splash)
        {
            InitializeComponent();

            ServiceLocator.ServiceProvider = serviceProvider;

            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            TaskScheduler.UnobservedTaskException += HandleTaskSchedulerException;

            // Set MainPage immediately — must happen before any optional work that could throw,
            // otherwise a swallowed exception leaves the app on a black screen.
            MainPage = splash;

            try
            {
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXpfd3RQR2VZUUFwWERWYEo=");

                themeHandler.LoadDefaultStyle();

                // Restore purchases silently in background — never block the constructor
                // and never show Apple sign-in on simulator (isSilent=true suppresses the dialog)
                if (SharedServiceCore.IsTrialUser)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(3000);
                            var productId = ServiceLocator.GetService<IAppInformation>()?.InAppProductId;
                            if (productId != null)
                                await ServiceLocator.GetService<IInAppPurchaseService>().RestorePurchasesAsync(productId, true);
                        }
                        catch (Exception e)
                        {
                            LogException(e, "Error restoring purchases");
                        }
                    });
                }

                Helper.CurrencySymbol =
                    SharedServiceCore.GetCurrencySymbol(
                        Preferences.Get(SharedServiceCore.SelectedCurrencyKey, SharedServiceCore.GetDefaultCurrencyIso()));
            }
            catch (Exception ex)
            {
                LogException(ex, "App initialization failed");
            }
        }

        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            WriteCrashLog("FATAL", ex);
            LogException(ex, "AppDomain Unhandled Exception");
            if (e.IsTerminating)
                ShowFatalCrashAlert(ex);
        }

        private void HandleTaskSchedulerException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            WriteCrashLog("TASK", e.Exception);
            LogException(e.Exception, "TaskScheduler Unobserved Task Exception");
            e.SetObserved();
            var errorHandlingService = ServiceLocator.GetService<IErrorHandlingService>();
            errorHandlingService?.HandleException(e.Exception, "A background operation failed.");
        }

        private void LogException(Exception? exception, string source)
        {
            if (exception == null) return;
            WriteCrashLog(source, exception);
            var errorHandlingService = ServiceLocator.GetService<IErrorHandlingService>();
            errorHandlingService?.HandleException(exception, source);
        }

        private static void ShowFatalCrashAlert(Exception? ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                    if (page == null) return;
#if DEBUG
                    await page.DisplayAlert("Fatal Error",
                        $"The app encountered a fatal error and needs to close.\n\n{ex?.GetType().Name}: {ex?.Message}",
                        "OK");
#else
                    await page.DisplayAlert("Unexpected Error",
                        "The app encountered a problem and needs to close. Your data has been saved. Please reopen the app.",
                        "OK");
#endif
                }
                catch { /* nothing left to do */ }
            });
        }

        // Writes crash info to a file in the app's Documents folder.
        // On simulator: ~/Library/Developer/CoreSimulator/Devices/<id>/data/Containers/Data/Application/<id>/Documents/crash.log
        // Read it with: cat $(find ~/Library/Developer/CoreSimulator -name "crash.log" 2>/dev/null | head -1)
        private static void WriteCrashLog(string tag, Exception? ex)
        {
            try
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var path = Path.Combine(docs, "crash.log");
                var entry = $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {ex?.GetType().FullName}: {ex?.Message}\n{ex?.StackTrace}\n\n";
                File.AppendAllText(path, entry);
                Console.WriteLine($"[CRASH] {entry}");
            }
            catch { /* must not throw */ }
        }
    }
}
