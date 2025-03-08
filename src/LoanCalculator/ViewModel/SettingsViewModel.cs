using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.Themes;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace LoanCalculatorMaui.ViewModel
{
    public class Theme
    {
        public string Name { get; set; }
        public Color BoxBackgroundGradientBrushStart { get; set; }
        public Color BoxBackgroundGradientBrushEnd { get; set; }

        public Color TextBackground { get; set; }
    }

    public class SettingsViewModel(IErrorHandlingService errorHandlingService, IAlertService alertService) : ViewModelUiBase
    {
        [JsonIgnore]
        private readonly IErrorHandlingService _errorHandlingService = errorHandlingService;
        [JsonIgnore]
        private readonly IAlertService _alertService = alertService;

        [JsonIgnore]
        public ICommand ClearLoanDataCommand { get; }
        [JsonIgnore]
        public ICommand ClearExpenseDataCommand { get; }
        [JsonIgnore]
        public ICommand ClearIncomeDataCommand { get; }
        [JsonIgnore]
        public ICommand ClearAllDataCommand { get; }
        [JsonIgnore]
        public ICommand ShowDisclaimerCommand { get; }
        [JsonIgnore]
        public ICommand PopupCloseCommand { get; }

        public SettingsViewModel() : this(ServiceLocator.GetService<IErrorHandlingService>(), ServiceLocator.GetService<IAlertService>())
        {
            Themes = new ObservableCollection<string>(EnumHelper<AppThemes>.List);

            ClearLoanDataCommand = new Command(async () => await ClearLoanData());
            ClearIncomeDataCommand = new Command(async () => await ClearIncomeData());
            ClearExpenseDataCommand = new Command(async () => await ClearExpenseData());
            ClearAllDataCommand = new Command(async () => await ClearDisclaimerData());
            ShowDisclaimerCommand = new Command( async () => await ShowDisclaimer());
            PopupCloseCommand = new Command(() => IsPopupRequired = false);
        }

        [JsonIgnore]
        public ObservableCollection<string> Themes { get; }
        
        [JsonIgnore]
        private string? _selectedTheme;
        [JsonIgnore]
        public string? SelectedTheme
        {
            get
            {
                if (_selectedTheme == null)
                {
                    StyleProvider.GetCurrentThemeAsync().ContinueWith(task =>
                    {
                        _selectedTheme = task.Result != null ? 
                            Themes.FirstOrDefault(t => t == task.Result.ToString()) : 
                            Themes.FirstOrDefault(t => t == AppThemes.Light.ToString());
                    });
                }
                return _selectedTheme;
            }
            set
            {
                if (value == null) return;
                if (isUpdating) return;
                if (_selectedTheme == value) return;

                _selectedTheme = value;
                IsBusy = true;

                MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        isUpdating = true;

                        var appTheme = EnumHelper<AppThemes>.FromString(_selectedTheme);
                        await SaveAndApplyApplicationThemeAsync(appTheme);

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception (e.g., log it, show a message to the user, etc.)
                        _errorHandlingService.HandleException(ex);
                    }
                    finally
                    {
                        isUpdating = false;
                        IsBusy = false;
                    }
                });
            }
        }

        private async Task SaveAndApplyApplicationThemeAsync(AppThemes theme)
        {
            SharedServices.ClearDisclaimerData();
            await SharedServices.SaveData(new ThemeSelect { Theme = theme });
            await ApplyApplicationThemeAsync(theme);
        }

        private async Task ApplyApplicationThemeAsync(AppThemes theme)
        {
            await MainThread.InvokeOnMainThreadAsync(() => StyleProvider.LoadDefaultStyle(theme));
        }

        private async Task ClearLoanData() => await SharedServices.LocalStorage.ClearData<LoanViewModel>();
        private async Task ClearIncomeData() => await SharedServices.LocalStorage.ClearData<IncomeViewModel>();
        private async Task ClearExpenseData() => await SharedServices.LocalStorage.ClearData<ExpenseViewModel>();
        private async Task ClearDisclaimerData() //TODO: Change this from clearing disclaimer to clearing all data
        {
            await Task.Run(() =>
            {
                //new PdfGenerator().GeneratePdf();
                SharedServices.NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer = false;
                SharedServices.NameValueDataService.SaveNameValueData();
            });
        }
        public string AppLaunchDisclaimerData => SharedServices.DisclaimerData;
        private bool _isPopupRequired;
        public bool IsPopupRequired
        {
            get => _isPopupRequired;
            set
            {
                _isPopupRequired = value;
                OnPropertyChanged(nameof(IsPopupRequired));
            }
        }
        private async Task ShowDisclaimer()
        {
            IsPopupRequired = true;
            IsActive = false;

            await Task.Delay(3000);

            IsActive = true;
        }
        public void RefreshProperties()
        {
            OnPropertyChanged(nameof(SelectedTheme));
        }
    }
}
