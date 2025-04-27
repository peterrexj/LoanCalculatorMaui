using LoanCalculator.Core.Models.BaseExtensions;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class InAppPurchaseViewModel : BasePropertyChangeModel
    {
        public InAppPurchaseViewModel()
        {
            Features = new ObservableCollection<string>
            {
                "Get detailed information on everything",
                "Export all tracked data easily",
                "Save your entered data",
                "Track expenses for new investments",
                "Estimate your yearly income over a period",
                "Estimate your yearly expenditure over a period"
            };

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

        public ICommand IgnoreOfferCommand { get; }
    }
}
