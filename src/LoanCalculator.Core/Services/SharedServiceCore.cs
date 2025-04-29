using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income.Summary;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculatorMaui.Services;
using Pj.Library;

namespace LoanCalculator.Core.Services
{
    public static class SharedServiceCore
    {
        private static ILocalStorage? _localStorage;
        public static ILocalStorage LocalStorage => _localStorage ??= ServiceLocator.GetService<ILocalStorage>();

        private static IErrorHandlingService? _errorHandlingService;
        public static IErrorHandlingService ErrorHandlingService => _errorHandlingService ??= ServiceLocator.GetService<IErrorHandlingService>();

        private static IAlertService? _alertService;
        public static IAlertService AlertService => _alertService ??= ServiceLocator.GetService<IAlertService>();

        private static INameValueDataService? _nameValueDataService;
        public static INameValueDataService? NameValueDataService => _nameValueDataService ??= ServiceLocator.GetService<INameValueDataService>();

        private static IAppInformation? _appInformation;
        public static IAppInformation? AppInformation => _appInformation ??= ServiceLocator.GetService<IAppInformation>();

        private static bool _loadSafe = false;
        public static bool LoadSafe => _loadSafe;
        public static void LoadSafeOn()
        {
            _loadSafe = true;
        }
        public static void LoadSafeOff()
        {
            _loadSafe = false;
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

        private static readonly object _saveDataLock = new object();

        public static Task SaveData<T>(T data)
        {
            try
            {
                if (_loadSafe || PageHelper.IsFormLoading)
                {
                    return Task.CompletedTask;
                }

                lock (_saveDataLock)
                {
                    Task.Run(async () =>
                    {
                        await LocalStorage.SaveData(data).ConfigureAwait(false);
                    }).Wait();
                }
            }
            catch (Exception e)
            {
                ErrorHandlingService.HandleException(e);
            }

            return Task.CompletedTask;
        }

        #region Inter Model Data Transfer

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

        public static ExpenseViewModel ExpenseSummary
        {
            get
            {
                ExpenseViewModel? temp = null;
                Task.Run(async () => temp = await LocalStorage.GetData<ExpenseViewModel>()).Wait();

                if (temp == null)
                {
                    return new ExpenseViewModel();
                }
                temp.TransactionRecords?.SumUpData();
                return temp;
            }
        }

        public static IncomeViewModel IncomeSummary
        {
            get
            {
                IncomeViewModel? temp = null;
                Task.Run(async () => temp = await LocalStorage.GetData<IncomeViewModel>()).Wait();

                if (temp == null)
                {
                    return new IncomeViewModel();
                }
                temp.TransactionRecords?.SumUpData();
                return temp;
            }
        }

        //public static IncomeExpenseSummary LoanPropertyExpenseSummary => GetIncomeExpenseSummary<LoanViewModel>();

        public static (IncomeExpenseSummary?, PaymentOutput?) GetLoanViewModel()
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


        #endregion

        #region Disclaimer Data

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

        #endregion

        public static bool IsPremiumUser => Preferences.Get("IsPremium", false);
        public static bool IsTrialUser => !IsPremiumUser;
    }
}
