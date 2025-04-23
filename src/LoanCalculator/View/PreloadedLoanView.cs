using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;

namespace LoanCalculatorMaui.View
{
    public class PreloadedView
    {
        public static readonly LoanView LoanViewInstance = new LoanView(SharedServiceCore.ErrorHandlingService, ServiceLocator.GetService<LoanViewModel>(), ServiceLocator.GetService<IThemeHandler>());
        public static readonly IncomeView IncomeInstance = new IncomeView(SharedServiceCore.ErrorHandlingService, SharedServiceCore.AlertService, ServiceLocator.GetService<IncomeViewModel>(), ServiceLocator.GetService<IThemeHandler>());
        public static readonly ExpenseView ExpenseInstance = new ExpenseView(SharedServiceCore.ErrorHandlingService, SharedServiceCore.AlertService, ServiceLocator.GetService<ExpenseViewModel>(), ServiceLocator.GetService<IThemeHandler>());
        public static readonly SettingsView SettingInstance = new SettingsView(SharedServiceCore.ErrorHandlingService, ServiceLocator.GetService<SettingsViewModel>());
    }
}
