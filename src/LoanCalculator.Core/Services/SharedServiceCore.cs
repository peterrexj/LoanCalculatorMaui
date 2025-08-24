using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income.Summary;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculatorMaui.Services;
using Pj.Library;
using System.Globalization;

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

        public static string DisclaimerData
        {
            get
            {
                var disclaimerData = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui")
                    .GetEmbeddedResourceAsText(
                        "LoanCalculatorMaui.Extensions.DisclaimerData.AppLaunchDisclaimerData.html")
                    .Replace("{{AppName}}", AppInformation?.ApplicationTitle ?? "Loan Affordability Calculator");

                return ReplaceColorsWithResourceKeys(disclaimerData);
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
                    { "#091817", "LoanAppDisclaimerHeader2TextColor" },
                    { "#7355dc", "LoanAppDisclaimerContentForegroundColor" }
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

        #region Premium
        public static bool IsTrialUser => !IsPremiumUser();

        public static bool IsPremiumUser()
        {
            try
            {
                if (AppInformation is { IsFullyPaidApplication: true }) return true;

                var value = Task.Run(() => SecureStorage.GetAsync("IsPremium")).Result;
                return value == "true";
            }
            catch (Exception ex)
            {
                ErrorHandlingService.HandleException(ex, "Failed to get IsPremium from SecureStorage.");
                return false;
            }
        }

        public static void UpdateToPremium()
        {
            try
            {
                Task.Run(() => SecureStorage.SetAsync("IsPremium", "true")).Wait();
            }
            catch (Exception ex)
            {
                ErrorHandlingService.HandleException(ex, "Failed to set IsPremium in SecureStorage.");
            }
        }

        #endregion

        #region Trial Current Day

        private const string LastAccessKey = "LastAccessDate";

        public static bool IsCurrentDay()
        {
            try
            {
                var storedDateStr = Task.Run(() => SecureStorage.GetAsync(LastAccessKey)).Result;

                var todayStr = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

                if (storedDateStr == todayStr)
                {
                    return true;
                }

                // It's a new day; update the stored value
                Task.Run(() => SecureStorage.SetAsync(LastAccessKey, todayStr)).Wait();

                return false;
            }
            catch (Exception ex)
            {
                ErrorHandlingService.HandleException(ex, "Failed to get or set LastAccessDate in SecureStorage.");
                return false;
            }
        }

        private const string DataWipeAlertKey = "DataWipeAlertDate";

        public static bool HasAlertedUserForDataWipe()
        {
            try
            {
                var storedDateStr = Task.Run(() => SecureStorage.GetAsync(DataWipeAlertKey)).Result;
                var todayStr = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

                if (storedDateStr == todayStr)
                {
                    // Already alerted today
                    return true;
                }

                // New day or never alerted, update the stored value
                Task.Run(() => SecureStorage.SetAsync(DataWipeAlertKey, todayStr)).Wait();
                return false;
            }
            catch (Exception ex)
            {
                ErrorHandlingService.HandleException(ex, "Failed to get or set DataWipeAlertDate in SecureStorage.");
                return false;
            }
        }

        public static async Task<bool> AlertUserForDataWipe()
        {
            try
            {
                return await AlertService.ShowConfirmationAsync("Trial Version Notice",
                    "Your data will be cleared at the end of the session. To keep your data, access all features, and continue using the app beyond today, please upgrade to the premium version.",
                    "See Plan", "Understood");
            }
            catch (Exception ex)
            {
                ErrorHandlingService.HandleException(ex, "Failed to get or set DataWipeAlertDate in SecureStorage.");
                return false;
            }
        }

        #endregion

        #region Currency

        public const string SelectedCurrencyKey = "SelectedCurrencyISO";
        private static List<CurrencyModel?>? _currencies;

        public static List<CurrencyModel?>? Currencies
        {
            get
            {
                if (_currencies == null)
                {
                    var topISO = new[]
                    {
                        "USD", "EUR", "JPY", "GBP", "AUD", "CAD", "CHF", "CNY", "HKD", "NZD",
                        "SEK", "KRW", "SGD", "NOK", "MXN", "INR", "RUB", "ZAR", "TRY", "BRL"
                    };

                    var allCurrencies = CultureInfo
                        .GetCultures(CultureTypes.SpecificCultures)
                        .Select(culture =>
                        {
                            try
                            {
                                var region = new RegionInfo(culture.Name);
                                return new CurrencyModel(region.CurrencyEnglishName, region.CurrencySymbol,
                                    region.ISOCurrencySymbol);
                            }
                            catch
                            {
                                return null;
                            }
                        })
                        .Where(x => x != null)
                        .DistinctBy(x => x.IsoCode)
                        .ToList();

                    var topCurrencies = allCurrencies
                        .Where(c => topISO.Contains(c.IsoCode))
                        .OrderBy(c => Array.IndexOf(topISO, c.IsoCode))
                        .ToList();

                    var otherCurrencies = allCurrencies
                        .Where(c => !topISO.Contains(c.IsoCode))
                        .OrderBy(c => c.Name)
                        .ToList();

                    _currencies = topCurrencies.Concat(otherCurrencies).ToList();
                }
                return _currencies;
            }
        }

        public static string GetCurrencySymbol(string? isoCode)
        {
            if (string.IsNullOrEmpty(isoCode) || string.IsNullOrWhiteSpace(isoCode))
            {
                return "$";
            }
            var currency = Currencies?.FirstOrDefault(c => c?.IsoCode == isoCode);
            return currency?.Symbol ?? "$";
        }

        public static CultureInfo? GetCultureFromIsoCurrency(string isoCode)
        {
            return CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .Select(culture =>
                {
                    try
                    {
                        var region = new RegionInfo(culture.Name);
                        return (region.ISOCurrencySymbol == isoCode) ? culture : null;
                    }
                    catch
                    {
                        return null;
                    }
                })
                .FirstOrDefault(c => c != null);
        }


        #endregion

        public const AppThemes DefaultAppTheme = AppThemes.Dark;
    }
}
