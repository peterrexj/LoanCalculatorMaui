using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Services;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// State and behaviour for the Quick Setup wizard, which collects the handful of figures needed
    /// to produce a first loan estimate.
    /// </summary>
    /// <remarks>
    /// <para>This used to live on <see cref="LoanViewModel"/> as three <c>SfPopup</c> steps driven by
    /// <c>IsWizardStepNVisible</c> flags. It is now one page, so there is no step state, no
    /// inter-step refresh timing, and no <c>DataTemplate</c> inflation to work around.</para>
    /// <para>Never serialised — unlike <see cref="WhatIfViewModel"/> this is not round-tripped
    /// through <c>LoadDataFile</c>, so peers are constructor-injected rather than assigned by a
    /// <c>Set…</c> call, and no <c>[JsonIgnore]</c> attributes are needed.</para>
    /// </remarks>
    /// <summary>Which side of the deposit/loan split a value was entered against.</summary>
    public enum LoanSplit
    {
        Deposit,
        Loan,
    }

    public class WizardViewModel : ViewModelUiBase
    {
        private readonly LoanViewModel _loanVm;
        private readonly IncomeViewModel _incomeVm;
        private readonly ExpenseViewModel _expenseVm;
        private readonly IErrorHandlingService _errorHandlingService;

        public WizardViewModel(
            LoanViewModel loanVm,
            IncomeViewModel incomeVm,
            ExpenseViewModel expenseVm,
            IErrorHandlingService errorHandlingService)
        {
            _loanVm = loanVm;
            _incomeVm = incomeVm;
            _expenseVm = expenseVm;
            _errorHandlingService = errorHandlingService;
        }

        // Always go through these rather than the injected singletons. The Budget tab owns its own
        // Income/Expense instances and those are what every reader consults, so writing the
        // singleton would make this commit invisible and the next page appearance would overwrite
        // it. See LoanViewModel.ResolveAuthoritativeIncome.
        private IncomeViewModel Income => _loanVm.ResolveAuthoritativeIncome(_incomeVm) ?? _incomeVm;
        private ExpenseViewModel Expense => _loanVm.ResolveAuthoritativeExpense(_expenseVm) ?? _expenseVm;

        // ── Transient text inputs ────────────────────────────────────────────────
        //
        // These are NOT bound to the Entry controls, and must not become bound. The page writes
        // them from its TextChanged handler and populates the entries imperatively instead.
        //
        // History, so this is not undone: the entries once had Text="{Binding Wizard*Text,
        // Mode=TwoWay}" while the handler also wrote entry.Text directly — two write paths into one
        // control. The unchanged-value guard below is what stops the property notification pushing
        // a value straight back into the Entry mid-keystroke. BudgetView's amount entries avoid the
        // whole class of problem the same way: no Text binding, populated imperatively.
        private string _wizardAssetText;
        public string WizardAssetText
        {
            get => _wizardAssetText;
            set
            {
                if (_wizardAssetText == value) return;
                _wizardAssetText = value;
                OnPropertyChanged(nameof(WizardAssetText));
            }
        }

        private string _wizardDepositText;
        public string WizardDepositText
        {
            get => _wizardDepositText;
            set
            {
                if (_wizardDepositText == value) return;
                _wizardDepositText = value;
                OnPropertyChanged(nameof(WizardDepositText));
            }
        }

        private string _wizardLoanText;
        public string WizardLoanText
        {
            get => _wizardLoanText;
            set
            {
                if (_wizardLoanText == value) return;
                _wizardLoanText = value;
                OnPropertyChanged(nameof(WizardLoanText));
            }
        }

        /// <summary>
        /// Which side of the deposit/loan split the user edited most recently, so
        /// <see cref="Commit"/> knows which one to write last.
        /// </summary>
        /// <remarks>
        /// The model cannot hold an independent deposit AND loan: it stores a total plus one split
        /// point, and whichever amount is written last derives the other
        /// (<c>ProcessDepositCalc</c>). So "the user typed a loan amount" and "the user typed a
        /// deposit" are genuinely different intents, and the page has to tell us which it was.
        /// Defaults to <see cref="LoanSplit.Deposit"/> because that is the field a first-run user
        /// fills in.
        /// </remarks>
        public LoanSplit LastEditedSplit { get; set; } = LoanSplit.Deposit;

        private string _wizardUpfrontText;
        public string WizardUpfrontText
        {
            get => _wizardUpfrontText;
            set
            {
                if (_wizardUpfrontText == value) return;
                _wizardUpfrontText = value;
                OnPropertyChanged(nameof(WizardUpfrontText));
            }
        }

        private string _wizardRunningCostText;
        public string WizardRunningCostText
        {
            get => _wizardRunningCostText;
            set
            {
                if (_wizardRunningCostText == value) return;
                _wizardRunningCostText = value;
                OnPropertyChanged(nameof(WizardRunningCostText));
            }
        }

        private string _wizardIncomeText;
        public string WizardIncomeText
        {
            get => _wizardIncomeText;
            set
            {
                if (_wizardIncomeText == value) return;
                _wizardIncomeText = value;
                OnPropertyChanged(nameof(WizardIncomeText));
            }
        }

        private string _wizardExpenseText;
        public string WizardExpenseText
        {
            get => _wizardExpenseText;
            set
            {
                if (_wizardExpenseText == value) return;
                _wizardExpenseText = value;
                OnPropertyChanged(nameof(WizardExpenseText));
            }
        }

        // ── Existing-value indicators ────────────────────────────────────────────
        //
        // These four figures can all be edited on their own screen — upfront costs in the Upfront
        // Costs popup, running costs on the Running Cost tab, income and expenses on the Budget
        // tab. So once a value exists, this page hides the field entirely rather than showing it
        // locked: if a field is on this page, you can change it here.
        //
        // HasValue reads the entries directly rather than a summary total, so it is correct even
        // before SumUpData has run. The *NeedsInput inverses are what the page binds IsVisible to.

        public bool WizardUpfrontHasValue => (_loanVm.HomeLoanInfo?.OtherExpenseTotalAmount ?? 0) > 0;

        /// <summary>
        /// Show the upfront field only when nothing is recorded.
        /// </summary>
        /// <remarks>
        /// Caveat worth knowing: <see cref="WizardUpfrontHasValue"/> reads
        /// <c>OtherExpenseTotalAmount</c>, which includes <b>auto-computed stamp duty</b> — so in
        /// Australian mode this turns false as soon as an asset price exists, without the user
        /// having typed an upfront cost. That is intentional (there genuinely is an upfront figure
        /// to see and edit on the Asset tab), but it means a first-run Australian user will not be
        /// offered the field.
        /// </remarks>
        public bool WizardUpfrontNeedsInput => !WizardUpfrontHasValue;
        public string WizardUpfrontSummary => $"Total upfront: {_loanVm.OtherExpenseTotalAmount}";

        public bool WizardRunningCostHasValue =>
            _loanVm.TransactionRecords?.IncomeExpenseEntries?.Any(e => e.Amount > 0) == true;
        public bool WizardRunningCostNeedsInput => !WizardRunningCostHasValue;
        public string WizardRunningCostSummary =>
            $"Total running costs: {CurrencySymbol}" +
            $"{_loanVm.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0:N0}/mo";

        public bool WizardIncomeHasValue =>
            Income?.TransactionRecords?.IncomeExpenseEntries?.Any(e => e.Amount > 0) == true;
        public bool WizardIncomeNeedsInput => !WizardIncomeHasValue;
        public string WizardIncomeSummary =>
            $"Recorded: {CurrencySymbol}" +
            $"{Income?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0:N0}/mo";

        public bool WizardExpenseHasValue =>
            Expense?.TransactionRecords?.IncomeExpenseEntries?.Any(e => e.Amount > 0) == true;
        public bool WizardExpenseNeedsInput => !WizardExpenseHasValue;
        public string WizardExpenseSummary =>
            $"Recorded: {CurrencySymbol}" +
            $"{Expense?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0:N0}/mo";

        /// <summary>
        /// True when at least one figure was picked up from elsewhere and its field is therefore
        /// hidden.
        /// </summary>
        public bool WizardShowInheritedNote =>
            WizardUpfrontHasValue || WizardRunningCostHasValue
            || WizardIncomeHasValue || WizardExpenseHasValue;

        /// <summary>
        /// Names the figures this page is using but not showing, so the calculation never depends
        /// on something invisible — while keeping them out of the way of what the user came to
        /// change.
        /// </summary>
        public string WizardInheritedNote
        {
            get
            {
                var inherited = new List<string>(4);
                if (WizardUpfrontHasValue) inherited.Add("upfront costs");
                if (WizardRunningCostHasValue) inherited.Add("running costs");
                if (WizardIncomeHasValue) inherited.Add("income");
                if (WizardExpenseHasValue) inherited.Add("expenses");

                if (inherited.Count == 0) return string.Empty;

                var list = inherited.Count == 1
                    ? inherited[0]
                    : string.Join(", ", inherited.Take(inherited.Count - 1)) + " and " + inherited[^1];

                return $"Also using your recorded {list}. Edit those on their own screens.";
            }
        }

        // ── Live labels under the asset and deposit fields ───────────────────────
        public bool WizardShowAssetTotal => (_loanVm.HomeLoanInfo?.PropertyTotalAmount ?? 0) > 0;
        public string WizardAssetTotalLabel => $"Total asset cost: {_loanVm.PropertyTotalAmount}";

        public bool WizardShowLoanAmount => (_loanVm.HomeLoanInfo?.LoanAmountDirectInput ?? 0) > 0;
        public string WizardLoanAmountLabel => $"Loan amount: {_loanVm.LoanAmountStrFormatted}";

        /// <summary>
        /// Re-reads just the four preview labels. Call after each keystroke in the asset, deposit
        /// or loan field.
        /// </summary>
        /// <remarks>
        /// These read committed loan state, so they only move when something tells them to. The page
        /// writes the model live as you type, but <see cref="Refresh"/> was the only notifier — so
        /// the labels showed the <em>previous</em> figures while typing, e.g. an asset field reading
        /// 12,323,232,334 above "Total asset cost: $1,000,000". This is deliberately narrower than
        /// <see cref="Refresh"/>: that one also calls <c>SumUpData</c> on three record sets and
        /// notifies ~25 properties, which is far too much to run per keystroke.
        /// </remarks>
        public void NotifyPreviewLabels()
        {
            OnPropertyChanged(nameof(WizardShowAssetTotal));
            OnPropertyChanged(nameof(WizardAssetTotalLabel));
            OnPropertyChanged(nameof(WizardShowLoanAmount));
            OnPropertyChanged(nameof(WizardLoanAmountLabel));
        }


        /// <summary>
        /// Fills the input text from whatever the user already has recorded, so reopening the wizard
        /// shows their figures rather than blanks.
        /// </summary>
        public void Prepopulate()
        {
            CurrencySymbol = _loanVm.CurrencySymbol;

            WizardAssetText = Format(_loanVm.PropertyAmount);
            WizardDepositText = Format(_loanVm.DepositAmountDirectInput);
            WizardUpfrontText = Format(_loanVm.HomeLoanInfo?.OtherExpenseTotalAmount ?? 0);

            _loanVm.TransactionRecords?.SumUpData();
            WizardRunningCostText = Format(_loanVm.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0);

            Income?.TransactionRecords?.SumUpData();
            WizardIncomeText = Format(Income?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0);

            Expense?.TransactionRecords?.SumUpData();
            WizardExpenseText = Format(Expense?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0);
        }

        /// <summary>
        /// Re-evaluates the label and lock-state bindings. The <c>SumUpData</c> calls live here, not
        /// in the property getters, so evaluating a binding never mutates the model.
        /// </summary>
        public void Refresh()
        {
            _loanVm.TransactionRecords?.SumUpData();
            Income?.TransactionRecords?.SumUpData();
            Expense?.TransactionRecords?.SumUpData();

            OnPropertyChanged(nameof(WizardUpfrontHasValue));
            OnPropertyChanged(nameof(WizardUpfrontNeedsInput));
            OnPropertyChanged(nameof(WizardUpfrontSummary));
            OnPropertyChanged(nameof(WizardRunningCostHasValue));
            OnPropertyChanged(nameof(WizardRunningCostNeedsInput));
            OnPropertyChanged(nameof(WizardRunningCostSummary));
            OnPropertyChanged(nameof(WizardIncomeHasValue));
            OnPropertyChanged(nameof(WizardIncomeNeedsInput));
            OnPropertyChanged(nameof(WizardIncomeSummary));
            OnPropertyChanged(nameof(WizardExpenseHasValue));
            OnPropertyChanged(nameof(WizardExpenseNeedsInput));
            OnPropertyChanged(nameof(WizardShowInheritedNote));
            OnPropertyChanged(nameof(WizardInheritedNote));
            OnPropertyChanged(nameof(WizardExpenseSummary));
            OnPropertyChanged(nameof(WizardShowAssetTotal));
            OnPropertyChanged(nameof(WizardAssetTotalLabel));
            OnPropertyChanged(nameof(WizardShowLoanAmount));
            OnPropertyChanged(nameof(WizardLoanAmountLabel));
        }

        /// <summary>
        /// Writes the collected figures into the loan, income and expense view models.
        /// </summary>
        /// <returns><c>true</c> when anything was written, so the caller knows whether to switch
        /// the Loan page back to its first tab.</returns>
        public bool Commit()
        {
            try
            {
                if (!_loanVm.HasInitialized) return false;

                // ── The order of these three calls is load-bearing. Do not rearrange. ──
                //
                // HomeLoanInformation has two degrees of freedom, not three: it stores a total
                // (PropertyAmount + OtherExpenseTotalAmount) plus ONE split point. Every amount
                // setter rewrites both sides of that split (ProcessDepositCalc), so whichever
                // value is written last wins and the other is derived from it.
                //
                // The six expense setters — OtherExpenses included — preserve the LOAN and
                // re-derive the DEPOSIT. So upfront costs written after a deposit make the deposit
                // absorb the entire upfront delta: deposit_new = deposit_old + upfront. This used
                // to run asset+deposit first and upfront second, which inflated the deposit by the
                // upfront amount for anyone who entered both.
                //
                // Expenses first, then the asset, then exactly ONE side of the deposit/loan split
                // last so that it is the value which wins.
                var wrote = ApplyUpfrontAndRunningCosts();
                wrote |= ApplyIncomeAndExpenses();
                wrote |= ApplyAssetPrice();
                wrote |= ApplySplitPoint();

                RepointLoanSummaries();
                return wrote;
            }
            catch (Exception ex)
            {
                _errorHandlingService?.HandleException(ex);
                return false;
            }
        }

        /// <summary>Writes the asset price only. The split point is applied separately, after.</summary>
        private bool ApplyAssetPrice()
        {
            if (!TryParseAmount(WizardAssetText, out var asset) || asset <= 0) return false;

            _loanVm.PropertyAmount = asset;
            return true;
        }

        /// <summary>
        /// Writes exactly one side of the deposit/loan split — the side the user last edited — and
        /// must be the final write of <see cref="Commit"/>.
        /// </summary>
        /// <remarks>
        /// <para>Writing both is not possible: each setter derives the other from the total
        /// (<c>ProcessDepositCalc</c> lines 243/251), so the second write would silently discard the
        /// first. <see cref="LastEditedSplit"/> records which value the user actually meant.</para>
        /// <para>The chosen value is written <b>unconditionally, including 0</b>. That is deliberate:
        /// <see cref="LoanViewModel.PropertyAmount"/>'s setter self-assigns the loan amount to force
        /// a recompute, and with no loan recorded that lands in <c>byLoanDirectOnZero</c>, which
        /// makes the deposit the entire <c>PropertyTotalAmount</c>. Writing the real figure last
        /// always corrects that clobber — and the same applies to the upfront costs applied before
        /// this, which preserve the loan and re-derive the deposit.</para>
        /// </remarks>
        private bool ApplySplitPoint()
        {
            var assetEntered = TryParseAmount(WizardAssetText, out var asset) && asset > 0;

            if (LastEditedSplit == LoanSplit.Loan)
            {
                if (!TryParseAmount(WizardLoanText, out var loan) || loan <= 0) return false;
                _loanVm.LoanAmountDirectInput = loan;
                return true;
            }

            var depositEntered = TryParseAmount(WizardDepositText, out var deposit) && deposit > 0;

            // With an asset price in play the deposit is written even when it is 0, to undo the
            // clobber described above. Without one there is nothing to correct, so a blank deposit
            // is left alone rather than forcing the loan to the whole total.
            if (!assetEntered && !depositEntered) return false;

            _loanVm.DepositAmountDirectInput = deposit;
            return depositEntered;
        }

        private bool ApplyUpfrontAndRunningCosts()
        {
            var wrote = false;

            if (!WizardUpfrontHasValue
                && TryParseAmount(WizardUpfrontText, out var upfront) && upfront > 0)
            {
                _loanVm.OtherExpenses = upfront;
                wrote = true;
            }

            if (!WizardRunningCostHasValue
                && TryParseAmount(WizardRunningCostText, out var running) && running > 0)
            {
                _loanVm.TransactionRecords ??= new Incomes { IncomeExpenseEntries = [] };
                _loanVm.TransactionRecords.Add(
                    "Running Costs", running, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
                _loanVm.RefreshExpenseTabPropertyChanged();
                wrote = true;
            }

            return wrote;
        }

        private bool ApplyIncomeAndExpenses()
        {
            var wrote = false;

            if (!WizardIncomeHasValue
                && TryParseAmount(WizardIncomeText, out var income) && income > 0)
            {
                var target = Income;
                target.TransactionRecords ??= new Incomes { IncomeExpenseEntries = [] };
                target.TransactionRecords.Add(
                    "Total Income", income, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
                target.TransactionRecords.SumUpData();
                SharedServiceCore.SaveData(target);
                wrote = true;
            }

            if (!WizardExpenseHasValue
                && TryParseAmount(WizardExpenseText, out var expense) && expense > 0)
            {
                var target = Expense;
                target.TransactionRecords ??= new Incomes { IncomeExpenseEntries = [] };
                target.TransactionRecords.Add(
                    "Total Expenses", expense, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
                target.TransactionRecords.SumUpData();
                SharedServiceCore.SaveData(target);
                wrote = true;
            }

            return wrote;
        }

        /// <summary>
        /// Points the loan's cross-tab summaries at the instances just written and recomputes the
        /// affordability flag, so the Affordability box and What If's stress test switch on without
        /// waiting for another page appearance.
        /// </summary>
        private void RepointLoanSummaries()
        {
            var income = Income;
            var expense = Expense;

            income?.TransactionRecords?.SumUpData();
            expense?.TransactionRecords?.SumUpData();

            if (income != null) _loanVm.IncomeSummary = income;
            if (expense != null) _loanVm.ExpenseSummary = expense;

            _loanVm.HasIncomeExpensesRecorded =
                expense?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 &&
                income?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;

            _loanVm.TriggerPropertyChangedOnPropertyTab();
            _loanVm.RefreshExpenseTabPropertyChanged();
            _loanVm.RefreshInsightsTabPropertyChanged();

            SharedServiceCore.MarkIncomeDirty();
            SharedServiceCore.MarkExpenseDirty();
            _loanVm.FlushPendingSave(() => SharedServiceCore.SaveData(_loanVm));
        }

        private static string Format(double value) => value > 0 ? $"{value:N0}" : string.Empty;

        /// <summary>Parses the grouped text the entries hold (e.g. "650,000").</summary>
        private static bool TryParseAmount(string text, out double value) =>
            double.TryParse(text?.Replace(",", string.Empty), out value);
    }
}
