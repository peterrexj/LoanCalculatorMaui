using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.Themes;
using LoanCalculatorMaui.View;
using LoanCalculatorMaui.ViewModel;
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
                .ConfigureSyncfusionCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

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

            builder.Services.AddSingleton<INameValueDataService, NameValueDataService>();
            builder.Services.AddSingleton<IThemeHelper, ThemeHelper>();
            builder.Services.AddSingleton<LoanView>();
            builder.Services.AddSingleton<LoanViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            var serviceProvider = builder.Build();

            SharedServices.Initialize(
                serviceProvider.Services?.GetService<ILocalStorage>() ?? null,
                serviceProvider.Services?.GetService<INameValueDataService>(),
                serviceProvider.Services?.GetService<IAppInformation>(),
                serviceProvider.Services?.GetService<IThemeHelper>()
            );

            return serviceProvider;

        }
    }
}
