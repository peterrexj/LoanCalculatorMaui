using LoanCalculator.Models.Enums;
using LoanCalculator.Models.Income;
using Pj.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LoanCalculatorMaui.Services;
using Syncfusion.Maui.Core.Carousel;

namespace LoanCalculatorMaui.ViewModel
{
    public class ExpenseEntryViewBaseModel : ViewModelUiBase
    {
        [JsonIgnore]
        public bool HasInitialized { get; private set; } = false;

        public void MarkInitializationComplete()
        {
            HasInitialized = true;
        }

        [JsonIgnore]
        public ObservableCollection<Brush> CustomChartColors { get; set; }

        public void InitializeBrushes()
        {
            var appResources = Application.Current?.Resources;
            if (appResources != null)
            {
                CustomChartColors =
                [
                    new SolidColorBrush((Color)appResources["ChartColor1"]),
                    new SolidColorBrush((Color)appResources["ChartColor2"]),
                    new SolidColorBrush((Color)appResources["ChartColor3"])
                ];
            }
            else
            {
                CustomChartColors =
                [
                    new SolidColorBrush(Color.FromArgb("#d7bde2")),
                    new SolidColorBrush(Color.FromArgb("#d6eaf8")),
                    new SolidColorBrush(Color.FromArgb("#fdebd0"))
                ];
            }
        }

        #region Expense Entry

        [JsonIgnore]
        private IncomeExpense _incomeExpenseEntry;
        [JsonIgnore]
        public IncomeExpense IncomeExpenseEntry
        {
            get => _incomeExpenseEntry;
            set
            {
                _incomeExpenseEntry = value;
                OnPropertyChanged(nameof(IncomeExpenseEntry));
            }
        }

        [JsonIgnore]
        public bool HasErrorIncomeDescription
        {
            get => IncomeExpenseEntry == null || IncomeExpenseEntry.Name.IsEmpty();
        }
        [JsonIgnore]
        public string IncomeEntryName
        {
            get => IncomeExpenseEntry?.Name;
            set
            {
                if (value == null || IncomeExpenseEntry == null) return;
                IncomeExpenseEntry.Name = value;
                OnPropertyChanged(nameof(IncomeEntryName));
                OnPropertyChanged(nameof(HasErrorIncomeDescription));
                OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            }
        }

        [JsonIgnore]
        public bool HasErrorIncomeAmount => IncomeExpenseEntry == null || IncomeExpenseEntry.Amount <= 0;

        [JsonIgnore]
        public double IncomeEntryAmount
        {
            get => IncomeExpenseEntry?.Amount ?? 0;
            set
            {
                if (IncomeExpenseEntry == null) return;
                IncomeExpenseEntry.Amount = value;
                OnPropertyChanged(nameof(IncomeEntryAmount));
                OnPropertyChanged(nameof(HasErrorIncomeAmount));
                OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            }
        }

        [JsonIgnore]
        private string _IncomeExpenseFrequencySelectedIndex;
        [JsonIgnore]
        public string IncomeExpenseFrequencySelectedIndex
        {
            get => _IncomeExpenseFrequencySelectedIndex;
            set
            {
                if (value == null) return;
                _IncomeExpenseFrequencySelectedIndex = value;
                IncomeExpenseEntry.Frequency = IncomeExpenseHelper.TimeFrequencyFromString(value);
                OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
            }
        }

        [JsonIgnore]
        public bool IsExpenseDataFormReadyToSubmit => HasErrorIncomeDescription == false && HasErrorIncomeAmount == false;

        public bool AddOrUpdateEntryFromView()
        {
            if (IncomeExpenseEntry.Id != Guid.Empty && TransactionRecords.Exists(IncomeExpenseEntry.Id))
            {
                TransactionRecords.Delete(IncomeExpenseEntry.Id);
            }
            else if (TransactionRecords.Exists(IncomeExpenseEntry.Name))
            {
                TransactionRecords.Delete(TransactionRecords.Get(IncomeExpenseEntry.Name).Id);
            }

            TransactionRecords.Add(IncomeExpenseEntry.Name,
                IncomeExpenseEntry.Amount,
                IncomeExpenseEntry.Frequency, isCheckForExistingRequired: false);

            IncomeExpenseEntry.Name = string.Empty;
            IncomeExpenseEntry.Amount = 0;
            IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();

            return true;
        }

        public void ResetTransactionEntryData()
        {
            IncomeExpenseEntry.Name = string.Empty;
            IncomeExpenseEntry.Amount = 0;
            IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
            IncomeExpenseEntry.Id = Guid.Empty;
        }

        public Incomes TransactionRecords { get; set; }

        [JsonIgnore]
        public ObservableCollection<string> IncomeFrequencyCollection { get; set; }

        [JsonIgnore]
        public ObservableCollection<IncomeExpense>? Transactions => TransactionRecords.IncomeExpenseEntries;
        [JsonIgnore]
        public IEnumerable<SearchAutoCompleteViewModel> AutocompleteList
            => Transactions.Select(f => new SearchAutoCompleteViewModel { Id = 0, Name = f.Name });

        [JsonIgnore]
        public string SearchExpenseIncomeName { get; set; }
        #endregion

        public async Task<T?> LoadDataFile<T>()
        {
            T? data = default;

            try
            {
                SharedServices.LocalStorage!.Initialize();
                data = await SharedServices.LocalStorage.GetData<T>().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Log or handle the exception as needed
                // ExceptionHandler.CaptureException(e);
            }

            return data;
        }
    }
}
