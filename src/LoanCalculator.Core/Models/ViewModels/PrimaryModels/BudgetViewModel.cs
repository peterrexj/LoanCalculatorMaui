using LoanCalculator.Core.Constants;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class BudgetViewModel : ViewModelUiBase
    {
        [JsonIgnore] private ObservableCollection<Brush>? _customChartColors;
        [JsonIgnore]
        public ObservableCollection<Brush>? CustomChartColors
        {
            get => _customChartColors;
            set { _customChartColors = value; OnPropertyChanged(nameof(CustomChartColors)); }
        }

        // ── Owned Income/Expense VMs ──────────────────────────────────────────

        [JsonIgnore] private IncomeViewModel _income = new IncomeViewModel();
        [JsonIgnore]
        public IncomeViewModel Income
        {
            get => _income;
            set { _income = value; OnPropertyChanged(nameof(Income)); }
        }

        [JsonIgnore] private ExpenseViewModel _expense = new ExpenseViewModel();
        [JsonIgnore]
        public ExpenseViewModel Expense
        {
            get => _expense;
            set
            {
                if (_expense != null) _expense.PropertyChanged -= OnExpensePropertyChanged;
                _expense = value;
                if (_expense != null) _expense.PropertyChanged += OnExpensePropertyChanged;

                OnPropertyChanged(nameof(Expense));
                NotifyExpensesTabNet();
            }
        }

        public BudgetViewModel()
        {
            // The field initialiser above bypasses the setter, so the first instance has to be
            // subscribed here or the Expenses tab's switch would work only after Expense was
            // reassigned — which is to say, never in practice.
            _expense.PropertyChanged += OnExpensePropertyChanged;
        }

        /// <summary>
        /// Keeps the Expenses tab's Net figure in step with the switch on that tab.
        /// </summary>
        /// <remarks>
        /// That switch lives on <see cref="ExpenseViewModel"/> but the figure it changes lives here,
        /// and nothing connected the two — which is why the Budget ▸ Expenses switches did nothing
        /// at all. They were left over from the standalone ExpenseView, where the top box showed
        /// income; on the merged Budget tab they drove properties no longer bound to anything.
        /// </remarks>
        private void OnExpensePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ExpenseViewModel.ShowPropertyExpense)) return;

            NotifyExpensesTabNet();
        }

        private void NotifyExpensesTabNet()
        {
            OnPropertyChanged(nameof(ExpensesTabNetMonthly));
            OnPropertyChanged(nameof(ExpensesTabNetIsPositive));
            OnPropertyChanged(nameof(ExpensesTabNetIsNegative));
            OnPropertyChanged(nameof(ExpensesTabNetMonthlyFormatted));
            OnPropertyChanged(nameof(ExpensesTabNetCaption));
            OnPropertyChanged(nameof(ExpensesTabNetBreakdown));
            OnPropertyChanged(nameof(LoanRepaymentMonthly));
        }

        // Kept for LoanView Wizard compatibility — also assigns Income/Expense
        [JsonIgnore] private LoanViewModel? _loanVm;

        public void SetPeerViewModels(IncomeViewModel income, ExpenseViewModel expense, LoanViewModel loan)
        {
            Income = income;
            Expense = expense;
            _loanVm = loan;
        }

        /// <summary>
        /// Gives Income and Expense the loan's running costs and repayment, which is what the
        /// "take off the loan and its running costs" switches actually deduct.
        /// </summary>
        /// <remarks>
        /// <b>This has to be called before any projection that honours those switches.</b> Both
        /// <c>IncomeViewModel.UpdateProjectionData</c> and
        /// <c>ExpenseViewModel.UpdateProjectionData</c> add up
        /// <c>PropertyExpenseSummary.TotalYearly</c> and <c>PropertyPayment.TermPaymentYearly</c>
        /// — but only when those are non-null, and they do it with <c>?? 0</c>, so a null one
        /// contributes nothing and reports nothing.
        ///
        /// It used to live in BudgetView.WirePropertyExpenseDataAsync and nowhere else, so the
        /// figures were populated only by visiting the Budget tab. Going straight to What If ▸
        /// Projection — which the Loan page's own link does — left both of them null, and the two
        /// "take off the loan" switches then deducted zero: they moved the switch, redrew
        /// everything, and changed not one number. Indistinguishable from a dead control.
        ///
        /// Lives here rather than on either page so the two cannot drift apart again.
        /// </remarks>
        public async Task WireLoanFiguresAsync()
        {
            var loanData = await SharedServiceCore.GetLoanViewModelAsync();

            Income.PropertyExpenseSummary = loanData.Item1;
            Income.PropertyPayment = loanData.Item2;
            Expense.PropertyExpenseSummary = loanData.Item1;
            Expense.PropertyPayment = loanData.Item2;

            // BOTH directions. Expense.IncomeSummary alone is not enough: "take off my everyday
            // spending" is computed by IncomeViewModel.UpdateProjectionData from
            // ExpenseSummary.TransactionRecords.IncomeExpenseSummary.TotalYearly, and it skips the
            // deduction entirely when ExpenseSummary is null — so that switch had exactly the same
            // silent-no-op fault as the two loan ones until this line existed.
            //
            // Income.ExpenseSummary was previously assigned only in InitializeBudget, which runs
            // from BudgetView. Reaching What If without visiting Budget therefore left it null.
            // Re-affirmed here as well as there, in case either side changed since.
            Income.ExpenseSummary = Expense;
            Expense.IncomeSummary = Income;
        }

        // Loads Income and Expense from disk if their standalone tabs haven't done it yet.
        // Safe to call multiple times — skips VMs that are already initialised.
        // Call from SplashPage to pre-warm during the animation, and from BudgetView.OnAppearing
        // as a cheap no-op on subsequent visits.
        public async Task EnsureSubVmsLoadedAsync()
        {
            var incomeTask = !Income.HasInitialized ? LoadIncomeAsync() : Task.CompletedTask;
            var expenseTask = !Expense.HasInitialized ? LoadExpenseAsync() : Task.CompletedTask;
            await Task.WhenAll(incomeTask, expenseTask);
        }

        private async Task LoadIncomeAsync()
        {
            try
            {
                Income.IsUpdating = true;
                SharedServiceCore.LoadSafeOn();
                var data = await SharedServiceCore.LoadDataFile<IncomeViewModel>();
                if (data == null || data.TransactionRecords == null)
                    Income.AddDefaultToExpenses();
                else
                    Income.CopyPropertiesFrom(data);

                if (Income.TransactionRecords == null)
                    Income.AddDefaultToExpenses();

                Income.InitializeViewData();
                Income.IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
                Income.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
                Income.CurrencySymbol = Helper.CurrencySymbol;

                // Sum before marking initialised. Nothing on this load path does it —
                // LoadDataFile/CopyPropertiesFrom/InitializeViewData all leave the summary alone —
                // and TotalMonthly/TotalYearly are [JsonIgnore], so they arrive as 0 no
                // matter what the file contained. SplashPage pre-warms through here, and
                // LoanViewModel.HasIncomeExpensesRecorded gates on TotalYearly > 0, so
                // without this the affordability box was hidden on the first Loan page appearance
                // of every launch until the user happened to visit Budget.
                Income.TransactionRecords?.SumUpData();

                Income.MarkInitializationComplete();
            }
            catch (Exception ex)
            {
                SharedServiceCore.ErrorHandlingService.HandleException(ex);
            }
            finally
            {
                SharedServiceCore.LoadSafeOff();
                Income.IsUpdating = false;
            }
        }

        private async Task LoadExpenseAsync()
        {
            try
            {
                Expense.IsUpdating = true;
                SharedServiceCore.LoadSafeOn();
                var data = await SharedServiceCore.LoadDataFile<ExpenseViewModel>();
                if (data == null || data.TransactionRecords == null)
                    Expense.AddDefaultToExpenses();
                else
                    Expense.CopyPropertiesFrom(data);

                if (Expense.TransactionRecords == null)
                    Expense.AddDefaultToExpenses();

                Expense.InitializeViewData();
                Expense.IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
                Expense.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
                Expense.CurrencySymbol = Helper.CurrencySymbol;

                // Sum before marking initialised. Nothing on this load path does it —
                // LoadDataFile/CopyPropertiesFrom/InitializeViewData all leave the summary alone —
                // and TotalMonthly/TotalYearly are [JsonIgnore], so they arrive as 0 no
                // matter what the file contained. SplashPage pre-warms through here, and
                // LoanViewModel.HasIncomeExpensesRecorded gates on TotalYearly > 0, so
                // without this the affordability box was hidden on the first Loan page appearance
                // of every launch until the user happened to visit Budget.
                Expense.TransactionRecords?.SumUpData();

                Expense.MarkInitializationComplete();
            }
            catch (Exception ex)
            {
                SharedServiceCore.ErrorHandlingService.HandleException(ex);
            }
            finally
            {
                SharedServiceCore.LoadSafeOff();
                Expense.IsUpdating = false;
            }
        }

        /// <summary>
        /// Initialize Income and Expense frequency collections, wire cross-VM references,
        /// and propagate currency symbol. Safe to call multiple times (idempotent for
        /// frequency collections).
        /// </summary>
        public void InitializeBudget()
        {
            Income.InitializeViewData();
            Expense.InitializeViewData();

            // Wire cross-VM references so each side can see the other's totals
            Expense.IncomeSummary = Income;
            Income.ExpenseSummary = Expense;

            // Propagate currency
            CurrencySymbol = _loanVm?.CurrencySymbol
                ?? Income.CurrencySymbol
                ?? Helper.CurrencySymbol;
        }

        // ── Summary tab ────────────────────────────────────────────────────

        [JsonIgnore] public double TotalIncomeMonthly =>
            Income.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;

        [JsonIgnore] public double TotalExpenseMonthly =>
            Expense.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;

        [JsonIgnore] public double NetMonthly => TotalIncomeMonthly - TotalExpenseMonthly;
        [JsonIgnore] public bool NetIsPositive => NetMonthly >= 0;

        [JsonIgnore] public string TotalIncomeMonthlyFormatted =>
            $"{CurrencySymbol}{TotalIncomeMonthly:N0}";

        [JsonIgnore] public string TotalExpenseMonthlyFormatted =>
            $"{CurrencySymbol}{TotalExpenseMonthly:N0}";

        [JsonIgnore] public string NetMonthlyFormatted =>
            $"{(NetIsPositive ? "+" : "-")}{CurrencySymbol}{Math.Abs(NetMonthly):N0}";

        [JsonIgnore] public double TotalIncomeYearly =>
            Income.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;

        [JsonIgnore] public double TotalExpenseYearly =>
            Expense.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;

        [JsonIgnore] public double NetYearly => TotalIncomeYearly - TotalExpenseYearly;
        [JsonIgnore] public string NetYearlyFormatted =>
            $"{(NetYearly >= 0 ? "+" : "-")}{CurrencySymbol}{Math.Abs(NetYearly):N0}";

        // ── Whole-budget outgoings, for the Summary tab ────────────────────
        //
        // TotalExpenseMonthly above is tab-scoped: it is what the Expenses tab totals, and that is
        // the right figure for the "Net / month" box on that tab. The SUMMARY is the whole-budget
        // view, and it was showing the same tab-scoped number — so the asset running costs recorded
        // on Budget's own third tab were missing from "out monthly" and from NET EVERY MONTH, while
        // the "spare after loan" box beside them DID include them (it comes from
        // LoanViewModel.MonthlySurplus). Three boxes, two different ideas of what counts.
        //
        // Separate properties rather than widening TotalExpenseMonthly, so the Expenses tab keeps
        // answering the question it is asking.

        /// <summary>Everything going out each month: recorded expenses plus asset running costs.</summary>
        [JsonIgnore] public double TotalOutgoingsMonthly => TotalExpenseMonthly + AssetRunningCostMonthly;

        [JsonIgnore] public string TotalOutgoingsMonthlyFormatted =>
            $"{CurrencySymbol}{TotalOutgoingsMonthly:N0}";

        /// <summary>Income less every outgoing, but before the loan repayment.</summary>
        /// <remarks>
        /// Before the loan on purpose: the third summary box ("spare after loan") is what takes the
        /// repayment off, so including it here would state it twice and leave the two boxes looking
        /// like they disagreed.
        /// </remarks>
        [JsonIgnore] public double NetAfterOutgoingsMonthly => TotalIncomeMonthly - TotalOutgoingsMonthly;
        [JsonIgnore] public bool NetAfterOutgoingsIsPositive => NetAfterOutgoingsMonthly >= 0;

        [JsonIgnore] public string NetAfterOutgoingsMonthlyFormatted =>
            $"{(NetAfterOutgoingsIsPositive ? "+" : "-")}{CurrencySymbol}{Math.Abs(NetAfterOutgoingsMonthly):N0}";

        [JsonIgnore] public double AssetRunningCostYearly =>
            _loanVm?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly ?? 0;

        [JsonIgnore] public double TotalOutgoingsYearly => TotalExpenseYearly + AssetRunningCostYearly;
        [JsonIgnore] public double NetAfterOutgoingsYearly => TotalIncomeYearly - TotalOutgoingsYearly;

        [JsonIgnore] public string NetAfterOutgoingsYearlyFormatted =>
            $"{(NetAfterOutgoingsYearly >= 0 ? "+" : "-")}{CurrencySymbol}{Math.Abs(NetAfterOutgoingsYearly):N0}";

        // ── The Expenses tab's Net box ─────────────────────────────────────
        //
        // The Expenses tab has one switch — "Also take off the loan and its running costs" — and
        // this is the figure it changes. Its companion, "Take off my everyday spending", was
        // removed from that tab: the Net box already subtracts the expenses listed there, so the
        // switch could never do anything, which is exactly how it behaved.

        /// <summary>The loan repayment, as a monthly figure.</summary>
        /// <remarks>
        /// TermPaymentMonthly rather than TermPayment: the latter is the payment at whatever
        /// frequency the user chose, and every figure on the Budget tabs is monthly.
        /// </remarks>
        [JsonIgnore]
        public double LoanRepaymentMonthly =>
            _loanVm?.HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0;

        /// <summary>True when the Expenses tab's switch is on AND there is a loan to take off.</summary>
        [JsonIgnore]
        public bool ExpensesTabIncludesLoan => Expense.ShowPropertyExpense && IsSpareAfterLoanAvailable;

        /// <summary>
        /// Income less the expenses on the Expenses tab, and — when that tab's switch is on — less
        /// the loan repayment and the asset running costs as well.
        /// </summary>
        /// <remarks>
        /// Uses <c>SpareAfterLoan</c> for the switched-on case rather than subtracting the pieces
        /// again, so this cannot drift from the "spare after loan" figure on the Summary tab. Falls
        /// back to the plain net when there is no affordability data, because the loan figure would
        /// be zero and the box would appear to ignore the switch.
        /// </remarks>
        [JsonIgnore]
        public double ExpensesTabNetMonthly => ExpensesTabIncludesLoan ? SpareAfterLoan : NetMonthly;

        [JsonIgnore] public bool ExpensesTabNetIsPositive => ExpensesTabNetMonthly >= 0;

        /// <summary>
        /// The same condition the other way round, because that is what the colour converter takes.
        /// </summary>
        /// <remarks>
        /// ExpensesTabNetIsPositive existed but was never used by any markup — the Expenses tab's
        /// Net figure went negative in the ordinary colour. The converter is written around "is
        /// negative" (true selects the negative key), so this is the form the binding needs.
        /// </remarks>
        [JsonIgnore] public bool ExpensesTabNetIsNegative => !ExpensesTabNetIsPositive;

        [JsonIgnore] public string ExpensesTabNetMonthlyFormatted =>
            $"{(ExpensesTabNetIsPositive ? "+" : "-")}{CurrencySymbol}{Math.Abs(ExpensesTabNetMonthly):N0}";

        /// <summary>
        /// How the Net figure was arrived at, as a sum — the same courtesy the first box on that
        /// tab already extends with its own "(130 + 890 + 5,266)" breakdown.
        /// </summary>
        /// <remarks>
        /// Empty when there is nothing to break down, so the label collapses rather than showing
        /// an equation made of zeros.
        /// </remarks>
        [JsonIgnore]
        public string ExpensesTabNetBreakdown
        {
            get
            {
                if (TotalIncomeMonthly <= 0) return string.Empty;

                var parts = $"{CurrencySymbol}{TotalIncomeMonthly:N0} − {CurrencySymbol}{TotalExpenseMonthly:N0}";

                if (ExpensesTabIncludesLoan)
                {
                    parts += $" − {CurrencySymbol}{AssetRunningCostMonthly:N0}"
                             + $" − {CurrencySymbol}{LoanRepaymentMonthly:N0}";
                }

                return $"({parts})";
            }
        }

        /// <summary>States what the Net box is net of, so the switch's effect is readable.</summary>
        [JsonIgnore]
        public string ExpensesTabNetCaption =>
            ExpensesTabIncludesLoan ? "Net / month (after loan & asset costs)" : "Net / month";

        // ── "With your numbers" sentences for the switch explanations ─────
        //
        // The ⓘ on each Budget switch explains what the switch does in general; these say what it
        // would do to this user's own figures, which is what people actually wanted. They are
        // here rather than in BudgetToggleExplanations because they need live values, and that
        // table is static copy pinned by a unit test.

        /// <summary>What the Income tab's "everyday spending" switch would do, in real figures.</summary>
        [JsonIgnore]
        public string IncomeAfterExpensesLiveExample
        {
            get
            {
                if (TotalIncomeMonthly <= 0)
                    return "Add some income on this tab and some spending on the Expenses tab, and "
                           + "this will show you what the switch would do to your own figures.";

                var gross = $"{CurrencySymbol}{TotalIncomeMonthly:N0}";
                var spend = $"{CurrencySymbol}{TotalExpenseMonthly:N0}";
                var net = $"{CurrencySymbol}{Math.Abs(NetMonthly):N0}";
                var sign = NetMonthly < 0 ? "short by " : string.Empty;

                return $"With your numbers: the top shows {gross} a month now. Your spending comes to "
                       + $"{spend}, so with this on it would show {sign}{net} instead.";
            }
        }

        /// <summary>What the Income tab's "loan and asset costs" switch would do, in real figures.</summary>
        [JsonIgnore]
        public string IncomeAfterLoanLiveExample
        {
            get
            {
                if (TotalIncomeMonthly <= 0)
                    return "Add your income, and the loan details on the Loan tab, and this will show "
                           + "you what the switch would do to your own figures.";

                var assetCosts = $"{CurrencySymbol}{AssetRunningCostMonthly:N0}";

                if (!IsSpareAfterLoanAvailable)
                    return $"With your numbers: your asset running costs come to {assetCosts} a month. "
                           + "Record your income and expenses to see the loan repayment taken off too.";

                var spare = $"{CurrencySymbol}{Math.Abs(SpareAfterLoan):N0}";
                var sign = SpareAfterLoan < 0 ? "short by " : "left with ";

                return $"With your numbers: the loan repayment plus {assetCosts} a month of asset "
                       + $"running costs. With this and the switch above both on, you would be "
                       + $"{sign}{spare} a month.";
            }
        }

        /// <summary>What the Expenses tab's single switch would do, in real figures.</summary>
        /// <remarks>
        /// Names both amounts it takes off rather than calling them "the loan repayment and the
        /// asset running costs" — the sentence is there precisely so the reader does not have to go
        /// and look them up.
        /// </remarks>
        [JsonIgnore]
        public string ExpenseAfterLoanLiveExample
        {
            get
            {
                if (TotalIncomeMonthly <= 0)
                    return "Add your income on the Income tab and this will show you what the switch "
                           + "would do to your own figures.";

                var net = $"{(NetMonthly < 0 ? "-" : string.Empty)}{CurrencySymbol}{Math.Abs(NetMonthly):N0}";
                var assetCosts = $"{CurrencySymbol}{AssetRunningCostMonthly:N0}";

                if (!IsSpareAfterLoanAvailable || LoanRepaymentMonthly <= 0)
                    return $"With your numbers: Net / month is {net}, which is your income less the "
                           + $"spending on this tab. Your asset running costs come to {assetCosts} a "
                           + "month — enter your loan details on the Loan tab and this switch will "
                           + "take the repayment off as well.";

                var repayment = $"{CurrencySymbol}{LoanRepaymentMonthly:N0}";
                var after = $"{(SpareAfterLoan < 0 ? "-" : string.Empty)}{CurrencySymbol}{Math.Abs(SpareAfterLoan):N0}";
                var shortBy = SpareAfterLoan < 0
                    ? $" In other words you would be short by {CurrencySymbol}{Math.Abs(SpareAfterLoan):N0} every month."
                    : $" In other words you would have {CurrencySymbol}{SpareAfterLoan:N0} a month left over.";

                return $"With your numbers: Net / month is {net} at the moment. Switching this on "
                       + $"takes off the {repayment} a month loan repayment and the {assetCosts} a "
                       + $"month of asset running costs, which leaves {after}.{shortBy}";
            }
        }

        // ── Insights rows ─────────────────────────────────────────────────
        // Counts only entries carrying a figure: the expense list is pre-seeded with
        // every category at zero, so a raw Count would read "23 recorded" on a budget
        // the user has entered one number into.
        [JsonIgnore] public int IncomeSourceCount =>
            Income.TransactionRecords?.IncomeExpenseEntries?.Count(e => e.Amount > 0) ?? 0;

        [JsonIgnore] public int ExpenseRecordCount =>
            Expense.TransactionRecords?.IncomeExpenseEntries?.Count(e => e.Amount > 0) ?? 0;

        [JsonIgnore] public string IncomeSourceCountText =>
            $"{IncomeSourceCount} {(IncomeSourceCount == 1 ? "source" : "sources")}";

        [JsonIgnore] public string ExpenseRecordCountText => $"{ExpenseRecordCount} recorded";

        [JsonIgnore] public double AssetRunningCostMonthly =>
            _loanVm?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;

        [JsonIgnore] public string AssetRunningCostMonthlyFormatted =>
            $"-{CurrencySymbol}{AssetRunningCostMonthly:N0}";

        [JsonIgnore] public string TotalIncomeMonthlySigned => $"+{TotalIncomeMonthlyFormatted}";
        [JsonIgnore] public string TotalExpenseMonthlySigned => $"-{TotalExpenseMonthlyFormatted}";

        // "Spare after loan" — the net above does NOT include the repayment, so this is the
        // only figure on the page that answers what is actually left. Deliberately reuses
        // LoanViewModel.MonthlySurplus rather than subtracting a repayment here: that figure
        // is already net of income, living expenses, asset running costs AND the repayment,
        // so doing the arithmetic locally would double-count the asset costs.
        [JsonIgnore] public bool IsSpareAfterLoanAvailable => _loanVm?.IsAffordabilityAvailable ?? false;

        [JsonIgnore] public double SpareAfterLoan => _loanVm?.MonthlySurplus ?? 0;

        /// <summary>
        /// True when there is a figure AND it is negative — not simply <c>SpareAfterLoan &lt; 0</c>,
        /// so that the colour and the displayed value are deciding off the same condition.
        /// </summary>
        [JsonIgnore] public bool IsSpareAfterLoanNegative => IsSpareAfterLoanAvailable && SpareAfterLoan < 0;

        [JsonIgnore] public string SpareAfterLoanFormatted =>
            IsSpareAfterLoanAvailable
                ? $"{(SpareAfterLoan >= 0 ? "+" : "-")}{CurrencySymbol}{Math.Abs(SpareAfterLoan):N0}"
                : "--";

        // Top 5 expenses by amount
        [JsonIgnore] public List<IncomeExpense> TopExpenses =>
            Expense.TransactionRecords?.IncomeExpenseEntries?
                .Where(e => e.Amount > 0)
                .OrderByDescending(e => e.AmountMonthly)
                .Take(5)
                .ToList() ?? new List<IncomeExpense>();

        // Chart: income vs expense donut — two slices in one collection
        [JsonIgnore] private ObservableCollection<ChartDataModel> _summaryDonutData = new();
        [JsonIgnore] public ObservableCollection<ChartDataModel> SummaryDonutData => _summaryDonutData;

        // Affordability from loan VM
        [JsonIgnore] public string Affordability => _loanVm?.Affordability ?? "--";
        [JsonIgnore] public string AffordabilityCurrencySymbol => _loanVm?.AffordabilityCurrencySymbol ?? string.Empty;
        [JsonIgnore] public bool IsAffordabilityAvailable => _loanVm?.IsAffordabilityAvailable ?? false;
        [JsonIgnore] public string AffordabilityTextDescription => _loanVm?.AffordabilityTextDescription ?? string.Empty;

        // ── Projection tab ────────────────────────────────────────────────

        [JsonIgnore]
        public int MaxProjectionYears => SharedServiceCore.GetMaxProjectionYears();

        [JsonIgnore] private int _projectionYears = SharedServiceCore.GetDefaultProjectionYears();
        [JsonIgnore]
        public int ProjectionYears
        {
            get => _projectionYears;
            set
            {
                _projectionYears = value;
                OnPropertyChanged(nameof(ProjectionYears));
                OnPropertyChanged(nameof(ChartProjectionSubtitle));
                Income.TotalYearsToProject = value;
                Expense.TotalYearsToProject = value;
                RecalculateProjection();
            }
        }

        [JsonIgnore] public string ChartProjectionSubtitle =>
            $"Projected over {ProjectionYears} yr{(ProjectionYears == 1 ? "" : "s")}";

        /// <summary>
        /// What the Summary tab's hero figure and its three tiles actually mean, in the reader's
        /// own numbers.
        /// </summary>
        /// <remarks>
        /// Revealed by the ⓘ beside the "NET EVERY MONTH" heading. An ⓘ and not an SfExpander:
        /// BudgetView calls SyncfusionIosTouchFix.ApplyToTabView but NOT ApplyToExpanders, so an
        /// expander here would silently refuse to open on iOS until that fix was added and
        /// re-verified on device. The switch rows on this same page use the ⓘ for exactly that
        /// reason — see SwitchWithInfoView.
        ///
        /// Quotes live figures rather than a worked example, because the question this answers is
        /// "where did MY number come from" — four summary figures sat here with no account of how
        /// they relate, and the last tile in particular (spare after loan) is the only one that
        /// involves the loan at all, which was not apparent from a caption.
        /// </remarks>
        [JsonIgnore]
        public string SummaryExplanationDetail
        {
            get
            {
                var hasData = TotalIncomeMonthly > 0 || TotalOutgoingsMonthly > 0;
                if (!hasData)
                    return "Once you have added some money coming in and going out, this will "
                           + "explain where each of the figures above comes from, using your own "
                           + "numbers.";

                var direction = NetAfterOutgoingsIsPositive
                    ? "So more comes in than goes out, which is what you want."
                    : "So more goes out than comes in, which means you are short each month.";

                return
                    $"The big number ({NetAfterOutgoingsMonthlyFormatted}) is everything that comes "
                    + $"in each month, minus everything that goes out. {direction} The smaller "
                    + "figure under it is the same thing over a whole year, which is where a small "
                    + "monthly gap starts to look like real money.\n\n"
                    + $"\"{TotalIncomeMonthlyFormatted} in monthly\" is all the money you have "
                    + "coming in, turned into a monthly amount. It does not matter whether you "
                    + "entered something as weekly, monthly or yearly — it is all counted here as "
                    + "what it works out to per month.\n\n"
                    + $"\"{TotalOutgoingsMonthlyFormatted} out monthly\" is all the money going "
                    + "out. This is more than just your everyday spending: it also includes the "
                    + "regular costs of looking after the thing you are buying, such as upkeep, "
                    + "insurance and bills. Those are real monthly costs, so they are counted here "
                    + "too. That is why this number can be bigger than your everyday expenses "
                    + "alone.\n\n"
                    + $"\"{SpareAfterLoanFormatted} spare after loan\" is the only one of the three "
                    + "that includes the loan. It takes what is left over and then takes off the "
                    + $"loan repayment as well ({CurrencySymbol}{LoanRepaymentMonthly:N0} a month). "
                    + "So it answers the question that matters most: once you are paying the loan "
                    + "every month, how much money do you actually have left?\n\n"
                    + "If that last number feels comfortable, you have room for surprises like a "
                    + "car repair or a quiet month at work. If it is very small, or below zero, "
                    + "then the loan only works on paper and not in real life. These are estimates "
                    + "to help you think it through, not financial advice.";
            }
        }

        /// <summary>
        /// One line naming every Projection setting currently in force.
        /// </summary>
        /// <remarks>
        /// Shown under the collapsed "Adjust the settings" header, and it is the thing that makes
        /// collapsing those settings safe. A closed panel that merely says "Settings" hides state:
        /// a reader cannot tell whether the totals above them are after the loan or before it, and
        /// the two readings differ enormously. This spells it out without being opened.
        ///
        /// Deliberately phrased as plain English rather than a list of control names — "after the
        /// loan" rather than "IncludePropertyExpenses: true".
        /// </remarks>
        [JsonIgnore]
        public string ProjectionSettingsSummary
        {
            get
            {
                var parts = new List<string>
                {
                    $"money in +{Income.AnnualGrowthRate:0.##}% a year",
                    $"money out +{Expense.AnnualGrowthRate:0.##}% a year",
                    $"next {ProjectionYears} year{(ProjectionYears == 1 ? "" : "s")}",
                };

                // Only the deductions that are actually on. Listing the off ones too would make
                // every reading look the same at a glance, which defeats the point.
                if (Income.IncludeExpenses) parts.Add("after everyday spending");
                if (Income.IncludePropertyExpenses || Expense.IncludePropertyExpenses)
                    parts.Add("after the loan");

                return string.Join(" · ", parts);
            }
        }

        /// <summary>
        /// The Projection tab's two totals, restated in the reader's own figures.
        /// </summary>
        /// <remarks>
        /// Shown under the ⓘ beside those totals, as its live example. There is deliberately no
        /// view-model property for the static copy: that lives in
        /// <see cref="Constants.ProjectionInfo"/> and the markup binds it with <c>x:Static</c>, so
        /// there is no plumbing and no notification to forget. Only this sentence needs the view
        /// model, because only this sentence contains live numbers.
        ///
        /// Defaults to a real sentence rather than an empty string: the hint hides its live-example
        /// line when this is blank, and before anything is entered the useful thing to say is where
        /// to go and enter it.
        /// </remarks>
        [JsonIgnore]
        public string ProjectionPersonalLine
        {
            get
            {
                var income = Income.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;
                var expense = Expense.TransactionRecords?.IncomeExpenseSummary?.ProjectTotalYearly ?? 0;

                if (income <= 0 && expense <= 0)
                    return "Add the money you have coming in and going out, and this will show "
                           + "what your own figures add up to.";

                var years = ProjectionYears;
                var period = $"over the next {years} year{(years == 1 ? "" : "s")}";
                var gap = income - expense;

                return gap >= 0
                    ? $"Your numbers: {period} you expect {CurrencySymbol}{income:N0} to come in "
                      + $"and {CurrencySymbol}{expense:N0} to go out. That leaves "
                      + $"{CurrencySymbol}{gap:N0}, and that is the money the loan has to come out "
                      + "of."
                    : $"Your numbers: {period} you expect {CurrencySymbol}{income:N0} to come in "
                      + $"but {CurrencySymbol}{expense:N0} to go out — that is "
                      + $"{CurrencySymbol}{Math.Abs(gap):N0} more going out than coming in. On "
                      + "these growth figures you would run short before the end, so it is worth "
                      + "changing something now.";
            }
        }

        [JsonIgnore] private bool _isProjectionRefreshing;
        /// <summary>
        /// True while a debounced projection rebuild is pending or running, so the Projection
        /// tab can show that the chart/grids are catching up with the latest growth-rate taps.
        /// </summary>
        [JsonIgnore]
        public bool IsProjectionRefreshing
        {
            get => _isProjectionRefreshing;
            set
            {
                if (_isProjectionRefreshing == value) return;
                _isProjectionRefreshing = value;
                OnPropertyChanged(nameof(IsProjectionRefreshing));
            }
        }

        [JsonIgnore] private ObservableCollection<ChartDataModel> _projectionIncomeAxis = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _projectionExpenseAxis = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _projectionNetAxis = new();

        [JsonIgnore] public ObservableCollection<ChartDataModel> ProjectionIncomeAxis => _projectionIncomeAxis;
        [JsonIgnore] public ObservableCollection<ChartDataModel> ProjectionExpenseAxis => _projectionExpenseAxis;
        [JsonIgnore] public ObservableCollection<ChartDataModel> ProjectionNetAxis => _projectionNetAxis;

        [JsonIgnore] private List<BudgetProjectionRow> _projectionRows = new();
        [JsonIgnore] public List<BudgetProjectionRow> ProjectionRows => _projectionRows;

        [JsonIgnore] public bool HasData =>
            TotalIncomeMonthly > 0 || TotalExpenseMonthly > 0;

        [JsonIgnore] public bool HasNoData => !HasData;

        // ── Recalculate ───────────────────────────────────────────────────

        public void RecalculateSummary()
        {
            Income.TransactionRecords?.SumUpData();
            Expense.TransactionRecords?.SumUpData();

            // Ensure LoanViewModel reads from the same live instances so
            // Affordability reflects any income/expense changes made on this page.
            if (_loanVm != null)
            {
                _loanVm.IncomeSummary = Income;
                _loanVm.ExpenseSummary = Expense;
            }

            CurrencySymbol = _loanVm?.CurrencySymbol
                ?? Income.CurrencySymbol
                ?? Helper.CurrencySymbol;

            OnPropertyChanged(nameof(TotalIncomeMonthly));
            OnPropertyChanged(nameof(TotalExpenseMonthly));
            OnPropertyChanged(nameof(NetMonthly));
            OnPropertyChanged(nameof(NetIsPositive));
            OnPropertyChanged(nameof(TotalIncomeMonthlyFormatted));
            OnPropertyChanged(nameof(TotalExpenseMonthlyFormatted));
            OnPropertyChanged(nameof(NetMonthlyFormatted));
            OnPropertyChanged(nameof(TotalIncomeYearly));
            OnPropertyChanged(nameof(TotalExpenseYearly));
            OnPropertyChanged(nameof(NetYearly));
            OnPropertyChanged(nameof(NetYearlyFormatted));
            OnPropertyChanged(nameof(TopExpenses));
            OnPropertyChanged(nameof(IncomeSourceCount));
            OnPropertyChanged(nameof(ExpenseRecordCount));
            OnPropertyChanged(nameof(IncomeSourceCountText));
            OnPropertyChanged(nameof(ExpenseRecordCountText));
            OnPropertyChanged(nameof(AssetRunningCostMonthly));
            OnPropertyChanged(nameof(AssetRunningCostMonthlyFormatted));

            // The whole-budget figures the Summary tab shows. They derive from the tab-scoped
            // totals above plus the asset running costs, so they have to be raised alongside
            // them — otherwise adding an asset cost updates its own box and leaves "out monthly"
            // and NET EVERY MONTH stale.
            OnPropertyChanged(nameof(TotalOutgoingsMonthly));
            OnPropertyChanged(nameof(TotalOutgoingsMonthlyFormatted));
            OnPropertyChanged(nameof(NetAfterOutgoingsMonthly));
            OnPropertyChanged(nameof(NetAfterOutgoingsIsPositive));
            OnPropertyChanged(nameof(NetAfterOutgoingsMonthlyFormatted));
            OnPropertyChanged(nameof(AssetRunningCostYearly));
            OnPropertyChanged(nameof(TotalOutgoingsYearly));
            OnPropertyChanged(nameof(NetAfterOutgoingsYearly));
            OnPropertyChanged(nameof(NetAfterOutgoingsYearlyFormatted));

            // The ⓘ "with your numbers" sentences quote these figures, so they go stale with them.
            OnPropertyChanged(nameof(IncomeAfterExpensesLiveExample));
            OnPropertyChanged(nameof(IncomeAfterLoanLiveExample));
            OnPropertyChanged(nameof(ExpenseAfterLoanLiveExample));
            NotifyExpensesTabNet();
            OnPropertyChanged(nameof(TotalIncomeMonthlySigned));
            OnPropertyChanged(nameof(TotalExpenseMonthlySigned));
            OnPropertyChanged(nameof(IsSpareAfterLoanAvailable));
            OnPropertyChanged(nameof(IsSpareAfterLoanNegative));
            OnPropertyChanged(nameof(SpareAfterLoan));
            OnPropertyChanged(nameof(SpareAfterLoanFormatted));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(AffordabilityTextDescription));
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(HasNoData));

            // The ⓘ detail quotes all four of the figures above, so it has to be raised with them
            // or it explains numbers that are no longer on screen.
            OnPropertyChanged(nameof(SummaryExplanationDetail));

            // Update donut chart slices
            _summaryDonutData.Clear();
            _summaryDonutData.Add(new ChartDataModel("Monthly Income", TotalIncomeMonthly));
            _summaryDonutData.Add(new ChartDataModel("Monthly Expenses", TotalExpenseMonthly));
            OnPropertyChanged(nameof(SummaryDonutData));
        }

        public void RecalculateProjection()
        {
            var incomeTerms = Income.IncomeProjectList ?? new List<Income.Summary.IncomeExpenseProjectionOutput>();
            var expenseTerms = Expense.IncomeProjectList ?? new List<Income.Summary.IncomeExpenseProjectionOutput>();

            _projectionIncomeAxis.Clear();
            _projectionExpenseAxis.Clear();
            _projectionNetAxis.Clear();
            _projectionRows.Clear();

            var count = Math.Min(incomeTerms.Count, expenseTerms.Count);
            for (int i = 0; i < count; i++)
            {
                var inc = incomeTerms[i];
                var exp = expenseTerms[i];
                var net = inc.IncomeExpenseAmount - exp.IncomeExpenseAmount;
                var yearLabel = inc.YearOfPayment.ToString();

                _projectionIncomeAxis.Add(new ChartDataModel(yearLabel, inc.IncomeExpenseAmount));
                _projectionExpenseAxis.Add(new ChartDataModel(yearLabel, exp.IncomeExpenseAmount));
                _projectionNetAxis.Add(new ChartDataModel(yearLabel, net));

                _projectionRows.Add(new BudgetProjectionRow
                {
                    Period = inc.PaymentPeriod ?? yearLabel,
                    Income = $"{CurrencySymbol}{inc.IncomeExpenseAmount:N0}",
                    Expense = $"{CurrencySymbol}{exp.IncomeExpenseAmount:N0}",
                    Net = $"{(net >= 0 ? "+" : "-")}{CurrencySymbol}{Math.Abs(net):N0}",
                    NetIsPositive = net >= 0
                });
            }

            OnPropertyChanged(nameof(ProjectionIncomeAxis));
            OnPropertyChanged(nameof(ProjectionExpenseAxis));
            OnPropertyChanged(nameof(ProjectionNetAxis));
            OnPropertyChanged(nameof(ProjectionRows));

            // The explanation card quotes these same totals, so it has to be raised alongside them
            // or it describes figures no longer on screen — exactly the fault the Answer panels'
            // explanation had before its properties were notified here.
            OnPropertyChanged(nameof(ProjectionPersonalLine));

            // The collapsed settings header names every setting in force. Every path that changes
            // one of those settings ends up here, so this is the one place it needs raising.
            OnPropertyChanged(nameof(ProjectionSettingsSummary));
        }
    }
}
