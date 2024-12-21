using System.Collections.ObjectModel;
using System.Windows.Input;
using LoanCalculator.Models.BaseExtensions;
using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.Themes;
using Newtonsoft.Json;

namespace LoanCalculatorMaui.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool isUpdating = false;

        public SettingsViewModel()
        {
            themes = new ObservableCollection<string>(Enum.GetNames(typeof(AppThemes)));
            ClearLoanCommand = new Command(async () => await ClearLoanData());
            ClearIncomeCommand = new Command(async () => await ClearIncomeData());
            ClearExpenseCommand = new Command(async () => await ClearExpenseData());
            ClearDisclaimersCommand = new Command(async () => await ClearDisclaimerData());
        }

        private string selectedTheme;
        public string SelectedTheme
        {
            get
            {
                return selectedTheme;
            }
            set
            {
                if (isUpdating == false)
                {
                    isUpdating = true;
                    selectedTheme = value;
                    OnPropertyChanged("SelectedTheme");
                    SaveData();
                    //DefaultStyle = ThemeHelper.GetDefaultStyleTheme(SelectedAppTheme);
                    SharedServices.ThemeHelper.UpdateAppThemes(DefaultStyle);
                    OnPropertyChanged("DefaultStyle");
                    isUpdating = false;
                }
            }
        }
        public AppThemes SelectedAppTheme => EnumHelper<AppThemes>.FromString(selectedTheme);

        #region Commands

        public ICommand ClearLoanCommand { get; }
        public ICommand ClearExpenseCommand { get; }
        public ICommand ClearIncomeCommand { get; }
        public ICommand ClearDisclaimersCommand { get; }

        private async Task ClearLoanData() => await SharedServices.LocalStorage.ClearData<LoanViewModel>();
        private async Task ClearIncomeData() => await SharedServices.LocalStorage.ClearData<IncomeViewModel>();
        private async Task ClearExpenseData() => await SharedServices.LocalStorage.ClearData<ExpenseViewModel>();
        private async Task ClearDisclaimerData()
        {
            await Task.Run(() =>
            {
                SharedServices.NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer = false;
                SharedServices.NameValueDataService.SaveNameValueData();
            });
        }

        #endregion


        [JsonIgnore]
        private ObservableCollection<string> themes;
        [JsonIgnore]
        public ObservableCollection<string> Themes
        {
            get
            {
                return themes;
            }
            set
            {
                Themes = value;
                OnPropertyChanged("Themes");
            }
        }

        [JsonIgnore]
        private StyleModelDefault styleModelDefault;
        [JsonIgnore]
        public StyleModelDefault DefaultStyle
        {
            get => styleModelDefault;
            set
            {
                styleModelDefault = value;
                OnPropertyChanged(nameof(StyleModelDefault));
            }
        }

        private void SaveData()
        {
            Task.Run(async () =>
            {
                await SharedServices.LocalStorage.SaveData<SettingsViewModel>(this);
            }).Wait();
        }
    }
}
