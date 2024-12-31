using LoanCalculator.Models.BaseExtensions;
using LoanCalculatorMaui.Services;
using System.Windows.Input;

namespace LoanCalculatorMaui.ViewModel;
public class PopupDisclaimerViewModel : BaseViewModel
{
    private bool? _isPopupRequired;
    public bool IsPopupRequired
    {
        get
        {
            if (_isPopupRequired.HasValue == false)
            {
                //SharedServices.NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer = false;
                //SharedServices.NameValueDataService.SaveNameValueData();

                _isPopupRequired = SharedServices.ShouldShowAppLaunchDisclaimer();
            }

            return _isPopupRequired.Value;
        }
        set
        {
            _isPopupRequired = value;
            OnPropertyChanged(nameof(IsPopupRequired));
        }
    }

    public string AppLaunchDisclaimerData => SharedServices.GetAppLaunchDisclaimerData();

    public ICommand PopupAcceptCommand { get; }

    public PopupDisclaimerViewModel()
    {
        PopupAcceptCommand = new Command(OnPopupAccept);
    }

    private void OnPopupAccept()
    {
        SharedServices.NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer = true;
        SharedServices.NameValueDataService.SaveNameValueData();
        IsPopupRequired = false;
    }
}
