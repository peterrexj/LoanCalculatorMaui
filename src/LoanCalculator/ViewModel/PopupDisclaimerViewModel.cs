using System.Text.Json.Serialization;
using LoanCalculator.Models.BaseExtensions;
using LoanCalculatorMaui.Services;
using System.Windows.Input;

namespace LoanCalculatorMaui.ViewModel;
public class PopupDisclaimerViewModel : BaseViewModel
{
    [JsonIgnore]
    private readonly IErrorHandlingService _errorHandlingService;
    [JsonIgnore]
    private readonly IAlertService _alertService;

    private bool? _isPopupRequired;
    public bool IsPopupRequired
    {
        get
        {
            if (_isPopupRequired.HasValue == false)
            {
                if (SharedServices.LocalStorage.IsInitialized)
                {
                    _isPopupRequired = SharedServices.ShouldShowAppLaunchDisclaimer();
                }
            }

            if (_isPopupRequired.HasValue == false)
            {
                return true;
            }

            return _isPopupRequired.Value;
        }
        set
        {
            _isPopupRequired = value;
            OnPropertyChanged(nameof(IsPopupRequired));
        }
    }

    public string AppLaunchDisclaimerData => SharedServices.DisclaimerData;

    public ICommand PopupAcceptCommand { get; }
    public PopupDisclaimerViewModel(IErrorHandlingService errorHandlingService, IAlertService alertService)
    {
        _errorHandlingService = errorHandlingService;
        _alertService = alertService;
        PopupAcceptCommand = new Command(OnPopupAccept);
        IsActive = false;
    }

    private void OnPopupAccept()
    {
        SharedServices.SetAppLaunchDisclaimerShown();
        IsPopupRequired = false;
    }
}
