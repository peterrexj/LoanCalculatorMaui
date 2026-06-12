using System.Text.Json.Serialization;
using LoanCalculator.Core.Models.BaseExtensions;
using LoanCalculator.Core.Services;
namespace LoanCalculator.Core.Models.ViewModels
{
    public class ViewModelUiBase : BaseViewModel
    {
        private string _currencySymbol;

        [JsonIgnore]
        public string CurrencySymbol
        {
            get => _currencySymbol;
            set
            {
                _currencySymbol = value;
                OnPropertyChanged(nameof(CurrencySymbol));
                OnPropertyChanged(nameof(CurrencyFormat));
            }
        }

        [JsonIgnore]
        public string CurrencyFormat => $"{CurrencySymbol}#,##0";


        [JsonIgnore] public string NewLine { get; set; }

        protected bool isUpdating = false;
        [JsonIgnore] public bool IsUpdating
        {
            get => isUpdating;
            set
            {
                isUpdating = value;
            }
        }

        private bool _showPremiumBuyOption;
        [JsonIgnore] public bool ShowPremiumBuyOption
        {
            get => _showPremiumBuyOption;
            set
            {
                _showPremiumBuyOption = value;
                OnPropertyChanged(nameof(ShowPremiumBuyOption));
            }
        }

        public ViewModelUiBase()
        {
            NewLine = Environment.NewLine;

            // Auto-update when the user changes currency in Settings
            Helper.CurrencySymbolChanged += OnCurrencySymbolChanged;
        }

        private void OnCurrencySymbolChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrencySymbol = Helper.CurrencySymbol;
                OnCurrencyChanged();
            });
        }

        // Override in subclasses to fire additional currency-dependent property notifications
        protected virtual void OnCurrencyChanged() { }

        // ── Debounced save ──────────────────────────────────────────────────────
        // Cancels any pending save and schedules a new one 600 ms in the future.
        // Rapid input (slider drag, keystrokes) collapses to a single disk write.

        [JsonIgnore] private CancellationTokenSource? _saveCts;

        protected void ScheduleSave(Action saveAction)
        {
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            Task.Delay(600, token).ContinueWith(t =>
            {
                if (!t.IsCanceled) saveAction();
            }, TaskScheduler.Default);
        }

        // Call from OnDisappearing or after an explicit add/delete to flush immediately.
        public void FlushPendingSave(Action saveAction)
        {
            _saveCts?.Cancel();
            _saveCts = null;
            saveAction();
        }
    }
}
