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
            set { _expense = value; OnPropertyChanged(nameof(Expense)); }
        }

        // Kept for LoanView Wizard compatibility — also assigns Income/Expense
        [JsonIgnore] private LoanViewModel? _loanVm;

        public void SetPeerViewModels(IncomeViewModel income, ExpenseViewModel expense, LoanViewModel loan)
        {
            Income = income;
            Expense = expense;
            _loanVm = loan;
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

        [JsonIgnore] private int _projectionYears = 10;
        [JsonIgnore]
        public int ProjectionYears
        {
            get => _projectionYears;
            set
            {
                _projectionYears = value;
                OnPropertyChanged(nameof(ProjectionYears));
                Income.TotalYearsToProject = value;
                Expense.TotalYearsToProject = value;
                RecalculateProjection();
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
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(AffordabilityTextDescription));
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(HasNoData));

            // Update donut chart slices
            _summaryDonutData.Clear();
            _summaryDonutData.Add(new ChartDataModel("Income", TotalIncomeMonthly));
            _summaryDonutData.Add(new ChartDataModel("Expenses", TotalExpenseMonthly));
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
        }
    }
}
