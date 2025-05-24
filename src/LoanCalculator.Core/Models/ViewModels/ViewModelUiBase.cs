using System.Text.Json.Serialization;
using LoanCalculator.Core.Models.BaseExtensions;

namespace LoanCalculator.Core.Models.ViewModels
{
    public class ViewModelUiBase : BaseViewModel
    {
        private string _currencySymbol;

        [JsonIgnore]
        public string CurrencySymbol
        {
            get => _currencySymbol;
            set
            {
                _currencySymbol = value;
                OnPropertyChanged(nameof(CurrencySymbol));
                OnPropertyChanged(nameof(CurrencyFormat));
            }
        }

        [JsonIgnore]
        public string CurrencyFormat => $"{CurrencySymbol}#,##0";


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
            NewLine = Environment.NewLine;
        }
    }
}
