using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using LoanCalculator.Core.Themes;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class SettingsViewModel : ViewModelUiBase
    {
        [JsonIgnore] private readonly IErrorHandlingService _errorHandlingService;
        [JsonIgnore] private readonly IAlertService _alertService;
        [JsonIgnore] private readonly IThemeHandler _themeHandler;


        [JsonIgnore] public ICommand DeleteLoanDataCommand { get; }
        [JsonIgnore] public ICommand DeleteExpenseDataCommand { get; }
        [JsonIgnore] public ICommand DeleteIncomeDataCommand { get; }
        [JsonIgnore] public ICommand DeleteAllDataCommand { get; }
        [JsonIgnore] public ICommand ShowDisclaimerCommand { get; }
        [JsonIgnore] public ICommand OnShareAppRequestCommand { get; }
        [JsonIgnore] public ICommand OnRateAppRequestCommand { get; }
        [JsonIgnore] public ICommand PopupCloseCommand { get; }
        [JsonIgnore]
        public bool IsAllDataDeleteVisible
        {
            get
            {
#if DEBUG
                return true; // Enable in debug mode for testing
#else
                 return false; // Disable in release mode
#endif
            }
        }

        public SettingsViewModel()
        {

        }

        public SettingsViewModel(IErrorHandlingService errorHandlingService, IAlertService alertService, IThemeHandler themeHandler)
        {
            _errorHandlingService = errorHandlingService;
            _alertService = alertService;
            _themeHandler = themeHandler;

            Themes = new ObservableCollection<string>(EnumHelper<AppThemes>.List);

            InitializeSelectedTheme();

            DeleteLoanDataCommand = new Command(async () => await DeleteLoanData());
            DeleteIncomeDataCommand = new Command(async () => await DeleteIncomeData());
            DeleteExpenseDataCommand = new Command(async () => await DeleteExpenseData());
            DeleteAllDataCommand = new Command(async () => await DeleteAllData());
            ShowDisclaimerCommand = new Command(async () => await ShowDisclaimer());
            PopupCloseCommand = new Command(() => IsPopupRequired = false);
            OnShareAppRequestCommand = new Command(OnShareAppRequest);
            OnRateAppRequestCommand = new Command(OnRateAppRequest);
        }

        #region Currencies

        [JsonIgnore]
        public ObservableCollection<CurrencyModel> Currencies { get; } =
            new ObservableCollection<CurrencyModel>(
                (SharedServiceCore.Currencies ?? new List<CurrencyModel?>
                {
                    new CurrencyModel("Australian Dollar", "$", "AUD")
                })
                .Where(c => c != null)!
            );

        private CurrencyModel? _selectedCurrency;
        [JsonIgnore]
        public CurrencyModel? SelectedCurrency
        {
            get => _selectedCurrency;
            set
            {
                if (_selectedCurrency != value)
                {
                    _selectedCurrency = value;
                    OnPropertyChanged(nameof(SelectedCurrency));
                    Preferences.Set(SharedServiceCore.SelectedCurrencyKey, _selectedCurrency?.IsoCode);
                    Helper.CurrencySymbol = SharedServiceCore.GetCurrencySymbol(_selectedCurrency?.IsoCode);
                }
            }
        }

        public void LoadSelectedCurrency()
        {
            SelectedCurrency = Currencies.FirstOrDefault(c => c.IsoCode == Preferences.Get(SharedServiceCore.SelectedCurrencyKey, "AUD"));
            OnPropertyChanged(nameof(SelectedCurrency));
        }

        #endregion


        [JsonIgnore]
        public ObservableCollection<string> Themes { get; }

        private void InitializeSelectedTheme()
        {
            var currentTheme = Task.Run(() => _themeHandler.GetCurrentThemeAsync()).Result;
            _selectedTheme = currentTheme != null
                ? Themes.FirstOrDefault(t => t == currentTheme.ToString())
                : Themes.FirstOrDefault(t => t == SharedServiceCore.DefaultAppTheme.ToString());
        }

        [JsonIgnore]
        private string? _selectedTheme;
        [JsonIgnore]
        public string? SelectedTheme
        {
            get
            {
                if (_selectedTheme == null)
                {
                    _themeHandler.GetCurrentThemeAsync().ContinueWith(task =>
                    {
                        _selectedTheme = task.Result != null ?
                            Themes.FirstOrDefault(t => t == task.Result.ToString()) :
                            Themes.FirstOrDefault(t => t == SharedServiceCore.DefaultAppTheme.ToString());
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

                // Await the theme change operation
                MainThread.BeginInvokeOnMainThread(async void () =>
                {
                    IsBusy = true; // Show spinner
                    IsUpdating = true;

                    await Task.Delay(500);
                    await ChangeThemeAsync(_selectedTheme);
                });
            }
        }

        private async Task ChangeThemeAsync(string selectedTheme)
        {
            try
            {
                var appTheme = EnumHelper<AppThemes>.FromString(selectedTheme);
                await SaveAndApplyApplicationThemeAsync(appTheme);

                OnPropertyChanged(nameof(SelectedTheme)); // Notify UI of the change
            }
            catch (Exception ex)
            {
                _errorHandlingService.HandleException(ex); // Handle any errors
            }
            finally
            {
                IsUpdating = false;
                IsBusy = false; // Hide spinner
            }
        }

        private async Task SaveAndApplyApplicationThemeAsync(AppThemes theme)
        {
            await SharedServiceCore.SaveData(new ThemeSelect { Theme = theme });
            await ApplyApplicationThemeAsync(theme);
        }

        private async Task ApplyApplicationThemeAsync(AppThemes theme)
        {
            await MainThread.InvokeOnMainThreadAsync(() => _themeHandler.LoadDefaultStyle(theme));
        }


        private async Task DeleteDataWithConfirmationAsync<T>(string title = "Important", string message = "Do you wish to delete the data?", string accept = "Yes", string cancel = "No")
        {
            var response = await _alertService.ShowConfirmationAsync(title, message, accept, cancel);
            if (!response)
                return;

            await SharedServiceCore.LocalStorage.ClearData<T>();
        }
        private async Task DeleteMultipleDataWithConfirmationAsync(IEnumerable<Func<Task>> clearActions, string title = "Important", string message = "Do you wish to delete the data?", string accept = "Yes", string cancel = "No")
        {
            var response = await _alertService.ShowConfirmationAsync(title, message, accept, cancel);
            if (!response)
                return;

            foreach (var action in clearActions)
            {
                await action();
            }
        }

        private async Task DeleteAllData()
        {
            await DeleteMultipleDataWithConfirmationAsync([
                () => SharedServiceCore.LocalStorage.ClearData<LoanViewModel>(),
                () => SharedServiceCore.LocalStorage.ClearData<IncomeViewModel>(),
                () => SharedServiceCore.LocalStorage.ClearData<ExpenseViewModel>()
            ]);
        }

        private async Task DeleteLoanData() => await DeleteDataWithConfirmationAsync<LoanViewModel>();
        private async Task DeleteIncomeData() => await DeleteDataWithConfirmationAsync<IncomeViewModel>();
        private async Task DeleteExpenseData() => await DeleteDataWithConfirmationAsync<ExpenseViewModel>();

        public string AppLaunchDisclaimerData => SharedServiceCore.DisclaimerData;
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
            ServiceLocator.GetService<PopupDisclaimerViewModel>().TriggerChange();

            await Task.Delay(3000);

            IsActive = true;
        }




        public void RefreshProperties()
        {
            OnPropertyChanged(nameof(SelectedTheme));
        }

        private async void OnShareAppRequest()
        {
            try
            {
                await Share.RequestAsync(new ShareTextRequest
                {
                    Uri = SharedServiceCore.AppInformation?.AppShareLink ?? "https://www.yoursimpleapps.com", //TODO: Change this to your app link
                    Title = "Check out this app!"
                });
            }
            catch (Exception e)
            {
                _errorHandlingService.HandleException(e);
            }
        }

        private async void OnRateAppRequest()
        {
            try
            {
                await Launcher.Default.OpenAsync(SharedServiceCore.AppInformation.RateAppLink);
            }
            catch (Exception e)
            {
                _errorHandlingService.HandleException(e);
            }
        }
    }
}
