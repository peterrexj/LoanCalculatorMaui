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
/// </summary>
/// <remarks>
/// The handlers came over verbatim from LoanView.xaml.cs when this page was extracted from the
/// popup, and that is how it ended up as the only data-entry page still reformatting inside
/// <c>TextChanged</c> — which crashed real Android devices at the fourth digit. See
/// <see cref="OnUpfrontEntryUnfocused"/>. Grouping now happens on unfocus only, matching
/// LoanDetailsPage and TransactionEntryPage; do not reintroduce a mid-keystroke rewrite here.
/// </remarks>
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

        BeginLiveEdit();
    }

    /// <summary>
    /// Balances <see cref="LoanViewModel.BeginLiveEdit"/> against
    /// <see cref="LoanViewModel.EndLiveEdit"/> from this page — see the matching pair on
    /// LoanDetailsPage for why this is a page-local flag and not a bare call.
    /// </summary>
    /// <remarks>
    /// This page keeps updating its own running total while coalescing is on, because
    /// OtherExpenseTotalAmount — the one LoanViewModel property its markup binds that moves as you
    /// type — is in the live subset TriggerPropertyChangedOnPropertyTab still raises. Bind another
    /// LoanViewModel property here and it has to be added to that subset.
    /// </remarks>
    private bool _liveEditActive;

    private void BeginLiveEdit()
    {
        if (_liveEditActive) return;

        _liveEditActive = true;
        _viewModel.BeginLiveEdit();
    }

    private void EndLiveEdit()
    {
        if (!_liveEditActive) return;

        _liveEditActive = false;
        _viewModel.EndLiveEdit();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(false);

        // Flushes the one coalesced pass for everything typed while this page was open. Dismiss()
        // also calls TriggerPropertyChangedOnPropertyTab when committing, but that call lands
        // while coalescing is still on, so it only marks the pass as needed — this is what
        // actually runs it.
        EndLiveEdit();
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

    /// <summary>
    /// Updates the word form and the model as the user types. Deliberately does NOT rewrite the
    /// entry — see <see cref="OnUpfrontEntryUnfocused"/>.
    /// </summary>
    private void OnUpfrontEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry entry) return;

        var id = entry.AutomationId;
        if (string.IsNullOrEmpty(id)) return;

        var value = ParseAmount(e.NewTextValue);

        var wordsId = id.Replace("Entry", "Words");
        if (_upfrontLabels.TryGetValue(wordsId, out var lbl))
            lbl.Text = LoanViewModel.AmountToWords(value);

        SetUpfrontValue(id, value);
    }

    /// <summary>
    /// Applies thousands separators once the user has finished with a field — never mid-keystroke.
    /// </summary>
    /// <remarks>
    /// <b>This is the fix for a hard crash on real Android devices.</b> This page used to reformat
    /// inside <c>TextChanged</c> and then assign <c>entry.CursorPosition</c>. Android's native
    /// <c>EditText</c> applies replacement text <i>asynchronously</i>, so at the moment the cursor
    /// was assigned the control could still hold the shorter previous string, and
    /// <c>setSelection</c> past the end throws.
    ///
    /// That is why it needed four digits to reproduce: below a thousand the grouped string is the
    /// same length as the digits typed, so the out-of-range assignment never happened. At the
    /// fourth digit the first separator appears, the formatted string is one character longer, and
    /// the app died. It survived the simulator and emulator because the timing differs and iOS's
    /// <c>UITextField</c> does not throw on an out-of-range selection.
    ///
    /// LoanDetailsPage and TransactionEntryPage have always grouped on unfocus for exactly this
    /// reason; this page kept the original popup's handlers verbatim and so kept the defect. The
    /// caret is now never positioned by us at all, which is the only reliably safe option — see
    /// the Entry notes in CLAUDE.md.
    /// </remarks>
    private void OnUpfrontEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is not Entry entry) return;

        var grouped = Grouped(ParseAmount(entry.Text));
        if (grouped == entry.Text) return;

        _suppressTextChanged = true;
        entry.Text = grouped;
        _suppressTextChanged = false;
    }

    private static double ParseAmount(string? text)
    {
        var digits = new string((text ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!double.TryParse(digits, out var value)) return 0;

        // See LoanViewModel.MaxEnterableAmount: past 2^53 a double no longer holds the integer
        // that was typed, so the displayed figure diverges from the entry.
        return Math.Min(value, LoanViewModel.MaxEnterableAmount);
    }

    private static string Grouped(double value) => value > 0 ? $"{value:N0}" : string.Empty;

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

}
