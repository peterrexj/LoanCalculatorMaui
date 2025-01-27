using LoanCalculator.Models.BaseExtensions;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LoanCalculatorMaui.Services;
using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Themes;
using Microsoft.Maui.ApplicationModel;

namespace LoanCalculatorMaui.ViewModel
{
    public class Theme
    {
        public string Name { get; set; }
        public Color BoxBackgroundGradientBrushStart { get; set; }
        public Color BoxBackgroundGradientBrushEnd { get; set; }

        public Color TextBackground { get; set; }
    }

    public class SettingsViewModel : ViewModelUiBase
    {
        public ICommand ClearLoanDataCommand { get; }
        public ICommand ClearExpenseDataCommand { get; }
        public ICommand ClearIncomeDataCommand { get; }
        public ICommand ClearAllDataCommand { get; }

        public SettingsViewModel()
        {
            Themes = new ObservableCollection<Theme>
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

        public ObservableCollection<Theme> Themes { get; }

        private Theme _selectedTheme;
        public Theme SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (isUpdating) return;
                IsBusy = true;
                MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        isUpdating = true;
                        _selectedTheme = value;
                        OnPropertyChanged();
                        await SaveAndApplyThemeAsync(_selectedTheme);
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception (e.g., log it, show a message to the user, etc.)
                        Console.WriteLine($"Error applying theme: {ex.Message}");
                    }
                    finally
                    {
                        isUpdating = false;
                        IsBusy = false;
                    }
                });
            }
        }

        private async Task SaveAndApplyThemeAsync(Theme theme)
        {
            await SaveData(this);
            await ApplyThemeAsync(theme);
        }

        private async Task ApplyThemeAsync(Theme theme)
        {
            try
            {
                var appTheme = EnumHelper<AppThemes>.FromString(theme.Name);
                await MainThread.InvokeOnMainThreadAsync(() => StyleProvider.LoadDefaultStyle(appTheme));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task ClearLoanData() => await SharedServices.LocalStorage.ClearData<LoanViewModel>();
        private async Task ClearIncomeData() => await SharedServices.LocalStorage.ClearData<IncomeViewModel>();
        private async Task ClearExpenseData() => await SharedServices.LocalStorage.ClearData<ExpenseViewModel>();
        private async Task ClearDisclaimerData() //TODO: Change this from clearing disclaimer to clearing all data
        {
            await Task.Run(() =>
            {
                new PdfGenerator().GeneratePdf();
                //SharedServices.NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer = false;
                //SharedServices.NameValueDataService.SaveNameValueData();
            });
        }
    }
}
