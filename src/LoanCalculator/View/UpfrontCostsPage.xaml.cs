using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.View;

/// <summary>
/// Data-entry page for upfront costs (stamp duty, mortgage charges, conveyance fees,
/// bank fees, inspection fees, other expenses) and Australian state selection.
/// Replaces the UpfrontCostsPopup from LoanView.xaml.
///
/// All fields write the model live. The Done button closes the page — there is no separate
/// save/apply logic, only dismissal. This preserves the always-commits behavior of the
/// original popup.
///
/// Code-behind handlers are relocated verbatim from LoanView.xaml.cs.
/// </summary>
public partial class UpfrontCostsPage : ContentPage
{
    private readonly LoanViewModel _viewModel;
    private readonly IErrorHandlingService _errorHandlingService;

    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Dictionary<string, Entry> _upfrontEntries = new();
    private readonly Dictionary<string, Label> _upfrontLabels = new();

    private bool _suppressTextChanged;
    private bool _isClosing;

    public UpfrontCostsPage(LoanViewModel viewModel, IErrorHandlingService errorHandlingService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _errorHandlingService = errorHandlingService;
        BindingContext = viewModel;
    }

    public Task<bool> Completion => _completion.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // The Loaded handlers populate entries from model state
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(false);
    }

    protected override bool OnBackButtonPressed()
    {
        Dismiss(committed: false);
        return true;
    }

    private void OnDone(object sender, EventArgs e) => Dismiss(committed: true);

    private async void Dismiss(bool committed)
    {
        if (_isClosing) return;
        _isClosing = true;

        try
        {
            if (committed)
                _viewModel.TriggerPropertyChangedOnPropertyTab();

            _completion.TrySetResult(committed);

            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    // ── Handlers relocated from LoanView.xaml.cs ──────────────────────────────

    private void OnUpfrontEntryLoaded(object sender, EventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrEmpty(entry.AutomationId))
        {
            _upfrontEntries[entry.AutomationId] = entry;
            PopulateUpfrontEntry(entry.AutomationId);
        }
    }

    private void OnUpfrontLabelLoaded(object sender, EventArgs e)
    {
        if (sender is Label lbl && !string.IsNullOrEmpty(lbl.AutomationId))
            _upfrontLabels[lbl.AutomationId] = lbl;
    }

    private void OnUpfrontControlLoaded(object sender, EventArgs e) { /* segment wired via binding */ }

    private void OnUpfrontEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry entry) return;
        var id = entry.AutomationId;
        FormatEntry(entry, e.NewTextValue,
            formatted => { /* no formatted label for upfront */ },
            words =>
            {
                var wordsId = id.Replace("Entry", "Words");
                if (_upfrontLabels.TryGetValue(wordsId, out var lbl)) lbl.Text = words;
            },
            val => SetUpfrontValue(id, val));
    }

    private void SetUpfrontValue(string automationId, double val)
    {
        if (!_viewModel.HasInitialized) return;
        switch (automationId)
        {
            case "StampDutyEntry":     _viewModel.StampDuty         = val; break;
            case "MortgageEntry":      _viewModel.MortgageCharges   = val; break;
            case "ConveyanceEntry":    _viewModel.ConveyancerFee    = val; break;
            case "BankFeeEntry":       _viewModel.BankFee           = val; break;
            case "InspectionEntry":    _viewModel.InspectionFee     = val; break;
            case "OtherExpensesEntry": _viewModel.OtherExpenses     = val; break;
        }
    }

    private void PopulateUpfrontEntry(string automationId)
    {
        if (!_upfrontEntries.TryGetValue(automationId, out var entry)) return;
        var val = automationId switch
        {
            "StampDutyEntry"     => _viewModel.StampDuty,
            "MortgageEntry"      => _viewModel.MortgageCharges,
            "ConveyanceEntry"    => _viewModel.ConveyancerFee,
            "BankFeeEntry"       => _viewModel.BankFee,
            "InspectionEntry"    => _viewModel.InspectionFee,
            "OtherExpensesEntry" => _viewModel.OtherExpenses,
            _                    => 0.0
        };
        _suppressTextChanged = true;
        entry.Text = val > 0 ? $"{val:N0}" : string.Empty;
        _suppressTextChanged = false;

        var wordsId = automationId.Replace("Entry", "Words");
        if (_upfrontLabels.TryGetValue(wordsId, out var lbl))
            lbl.Text = LoanViewModel.AmountToWords(val);
    }

    private void FormatEntry(Entry entry, string rawText,
        Action<string> setFormatted, Action<string> setWords, Action<double> setViewModel)
    {
        // Strip everything except digits
        var digits = new string(rawText.Where(char.IsDigit).ToArray());
        if (!double.TryParse(digits, out var val)) val = 0;
        // See LoanViewModel.MaxEnterableAmount: past 2^53 a double no longer holds the integer
        // that was typed, so the displayed figure diverges from the entry.
        val = Math.Min(val, LoanViewModel.MaxEnterableAmount);

        // Format with commas
        var formatted = val > 0 ? $"{val:N0}" : string.Empty;
        var words = LoanViewModel.AmountToWords(val);

        // Update text without re-triggering TextChanged
        _suppressTextChanged = true;
        var cursorPos = Math.Min(entry.CursorPosition, formatted.Length);
        entry.Text = formatted;
        entry.CursorPosition = Math.Max(0, formatted.Length);
        _suppressTextChanged = false;

        setFormatted($"{_viewModel.CurrencySymbol}{formatted}");
        setWords(words);
        setViewModel(val);
    }
}
