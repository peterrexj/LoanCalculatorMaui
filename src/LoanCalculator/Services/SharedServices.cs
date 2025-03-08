using LoanCalculator.Models;
using LoanCalculator.Models.Income.Summary;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Themes;
using LoanCalculatorMaui.ViewModel;
using Pj.Library;

namespace LoanCalculatorMaui.Services
{
    public static class SharedServices
    {
        private static ILocalStorage? _localStorage;
        public static ILocalStorage LocalStorage => _localStorage ??= ServiceLocator.GetService<ILocalStorage>();

        private static INameValueDataService? _nameValueDataService;
        public static INameValueDataService? NameValueDataService => _nameValueDataService ??= ServiceLocator.GetService<INameValueDataService>();

        private static IAppInformation? _appInformation;
        public static IAppInformation? AppInformation => _appInformation ??= ServiceLocator.GetService<IAppInformation>();

        private static IThemeHelper? _themeHelper;
        public static IThemeHelper? ThemeHelper => _themeHelper ??= ServiceLocator.GetService<IThemeHelper>();

        private static IErrorHandlingService? _errorHandlingService;

        public static IErrorHandlingService ErrorHandlingService =>
            _errorHandlingService ??= ServiceLocator.GetService<IErrorHandlingService>();

        //// Initialization method for dependency injection
        //public static void Initialize(
        //    INameValueDataService? nameValueDataService,
        //    IAppInformation? appInformation,
        //    IThemeHelper? themeHelper)
        //{
        //    //LocalStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        //    NameValueDataService = nameValueDataService ?? throw new ArgumentNullException(nameof(nameValueDataService));
        //    AppInformation = appInformation ?? throw new ArgumentNullException(nameof(appInformation));
        //    ThemeHelper = themeHelper ?? throw new ArgumentNullException(nameof(themeHelper));
        //}

        private static IncomeExpenseSummary GetIncomeExpenseSummary<TViewModel>() where TViewModel : class
        {
            TViewModel? temp = null;
            Task.Run(async () => temp = await LocalStorage.GetData<TViewModel>()).Wait();

            if (temp == null)
            {
                return new IncomeExpenseSummary();
            }

            var transactionRecords = (temp as dynamic)?.TransactionRecords;
            transactionRecords?.SumUpData();
            return transactionRecords?.IncomeExpenseSummary;
        }

        public static IncomeExpenseSummary ExpenseSummary => GetIncomeExpenseSummary<ExpenseViewModel>();

        public static IncomeExpenseSummary IncomeSummary => GetIncomeExpenseSummary<IncomeViewModel>();

        public static IncomeExpenseSummary LoanPropertyExpenseSummary => GetIncomeExpenseSummary<LoanViewModel>();

        public static (IncomeExpenseSummary?, PaymentOutput?)  GetLoanViewModel()
        {
            LoanViewModel? temp = null;
            Task.Run(async () => temp = await LocalStorage.GetData<LoanViewModel>()).Wait();
            if (temp == null)
            {
                return (new IncomeExpenseSummary(), new PaymentOutput());
            }
            else
            {
                temp?.TransactionRecords?.SumUpData();
                return (temp?.TransactionRecords?.IncomeExpenseSummary, temp?.HomeLoanInfo?.PaymentSummary?.Payment);
            }
        }

        public static bool ShouldShowAppLaunchDisclaimer()
        {
            return NameValueDataService != null && NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer != true;
        }

        public static void SetAppLaunchDisclaimerShown()
        {
            if (NameValueDataService != null)
            {
                NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer = true;
                NameValueDataService.SaveNameValueData();
            }
        }

        public static void ClearDisclaimerData()
        {
            _disclaimerData = string.Empty;
        }

        private static string _disclaimerData = string.Empty;
        public static string DisclaimerData
        {
            get
            {
                if (_disclaimerData.IsEmpty())
                {
                    _disclaimerData = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui").GetEmbeddedResourceAsText("LoanCalculatorMaui.Extensions.DisclaimerData.AppLaunchDisclaimerData.html")
                        .Replace("{{AppName}}", "LoanCalcPro");
                }
                return ReplaceColorsWithResourceKeys(_disclaimerData);
            }
        }
        private static string ReplaceColorsWithResourceKeys(string content)
        {
            try
            {
                var colorMappings = new Dictionary<string, string>
                {
                    { "#758d84", "LoanAppDisclaimerBodyBackgroundColor" },
                    { "#091818", "LoanAppDisclaimerHeaderBackgroundColor" },
                    { "#b9c4c4", "LoanAppDisclaimerHeaderTextColor" },
                    { "#0E8388", "LoanAppDisclaimerHeaderBorderColor" },
                    { "#dee7e4", "LoanAppDisclaimerContentBackgroundColor" },
                    { "#2c3531", "LoanAppDisclaimerContentBoxShadowColor" },
                    { "#091817", "LoanAppDisclaimerHeader2TextColor" }
                };

                foreach (var mapping in colorMappings)
                {
                    if (Application.Current.Resources.TryGetValue(mapping.Value, out var resourceValue) && resourceValue is Color color)
                    {
                        var colorHex = color.ToHex();
                        content = content.Replace(mapping.Key, colorHex);
                    }
                }
            }
            catch (Exception e)
            {
                _errorHandlingService.HandleException(e);
            }

            return content;
        }

        public static async Task<T?> LoadDataFile<T>()
        {
            T? data = default;

            try
            {
                LocalStorage.Initialize();
                data = await LocalStorage.GetData<T>().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ErrorHandlingService.HandleException(e);
            }

            return data;
        }

        public static Task SaveData<T>(T data)
        {
            try
            {
                if (PageHelper.IsFormLoading)
                {
                    return Task.CompletedTask;
                }

                Task.Run(async () =>
                {
                    await LocalStorage.SaveData(data).ConfigureAwait(false);
                }).Wait();
            }
            catch (Exception e)
            {
                ErrorHandlingService.HandleException(e);
            }
            
            return Task.CompletedTask;
        }
    }
}
