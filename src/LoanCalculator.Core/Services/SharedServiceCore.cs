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

        // Test-only injection points — not for production use.
        internal static void SetLocalStorage(ILocalStorage storage) => _localStorage = storage;
        internal static void ResetLocalStorage() => _localStorage = null;

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
        public static void LoadSafeOn() => _loadSafe = true;
        public static void LoadSafeOff() => _loadSafe = false;

        // Dirty flags — set when a tab saves new data so other tabs know to refresh
        // their cross-tab summaries on next appearance instead of skipping the load.
        public static bool IsIncomeDirty { get; private set; }
        public static bool IsExpenseDirty { get; private set; }
        public static bool IsLoanDirty { get; private set; }
        public static bool IsCurrencyDirty { get; private set; }

        public static void MarkIncomeDirty() => IsIncomeDirty = true;
        public static void MarkExpenseDirty() => IsExpenseDirty = true;
        public static void MarkLoanDirty() => IsLoanDirty = true;
        public static void MarkCurrencyDirty() => IsCurrencyDirty = true;

        public static void ClearIncomeDirty() => IsIncomeDirty = false;
        public static void ClearExpenseDirty() => IsExpenseDirty = false;
        public static void ClearLoanDirty() => IsLoanDirty = false;
        public static void ClearCurrencyDirty() => IsCurrencyDirty = false;

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

        public static void SaveData<T>(T data)
        {
            if (_loadSafe) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await LocalStorage.SaveData(data).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ErrorHandlingService.HandleException(e);
                }
            });
        }

        #region Inter Model Data Transfer

        public static async Task<ExpenseViewModel> GetExpenseSummaryAsync()
        {
            var temp = await LocalStorage.GetData<ExpenseViewModel>().ConfigureAwait(false);
            if (temp == null) return new ExpenseViewModel();
            temp.TransactionRecords?.SumUpData();
            return temp;
        }

        public static async Task<IncomeViewModel> GetIncomeSummaryAsync()
        {
            var temp = await LocalStorage.GetData<IncomeViewModel>().ConfigureAwait(false);
            if (temp == null) return new IncomeViewModel();
            temp.TransactionRecords?.SumUpData();
            return temp;
        }

        public static async Task<(IncomeExpenseSummary?, PaymentOutput?)> GetLoanViewModelAsync()
        {
            var temp = await LocalStorage.GetData<LoanViewModel>().ConfigureAwait(false);
            if (temp == null) return (new IncomeExpenseSummary(), new PaymentOutput());
            temp.TransactionRecords?.SumUpData();
            return (temp.TransactionRecords?.IncomeExpenseSummary, temp.HomeLoanInfo?.PaymentSummary?.Payment);
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
            DisclaimerAccepted?.Invoke(null, EventArgs.Empty);
        }

        // Fired when the user accepts the disclaimer — used to auto-show the wizard on first launch.
        public static event EventHandler? DisclaimerAccepted;

        public static bool ShouldShowWizard()
        {
            return NameValueDataService != null && NameValueDataService.NameValueDataModel.HasShownWizard != true;
        }

        public static void SetWizardShown()
        {
            if (NameValueDataService != null)
            {
                NameValueDataService.NameValueDataModel.HasShownWizard = true;
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

        // ╔══════════════════════════════════════════════════════════════════════╗
        // ║  ⚠️  TESTING ONLY — COMMENT OUT BEFORE RELEASE / APP STORE SUBMIT  ⚠️  ║
        // ║  Set to true to bypass all trial restrictions during local testing.   ║
        // ║  Search for TESTING_PREMIUM_OVERRIDE to find this flag.              ║
        // ╚══════════════════════════════════════════════════════════════════════╝
        private const bool TESTING_PREMIUM_OVERRIDE = false; // ← set true to bypass trial gates during local testing
        // ────────────────────────────────────────────────────────────────────────

        public static bool IsTrialUser => !IsPremiumUser();

        public static bool IsPremiumUser()
        {
            try
            {
                if (TESTING_PREMIUM_OVERRIDE) return true; // ← comment out for release

                if (AppInformation is { IsFullyPaidApplication: true }) return true;

                var value = SecureStorage.GetAsync("IsPremium").GetAwaiter().GetResult();
                return value == "true";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage] IsPremium read failed: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> IsPremiumUserAsync()
        {
            try
            {
                if (AppInformation is { IsFullyPaidApplication: true }) return true;

                var value = await SecureStorage.GetAsync("IsPremium").ConfigureAwait(false);
                return value == "true";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage] IsPremium read failed: {ex.Message}");
                return false;
            }
        }

        public static void UpdateToPremium()
        {
            try
            {
                _ = Task.Run(() => SecureStorage.SetAsync("IsPremium", "true"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage] IsPremium write failed: {ex.Message}");
            }
        }

        #endregion

        #region Trial Current Day

        private const string LastAccessKey = "LastAccessDate";

        public static async Task<bool> IsCurrentDayAsync()
        {
            try
            {
                var storedDateStr = await SecureStorage.GetAsync(LastAccessKey).ConfigureAwait(false);
                var todayStr = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

                if (storedDateStr == todayStr) return true;

                // It's a new day; update the stored value
                await SecureStorage.SetAsync(LastAccessKey, todayStr).ConfigureAwait(false);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage] LastAccessDate failed: {ex.Message}");
                return false;
            }
        }

        private const string DataWipeAlertKey = "DataWipeAlertDate";

        public static async Task<bool> HasAlertedUserForDataWipeAsync()
        {
            try
            {
                var storedDateStr = await SecureStorage.GetAsync(DataWipeAlertKey).ConfigureAwait(false);
                var todayStr = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

                if (storedDateStr == todayStr) return true;

                // New day or never alerted; update the stored value
                await SecureStorage.SetAsync(DataWipeAlertKey, todayStr).ConfigureAwait(false);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage] DataWipeAlertDate failed: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[SecureStorage] DataWipeAlertDate failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Currency

        public const string SelectedCurrencyKey = "SelectedCurrencyISO";
        private static List<CurrencyModel?>? _currencies;

        private static readonly string[] TopISO =
        {
            "USD", "EUR", "JPY", "GBP", "AUD", "CAD", "CHF", "CNY", "HKD", "NZD",
            "SEK", "KRW", "SGD", "NOK", "MXN", "INR", "RUB", "ZAR", "TRY", "BRL"
        };

        // .NET resolves a currency symbol per culture, and CultureInfo.GetCultures returns them
        // in an unpredictable order. For some ISO codes the first culture reports the ISO code
        // itself ("USD") or a region-prefixed glyph ("JP¥", "CN¥") instead of the clean symbol.
        // This curated map guarantees the canonical symbol for the common currencies.
        private static readonly Dictionary<string, string> CuratedSymbols = new()
        {
            // Top 20 — guarantee the clean glyph regardless of culture order.
            ["USD"] = "$",   ["EUR"] = "€",   ["JPY"] = "¥",   ["GBP"] = "£",
            ["AUD"] = "$",   ["CAD"] = "CA$", ["CHF"] = "CHF", ["CNY"] = "¥",
            ["HKD"] = "HK$", ["NZD"] = "$",   ["SEK"] = "kr",  ["KRW"] = "₩",
            ["SGD"] = "$",   ["NOK"] = "kr",  ["MXN"] = "$",   ["INR"] = "₹",
            ["RUB"] = "₽",   ["ZAR"] = "R",   ["TRY"] = "₺",   ["BRL"] = "R$",

            // Currencies where .NET has no glyph and falls back to the bare ISO code —
            // supply the established symbol so the dropdown doesn't read e.g. "(RON) RON".
            ["RON"] = "lei", ["RSD"] = "дин.", ["KPW"] = "₩",  ["TMT"] = "m",
            ["ZWG"] = "ZiG",
        };

        // Pick the cleanest symbol .NET offers for an ISO code: prefer one that is NOT the
        // ISO code itself and is the shortest (glyphs like "$", "¥" over "US$", "JP¥").
        private static string BestSymbolForIso(string iso, IEnumerable<string> candidates)
        {
            if (CuratedSymbols.TryGetValue(iso, out var curated)) return curated;

            var usable = candidates
                .Where(s => !string.IsNullOrWhiteSpace(s) && s != iso)
                .OrderBy(s => s.Length)
                .ToList();

            return usable.FirstOrDefault() ?? iso;
        }

        public static List<CurrencyModel?>? Currencies
        {
            get
            {
                if (_currencies == null)
                {
                    // Group all specific cultures by ISO currency so we can choose the best symbol.
                    var byIso = CultureInfo
                        .GetCultures(CultureTypes.SpecificCultures)
                        .Select(culture =>
                        {
                            try
                            {
                                var region = new RegionInfo(culture.Name);
                                return (region.ISOCurrencySymbol, region.CurrencySymbol, region.CurrencyEnglishName);
                            }
                            catch
                            {
                                return default;
                            }
                        })
                        .Where(x => !string.IsNullOrEmpty(x.ISOCurrencySymbol))
                        .GroupBy(x => x.ISOCurrencySymbol)
                        .Select(g => new CurrencyModel(
                            g.First().CurrencyEnglishName,
                            BestSymbolForIso(g.Key, g.Select(x => x.CurrencySymbol)),
                            g.Key))
                        .ToList();

                    var topCurrencies = byIso
                        .Where(c => TopISO.Contains(c.IsoCode))
                        .OrderBy(c => Array.IndexOf(TopISO, c.IsoCode))
                        .ToList();

                    var otherCurrencies = byIso
                        .Where(c => !TopISO.Contains(c.IsoCode))
                        .OrderBy(c => c.Name)
                        .ToList();

                    _currencies = topCurrencies.Concat(otherCurrencies).Cast<CurrencyModel?>().ToList();
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
            if (CuratedSymbols.TryGetValue(isoCode, out var curated)) return curated;
            var currency = Currencies?.FirstOrDefault(c => c?.IsoCode == isoCode);
            return currency?.Symbol ?? "$";
        }

        // The ISO currency code for the device's current region (e.g. "USD", "GBP", "INR"),
        // used as the first-launch default before the user picks one explicitly. Falls back
        // to AUD if the device region can't be resolved or isn't in our currency list.
        public static string GetDefaultCurrencyIso()
        {
            try
            {
                var iso = RegionInfo.CurrentRegion?.ISOCurrencySymbol;
                if (!string.IsNullOrWhiteSpace(iso) &&
                    Currencies?.Any(c => c?.IsoCode == iso) == true)
                {
                    return iso;
                }
            }
            catch
            {
                // ignore — fall through to default
            }
            return "AUD";
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
