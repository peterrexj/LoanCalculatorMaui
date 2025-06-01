using LoanCalculator.Core.Models.BaseExtensions;
using LoanCalculator.Core.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LoanCalculatorMaui.Services;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class InAppPurchaseViewModel : BasePropertyChangeModel
    {
        private readonly IInAppPurchaseService _inAppPurchaseService;
        private readonly IAlertService _alertService;
        private readonly IAppInformation _appInformation;

        public ICommand IgnoreOfferCommand { get; }
        public ICommand PurchaseCommand { get; }
        public ICommand RestoreCommand { get; }

        public InAppPurchaseViewModel(IInAppPurchaseService inAppPurchaseService, IAlertService alertService, IAppInformation appInformation)
        {
            _alertService = alertService;
            _inAppPurchaseService = inAppPurchaseService;
            _appInformation = appInformation;

            Features = new ObservableCollection<string>
            {
                "Get detailed information on everything",
                "Export all tracked data easily",
                "Save your entered data",
                "Track expenses for new investments",
                "Estimate your yearly income over a period",
                "Estimate your yearly expenditure over a period"
            };


            PurchaseCommand = new Command(async () => await PurchaseProductAsync());
            RestoreCommand = new Command(async () => await RestorePurchasesAsync());
            IgnoreOfferCommand = new Command(() => ShowPremiumBuyWindow = false);
        }

        private ObservableCollection<string> _features;
        public ObservableCollection<string> Features
        {
            get => _features;
            set
            {
                if (SetProperty(ref _features, value))
                {
                    OnPropertyChanged(nameof(Features));
                }
            }
        }


        private bool _showPremiumBuyWindow;
        public bool ShowPremiumBuyWindow
        {
            get => _showPremiumBuyWindow;
            set
            {
                if (SetProperty(ref _showPremiumBuyWindow, value))
                {
                    OnPropertyChanged(nameof(ShowPremiumBuyWindow));
                }
            }
        }

        /// <summary>
        /// Calls the purchase method in the in-app purchase service.
        /// </summary>
        private async Task PurchaseProductAsync()
        {
            var productId = _appInformation.InAppProductId;
            if (string.IsNullOrWhiteSpace(productId))
                return;

            var result = await _inAppPurchaseService.PurchaseProductAsync(productId);
            if (result?.Success == true)
            {
                SharedServiceCore.UpdateToPremium();
                ShowPremiumBuyWindow = false;
            }
        }

        /// <summary>
        /// Calls the restore method in the in-app purchase service.
        /// </summary>
        private async Task RestorePurchasesAsync()
        {
            var result = await _inAppPurchaseService.RestorePurchasesAsync(_appInformation.InAppProductId);
            if (result) //restore was successful
            {
                ShowPremiumBuyWindow = false;
            }
        }

    }
}
