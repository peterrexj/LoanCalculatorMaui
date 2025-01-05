using LoanCalculatorMaui.View;

namespace LoanCalculatorMaui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(LoanView), typeof(LoanView));
            Routing.RegisterRoute(nameof(ExpenseView), typeof(ExpenseView));
            Routing.RegisterRoute(nameof(SettingsView), typeof(SettingsView));
        }
    }
}
