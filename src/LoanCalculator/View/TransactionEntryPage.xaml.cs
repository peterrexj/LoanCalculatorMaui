using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.View;

/// <summary>
/// Single data-entry page for Income/Expense across all call sites: Loan Add Asset Expense,
/// Budget Income add/edit, Budget Expense add/edit. Replaces the three SfPopup forms that were
/// distributed across LoanView and BudgetView. Parameterized at open time via Configure(Config).
/// </summary>
public partial class TransactionEntryPage : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Config? _config;
    private bool _suppressTextChanged;
    private bool _isClosing;

    public class Config
    {
        public required ExpenseEntryViewBaseModel Target { get; init; }
        public required string TitleNoun { get; init; }
        public required string NameAutomationId { get; init; }
        public required string AmountAutomationId { get; init; }
        public required string SaveAutomationId { get; init; }
        public required Action OnSaved { get; init; }

        /// <summary>
        /// Deletes the entry being edited. Null when adding, which is what hides the button —
        /// there is nothing to delete before the entry exists.
        /// </summary>
        /// <remarks>
        /// This is the only delete path for Budget entries. The lists used to carry a delete
        /// button per row; they now open this page on tap instead, so dropping this callback
        /// silently removes the user's ability to delete anything.
        /// </remarks>
        public Action? OnDelete { get; init; }

        public string? DeleteAutomationId { get; init; }
    }

    public TransactionEntryPage(IErrorHandlingService errorHandlingService)
    {
        InitializeComponent();
        _errorHandlingService = errorHandlingService;
    }

    public Task<bool> Completion => _completion.Task;

    /// <summary>Called by the launcher to configure this page for a specific target ViewModel and call site.</summary>
    public void Configure(Config config)
    {
        _config = config;
        BindingContext = config.Target;

        lblTitle.Text = $"{config.TitleNoun} Entry";
        entryName.AutomationId = config.NameAutomationId;
        entryAmount.AutomationId = config.AmountAutomationId;
        btnSave.AutomationId = config.SaveAutomationId;

        btnDelete.IsVisible = config.OnDelete != null;
        if (config.DeleteAutomationId != null)
            btnDelete.AutomationId = config.DeleteAutomationId;
    }

    /// <summary>
    /// Deletes the entry and closes, once the user confirms.
    /// </summary>
    /// <remarks>
    /// This deleted on the single tap until now, on the stated grounds that the per-row delete
    /// button it replaced had none either. That reasoning stopped holding the moment those per-row
    /// buttons were removed: this became the ONLY delete path, it is irreversible, and there is no
    /// undo anywhere in the app. The entry's own name goes in the message, because the one mistake
    /// worth catching is deleting the right kind of thing from the wrong row.
    ///
    /// "Important" / "Yes" / "No" matches SettingsViewModel's delete confirmations rather than
    /// inventing a second wording — which is also what lets the UI suite confirm this the same way
    /// it already confirms those.
    /// </remarks>
    private async void OnDelete(object sender, EventArgs e)
    {
        if (_config?.OnDelete == null) return;

        try
        {
            var name = _config.Target?.IncomeEntryName;
            var subject = string.IsNullOrWhiteSpace(name) ? "this entry" : $"\"{name.Trim()}\"";

            if (!await DisplayAlert("Important", $"Do you wish to delete {subject}?", "Yes", "No"))
                return;

            _config.OnDelete();
            Dismiss(committed: true);
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_config?.Target == null) return;

        // The Amount entry is deliberately unbound (see OnAmountTextChanged), so an edit of an
        // existing row has to have its figure written in by hand — a binding here would be the
        // competing write path the whole page exists to avoid.
        var amount = _config.Target.IncomeEntryAmount;

        _suppressTextChanged = true;
        entryAmount.Text = Grouped(amount);
        _suppressTextChanged = false;

        SetAmountLabels(amount);
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

    private void OnSave(object sender, EventArgs e)
    {
        if (_config?.Target == null) return;

        try
        {
            _config.Target.ShowValidationErrors = true;
            if (_config.Target.HasErrorIncomeDescription || _config.Target.HasErrorIncomeAmount)
                return;

            if (_config.Target.AddOrUpdateEntryFromView() == false)
                return;

            _config.OnSaved();
            Dismiss(committed: true);
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void OnCancel(object sender, EventArgs e)
    {
        if (_config?.Target == null) return;

        try
        {
            _config.Target.ResetTransactionEntryData();
            Dismiss(committed: false);
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
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

    /// <summary>
    /// Formats the amount entry with thousands separators and updates preview labels on every keystroke.
    /// Uses the suppress-flag pattern to prevent reentrant reformatting. See LoanDetailsPage.xaml.cs
    /// for detailed commentary on this pattern.
    /// </summary>
    private void OnAmountTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not Entry entry) return;

        var value = ParseAmount(e.NewTextValue);
        SetAmountLabels(value);

        if (_config?.Target != null)
            _config.Target.IncomeEntryAmount = value;
    }

    /// <summary>Applies thousands separators once the user has finished with the field.</summary>
    private void OnAmountUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is not Entry entry) return;

        var grouped = Grouped(ParseAmount(entry.Text));
        if (grouped == entry.Text) return;

        _suppressTextChanged = true;
        entry.Text = grouped;
        _suppressTextChanged = false;
    }

    /// <summary>Refreshes the card foot when the frequency changes but the amount has not.</summary>
    private void OnFrequencyChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e) =>
        SetAmountLabels(_config?.Target?.IncomeEntryAmount ?? 0);

    /// <summary>
    /// Updates the word form under the Amount field and the card foot's two period figures.
    /// </summary>
    /// <remarks>
    /// The foot states the same amount converted to BOTH periods the rest of the app reports in,
    /// which is the only place a wrong frequency becomes visible — the fields themselves look
    /// correct whichever period is selected. Conversion is <c>ModelHelper</c>'s, the same code the
    /// Budget lists and totals use, so the figures here cannot disagree with the ones the entry
    /// will produce once saved.
    ///
    /// A dash, not the empty string, when there is nothing to show: an empty Label measures to
    /// zero height and iOS then reports it as absent, so the row would be unfindable before the
    /// user types anything.
    /// </remarks>
    private void SetAmountLabels(double value)
    {
        var currencySymbol = _config?.Target?.CurrencySymbol;

        lblAmountWords.Text = LoanViewModel.AmountToWords(value);

        if (value <= 0)
        {
            lblEquivMonthly.Text = "—";
            lblEquivYearly.Text = "—";
            return;
        }

        var frequency = Enum.TryParse<TimeFrequencyEnum>(
            _config?.Target?.IncomeExpenseFrequencySelectedIndex, out var parsed)
            ? parsed
            : TimeFrequencyEnum.Monthly;

        var monthly = ModelHelper.ConvertAmountToMonthlyFrequency(value, frequency);
        var yearly = ModelHelper.ConvertAmountToYearlyFrequency(value, frequency);

        lblEquivMonthly.Text = $"{currencySymbol}{monthly:N0} / month";
        lblEquivYearly.Text = $"{currencySymbol}{yearly:N0} a year";
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
