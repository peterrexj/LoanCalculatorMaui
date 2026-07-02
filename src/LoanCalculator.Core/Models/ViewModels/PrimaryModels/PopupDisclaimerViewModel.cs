using LoanCalculator.Core.Models.BaseExtensions;
using LoanCalculator.Core.Services;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Input;
#pragma warning disable SYSLIB1045

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels;

public class DisclaimerSection
{
    public string Text { get; set; } = string.Empty;
    public bool IsHeader { get; set; }
}

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

    private ObservableCollection<DisclaimerSection> _disclaimerSections = new();
    public ObservableCollection<DisclaimerSection> DisclaimerSections
    {
        get => _disclaimerSections;
        set
        {
            _disclaimerSections = value;
            OnPropertyChanged(nameof(DisclaimerSections));
        }
    }

    public void TriggerChange()
    {
        DisclaimerSections = ParseDisclaimerHtml(SharedServiceCore.DisclaimerData);
    }

    private static ObservableCollection<DisclaimerSection> ParseDisclaimerHtml(string html)
    {
        var sections = new ObservableCollection<DisclaimerSection>();
        if (string.IsNullOrWhiteSpace(html))
            return sections;

        foreach (Match m in Regex.Matches(html, @"<h2[^>]*>(.*?)</h2>|<p[^>]*>(.*?)</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
        {
            string raw = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            string text = Regex.Replace(raw, @"<[^>]+>", string.Empty).Trim();
            if (string.IsNullOrEmpty(text)) continue;
            sections.Add(new DisclaimerSection
            {
                Text = text,
                IsHeader = m.Groups[1].Success
            });
        }
        return sections;
    }

    private void OnPopupAccept()
    {
        SharedServiceCore.SetAppLaunchDisclaimerShown();
        IsPopupRequired = false;
    }
}