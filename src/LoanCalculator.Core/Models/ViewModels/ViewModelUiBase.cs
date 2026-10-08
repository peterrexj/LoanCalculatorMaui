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
            // This handler must not be allowed to throw, and that is not a test accommodation.
            //
            // The subscription above is never removed — there is no Dispose on this type — so
            // every view model ever constructed stays attached to a static event for the life of
            // the process. A .NET event invokes its handlers in order and the first exception
            // aborts the rest AND propagates to the caller, so one handler that throws turns
            // `Helper.CurrencySymbol = x` into a throwing statement for everybody.
            //
            // Outside a MAUI app there is no platform main thread and MainThread throws MAUI's
            // NotImplementedInReferenceAssemblyException. That type is internal to the MAUI
            // assembly and cannot be named here, but it derives from NotImplementedException —
            // which is narrow enough not to mask a real failure. With no UI thread there is also
            // nothing to marshal to, so applying the update directly is the correct answer rather
            // than a fallback. This surfaced as two unrelated currency tests failing as soon as
            // any fixture that constructs a view model happened to run before them.
            try
            {
                MainThread.BeginInvokeOnMainThread(ApplyCurrencySymbolChange);
            }
            catch (NotImplementedException)
            {
                ApplyCurrencySymbolChange();
            }
        }

        private void ApplyCurrencySymbolChange()
        {
            CurrencySymbol = Helper.CurrencySymbol;
            OnCurrencyChanged();
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
