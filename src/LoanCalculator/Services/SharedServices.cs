using LoanCalculator.Models;
using LoanCalculator.Models.Income.Summary;
using LoanCalculatorMaui.Themes;
using LoanCalculatorMaui.ViewModel;
using Pj.Library;

namespace LoanCalculatorMaui.Services
{
    public static class SharedServices
    {
        private static ILocalStorage? _localStorage;
        public static ILocalStorage? LocalStorage => _localStorage ??= ServiceLocator.GetService<ILocalStorage>();

        private static INameValueDataService? _nameValueDataService;
        public static INameValueDataService? NameValueDataService => _nameValueDataService ??= ServiceLocator.GetService<INameValueDataService>();

        private static IAppInformation? _appInformation;
        public static IAppInformation? AppInformation => _appInformation ??= ServiceLocator.GetService<IAppInformation>();

        private static IThemeHelper? _themeHelper;
        public static IThemeHelper? ThemeHelper => _themeHelper ??= ServiceLocator.GetService<IThemeHelper>();


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
            Task.Run(async () => temp = await LocalStorage?.GetData<TViewModel>()).Wait();

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
            Task.Run(async () => temp = await LocalStorage?.GetData<LoanViewModel>()).Wait();
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
            return NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer != true;
        }


        private static string disclaimerData;
        public static string GetAppLaunchDisclaimerData()
        {
            if (disclaimerData.IsEmpty())
            {
                disclaimerData = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui").GetEmbeddedResourceAsText("LoanCalculatorMaui.Extensions.DisclaimerData.AppLaunchDisclaimerData.html")
                    .Replace("{{AppName}}", "LoanCalcPro");
            }
            return disclaimerData;

        }
    }
}
