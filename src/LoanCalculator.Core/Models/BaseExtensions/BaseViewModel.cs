using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Models.BaseExtensions
{
    public class BaseViewModel : BasePropertyChangeModel
    {
        private bool _isBusy;
        [JsonIgnore]
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(IsCustomBusyIndicator));
                OnPropertyChanged(nameof(IsFree));
            }
        }
        public bool IsFree => !IsBusy;

        public bool IsCustomBusyIndicator => IsBusy || IsPageBusy;

        private bool _isPageBusy;
        [JsonIgnore]
        public bool IsPageBusy
        {
            get => _isPageBusy;
            set
            {
                _isPageBusy = value;
                OnPropertyChanged(nameof(IsPageBusy));
                OnPropertyChanged(nameof(IsCustomBusyIndicator));
            }
        }

        private bool _isActive { get; set; }
        [JsonIgnore]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        
    }
}
