using CommunityToolkit.Maui;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Controls;
using LoanCalculatorMaui.Services;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

namespace LoanCalculatorMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                 .UseSentry(options =>
                 {
                     // The DSN is the only required setting.
                     options.Dsn = "https://16ef66602415c605019107b0a5bd0978@o4508789158445056.ingest.de.sentry.io/4508789160280144";

                     // Use debug mode if you want to see what the SDK is doing.
                     // Debug messages are written to stdout with Console.Writeline,
                     // and are viewable in your IDE's debug console or with 'adb logcat', etc.
                     // This option is not recommended when deploying your application.
                     options.Debug = true;

                     // Set TracesSampleRate to 1.0 to capture 100% of transactions for tracing.
                     // We recommend adjusting this value in production.
                     options.TracesSampleRate = 1.0F;
                 })
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("FRSCRIPT.TTF", "French");
                    fonts.AddFont("CALIBRI.ttf", "Calibri");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            builder.Services.AddSingleton<IAlertService, AlertService>();

#if ANDROID
                        builder.Services.AddSingleton<IAppInformation, LoanCalculatorMaui.Platforms.Android.Services.AndroidAppInformation>();
                        builder.Services.AddSingleton<ILocalStorage, LoanCalculatorMaui.Platforms.Android.Services.AndroidLocalStorageService>();
#elif IOS
                        builder.Services.AddSingleton<IAppInformation, LoanCalculatorMaui.Platforms.iOS.Services.iOSAppInformation>();
                        builder.Services.AddSingleton<ILocalStorage, LoanCalculatorMaui.Platforms.iOS.Services.iOSLocalStorageService>();
#elif MACCATALYST

#elif WINDOWS
                        builder.Services.AddSingleton<IAppInformation, LoanCalculatorMaui.Platforms.Windows.Services.WindowsAppInformation>();
                        builder.Services.AddSingleton<ILocalStorage, LoanCalculatorMaui.Platforms.Windows.Services.WindowsLocalStorageService>();
#endif

            builder.Services.AddTransient<PopupDisclaimerView>();
            builder.Services.AddSingleton<INameValueDataService, NameValueDataService>();
            builder.Services.AddSingleton<LoanViewModel>();
            builder.Services.AddSingleton<ExpenseViewModel>();
            builder.Services.AddSingleton<IncomeViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddSingleton<PopupDisclaimerViewModel>();
            builder.Services.AddSingleton<IThemeHandler, ThemeHandler>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            var serviceProvider = builder.Build();

            //SharedServices.Initialize(
            //    serviceProvider.Services?.GetService<INameValueDataService>(),
            //    serviceProvider.Services?.GetService<IAppInformation>(),
            //    serviceProvider.Services?.GetService<IThemeHelper>()
            //);

            return serviceProvider;

        }
    }
}
