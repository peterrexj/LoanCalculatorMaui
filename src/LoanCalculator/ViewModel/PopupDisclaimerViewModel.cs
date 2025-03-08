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
                //SharedServices.NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer = false;
                //SharedServices.NameValueDataService.SaveNameValueData();
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

    public string AppLaunchDisclaimerData => ReplaceColorsWithResourceKeys(SharedServices.DisclaimerData);

    private string ReplaceColorsWithResourceKeys(string content)
    {
        try
        {
            var colorMappings = new Dictionary<string, string>
            {
                { "#758d84", "LoanAppDisclaimerBodyBackgroundColor" },
                { "#091818", "LoanAppDisclaimerHeaderBackgroundColor" },
                { "#b9c4c4", "LoanAppDisclaimerHeaderTextColor" },
                { "#0E8388", "LoanAppDisclaimerHeaderBorderColor" },
                { "#dee7e4", "LoanAppDisclaimerContentBackgroundColor" },
                { "#2c3531", "LoanAppDisclaimerContentBoxShadowColor" },
                { "#091817", "LoanAppDisclaimerHeader2TextColor" }
            };

            foreach (var mapping in colorMappings)
            {
                if (Application.Current.Resources.TryGetValue(mapping.Value, out var resourceValue) && resourceValue is Color color)
                {
                    var colorHex = color.ToHex();
                    content = content.Replace(mapping.Key, colorHex);
                }
            }
        }
        catch (Exception e)
        {
            _errorHandlingService.HandleException(e);
        }

        return content;
    }

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
