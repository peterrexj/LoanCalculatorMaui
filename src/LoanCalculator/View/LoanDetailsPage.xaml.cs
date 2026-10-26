using System.ComponentModel;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.View;

/// <summary>
/// The single data-entry page for the loan, replacing the separate Quick Setup and Quick Input
/// pages. Opened from the ⚡ FAB, by tapping the asset/deposit/loan figures on the Asset tab, and
/// automatically on first run after the disclaimer.
/// </summary>
/// <remarks>
/// <para>Two commit models live here, deliberately. <b>Section A</b> — asset, deposit, loan —
/// writes the view model live, because the three derive from one another and the user has to see
/// that happen as they type. <b>Section B</b> — upfront, running cost, income, expenses — is
/// deferred to <see cref="OnApply"/>, which commits everything in the one safe order.</para>
/// <para>No <c>Entry.Text</c> binding anywhere, and none should be added: the handlers write the
/// controls themselves, and the model rewrites the other two figures on every change, so a binding
/// would be a competing write path. Grouping is applied on unfocus only — reformatting
/// mid-keystroke moved the caret backwards on Android.</para>
/// </remarks>
public partial class LoanDetailsPage : ContentPage
{
    private readonly WizardViewModel _viewModel;
    private readonly LoanViewModel _loanViewModel;
    private readonly IErrorHandlingService _errorHandlingService;

    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _suppressTextChanged;
    private bool _isClosing;

    public LoanDetailsPage(
        WizardViewModel viewModel,
        LoanViewModel loanViewModel,
        IErrorHandlingService errorHandlingService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _loanViewModel = loanViewModel;
        _errorHandlingService = errorHandlingService;
        BindingContext = _viewModel;
    }

    /// <summary>Completes when the page is dismissed; <c>true</c> when figures were committed.</summary>
    public Task<bool> Completion => _completion.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _viewModel.Prepopulate();
        _viewModel.Refresh();
        PopulateAll();

        _loanViewModel.PropertyChanged += OnLoanViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _loanViewModel.PropertyChanged -= OnLoanViewModelPropertyChanged;

