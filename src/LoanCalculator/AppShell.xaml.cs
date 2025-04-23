using LoanCalculatorMaui.View;

namespace LoanCalculatorMaui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            //Routing.RegisterRoute(nameof(LoanView), typeof(LoanView));
            //Routing.RegisterRoute(nameof(ExpenseView), typeof(ExpenseView));
            //Routing.RegisterRoute(nameof(IncomeView), typeof(IncomeView));
            //Routing.RegisterRoute(nameof(SettingsView), typeof(SettingsView));
            //this.Navigating += OnShellNavigating;
        }

        //private async void OnShellNavigating(object sender, ShellNavigatingEventArgs e)
        //{
            
        //    //// Only act on tab navigation, not modal or stack
        //    //if (e.Source == ShellNavigationSource.ShellItemChanged)
        //    //{
        //    //    // Cancel navigation temporarily
        //    //    //e.Cancel();

        //    //    // Show your visual feedback
        //    //    //await ShowTabTapAnimationOrToast();

        //    //    // Then navigate manually
        //    //    await Shell.Current.GoToAsync(e.Target.Location.OriginalString, true);
        //    //}
        //}
    }
}
