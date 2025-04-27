using System.Text.Json.Serialization;
using LoanCalculator.Core.Models.BaseExtensions;

namespace LoanCalculator.Core.Models.ViewModels
{
    public class ViewModelUiBase : BaseViewModel
    {
        [JsonIgnore] public string CurrencySymbol { get; set; }
        [JsonIgnore] public string NewLine { get; set; }

        protected bool isUpdating = false;
        [JsonIgnore] public bool IsUpdating
        {
            get => isUpdating;
            set
            {
                isUpdating = value;
            }
        }

        private bool _showPremiumBuyOption;
        [JsonIgnore] public bool ShowPremiumBuyOption
        {
            get => _showPremiumBuyOption;
            set
            {
                _showPremiumBuyOption = value;
                OnPropertyChanged(nameof(ShowPremiumBuyOption));
            }
        }

        public ViewModelUiBase()
        {
            CurrencySymbol = Helper.CurrencySymbol;
            NewLine = Environment.NewLine;
        }
    }
}