        // Fallback for dismissal routes this page does not drive — the iPad/MacCatalyst sheet
        // swipe, or an exception between push and pop. TrySetResult means a real outcome wins.
        _completion.TrySetResult(false);
    }

    /// <summary>Android's hardware BACK would otherwise pop without recording an outcome.</summary>
    protected override bool OnBackButtonPressed()
    {
        Dismiss(committed: false);
        return true;
    }

    private void OnCancel(object sender, EventArgs e) => Dismiss(committed: false);

    private void OnApply(object sender, EventArgs e)
    {
        bool committed;
        try
        {
            committed = _viewModel.Commit();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
            committed = false;
        }

        Dismiss(committed);
    }

    private async void Dismiss(bool committed)
    {
        if (_isClosing) return;
        _isClosing = true;

        try
        {
            _completion.TrySetResult(committed);

            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    // ── Section A: live ──────────────────────────────────────────────────────

    /// <summary>
    /// Entering any one of asset/deposit/loan makes the model recompute the other two, so mirror
    /// them all back rather than only the field that was edited.
    /// </summary>
    private void OnLoanViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LoanViewModel.LoanAmountDirectInput)
            or nameof(LoanViewModel.LoanAmountStrFormatted)
            or nameof(LoanViewModel.DepositAmountDirectInput))
        {
            MainThread.BeginInvokeOnMainThread(RefreshDepositAndLoan);
        }
    }

    private void OnAssetTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry) return;

        var value = ParseAmount(e.NewTextValue);
        SetPair(lblAssetFormatted, lblAssetWords, value);
        _viewModel.WizardAssetText = Grouped(value);

        if (_loanViewModel.HasInitialized) _loanViewModel.PropertyAmount = value;

        // The preview labels read committed loan state; nothing else notifies them per keystroke.
        _viewModel.NotifyPreviewLabels();
    }

    private void OnDepositTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry) return;

        var typed = ParseAmount(e.NewTextValue);

        // Record the intent: the model cannot hold an independent deposit AND loan, so Commit()
        // needs to know which side the user actually typed last.
        _viewModel.LastEditedSplit = LoanSplit.Deposit;

        if (_loanViewModel.HasInitialized) _loanViewModel.DepositAmountDirectInput = typed;

        var effective = EffectiveDeposit(typed);
        SetPair(lblDepositFormatted, lblDepositWords, effective);
        ShowCapNote(lblDepositCapNote, typed, effective);
        _viewModel.WizardDepositText = Grouped(effective);

        // The preview labels read committed loan state; nothing else notifies them per keystroke.
        _viewModel.NotifyPreviewLabels();
    }

    private void OnLoanTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry) return;

        var typed = ParseAmount(e.NewTextValue);
        _viewModel.LastEditedSplit = LoanSplit.Loan;

        if (_loanViewModel.HasInitialized) _loanViewModel.LoanAmountDirectInput = typed;

        var effective = EffectiveLoan(typed);
        SetPair(lblLoanFormatted, lblLoanWords, effective);
        ShowCapNote(lblLoanCapNote, typed, effective);
        _viewModel.WizardLoanText = Grouped(effective);

        // The preview labels read committed loan state; nothing else notifies them per keystroke.
        _viewModel.NotifyPreviewLabels();
    }

    /// <summary>
    /// What the model ACCEPTED for the deposit, which is not always what was typed.
    /// </summary>
    /// <remarks>
    /// <see cref="HomeLoanInformation.DepositAmountDirectInput"/> clamps anything above
    /// <c>PropertyTotalAmount</c> — asset plus upfront costs — because deposit and loan are two
    /// views of one split, not independent amounts. The page used to display the raw typed figure
    /// instead, so the preview and the word form contradicted the model for as long as the user
    /// kept typing, and the Entry itself only snapped back on unfocus, when
    /// <see cref="RefreshDepositAndLoan"/> finally read the model. Reading the value back here is
    /// what makes the cap visible while it is being hit rather than after.
    ///
    /// Before the first load there is nothing to clamp against, so the typed value stands.
    /// </remarks>
    private double EffectiveDeposit(double typed) =>
        _loanViewModel.HasInitialized ? _loanViewModel.DepositAmountDirectInput : typed;

    /// <inheritdoc cref="EffectiveDeposit"/>
    private double EffectiveLoan(double typed) =>
        _loanViewModel.HasInitialized
            ? _loanViewModel.HomeLoanInfo?.LoanAmountDirectInput ?? typed
            : typed;

    /// <summary>
    /// Explains a clamp, and only while one is actually in effect.
    /// </summary>
    /// <remarks>
    /// Shown rather than blocking the keystroke: reverting <c>Entry.Text</c> from inside
    /// <c>TextChanged</c> is what moved the caret backwards on Android (see the remarks on this
    /// class), so the field keeps the digits and the page states the limit instead.
    ///
    /// With no asset value there is no cap to name — <c>PropertyTotalAmount</c> is zero, so every
    /// deposit clamps to zero and "cannot exceed 0" would be true but useless. Naming the missing
    /// input is the actionable message there.
    /// </remarks>
    private void ShowCapNote(Label note, double typed, double effective)
    {
        if (typed <= effective)
        {
            note.IsVisible = false;
            return;
        }

        var cap = _loanViewModel.HomeLoanInfo?.PropertyTotalAmount ?? 0;
        note.Text = cap > 0
            ? $"Cannot exceed the total asset cost of {_viewModel.CurrencySymbol}{cap:N0}"
            : "Enter the asset value first";
        note.IsVisible = true;
    }

    // ── Section B: deferred ──────────────────────────────────────────────────

    /// <summary>
    /// These four only update the view model; nothing reaches the loan until Apply, so the commit
    /// can order the writes safely.
    /// </summary>
    private void OnDeferredTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry entry) return;

        var value = ParseAmount(e.NewTextValue);
        var grouped = Grouped(value);

        switch (entry.AutomationId)
        {
            case "WizardUpfront":
                _viewModel.WizardUpfrontText = grouped;
                SetPair(lblUpfrontFormatted, lblUpfrontWords, value);
                // Asset total/breakdown preview the typed-but-not-yet-committed upfront figure
                // (WizardViewModel.PendingOrCommittedUpfront) — without this they would stay
                // frozen at the pre-upfront total while the user is mid-typing it.
                _viewModel.NotifyPreviewLabels();
                break;
            case "WizardRunning":
                _viewModel.WizardRunningCostText = grouped;
                SetPair(lblRunningFormatted, lblRunningWords, value);
                break;
            case "WizardIncome":
                _viewModel.WizardIncomeText = grouped;
                SetPair(lblIncomeFormatted, lblIncomeWords, value);
                break;
            case "WizardExpense":
                _viewModel.WizardExpenseText = grouped;
                SetPair(lblExpenseFormatted, lblExpenseWords, value);
                break;
        }
    }

    // ── Display ──────────────────────────────────────────────────────────────

    /// <summary>Applies thousands separators once the user has finished with a field.</summary>
    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is not Entry entry) return;

        var grouped = Grouped(ParseAmount(entry.Text));
        if (grouped != entry.Text)
        {
            _suppressTextChanged = true;
            entry.Text = grouped;
            _suppressTextChanged = false;
        }

        if (entry == entryDeposit || entry == entryLoan)
            RefreshDepositAndLoan();
    }

    private void PopulateAll()
    {
        _suppressTextChanged = true;
        entryAsset.Text = _viewModel.WizardAssetText ?? string.Empty;
        entryUpfront.Text = _viewModel.WizardUpfrontText ?? string.Empty;
        entryRunning.Text = _viewModel.WizardRunningCostText ?? string.Empty;
        entryIncome.Text = _viewModel.WizardIncomeText ?? string.Empty;
        entryExpense.Text = _viewModel.WizardExpenseText ?? string.Empty;
        _suppressTextChanged = false;

        SetPair(lblAssetFormatted, lblAssetWords, ParseAmount(entryAsset.Text));
        RefreshDepositAndLoan();

        SetPair(lblUpfrontFormatted, lblUpfrontWords, ParseAmount(entryUpfront.Text));
        SetPair(lblRunningFormatted, lblRunningWords, ParseAmount(entryRunning.Text));
        SetPair(lblIncomeFormatted, lblIncomeWords, ParseAmount(entryIncome.Text));
        SetPair(lblExpenseFormatted, lblExpenseWords, ParseAmount(entryExpense.Text));
    }

    /// <summary>
    /// Refreshes deposit and loan from the model, never the asset entry — the user may be mid-edit
    /// in it, and rewriting the field being typed into is what breaks text entry.
    /// </summary>
    private void RefreshDepositAndLoan()
    {
        var deposit = _loanViewModel.DepositAmountDirectInput;
        var loan = _loanViewModel.HomeLoanInfo?.LoanAmountDirectInput ?? 0;

        _suppressTextChanged = true;
        if (!entryDeposit.IsFocused) entryDeposit.Text = Grouped(deposit);
        if (!entryLoan.IsFocused) entryLoan.Text = Grouped(loan);
        _suppressTextChanged = false;

        if (!entryDeposit.IsFocused) SetPair(lblDepositFormatted, lblDepositWords, deposit);
        if (!entryLoan.IsFocused) SetPair(lblLoanFormatted, lblLoanWords, loan);

        // A field refreshed from the model now agrees with it by definition, so any clamp note on
        // it is stale. Clearing matters most for the field NOT being edited: lowering the asset
        // re-clamps the other side of the split, and without this its note would keep accusing the
        // user of exceeding a cap they are no longer anywhere near.
        if (!entryDeposit.IsFocused) lblDepositCapNote.IsVisible = false;
        if (!entryLoan.IsFocused) lblLoanCapNote.IsVisible = false;
    }

    private void SetPair(Label formatted, Label words, double value)
    {
        formatted.Text = value > 0 ? $"{_viewModel.CurrencySymbol}{value:N0}" : string.Empty;
        words.Text = LoanViewModel.AmountToWords(value);
    }

    private static double ParseAmount(string? text)
    {
        var digits = new string((text ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!double.TryParse(digits, out var value)) return 0;

        // Ceiling, not just a guard against silly input: above 2^53 a double stops holding
        // integers exactly, so the figure shown stops being the figure typed. See
        // LoanViewModel.MaxEnterableAmount.
        return Math.Min(value, LoanViewModel.MaxEnterableAmount);
    }

    private static string Grouped(double value) => value > 0 ? $"{value:N0}" : string.Empty;
}
