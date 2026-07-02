using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui
{
    public partial class AppShell : Shell
    {
        private static readonly HashSet<string> _premiumRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            "WhatIf"
        };

        public AppShell()
        {
            InitializeComponent();
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            if (!SharedServiceCore.IsTrialUser) return;

            var target = args.Target?.Location?.OriginalString ?? string.Empty;
            if (_premiumRoutes.Any(r => target.Contains(r, StringComparison.OrdinalIgnoreCase)))
            {
                args.Cancel();
                ServiceLocator.GetService<InAppPurchaseViewModel>().ShowPremiumBuyWindow = true;
            }
        }
    }
}
