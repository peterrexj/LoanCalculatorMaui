using LoanCalculator.Core.Models.BaseExtensions;
using LoanCalculator.Core.Services;
using Syncfusion.Maui.Core.Carousel;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels;

public class PopupDisclaimerViewModel : BaseViewModel
{
    [JsonIgnore]
    private readonly IErrorHandlingService _errorHandlingService;

    public ICommand PopupAcceptCommand { get; }
    public PopupDisclaimerViewModel(IErrorHandlingService errorHandlingService)
    {
        _errorHandlingService = errorHandlingService;
        PopupAcceptCommand = new Command(OnPopupAccept);
        IsActive = false;
    }

    private bool? _isPopupRequired;
    public bool IsPopupRequired
    {
        get
        {
            if (_isPopupRequired.HasValue == false)
            {
                if (SharedServiceCore.LocalStorage.IsInitialized)
                {
                    _isPopupRequired = SharedServiceCore.ShouldShowAppLaunchDisclaimer();
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

    private string _appLaunchDisclaimerData = string.Empty;
    public string AppLaunchDisclaimerData
    {
        get
        {
            return _appLaunchDisclaimerData;
        }
        set
        {
            _appLaunchDisclaimerData = value;
            OnPropertyChanged(nameof(AppLaunchDisclaimerData));
        }
    }

    public void TriggerChange()
    {
        AppLaunchDisclaimerData = string.Empty;
        AppLaunchDisclaimerData = SharedServiceCore.DisclaimerData;
        OnPropertyChanged(nameof(AppLaunchDisclaimerData));
    }

    private void OnPopupAccept()
    {
        SharedServiceCore.SetAppLaunchDisclaimerShown();
        IsPopupRequired = false;
    }
}