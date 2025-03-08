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

        public SettingsViewModel() : this(ServiceLocator.GetService<IErrorHandlingService>(), ServiceLocator.GetService<IAlertService>())
        {
            Themes = new ObservableCollection<Theme?>
            {
                new Theme { Name = AppThemes.Dark.ToString(), BoxBackgroundGradientBrushStart = Color.FromArgb("#5d6d7e"), BoxBackgroundGradientBrushEnd = Color.FromArgb("#212f3c"), TextBackground = Color.FromArgb("#ebedef")},
                new Theme { Name = AppThemes.Light.ToString(), BoxBackgroundGradientBrushStart = Color.FromArgb("#e5e7e9"), BoxBackgroundGradientBrushEnd = Color.FromArgb("#bdc3c7"), TextBackground = Color.FromArgb("#34495e")},
                new Theme { Name = AppThemes.FireBreather.ToString(), BoxBackgroundGradientBrushStart = Color.FromArgb("#f5cba7"), BoxBackgroundGradientBrushEnd = Color.FromArgb("#d68910"), TextBackground = Color.FromArgb("#6e2c00") } // Pastel Red
            };

            ClearLoanDataCommand = new Command(async () => await ClearLoanData());
            ClearIncomeDataCommand = new Command(async () => await ClearIncomeData());
            ClearExpenseDataCommand = new Command(async () => await ClearExpenseData());
            ClearAllDataCommand = new Command(async () => await ClearDisclaimerData());
        }

        [JsonIgnore]
        public ObservableCollection<Theme?> Themes { get; }

        [JsonIgnore]
        private Theme? _selectedTheme;
        [JsonIgnore]
        public Theme? SelectedTheme
        {
            get
            {
                if (_selectedTheme == null)
                {
                    StyleProvider.GetCurrentThemeAsync().ContinueWith(task =>
                    {
                        _selectedTheme = task.Result != null ? 
                            Themes.FirstOrDefault(t => t.Name == task.Result.ToString()) : 
                            Themes.FirstOrDefault(t => t.Name == AppThemes.Light.ToString());
                    });
                }
                return _selectedTheme;
            }
            set
            {
                if (value == null) return;
                if (_selectedTheme == value) return;
                if (isUpdating) return;

                IsBusy = true;
                MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        isUpdating = true;

                        var appTheme = EnumHelper<AppThemes>.FromString(_selectedTheme.Name);
                        await SaveAndApplyApplicationThemeAsync(appTheme);

                        _selectedTheme = value;

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
    }
}
