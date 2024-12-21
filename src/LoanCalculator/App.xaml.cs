using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.Extensions;

namespace LoanCalculatorMaui
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            
            Services = serviceProvider;

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpR2JGfV5ycEVCallSTnVfUiweQnxTdEFiW35acHBQRWNcVEZ3WQ==");

            DefaultStyleProvider.LoadDefaultStyle(AppThemes.Light);
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
    }
}
