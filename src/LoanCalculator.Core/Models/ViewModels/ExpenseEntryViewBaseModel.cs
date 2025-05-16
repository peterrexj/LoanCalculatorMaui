using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using Pj.Library;

namespace LoanCalculator.Core.Models.ViewModels
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

        public void TriggerOneTimeUpdateOnPage()
        {
            OnPropertyChanged(nameof(CustomChartColors));
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
        public bool HasErrorIncomeDescription => IncomeExpenseEntry?.Name?.IsEmpty() == true;

        [JsonIgnore]
        public string IncomeEntryName
        {
            get => IncomeExpenseEntry?.Name ?? "";
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
        public bool HasErrorIncomeAmount => IncomeExpenseEntry?.Amount <= 0;

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
            if (TransactionRecords == null) return false;

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
            if (IncomeExpenseEntry == null) return;

            IncomeExpenseEntry.Name = string.Empty;
            IncomeExpenseEntry.Amount = 0;
            IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
            IncomeExpenseEntry.Id = Guid.Empty;
        }

        public Incomes? TransactionRecords { get; set; }

        [JsonIgnore]
        public ObservableCollection<string> IncomeFrequencyCollection { get; set; }

        [JsonIgnore]
        public ObservableCollection<IncomeExpense>? Transactions => TransactionRecords?.IncomeExpenseEntries ?? new ObservableCollection<IncomeExpense>();
        [JsonIgnore]
        public IEnumerable<SearchAutoCompleteViewModel> AutocompleteList
            => Transactions.Select(f => new SearchAutoCompleteViewModel { Id = 0, Name = f.Name });

        [JsonIgnore]
        public string SearchExpenseIncomeName { get; set; }
        #endregion
    }
}
