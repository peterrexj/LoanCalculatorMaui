using LoanCalculator.Models.Income.Summary;
using LoanCalculatorMaui.Themes;
using LoanCalculatorMaui.ViewModel;
using Pj.Library;

namespace LoanCalculatorMaui.Services
{
    //public interface ISharedServices
    //{
    //    IAppInformation AppInformation { get; }
    //    ILocalStorage LocalStorage { get; }
    //    INameValueDataService NameValueDataService { get; }
    //    IThemeHelper ThemeHelper { get; }
    //    IncomeExpenseSummary ExpenseSummary { get; }
    //    IncomeExpenseSummary IncomeSummary { get; }
    //    bool ShouldShowAppLaunchDisclaimer();
    //    string GetAppLaunchDisclaimerData();
    //}

    public static class SharedServices
    {
        // Properties to hold dependencies
        public static ILocalStorage? LocalStorage { get; private set; }
        public static INameValueDataService? NameValueDataService { get; private set; }
        public static IAppInformation? AppInformation { get; private set; }
        public static IThemeHelper? ThemeHelper { get; private set; }

        // Initialization method for dependency injection
        public static void Initialize(
            ILocalStorage? localStorage,
            INameValueDataService? nameValueDataService,
            IAppInformation? appInformation,
            IThemeHelper? themeHelper)
        {
            LocalStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
            NameValueDataService = nameValueDataService ?? throw new ArgumentNullException(nameof(nameValueDataService));
            AppInformation = appInformation ?? throw new ArgumentNullException(nameof(appInformation));
            ThemeHelper = themeHelper ?? throw new ArgumentNullException(nameof(themeHelper));
        }

        public static IncomeExpenseSummary ExpenseSummary
        {
            get
            {
                ExpenseViewModel temp = null;
                Task.Run(async () => temp = await LocalStorage?.GetData<ExpenseViewModel>()).Wait();

                if (temp == null)
                {
                    return new IncomeExpenseSummary();
                }

                temp?.Expenses?.SumUpData();
                return temp?.Expenses?.IncomeExpenseSummary;
            }
        }

        public static IncomeExpenseSummary IncomeSummary
        {
            get
            {
                IncomeViewModel temp = null;
                Task.Run(async () => temp = await LocalStorage.GetData<IncomeViewModel>()).Wait();

                if (temp == null)
                {
                    return new IncomeExpenseSummary();
                }

                temp?.Incomes?.SumUpData();
                return temp?.Incomes?.IncomeExpenseSummary;
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
                disclaimerData = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui").GetEmbeddedResourceAsText("LoanCalculatorMaui.Extensions.DisclaimerData.AppLaunchDisclaimerData.txt");
            }
            return disclaimerData;

        }
    }




    //public class SharedServices1(ILocalStorage localStorage, INameValueDataService nameValueDataService, IAppInformation appInformation) 
    //    : ISharedServices
    //{
    //    public ILocalStorage LocalStorage { get; } = localStorage;
    //    public INameValueDataService NameValueDataService { get; } = nameValueDataService;
    //    public IAppInformation AppInformation { get; } = appInformation;


    //    public IncomeExpenseSummary ExpenseSummary
    //    {
    //        get
    //        {
    //            ExpenseViewModel temp = null;
    //            Task.Run(async () => temp = await LocalStorage.GetData<ExpenseViewModel>()).Wait();


    //            if (temp == null)
    //            {
    //                return new IncomeExpenseSummary();
    //            }

    //            temp?.Expenses?.SumUpData();
    //            return temp?.Expenses?.IncomeExpenseSummary;
    //        }
    //    }

    //    public IncomeExpenseSummary IncomeSummary
    //    {
    //        get
    //        {
    //            IncomeViewModel temp = null;
    //            Task.Run(async () => temp = await LocalStorage.GetData<IncomeViewModel>()).Wait();

    //            if (temp == null)
    //            {
    //                return new IncomeExpenseSummary();
    //            }

    //            temp?.Incomes?.SumUpData();
    //            return temp?.Incomes?.IncomeExpenseSummary;
    //        }
    //    }

    //    public bool ShouldShowAppLaunchDisclaimer()
    //    {
    //        return NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer != true;
    //    }


    //    private static string disclaimerData;
    //    public string GetAppLaunchDisclaimerData()
    //    {
    //        if (disclaimerData.IsEmpty())
    //        {
    //            disclaimerData = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui").GetEmbeddedResourceAsText("LoanCalculatorMaui.Extensions.DisclaimerData.AppLaunchDisclaimerData.txt");
    //        }
    //        return disclaimerData;

    //    }
    //}
}
