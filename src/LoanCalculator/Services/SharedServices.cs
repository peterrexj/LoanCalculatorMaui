using LoanCalculator.Models.Income.Summary;
using LoanCalculatorMaui.ViewModel;
using Pj.Library;

namespace LoanCalculatorMaui.Services
{
    public interface ISharedServices
    {
        IAppInformation AppInformation { get; }
        ILocalStorage LocalStorage { get; }
        INameValueDataService NameValueDataService { get; }
        IncomeExpenseSummary ExpenseSummary { get; }
        IncomeExpenseSummary IncomeSummary { get; }
        bool ShouldShowAppLaunchDisclaimer();
        string GetAppLaunchDisclaimerData();
    }

    public class SharedServices(ILocalStorage localStorage, INameValueDataService nameValueDataService, IAppInformation appInformation) 
        : ISharedServices
    {
        public ILocalStorage LocalStorage { get; } = localStorage;
        public INameValueDataService NameValueDataService { get; } = nameValueDataService;
        public IAppInformation AppInformation { get; } = appInformation;


        public IncomeExpenseSummary ExpenseSummary
        {
            get
            {
                ExpenseViewModel temp = null;
                Task.Run(async () => temp = await LocalStorage.GetData<ExpenseViewModel>()).Wait();


                if (temp == null)
                {
                    return new IncomeExpenseSummary();
                }

                temp?.Expenses?.SumUpData();
                return temp?.Expenses?.IncomeExpenseSummary;
            }
        }

        public IncomeExpenseSummary IncomeSummary
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

        public bool ShouldShowAppLaunchDisclaimer()
        {
            return NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer != true;
        }


        private static string disclaimerData;
        public string GetAppLaunchDisclaimerData()
        {
            if (disclaimerData.IsEmpty())
            {
                disclaimerData = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui").GetEmbeddedResourceAsText("LoanCalculatorMaui.Extensions.DisclaimerData.AppLaunchDisclaimerData.txt");
            }
            return disclaimerData;
            
        }
    }
}
