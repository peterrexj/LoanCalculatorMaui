using LoanCalculator.Models.BaseExtensions;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LoanCalculatorMaui.Services;

namespace LoanCalculatorMaui.ViewModel
{
    public class Theme
    {
        public string Name { get; set; }
        public Color BoxBackgroundGradientBrushStart { get; set; }
        public Color BoxBackgroundGradientBrushEnd { get; set; }

        public Color TextBackground { get; set; }
    }

    public class SettingsViewModel : BaseViewModel
    {
        public ICommand ClearLoanDataCommand { get; }
        public ICommand ClearExpenseDataCommand { get; }
        public ICommand ClearIncomeDataCommand { get; }
        public ICommand ClearAllDataCommand { get; }

        public SettingsViewModel()
        {
            Themes = new ObservableCollection<Theme>
            {
                new Theme { Name = "Dark", BoxBackgroundGradientBrushStart = Color.FromArgb("#5d6d7e"), BoxBackgroundGradientBrushEnd = Color.FromArgb("#212f3c"), TextBackground = Color.FromArgb("#ebedef")},
                new Theme { Name = "Light", BoxBackgroundGradientBrushStart = Color.FromArgb("#e5e7e9"), BoxBackgroundGradientBrushEnd = Color.FromArgb("#bdc3c7"), TextBackground = Color.FromArgb("#34495e")},
                new Theme { Name = "FireBreather", BoxBackgroundGradientBrushStart = Color.FromArgb("#f5cba7"), BoxBackgroundGradientBrushEnd = Color.FromArgb("#d68910"), TextBackground = Color.FromArgb("#6e2c00") } // Pastel Red
            };

            SelectedTheme = Themes[0]; // Default to Light theme

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
                _selectedTheme = value;
                OnPropertyChanged();
                // Implement theme change logic here
                ApplyTheme(_selectedTheme);
            }
        }

        private void ApplyTheme(Theme theme)
        {
            // Implement the logic to apply the selected theme
            // This might involve updating the resource dictionary or other UI elements
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
