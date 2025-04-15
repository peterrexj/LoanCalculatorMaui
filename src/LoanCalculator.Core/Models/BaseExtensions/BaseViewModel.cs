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
                //OnPropertyChanged(nameof(IsFree));
            }
        }
        //public bool IsFree => !IsBusy;

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
