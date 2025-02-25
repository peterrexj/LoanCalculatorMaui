using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.Themes;

namespace LoanCalculatorMaui
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            ServiceLocator.ServiceProvider = serviceProvider;

            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            TaskScheduler.UnobservedTaskException += HandleTaskSchedulerException;

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWH1ccXVSQ2dcV0Z0W0A=");

            StyleProvider.LoadDefaultStyle();
        }
        

        public App()
        {
            InitializeComponent();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpR2JGfV5ycEVCallSTnVfUiweQnxTdEFiW35acHBQRWNcVEZ3WQ==");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception, "AppDomain Unhandled Exception");
        }

        private void HandleTaskSchedulerException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception, "TaskScheduler Unobserved Task Exception");
            e.SetObserved();
        }

        private void LogException(Exception? exception, string source)
        {
            if (exception == null) return;

            var errorHandlingService = ServiceLocator.GetService<IErrorHandlingService>();
            errorHandlingService?.HandleException(exception, "An unhandled exception occurred.");
        }
    }
}
