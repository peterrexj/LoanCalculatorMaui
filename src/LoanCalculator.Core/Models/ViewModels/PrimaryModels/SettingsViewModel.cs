using LoanCalculator.Core.Constants;
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
                    SharedServiceCore.MarkCurrencyDirty();
                }
            }
        }

        public void LoadSelectedCurrency()
        {
            SelectedCurrency = Currencies.FirstOrDefault(c => c.IsoCode ==
                Preferences.Get(SharedServiceCore.SelectedCurrencyKey, SharedServiceCore.GetDefaultCurrencyIso()));
            OnPropertyChanged(nameof(SelectedCurrency));
        }

        #endregion

        #region Font Family

        [JsonIgnore]
        public ObservableCollection<string> FontFamilies { get; } =
            new ObservableCollection<string>(
                SharedServiceCore.AppInformation?.GetRegisteredFontFamilies()
                ?? RegisteredFonts.GetFontFamilies()
            );

        [JsonIgnore] private string? _selectedFontFamily;
        [JsonIgnore]
        public string? SelectedFontFamily
        {
            get
            {
                if (_selectedFontFamily == null)
                {
                    SharedServiceCore.GetAppFontFamilyAsync().ContinueWith(task =>
                    {
                        if (task.IsFaulted || task.IsCanceled) return;
                        _selectedFontFamily = task.Result;
                        MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(SelectedFontFamily)));
                    });
                }
                return _selectedFontFamily;
            }
            set
            {
                if (value == null || isUpdating || _selectedFontFamily == value) return;
                _selectedFontFamily = value;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    IsPageBusy = true;
                    IsUpdating = true;
                    await Task.Delay(300);
                    await ChangeFontFamilyAsync(_selectedFontFamily);
                });
            }
        }

        private async Task ChangeFontFamilyAsync(string fontFamily)
        {
            try
            {
                await SharedServiceCore.SetAppFontFamilyAsync(fontFamily);
                await ApplyFontFamilyAsync(fontFamily);
                OnPropertyChanged(nameof(SelectedFontFamily));
            }
            catch (Exception ex) { _errorHandlingService.HandleException(ex); }
            finally { IsUpdating = false; IsPageBusy = false; }
        }

        private async Task ApplyFontFamilyAsync(string fontFamily)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (Application.Current?.Resources != null)
                    Application.Current.Resources["DefaultFontFamily"] = fontFamily;
            });
        }

        #endregion


        [JsonIgnore]
        public ObservableCollection<string> Themes { get; }

        private void InitializeSelectedTheme()
        {
            // Called during startup on the main thread — Task.Run avoids sync-context deadlock.
            var currentTheme = Task.Run(() => _themeHandler.GetCurrentThemeAsync()).GetAwaiter().GetResult();
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
                    IsPageBusy = true;
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
                IsPageBusy = false;
            }
        }

        private async Task SaveAndApplyApplicationThemeAsync(AppThemes theme)
        {
            SharedServiceCore.SaveData(new ThemeSelect { Theme = theme });
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
            OnPropertyChanged(nameof(SelectedFontFamily));
            LoadAustralianModeSetting();
            LoadStampDutySetting();
            LoadCalculatorDefaults();
            LoadWhatIfDefaults();
            LoadStepperIncrements();
        }

        #region Australian Mode

        private const string AustralianModeKey = "IsAustralianModeEnabled";

        [JsonIgnore]
        private bool _isAustralianModeEnabled;

        [JsonIgnore]
        public bool IsAustralianModeEnabled
        {
            get => _isAustralianModeEnabled;
            set
            {
                if (_isAustralianModeEnabled == value) return;
                _isAustralianModeEnabled = value;
                Preferences.Set(AustralianModeKey, value);
                OnPropertyChanged(nameof(IsAustralianModeEnabled));

                // When Australian mode changes, stamp duty toggle state and visibility also change
                LoadStampDutySetting();
                OnPropertyChanged(nameof(IsStampDutyToggleEnabled));

                // Push immediately to LoanViewModel so segment reacts without tab switch
                var loanViewModel = ServiceLocator.GetService<LoanViewModel>();
                if (loanViewModel != null)
                    loanViewModel.IsAustralianModeEnabled = value;
            }
        }

        public void LoadAustralianModeSetting()
        {
            _isAustralianModeEnabled = Preferences.Get(AustralianModeKey, false);
            OnPropertyChanged(nameof(IsAustralianModeEnabled));
        }

        #endregion

        #region Stamp Duty Toggle

        private const string StampDutyKey = "IsStampDutyEnabled";

        [JsonIgnore]
        private bool _isStampDutyEnabled;

        [JsonIgnore]
        public bool IsStampDutyEnabled
        {
            get => _isStampDutyEnabled;
            set
            {
                if (_isStampDutyEnabled == value) return;
                _isStampDutyEnabled = value;
                Preferences.Set(StampDutyKey, value);
                OnPropertyChanged(nameof(IsStampDutyEnabled));

                var loanViewModel = ServiceLocator.GetService<LoanViewModel>();
                if (loanViewModel != null)
                {
                    loanViewModel.IsStampDutyEnabled = value;
                    loanViewModel.LoadStampDutySetting();
                }
            }
        }

        // User cannot turn OFF stamp duty when Australian mode is active
        [JsonIgnore]
        public bool IsStampDutyToggleEnabled => !_isAustralianModeEnabled;

        public void LoadStampDutySetting()
        {
            _isStampDutyEnabled = _isAustralianModeEnabled || Preferences.Get(StampDutyKey, false);
            OnPropertyChanged(nameof(IsStampDutyEnabled));
            OnPropertyChanged(nameof(IsStampDutyToggleEnabled));
        }

        #endregion

        #region Calculator Defaults

        [JsonIgnore] private int _maxLoanTermYears;
        [JsonIgnore]
        public int MaxLoanTermYears
        {
            get => _maxLoanTermYears;
            set
            {
                if (_maxLoanTermYears == value) return;
                _maxLoanTermYears = value;
                SharedServiceCore.SetMaxLoanTermYears(value);
                OnPropertyChanged(nameof(MaxLoanTermYears));
                // Revalidate default term — it cannot exceed the new max
                if (_defaultLoanTermYears > value)
                {
                    _defaultLoanTermYears = value;
                    OnPropertyChanged(nameof(DefaultLoanTermYears));
                }
            }
        }

        [JsonIgnore] private int _defaultLoanTermYears;
        [JsonIgnore]
        public int DefaultLoanTermYears
        {
            get => _defaultLoanTermYears;
            set
            {
                if (_defaultLoanTermYears == value) return;
                _defaultLoanTermYears = value;
                SharedServiceCore.SetDefaultLoanTermYears(value);
                OnPropertyChanged(nameof(DefaultLoanTermYears));
            }
        }

        [JsonIgnore] private string _defaultInterestRateText = string.Empty;
        [JsonIgnore]
        public string DefaultInterestRateText
        {
            get => _defaultInterestRateText;
            set
            {
                _defaultInterestRateText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetDefaultInterestRate(d);
                OnPropertyChanged(nameof(DefaultInterestRateText));
            }
        }

        [JsonIgnore] private int _defaultProjectionYears;
        [JsonIgnore]
        public int DefaultProjectionYears
        {
            get => _defaultProjectionYears;
            set
            {
                if (_defaultProjectionYears == value) return;
                _defaultProjectionYears = value;
                SharedServiceCore.SetDefaultProjectionYears(value);
                OnPropertyChanged(nameof(DefaultProjectionYears));
            }
        }

        [JsonIgnore] private int _maxProjectionYears;
        [JsonIgnore]
        public int MaxProjectionYears
        {
            get => _maxProjectionYears;
            set
            {
                if (_maxProjectionYears == value) return;
                _maxProjectionYears = value;
                SharedServiceCore.SetMaxProjectionYears(value);
                OnPropertyChanged(nameof(MaxProjectionYears));
                if (_defaultProjectionYears > value)
                {
                    _defaultProjectionYears = value;
                    OnPropertyChanged(nameof(DefaultProjectionYears));
                }
            }
        }

        public void LoadCalculatorDefaults()
        {
            _maxLoanTermYears = SharedServiceCore.GetMaxLoanTermYears();
            _defaultLoanTermYears = SharedServiceCore.GetDefaultLoanTermYears();
            _defaultInterestRateText = SharedServiceCore.GetDefaultInterestRate().ToString("F2");
            _defaultProjectionYears = SharedServiceCore.GetDefaultProjectionYears();
            _maxProjectionYears = SharedServiceCore.GetMaxProjectionYears();
            OnPropertyChanged(nameof(MaxLoanTermYears));
            OnPropertyChanged(nameof(DefaultLoanTermYears));
            OnPropertyChanged(nameof(DefaultInterestRateText));
            OnPropertyChanged(nameof(DefaultProjectionYears));
            OnPropertyChanged(nameof(MaxProjectionYears));
        }

        #endregion

        #region What-If Defaults

        [JsonIgnore] private string _whatIfRateDeltaText = string.Empty;
        [JsonIgnore]
        public string WhatIfRateDeltaText
        {
            get => _whatIfRateDeltaText;
            set
            {
                _whatIfRateDeltaText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetWhatIfRateDelta(d);
                OnPropertyChanged(nameof(WhatIfRateDeltaText));
            }
        }

        [JsonIgnore] private string _whatIfExtraRepaymentText = string.Empty;
        [JsonIgnore]
        public string WhatIfExtraRepaymentText
        {
            get => _whatIfExtraRepaymentText;
            set
            {
                _whatIfExtraRepaymentText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetWhatIfExtraRepayment(d);
                OnPropertyChanged(nameof(WhatIfExtraRepaymentText));
            }
        }

        [JsonIgnore] private string _whatIfLumpSumText = string.Empty;
        [JsonIgnore]
        public string WhatIfLumpSumText
        {
            get => _whatIfLumpSumText;
            set
            {
                _whatIfLumpSumText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetWhatIfLumpSum(d);
                OnPropertyChanged(nameof(WhatIfLumpSumText));
            }
        }

        [JsonIgnore] private string _whatIfOffsetBalanceText = string.Empty;
        [JsonIgnore]
        public string WhatIfOffsetBalanceText
        {
            get => _whatIfOffsetBalanceText;
            set
            {
                _whatIfOffsetBalanceText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetWhatIfOffsetBalance(d);
                OnPropertyChanged(nameof(WhatIfOffsetBalanceText));
            }
        }

        public void LoadWhatIfDefaults()
        {
            _whatIfRateDeltaText = SharedServiceCore.GetWhatIfRateDelta().ToString("F2");
            _whatIfExtraRepaymentText = SharedServiceCore.GetWhatIfExtraRepayment().ToString("F0");
            _whatIfLumpSumText = SharedServiceCore.GetWhatIfLumpSum().ToString("F0");
            _whatIfOffsetBalanceText = SharedServiceCore.GetWhatIfOffsetBalance().ToString("F0");
            OnPropertyChanged(nameof(WhatIfRateDeltaText));
            OnPropertyChanged(nameof(WhatIfExtraRepaymentText));
            OnPropertyChanged(nameof(WhatIfLumpSumText));
            OnPropertyChanged(nameof(WhatIfOffsetBalanceText));
        }

        #endregion

        #region Stepper Increments

        [JsonIgnore] private string _loanRateStepText = string.Empty;
        [JsonIgnore]
        public string LoanRateStepText
        {
            get => _loanRateStepText;
            set
            {
                _loanRateStepText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetLoanRateStep(d);
                OnPropertyChanged(nameof(LoanRateStepText));
            }
        }

        [JsonIgnore] private string _whatIfRateStepText = string.Empty;
        [JsonIgnore]
        public string WhatIfRateStepText
        {
            get => _whatIfRateStepText;
            set
            {
                _whatIfRateStepText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetWhatIfRateStep(d);
                OnPropertyChanged(nameof(WhatIfRateStepText));
            }
        }

        [JsonIgnore] private string _whatIfMonthlyStepText = string.Empty;
        [JsonIgnore]
        public string WhatIfMonthlyStepText
        {
            get => _whatIfMonthlyStepText;
            set
            {
                _whatIfMonthlyStepText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetWhatIfMonthlyStep(d);
                OnPropertyChanged(nameof(WhatIfMonthlyStepText));
            }
        }

        [JsonIgnore] private string _whatIfLumpSumStepText = string.Empty;
        [JsonIgnore]
        public string WhatIfLumpSumStepText
        {
            get => _whatIfLumpSumStepText;
            set
            {
                _whatIfLumpSumStepText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetWhatIfLumpSumStep(d);
                OnPropertyChanged(nameof(WhatIfLumpSumStepText));
            }
        }

        [JsonIgnore] private string _whatIfOffsetStepText = string.Empty;
        [JsonIgnore]
        public string WhatIfOffsetStepText
        {
            get => _whatIfOffsetStepText;
            set
            {
                _whatIfOffsetStepText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetWhatIfOffsetStep(d);
                OnPropertyChanged(nameof(WhatIfOffsetStepText));
            }
        }

        [JsonIgnore] private string _growthRateStepText = string.Empty;
        [JsonIgnore]
        public string GrowthRateStepText
        {
            get => _growthRateStepText;
            set
            {
                _growthRateStepText = value;
                if (double.TryParse(value, out var d))
                    SharedServiceCore.SetGrowthRateStep(d);
                OnPropertyChanged(nameof(GrowthRateStepText));
            }
        }

        public void LoadStepperIncrements()
        {
            _loanRateStepText = SharedServiceCore.GetLoanRateStep().ToString("F2");
            _whatIfRateStepText = SharedServiceCore.GetWhatIfRateStep().ToString("F2");
            _whatIfMonthlyStepText = SharedServiceCore.GetWhatIfMonthlyStep().ToString("F0");
            _whatIfLumpSumStepText = SharedServiceCore.GetWhatIfLumpSumStep().ToString("F0");
            _whatIfOffsetStepText = SharedServiceCore.GetWhatIfOffsetStep().ToString("F0");
            _growthRateStepText = SharedServiceCore.GetGrowthRateStep().ToString("F2");
            OnPropertyChanged(nameof(LoanRateStepText));
            OnPropertyChanged(nameof(WhatIfRateStepText));
            OnPropertyChanged(nameof(WhatIfMonthlyStepText));
            OnPropertyChanged(nameof(WhatIfLumpSumStepText));
            OnPropertyChanged(nameof(WhatIfOffsetStepText));
            OnPropertyChanged(nameof(GrowthRateStepText));
        }

        #endregion

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
