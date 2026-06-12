using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
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

        // True when the user tapped Edit on an existing entry (Id is set).
        // Used to change the Add button label to "Update".
        [JsonIgnore]
        public bool IsEditMode => IncomeExpenseEntry?.Id != Guid.Empty && IncomeExpenseEntry?.Id != null;

        // Controls visibility of the add/edit form popup.
        [JsonIgnore]
        private bool _isAddFormVisible;
        [JsonIgnore]
        public bool IsAddFormVisible
        {
            get => _isAddFormVisible;
            set
            {
                _isAddFormVisible = value;
                OnPropertyChanged(nameof(IsAddFormVisible));
            }
        }

        [JsonIgnore]
        public ObservableCollection<Brush> CustomChartColors { get; set; }

        public void TriggerOneTimeUpdateOnPage()
        {
            OnPropertyChanged(nameof(CustomChartColors));
            OnPropertyChanged(nameof(CurrencySymbol));
            OnPropertyChanged(nameof(TransactionRecords));
            OnPropertyChanged(nameof(IncomeFrequencyCollection));
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
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IncomeEntryName));
                OnPropertyChanged(nameof(IncomeEntryAmount));
                OnPropertyChanged(nameof(IncomeEntryAmountText));
                OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
                OnPropertyChanged(nameof(HasErrorIncomeDescription));
                OnPropertyChanged(nameof(HasErrorIncomeAmount));
                OnPropertyChanged(nameof(ShowErrorIncomeDescription));
                OnPropertyChanged(nameof(ShowErrorIncomeAmount));
            }
        }

        // Set to true when the user taps Save — keeps error labels hidden until first submit attempt.
        [JsonIgnore]
        private bool _showValidationErrors;
        [JsonIgnore]
        public bool ShowValidationErrors
        {
            get => _showValidationErrors;
            set
            {
                _showValidationErrors = value;
                OnPropertyChanged(nameof(ShowValidationErrors));
                OnPropertyChanged(nameof(ShowErrorIncomeDescription));
                OnPropertyChanged(nameof(ShowErrorIncomeAmount));
            }
        }

        [JsonIgnore]
        public bool ShowErrorIncomeDescription => ShowValidationErrors && (IncomeExpenseEntry?.Name?.IsEmpty() == true);
        [JsonIgnore]
        public bool ShowErrorIncomeAmount => ShowValidationErrors && (IncomeExpenseEntry?.Amount <= 0);

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
                OnPropertyChanged(nameof(ShowErrorIncomeDescription));
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
                OnPropertyChanged(nameof(IncomeEntryAmountText));
                OnPropertyChanged(nameof(HasErrorIncomeAmount));
                OnPropertyChanged(nameof(ShowErrorIncomeAmount));
                OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            }
        }

        // String-backed amount for plain Entry binding — updates on every keystroke.
        // TwoWay binding on Entry.Text is reliable; SfNumericEntry.Value is not (focus-dependent).
        [JsonIgnore]
        public string IncomeEntryAmountText
        {
            get => IncomeExpenseEntry?.Amount > 0 ? ((long)(IncomeExpenseEntry.Amount)).ToString("N0") : string.Empty;
            set
            {
                if (IncomeExpenseEntry == null) return;
                var cleaned = value?.Replace(",", "").Trim();
                if (double.TryParse(cleaned, out var parsed))
                    IncomeExpenseEntry.Amount = parsed;
                else if (string.IsNullOrWhiteSpace(cleaned))
                    IncomeExpenseEntry.Amount = 0;
                OnPropertyChanged(nameof(IncomeEntryAmountText));
                OnPropertyChanged(nameof(HasErrorIncomeAmount));
                OnPropertyChanged(nameof(ShowErrorIncomeAmount));
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
                if (IncomeExpenseEntry != null)
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

            // Notify list bindings before resetting the form entry
            OnPropertyChanged(nameof(Transactions));
            OnPropertyChanged(nameof(FilteredTransactions));
            OnPropertyChanged(nameof(AutocompleteNameList));

            ShowValidationErrors = false;
            IncomeExpenseEntry = new IncomeExpense();
            IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
            IsAddFormVisible = false;

            return true;
        }

        public void ResetTransactionEntryData()
        {
            // Clear validation flag FIRST so the IncomeExpenseEntry setter's notifications
            // evaluate ShowError* with the flag already false (amount=0 on a new entry would
            // otherwise briefly make ShowErrorIncomeAmount=true).
            ShowValidationErrors = false;
            IncomeExpenseEntry = new IncomeExpense();
            IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
            IsAddFormVisible = false;
        }

        private Incomes? _incomes;

        public Incomes? TransactionRecords
        {
            get => _incomes;
            set             {
                _incomes = value;
                OnPropertyChanged(nameof(TransactionRecords));
            }
        }

        [JsonIgnore]
        private ObservableCollection<string> _incomeFrequencyCollection;
        [JsonIgnore]
        public ObservableCollection<string> IncomeFrequencyCollection
        {
            get => _incomeFrequencyCollection;
            set
            {
                _incomeFrequencyCollection = value;
                OnPropertyChanged(nameof(IncomeFrequencyCollection));
            }
        }

        [JsonIgnore]
        public ObservableCollection<IncomeExpense>? Transactions => TransactionRecords?.IncomeExpenseEntries ?? new ObservableCollection<IncomeExpense>();

        // Filtered + sorted view of Transactions bound to the list and empty-state check.
        // Returns a List so .Count works in XAML bindings.
        [JsonIgnore]
        public List<IncomeExpense> FilteredTransactions
        {
            get
            {
                var all = Transactions;
                if (all == null) return new List<IncomeExpense>();
                if (string.IsNullOrWhiteSpace(SearchExpenseIncomeName))
                    return all.OrderBy(t => t.Name).ToList();
                return all
                    .Where(t => t.Name != null && t.Name.Contains(SearchExpenseIncomeName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.Name)
                    .ToList();
            }
        }

        [JsonIgnore]
        public IEnumerable<SearchAutoCompleteViewModel> AutocompleteList
            => Transactions.Select(f => new SearchAutoCompleteViewModel { Id = 0, Name = f.Name });

        // Flat name list for the autocomplete — simpler than SearchAutoCompleteViewModel for filtering
        [JsonIgnore]
        public List<string> AutocompleteNameList
            => Transactions?.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)).OrderBy(n => n).ToList()
               ?? new List<string>();

        [JsonIgnore]
        private string _searchExpenseIncomeName;
        [JsonIgnore]
        public string SearchExpenseIncomeName
        {
            get => _searchExpenseIncomeName;
            set
            {
                _searchExpenseIncomeName = value;
                OnPropertyChanged(nameof(SearchExpenseIncomeName));
                OnPropertyChanged(nameof(FilteredTransactions));
            }
        }
        #endregion
    }
}
