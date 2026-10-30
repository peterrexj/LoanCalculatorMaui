using LoanCalculator.Core.Constants;
﻿using LoanCalculator.Core.Exts;
using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.Charts;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Pdf;
using LoanCalculator.Core.Pdf;
using LoanCalculator.Core.Services;
using Syncfusion.Maui.Buttons;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace LoanCalculator.Core.Models.ViewModels.PrimaryModels
{
    public class LoanViewModel : ExpenseEntryViewBaseModel
    {
        [JsonIgnore] private readonly IErrorHandlingService _errorHandlingService;
        [JsonIgnore] private readonly IAlertService _alertService;

        public LoanViewModel()
        {
        }

        public LoanViewModel(IErrorHandlingService errorHandlingService, IAlertService alertService)
        {
            _errorHandlingService = errorHandlingService;
            _alertService = alertService;

            ExportInsightsReportCommand = new Command(async () =>
            {
                try
                {
                    await ExportInsights();
                }
                catch (Exception e)
                {
                    _errorHandlingService.HandleException(e);
                }
            });
        }

        public void CopyPropertiesFrom(LoanViewModel source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Assign the backing field directly — property setters guard on HasInitialized
            // which is false during load, so they would silently drop every value.
            _homeLoanInfo = source._homeLoanInfo;
            TransactionRecords = source.TransactionRecords;

            SanitiseLoadedLoan();
        }

        /// <summary>
        /// Brings a loan loaded from disk inside the app's hard limits.
        /// </summary>
        /// <remarks>
        /// <para>Needed precisely BECAUSE the assignment above bypasses the property setters: every
        /// bound the app enforces lives in a setter or a click handler, so a term or rate in
        /// <c>homeloandata.json</c> was previously restored verbatim and never checked. A file
        /// written by an older build with different limits, hand-edited, or carried over from a
        /// device backup could hold anything, and the +/- controls would then only stop the user
        /// making it worse.</para>
        ///
        /// <para>Written against the model rather than the properties for the same reason the copy
        /// above is: the setters would drop the writes while <c>HasInitialized</c> is false.</para>
        ///
        /// <para>Silent, deliberately. This runs during startup load, and a value this far out of
        /// range is a corrupt-file symptom rather than something the user chose — telling them
        /// their loan term has been adjusted, at a point where they have not asked for anything,
        /// explains nothing useful. It is the same self-heal stance CLAUDE.md records for a
        /// corrupted <c>namevaluedata.json</c>.</para>
        /// </remarks>
        private void SanitiseLoadedLoan()
        {
            var request = _homeLoanInfo?.HomeLoanRepaymentRequest;
            if (request == null) return;

            request.LoanTermInYears = Math.Clamp(
                request.LoanTermInYears,
                SharedServiceCore.MinLoanTermYears,
                SharedServiceCore.GetMaxLoanTermYears());

            // The adjacent field with the identical hole. An unbounded rate is as wrong as an
            // unbounded term and the guard is the same shape, so fixing one and not the other
            // would leave this method lying about what it does.
            request.InterestRate = Math.Clamp(
                request.InterestRate,
                SharedServiceCore.MinInterestRate,
                SharedServiceCore.MaxInterestRate);
        }

        protected override void OnCurrencyChanged()
        {
            // Re-notify all currency-formatted display properties
            OnPropertyChanged(nameof(LoanAmount));
            OnPropertyChanged(nameof(LoanAmountStrFormatted));
            OnPropertyChanged(nameof(DepositAmountStrFormatted));
            OnPropertyChanged(nameof(PropertyTotalAmount));
            OnPropertyChanged(nameof(DepositAmountStrFormatted));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityNegative));
            OnPropertyChanged(nameof(HomeLoanInfo));
        }

        // Controls the Upfront Costs popup on the Asset tab
        [JsonIgnore]
        private bool _isUpfrontInputVisible;
        [JsonIgnore]
        public bool IsUpfrontInputVisible
        {
            get => _isUpfrontInputVisible;
            set
            {
                _isUpfrontInputVisible = value;
                OnPropertyChanged(nameof(IsUpfrontInputVisible));
            }
        }

        [JsonIgnore] protected HomeLoanInformation _homeLoanInfo;
        public HomeLoanInformation HomeLoanInfo
        {
            get => _homeLoanInfo;
            set
            {
                _homeLoanInfo = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore] private ObservableCollection<DataModel> _chartPropertyValueWithInterestPayment;

        [JsonIgnore]
        public ObservableCollection<DataModel> ChartPropertyValueWithInterestPayment
        {
            get => _chartPropertyValueWithInterestPayment;
            set
            {
                _chartPropertyValueWithInterestPayment = value;
                OnPropertyChanged(nameof(ChartPropertyValueWithInterestPayment));
            }
        }

        [JsonIgnore] public ICommand ExportInsightsReportCommand { get; }
        private PdfInsightsGenerator _pdfGenerator;

        [JsonIgnore]
        public PdfInsightsGenerator PdfGenerator
        {
            get => _pdfGenerator;
            set
            {
                _pdfGenerator = value;
                OnPropertyChanged(nameof(PdfGenerator));
            }
        }
        [JsonIgnore]
        private bool _isGeneratingPdf;

        [JsonIgnore]
        public bool IsGeneratingPdf
        {
            get => _isGeneratingPdf;
            set
            {
                if (_isGeneratingPdf == value) return; // Avoid unnecessary updates
                _isGeneratingPdf = value;
                OnPropertyChanged(nameof(IsGeneratingPdf)); // Notify UI of the change
            }
        }

        // Cached, not computed: refreshed by RefreshIncomeExpenseSummariesAsync when a page that
        // reads affordability appears and SharedServiceCore reports income/expense as dirty.
        private bool _hasIncomeExpensesRecorded;
        [JsonIgnore]
        public bool HasIncomeExpensesRecorded
        {
            get => _hasIncomeExpensesRecorded;
            set
            {
                _hasIncomeExpensesRecorded = value;
                OnPropertyChanged(nameof(HasIncomeExpensesRecorded));
            }
        }

        /// <summary>
        /// Re-points <see cref="IncomeSummary"/>/<see cref="ExpenseSummary"/> at the live singleton
        /// view models — or, when a tab owns its own instance and so has never initialised these,
        /// at a fresh snapshot from disk — and recomputes <see cref="HasIncomeExpensesRecorded"/>.
        /// </summary>
        /// <remarks>
        /// <para><see cref="IsAffordabilityAvailable"/> gates on that cached flag and
        /// <see cref="MonthlySurplus"/> gates on it in turn, so <em>every</em> page that reads
        /// affordability has to refresh first — not just the Loan page. What If reads both for its
        /// stress test, and going Budget → What If without this left it computing from a stale
        /// cache: the unlock prompt after income/expenses were added, stale results after they were
        /// deleted.</para>
        /// <para>Deliberately does not clear SharedServiceCore's dirty flags. The caller owns that,
        /// so refreshing on one tab cannot rob another of the notification pass it still needs.</para>
        /// <para>Source order matters and is a correctness issue, not a preference. The Budget tab
        /// owns its <em>own</em> Income/Expense instances (they are not the DI singletons), so those
        /// are the authoritative copy of what the user just typed. Reading them avoids a disk
        /// round-trip — and since <see cref="SharedServiceCore.SaveData"/> is fire-and-forget and
        /// cannot be awaited by anyone, reading back from disk is a race by construction. Disk is
        /// the last resort, used only before any tab has loaded.</para>
        /// </remarks>
        public async Task RefreshIncomeExpenseSummariesAsync(
            IncomeViewModel liveIncome, ExpenseViewModel liveExpense)
        {
            ExpenseSummary = ResolveAuthoritativeExpense(liveExpense)
                ?? await SharedServiceCore.GetExpenseSummaryAsync();

            IncomeSummary = ResolveAuthoritativeIncome(liveIncome)
                ?? await SharedServiceCore.GetIncomeSummaryAsync();

            HasIncomeExpensesRecorded =
                ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0 &&
                IncomeSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalYearly > 0;
        }

        /// <summary>
        /// The one instance of income that everything must agree on — readers and writers alike.
        /// Returns <c>null</c> only when nothing has loaded yet, which is the sole case where a
        /// caller may fall back to disk.
        /// </summary>
        /// <remarks>
        /// A <em>writer</em> must never fall back to disk, and must never just write the DI
        /// singleton. The Budget tab owns its own Income/Expense instances, which SplashPage
        /// pre-warms from disk on every launch — so they report <c>HasInitialized</c> while the DI
        /// singletons do not. Anything that writes the singleton is therefore invisible to
        /// <see cref="RefreshIncomeExpenseSummariesAsync"/>, and the next page appearance silently
        /// overwrites that write with Budget's copy. That is exactly how the Quick Setup wizard's
        /// income/expense commit used to vanish, flipping
        /// <see cref="HasIncomeExpensesRecorded"/> back to <c>false</c> and hiding the affordability
        /// box the user had just unlocked.
        /// </remarks>
        internal IncomeViewModel? ResolveAuthoritativeIncome(IncomeViewModel? liveIncome)
        {
            var budget = TryGetBudgetViewModel();
            if (budget?.Income?.HasInitialized == true) return budget.Income;
            if (liveIncome?.HasInitialized == true) return liveIncome;
            return null;
        }

        /// <inheritdoc cref="ResolveAuthoritativeIncome"/>
        internal ExpenseViewModel? ResolveAuthoritativeExpense(ExpenseViewModel? liveExpense)
        {
            var budget = TryGetBudgetViewModel();
            if (budget?.Expense?.HasInitialized == true) return budget.Expense;
            if (liveExpense?.HasInitialized == true) return liveExpense;
            return null;
        }

        /// <summary>
        /// ServiceLocator dereferences a settable provider, so it throws if resolved before
        /// App.xaml.cs sets it. Callers here all run from page OnAppearing, which is well after
        /// that — but a null is a better failure than taking down a page load.
        /// </summary>
        private static BudgetViewModel? TryGetBudgetViewModel()
        {
            try
            {
                return ServiceLocator.ServiceProvider == null
                    ? null
                    : ServiceLocator.GetService<BudgetViewModel>();
            }
            catch
            {
                return null;
            }
        }


        private async Task ExportInsights()
        {
            try
            {
                if (IsGeneratingPdf) return; // Prevent multiple exports at the same time

                // The PDF is premium. Checked HERE rather than by hiding the button, because the
                // button was the only thing standing between a free user and a full report — and it
                // was not standing there on purpose: the export had no check anywhere, and was
                // merely sitting on a tab that happened to be gated. A command is reachable from
                // more than one place, so the command is what gets guarded.
                //
                // Before IsGeneratingPdf and the 500ms delay below, or the loader appears for half
                // a second and is then replaced by a paywall, which reads as a failed export.
                if (SharedServiceCore.IsTrialUser)
                {
                    ServiceLocator.GetService<InAppPurchaseViewModel>().ShowPremiumBuyWindow = true;
                    return;
                }

                IsGeneratingPdf = true;
                IsBusy = true;

                await Task.Delay(500); // Simulate a delay for the loader

                if (HasIncomeExpensesRecorded == false)
                {
                    await _alertService.ShowAlertAsync("Warning", "Please enter the income and expense details.", "OK");
                    return;
                }
                else
                {
                    try
                    {
                        IsBusy = true; // Show loader
                        IsActive = false;
                        await Task.Delay(500); // Simulate a delay for the loader

                        // Built here, lazily, on first export — not eagerly on every page load.
                        // PdfGeneratorBase's constructor loads three font files synchronously via
                        // FileSystem.OpenAppPackageFileAsync(...).GetAwaiter().GetResult() (see
                        // MauiFontUnicodeProvider.LoadFont), and that was previously run as soon as
                        // LoanView.OnAppearing resumed on the main thread. On a cold iOS Simulator
                        // Debug build that was slow enough to trip Sentry's AppHang detector, and on
                        // at least one run threw outright (an exception wrapped at the native
                        // marshal boundary, with no inner detail recoverable from the crash capture)
                        // — on EVERY launch, for a feature most sessions never touch. Deferring the
                        // construction to here means that cost, whatever it is, is paid only when
                        // the user actually asks to export.
                        PdfGenerator ??= new PdfInsightsGenerator(ServiceLocator.GetService<IFontUnicodeProvider>());

                        await PdfGenerator.GeneratePdf(SharedServiceCore.AppInformation.ApplicationTitle, taskDelay: 400);
                    }
                    catch (Exception ex)
                    {
                        _errorHandlingService.HandleException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors for the entire method
                _errorHandlingService.HandleException(ex, "An unexpected error occurred while exporting insights.");
            }
            finally
            {
                IsGeneratingPdf = false; // Ensure this is reset even if an error occurs
                IsBusy = false; // Hide loader
                IsActive = true;
            }
        }

        public void InitializeViewData()
        {
            if (RepaymentFrequencyCollection == null || RepaymentFrequencyCollection.Count == 0)
            {
                RepaymentFrequencyCollection =
                [
                    new SfSegmentItem { Text = "Monthly" },
                    new SfSegmentItem { Text = "Fortnightly" },
                    new SfSegmentItem { Text = "Weekly" }
                ];
            }


            if (AustraliaStateCollection == null || AustraliaStateCollection.Count == 0)
            {
                AustraliaStateCollection =
                    new ObservableCollection<SfSegmentItem>(
                        StampDutyOutput.AustralianStates.Select(f => new SfSegmentItem { Text = f.ToString() }));
            }

            HomeLoanInfo ??= new HomeLoanInformation
            {
                HomeLoanRepaymentRequest = new HomeLoanRepaymentInput()
            };

            // A fresh HomeLoanRepaymentInput defaults TotalNumberPaymentPerYear to 0, which makes
            // the payment calculation return 0 ("$0 monthly"). Default to Monthly (12) so any path
            // that builds a loan without touching the frequency segment still computes a payment.
            if (HomeLoanInfo.HomeLoanRepaymentRequest != null &&
                HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear == 0)
            {
                HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
            }

            IncomeFrequencyCollection ??=
                new ObservableCollection<string>(IncomeExpenseHelper.TimeFrequencies.Select(f => f.ToString()));

            IncomeExpenseEntry ??= new IncomeExpense();
            TransactionRecords ??= new Incomes
            {
                IncomeExpenseEntries = []
            };

            IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
            IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
        }

        public void AddDefaultValues()
        {
            PropertyAmount = 1000000;
            InterestRate = SharedServiceCore.GetDefaultInterestRate();
            LoanTermInYears = SharedServiceCore.GetDefaultLoanTermYears();
            DepositPercentage = 10;
            //AustraliaStateSelectedIndex = null;
        }

        /// <summary>
        /// Returns the live loan to exactly the state a first launch produces. Called when the user
        /// deletes their loan data in Settings.
        /// </summary>
        /// <remarks>
        /// <para><b>Why this exists.</b> Deleting loan data used to clear only
        /// <c>homeloandata.json</c>, on the reasoning that blanking the form mid-session would wipe
        /// inputs the user could still see. The effect was worse than that cure: Settings reported
        /// the data deleted while the Loan, What If and Budget tabs carried on showing and
        /// calculating from it, and a restart then produced different figures again — the same app
        /// giving two answers either side of a relaunch.</para>
        ///
        /// <para><b>Why it discards the objects instead of zeroing fields.</b> Nulling the model and
        /// re-running <see cref="InitializeViewData"/> is the actual first-launch path, so every
        /// field lands at its fresh value — including ones no screen exposes. A hand-written list of
        /// fields to zero would silently miss whatever the model gains next; the four upfront-cost
        /// groups summed by <c>OtherExpenseTotalAmount</c> already hold more fields than the six the
        /// Upfront Costs page shows.</para>
        ///
        /// <para><b>Order is load-bearing.</b> The new model carries no split point, which is what
        /// lets <see cref="AddDefaultValues"/>'s <c>DepositPercentage = 10</c> take effect:
        /// <c>HomeLoanInformation.LoanAmount</c> consults <c>LoanAmountDirectInput</c> and
        /// <c>DepositAmountDirectInput</c> <i>before</i> any percentage, so resetting the percentage
        /// while a stale direct input survived would be silently ignored.</para>
        ///
        /// <para>Income and expenses are deliberately untouched — they are the Budget tab's data and
        /// have their own delete actions, so <c>HasIncomeExpensesRecorded</c> must keep its value.</para>
        /// </remarks>
        public void ResetToDefaults()
        {
            // null! then InitializeViewData(): its `??=` only rebuilds what is missing, and it is
            // the one place that also wires HomeLoanRepaymentRequest and the 12-payments-a-year
            // fallback. Constructing them here instead would duplicate that and drift from it.
            HomeLoanInfo = null!;
            TransactionRecords = null;

            InitializeViewData();
            AddDefaultToExpenses();
            AddDefaultValues();

            AustraliaStateSelectedIndex = null;
            RepaymentFrequencySelectedIndex = 0;

            TransactionRecords?.SumUpData();

            EventsTriggerPriceUpdate();
            TriggerPropertyChangedOnPropertyTab();
        }

        public void AddDefaultToExpenses()
        {
            TransactionRecords ??= new Incomes
            {
                IncomeExpenseEntries = []
            };
            TransactionRecords?.Add("Maintenance cost", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            TransactionRecords?.Add("Water bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            TransactionRecords?.Add("Electricity bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            TransactionRecords?.Add("Gas bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
            TransactionRecords?.Add("Council bills", 0, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: true);
        }

        #region Loan Primary Values

        public double PropertyAmount
        {
            get => HomeLoanInfo?.PropertyAmount ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.PropertyAmount == value) return;

                isUpdating = true;
                HomeLoanInfo.PropertyAmount = value;
                EventsTriggerStampDutyUpdate();
                HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above
                OnPropertyChanged(nameof(PropertyAmountWords));
                OnPropertyChanged(nameof(PropertyAmountFormatted));
                OnPropertyChanged(nameof(LoanAmountWords));

                TriggerPropertyChangedOnPropertyTab();
                isUpdating = false;
            }
        }

        /// <summary>
        /// The loan term. Clamped to the configured maximum on the way in.
        /// </summary>
        /// <remarks>
        /// The clamp is here, not only in the +/- handlers that call this. Those bound what a user
        /// can step up to; they do nothing for a term arriving from a restored file, a settings
        /// change that lowered the maximum underneath an existing loan, or any future caller. See
        /// <see cref="SharedServiceCore.AbsoluteMaxLoanTermYears"/>.
        ///
        /// Note the clamp is applied BEFORE the unchanged-value check, so a caller passing an
        /// out-of-range value that clamps to the current term is correctly treated as a no-op
        /// rather than writing and re-notifying.
        /// </remarks>
        public int LoanTermInYears
        {
            get => HomeLoanInfo?.HomeLoanRepaymentRequest?.LoanTermInYears ?? SharedServiceCore.GetDefaultLoanTermYears();
            set
            {
                if (isUpdating || !HasInitialized) return;

                var clamped = Math.Clamp(
                    value, SharedServiceCore.MinLoanTermYears, SharedServiceCore.GetMaxLoanTermYears());

                if (HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears == clamped) return;

                isUpdating = true;

                HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears = clamped;

                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        public double InterestRate
        {
            get => HomeLoanInfo?.HomeLoanRepaymentRequest?.InterestRate ?? SharedServiceCore.GetDefaultInterestRate();
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate == value) return;

                isUpdating = true;

                HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate = value;

                TriggerPropertyChangedOnPropertyTab();
                EventsTriggerPriceUpdate();

                isUpdating = false;
            }
        }

        public string LoanAmount => $"{CurrencySymbol}{HomeLoanInfo?.LoanAmount ?? 0:N0}";

        [JsonIgnore] public string PropertyTotalAmount => $"{CurrencySymbol}{HomeLoanInfo?.PropertyTotalAmount ?? 0:N0}";

        /// <summary>
        /// True only once there is an upfront cost to explain.
        /// </summary>
        /// <remarks>
        /// Without the gate this would read "$650,000 asset + $0 upfront costs" as soon as any
        /// asset value exists — true, but noise, because the total and the asset value are then the
        /// same number and there is nothing to account for.
        /// </remarks>
        [JsonIgnore]
        public bool ShowAssetTotalBreakdown => (HomeLoanInfo?.OtherExpenseTotalAmount ?? 0) > 0;

        /// <summary>
        /// Accounts for the gap between "Asset Value" and "Total asset amount" on the Asset tab.
        /// </summary>
        /// <remarks>
        /// The two boxes sit side by side showing different figures with nothing to explain the
        /// difference — an asset of 125,789 next to a total of 129,689 and no sign of where the
        /// 3,900 came from. It is the upfront costs: <c>PropertyTotalAmount</c> is
        /// <c>PropertyAmount + OtherExpenseTotalAmount</c>, and those costs are folded into the
        /// loan rather than paid separately (see "Cost increases grow the loan" in CLAUDE.md), so
        /// they also move the deposit/loan split below.
        ///
        /// Same sentence shape as <c>WizardViewModel.WizardAssetBreakdownLabel</c>, which already
        /// solved this for the data-entry page — the two screens should not explain the same
        /// arithmetic in two different ways.
        /// </remarks>
        [JsonIgnore]
        public string AssetTotalBreakdown =>
            $"{CurrencySymbol}{HomeLoanInfo?.PropertyAmount ?? 0:N0} asset + {CurrencySymbol}{HomeLoanInfo?.OtherExpenseTotalAmount ?? 0:N0} upfront costs";

        [JsonIgnore]
        private bool _isDepositPercentageSliderEnabled;
        public bool IsDepositPercentageSliderEnabled
        {
            get => _isDepositPercentageSliderEnabled;
            set
            {
                _isDepositPercentageSliderEnabled = value;
                OnPropertyChanged(nameof(IsDepositPercentageSliderEnabled));
            }
        }

        #endregion

        #region Loan Calculations

        [JsonIgnore]
        public string DepositAmountStrFormatted => $"{CurrencySymbol}{HomeLoanInfo?.DepositAmountDirectInput ?? 0:N0}";

        [JsonIgnore]
        public string InterestRateFormatted =>
            InterestRate % 1 == 0
                ? $"{InterestRate:N0}%"
                : $"{InterestRate:N2}%";

        public double DepositPercentage
        {
            get => HomeLoanInfo?.DepositPercentage ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.DepositPercentage == value) return;

                isUpdating = true;

                HomeLoanInfo.DepositPercentage = value;
                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        public double DepositAmountDirectInput
        {
            get => HomeLoanInfo?.DepositAmountDirectInput ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;
                    HomeLoanInfo.DepositAmountDirectInput = value;
                    TriggerPropertyChangedOnPropertyTab();
                    isUpdating = false;
                }
            }
        }

        [JsonIgnore] public string LoanAmountStrFormatted => $"{CurrencySymbol}{HomeLoanInfo?.LoanAmountDirectInput ?? 0:N0}";

        // Flat wrappers over the nested PaymentSummary path. EventsTriggerPriceUpdate replaces
        // HomeLoanInfo.PaymentSummary with a NEW object but HomeLoanInformation does not raise
        // PropertyChanged, so XAML bindings to the deep path (HomeLoanInfo.PaymentSummary.Payment.X)
        // never refresh. Bind the Asset-tab repayment box and chart center to these instead and
        // notify them from TriggerPropertyChangedOnPropertyTab.
        [JsonIgnore] public string TermPaymentRoundedWithComma =>
            HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentRoundedWithComma ?? "0";

        [JsonIgnore] public string TotalPaymentRoundedWithComma =>
            HomeLoanInfo?.PaymentSummary?.Payment?.TotalPaymentRoundedWithComma ?? "0";

        [JsonIgnore] public string ChartInterestCategoryLabel =>
            $"Interest over {LoanTermInYears} yr{(LoanTermInYears == 1 ? "" : "s")}";

        [JsonIgnore] public string ChartTotalCostSubtitle =>
            $"Total cost over {LoanTermInYears} yr{(LoanTermInYears == 1 ? "" : "s")}";

        [JsonIgnore] public string ChartInsightSubtitle =>
            $"Deposit · loan · lifetime interest over {LoanTermInYears} yr{(LoanTermInYears == 1 ? "" : "s")}";

        public double LoanAmountPercentage
        {
            get => HomeLoanInfo?.LoanAmountPercentage ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.LoanAmountPercentage == value) return;

                isUpdating = true;

                HomeLoanInfo.LoanAmountPercentage = value;
                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        public double LoanAmountDirectInput
        {
            get => HomeLoanInfo?.LoanAmountDirectInput ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.LoanAmountDirectInput == value) return;

                isUpdating = true;
                HomeLoanInfo.LoanAmountDirectInput = value;
                OnPropertyChanged(nameof(LoanAmountWords));
                TriggerPropertyChangedOnPropertyTab();
                isUpdating = false;
            }
        }

        // Live word representations for Quick Input popup
        [JsonIgnore]
        public string PropertyAmountWords => AmountToWords(HomeLoanInfo?.PropertyAmount ?? 0);
        [JsonIgnore]
        public string PropertyAmountFormatted => $"{CurrencySymbol}{HomeLoanInfo?.PropertyAmount ?? 0:N0}";
        [JsonIgnore]
        public string LoanAmountWords => AmountToWords(HomeLoanInfo?.LoanAmountDirectInput ?? 0);

        private static string NumberToWords(long n) => NumberToWordsPublic(n);

        /// <summary>
        /// Largest amount any money field accepts, and the largest
        /// <see cref="NumberToWordsPublic"/> renders without naming a scale twice.
        /// </summary>
        /// <remarks>
        /// Callers must clamp to this before converting, which is what <see cref="AmountToWords"/>
        /// does. Two independent reasons it cannot simply be left unbounded:
        ///
        /// · <c>double</c> holds integers exactly only up to 2^53 (about 9.0e15), so a longer entry
        ///   silently becomes a different number — 22 typed ones displayed as
        ///   111,111,111,111,111,114,752.
        /// · casting an out-of-range <c>double</c> to <c>long</c> is unchecked and
        ///   platform-dependent: ARM64 saturates to <c>long.MaxValue</c> (which rendered as a
        ///   sixteen-word string beginning "Nine Billion") while x64 yields <c>long.MinValue</c>,
        ///   which this method returns the empty string for. The same input produced different
        ///   wrong output per CPU.
        /// </remarks>
        public const double MaxEnterableAmount = 999_999_999_999;

        /// <summary>
        /// Word form of an amount, clamped to <see cref="MaxEnterableAmount"/> so the cast to
        /// <c>long</c> can never be out of range. Use this rather than casting at the call site.
        /// </summary>
        public static string AmountToWords(double value) =>
            value <= 0
                ? string.Empty
                : NumberToWordsPublic((long)Math.Round(Math.Min(value, MaxEnterableAmount)));

        /// <summary>
        /// Scale names, largest first. Driving the tiers from a table rather than a fixed chain of
        /// <c>if</c>s is what fixes values of a trillion and up: the old version's largest tier was
        /// Billion and it recursed into itself for <c>n / 1_000_000_000</c>, so any quotient that
        /// was itself a billion or more appended a SECOND "Billion" — 173.7 trillion came out as
        /// "One Hundred, Seventy Three Thousand, Seven Hundred, One Billion".
        /// </summary>
        /// <remarks>
        /// Quintillion is the top entry because <c>long.MaxValue</c> is 9.2 quintillion: with it,
        /// every quotient <c>n / scale</c> is under a thousand for any <c>long</c>, so no scale
        /// name can recurse into another one and the method is total over its input type. Stopping
        /// at Quadrillion still produced "Nine Thousand, Two Hundred, Twenty Three Quadrillion …
        /// Seventy Five Thousand" — two "Thousand"s, which is the same class of defect one tier up.
        /// Amounts anywhere near this are unreachable through the UI (see
        /// <see cref="MaxEnterableAmount"/>), but this method is public and should not have a hole.
        /// </remarks>
        private static readonly (long Scale, string Name)[] Scales =
        [
            (1_000_000_000_000_000_000, "Quintillion"),
            (1_000_000_000_000_000, "Quadrillion"),
            (1_000_000_000_000, "Trillion"),
            (1_000_000_000, "Billion"),
            (1_000_000, "Million"),
            (1_000, "Thousand"),
        ];

        public static string NumberToWordsPublic(long n)
        {
            if (n <= 0) return string.Empty;
            var parts = new System.Collections.Generic.List<string>();
            foreach (var (scale, name) in Scales)
            {
                if (n < scale) continue;
                parts.Add(NumberToWords(n / scale) + " " + name);
                n %= scale;
            }
            if (n >= 100)           { parts.Add(Ones[n / 100] + " Hundred"); n %= 100; }
            if (n >= 20)            { parts.Add(Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "")); }
            else if (n > 0)         { parts.Add(Ones[n]); }
            return string.Join(", ", parts);
        }
        private static readonly string[] Ones = ["", "One","Two","Three","Four","Five","Six","Seven","Eight","Nine","Ten","Eleven","Twelve","Thirteen","Fourteen","Fifteen","Sixteen","Seventeen","Eighteen","Nineteen"];
        private static readonly string[] Tens  = ["","","Twenty","Thirty","Forty","Fifty","Sixty","Seventy","Eighty","Ninety"];


        #endregion

        #region Australian States

        [JsonIgnore] public ObservableCollection<SfSegmentItem> AustraliaStateCollection { get; set; }

        public int? AustraliaStateSelectedIndex
        {
            get => HomeLoanInfo?.StampDuty?.AustraliaStateIndex;
            set
            {
                if (HomeLoanInfo == null || value == null)
                    return;

                var newState = StampDutyOutput.AustraliaStateFromIndex(value.Value);
                if (HomeLoanInfo.StampDuty.AustraliaStateSelected != newState)
                {
                    HomeLoanInfo.StampDuty.AustraliaStateSelected = newState;
                    EventsTriggerStampDutyUpdate();
                    LoanAmountDirectInput = HomeLoanInfo.LoanAmountDirectInput;
                }
            }
        }

        #endregion

        #region Country specific functionality

        private const string AustralianModeKey = "IsAustralianModeEnabled";

        // Loaded from Preferences on page appearance and updated when Settings changes
        [JsonIgnore]
        private bool _isAustralianModeEnabled = false;

        [JsonIgnore]
        public bool IsAustralianModeEnabled
        {
            get => _isAustralianModeEnabled;
            set
            {
                _isAustralianModeEnabled = value;
                OnPropertyChanged(nameof(IsAustralianModeEnabled));
                OnPropertyChanged(nameof(ShowAustralianStateSelectorOnStampDuty));
                OnPropertyChanged(nameof(HeightOfGridRowToggledByCountryOnStampDuty));
                // Australian mode always forces stamp duty on
                OnPropertyChanged(nameof(ShowStampDutyInput));
                OnPropertyChanged(nameof(IsStampDutyToggleEnabled));
            }
        }

        public void LoadAustralianModeSetting()
        {
            IsAustralianModeEnabled = Preferences.Get(AustralianModeKey, false);
            LoadStampDutySetting(); // re-evaluate stamp duty after Australian mode changes
        }

        // Visibility driven purely by the user's toggle
        [JsonIgnore]
        public bool ShowAustralianStateSelectorOnStampDuty => IsAustralianModeEnabled;

        // ── Stamp Duty toggle ──────────────────────────────────────────────────
        private const string StampDutyKey = "IsStampDutyEnabled";
        [JsonIgnore] private bool _isStampDutyEnabled;

        [JsonIgnore]
        public bool IsStampDutyEnabled
        {
            get => _isStampDutyEnabled;
            set
            {
                _isStampDutyEnabled = value;
                OnPropertyChanged(nameof(IsStampDutyEnabled));
                OnPropertyChanged(nameof(ShowStampDutyInput));
            }
        }

        public void LoadStampDutySetting()
        {
            // Australian mode always forces stamp duty on regardless of saved preference
            _isStampDutyEnabled = IsAustralianModeEnabled || Preferences.Get(StampDutyKey, false);
            OnPropertyChanged(nameof(IsStampDutyEnabled));
            OnPropertyChanged(nameof(ShowStampDutyInput));
            OnPropertyChanged(nameof(IsStampDutyToggleEnabled));
        }

        // True when stamp duty field should be visible in the popup
        [JsonIgnore]
        public bool ShowStampDutyInput => IsAustralianModeEnabled || IsStampDutyEnabled;

        // User cannot turn OFF stamp duty when Australian mode is active
        [JsonIgnore]
        public bool IsStampDutyToggleEnabled => !IsAustralianModeEnabled;

        [JsonIgnore]
        public int HeightOfGridRowToggledByCountryOnStampDuty
        {
            get
            {
                if (!ShowAustralianStateSelectorOnStampDuty)
                    return 0;

                if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                    return 60;
                else if (DeviceInfo.Idiom == DeviceIdiom.Tablet || DeviceInfo.Idiom == DeviceIdiom.Desktop || DeviceInfo.Idiom == DeviceIdiom.TV)
                    return 110;

                return 40; // Default value
            }
        }

        #endregion

        #region Insights

        // Set by the view's tab selection event to avoid firing expensive
        // notifications for tabs the user isn't currently looking at.
        [JsonIgnore] public bool IsAmortizationTabActive { get; set; }
        [JsonIgnore] public bool IsInsightsTabActive { get; set; }

        [JsonIgnore] private ObservableCollection<ChartDataModel> _insightChartLoanAmountAxis = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _insightChartLoanInterestAxis = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _insightChartLoanDepositAxis = new();

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanAmountAxis => _insightChartLoanAmountAxis;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanInterestAxis => _insightChartLoanInterestAxis;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> InsightChartLoanDepositAxis => _insightChartLoanDepositAxis;

        /// <summary>
        /// Builds the Insights charts regardless of <see cref="IsInsightsTabActive"/>.
        /// </summary>
        /// <remarks>
        /// The guarded path only runs when that flag is already true, which is no use on arrival:
        /// the flag is being set in the same breath, and the notification that would have
        /// triggered a rebuild has already been and gone. Mirrors
        /// <see cref="ForceUpdateAmortizationCharts"/>, which exists for the same reason.
        /// </remarks>
        public void ForceUpdateInsightCharts()
        {
            UpdateInsightCharts();
            OnPropertyChanged(nameof(InsightChartLoanAmountAxis));
            OnPropertyChanged(nameof(InsightChartLoanInterestAxis));
            OnPropertyChanged(nameof(InsightChartLoanDepositAxis));
        }

        private void UpdateInsightCharts()
        {
            var year = DateTime.Now.Year.ToString();
            var loanAmount = HomeLoanInfo?.LoanAmountDirectInput ?? 0;
            var interest = HomeLoanInfo?.PaymentSummary?.Payment?.TotalInterestPayment ?? 0;
            var deposit = HomeLoanInfo?.DepositAmountDirectInput ?? 0;

            ReplaceChartPoint(_insightChartLoanAmountAxis, year, loanAmount);
            ReplaceChartPoint(_insightChartLoanInterestAxis, year, interest);
            ReplaceChartPoint(_insightChartLoanDepositAxis, year, deposit);
        }

        private static void ReplaceChartPoint(ObservableCollection<ChartDataModel> col, string name, double value)
        {
            if (col.Count == 0)
                col.Add(new ChartDataModel(name, value));
            else
            {
                col.Clear();
                col.Add(new ChartDataModel(name, value));
            }
        }

        [JsonIgnore]
        public string AffordabilityCurrencySymbol
        {
            get
            {
                if (!IsAffordabilityAvailable) return string.Empty;

                // The amount is rendered in a separate span (see Affordability), so when the
                // affordability is negative the minus must live with the symbol here to read
                // "-$6,328" rather than "$-6,328".
                var amount = AffordabilityRawValue;
                return amount < 0 ? $"-{CurrencySymbol}" : CurrencySymbol;
            }
        }

        // Exposed for WhatIfViewModel — monthly surplus after all income/expenses/loan repayment.
        // Returns 0 when income/expense data is not available.
        [JsonIgnore]
        public double MonthlySurplus => IsAffordabilityAvailable ? AffordabilityRawValue : 0;

        // Raw affordability value (monthly, after all expenses incl. property/loan) used to
        // decide sign placement and to render the absolute amount.
        [JsonIgnore]
        private double AffordabilityRawValue
        {
            get
            {
                if (!IsAffordabilityAvailable) return 0;
                // No defensive resets here any more: SumUpData no longer takes a deduction, so
                // nothing can leave these records holding a net figure (TECH-DEBT D1).
                var pdf = new PdfDataInsightsModel(this, IncomeSummary, ExpenseSummary);
                pdf.InitializeLocalDataSet();
                return pdf?.Income?.TotalAfterExpenseIncludingPropertyMonthly ?? 0;
            }
        }
        [JsonIgnore]
        public bool IsAffordabilityAvailable
        {
            get
            {
                if (SharedServiceCore.IsTrialUser) return false;
                if (HasIncomeExpensesRecorded == false) return false;

                return true;
            }
        }
        [JsonIgnore]
        public string Affordability
        {
            get
            {
                if (IsAffordabilityAvailable == false) return "Affordability";

                // Absolute value — the sign (and symbol) are rendered by AffordabilityCurrencySymbol.
                return $"{Math.Abs(AffordabilityRawValue):N0}";
            }
        }

        [JsonIgnore]
        public bool IsAffordabilityNegative => IsAffordabilityAvailable && AffordabilityRawValue < 0;

        [JsonIgnore]
        public string AffordabilityTextDescription
        {
            get
            {
                if (SharedServiceCore.IsTrialUser) return " try premium";
                if (HasIncomeExpensesRecorded == false) return " record your income & expenses";

                return " estimated monthly position (estimate only)";
            }
        }

        // ── What If ▸ Answer hero ──────────────────────────────────────────────
        //
        // The prototype's hero states the verdict in words before the number
        // ("AFFORDABLE — with room to spare"), rather than labelling the number
        // "AFFORDABILITY" and leaving the reader to judge the sign themselves.

        /// <summary>Verdict in words. Falls back to the neutral label when nothing is computable.</summary>
        [JsonIgnore]
        public string AnswerVerdictCaption
        {
            get
            {
                if (!IsAffordabilityAvailable) return "AFFORDABILITY";
                if (IsAffordabilityNegative) return "NOT AFFORDABLE — you are short each month";

                // "Room to spare" is claimed only above a month of repayments in hand; below that
                // the figure is real but calling it comfortable would be editorialising.
                var repayment = HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0;
                return MonthlySurplus >= repayment
                    ? "AFFORDABLE — with room to spare"
                    : "AFFORDABLE — but it is tight";
            }
        }

        /// <summary>
        /// The "monthly spare" tail on the hero figure. Empty when the figure is not a figure —
        /// <see cref="Affordability"/> returns the literal word "Affordability" in that state, and
        /// "Affordability monthly spare" is what appending unconditionally produces.
        /// </summary>
        /// <remarks>
        /// Spelled out rather than "/mo". It sits in a FormattedString Span at half the size of
        /// the figure, so the extra characters wrap inside the hero card rather than overflowing.
        /// </remarks>
        [JsonIgnore]
        public string AnswerSpareSuffix => IsAffordabilityAvailable ? " monthly spare" : string.Empty;

        /// <summary>
        /// What the figure is net of. Defers to <see cref="AffordabilityTextDescription"/> when
        /// there is no figure, so the card keeps carrying the "record your income &amp; expenses"
        /// and trial prompts rather than asserting a basis for a number it does not have.
        /// </summary>
        [JsonIgnore]
        public string AnswerSpareBasis =>
            IsAffordabilityAvailable
                ? "after the loan, running costs and your expenses"
                : AffordabilityTextDescription.Trim();

        [JsonIgnore] public InsightsDetailsViewModel InsightsDetails { get; set; }

        private void BuildInsights()
        {
            if (PageHelper.IsFormLoading) return;

            PdfDataInsightsModel pdfDataInsights = new PdfDataInsightsModel(this, IncomeSummary, ExpenseSummary);
            pdfDataInsights.InitializeLocalDataSet();

            InsightsDetails = new InsightsDetailsViewModel();

            InsightsDetails.AffordabilityMonthly.Value = pdfDataInsights.Income
                .TotalAfterExpenseIncludingPropertyMonthly.ToCustomCurrencyRounded();
            InsightsDetails.AffordabilityYearly.Value = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyYearly
                .ToCustomCurrencyRounded();

            #region Property

            InsightsDetails.PropertyAmount.Value = PropertyAmount.ToCustomCurrencyRounded();
            InsightsDetails.PropertyEstimatedUpfront.Value = (HomeLoanInfo?.OtherExpenseTotalAmount ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.PropertyTotalAmount.Value =
                (HomeLoanInfo?.PropertyTotalAmount ?? 0).ToCustomCurrencyRounded();

            #endregion

            #region Loan

            InsightsDetails.LoanAmount.Value = (HomeLoanInfo?.LoanAmountDirectInput ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.DepositAmount.Value = (HomeLoanInfo?.DepositAmountDirectInput ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.TotalRepaymentToBank.Value =
                (HomeLoanInfo?.PaymentSummary?.Payment?.TotalPaymentRounded ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.TotalInterestToBank.Value =
                (HomeLoanInfo?.PaymentSummary?.Payment?.TotalInterestPaymentRounded ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.LoanTerm.Value = $"{LoanTermInYears} years";
            InsightsDetails.InterestRate.Value = $"{InterestRate}%";
            InsightsDetails.RepaymentDetailSelectedFrequency.Value =
                $"{HomeLoanInfo?.PaymentSummary?.Payment?.TermPayment.ToCustomCurrencyRounded()} {RepaymentFrequencySelected.Trim()}";
            InsightsDetails.RepaymentFrequency.Value = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(RepaymentFrequencySelected.Trim());
            InsightsDetails.RepaymentDetailMonthly.Value =
                (HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0).ToCustomCurrencyRounded();
            InsightsDetails.RepaymentDetailYearly.Value =
                (HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentYearly ?? 0).ToCustomCurrencyRounded();

            #endregion

            #region Income

            InsightsDetails.IncomeTotalMonthly.Value = pdfDataInsights.Income.TotalMonthly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeTotalYearly.Value = pdfDataInsights.Income.TotalYearly.ToCustomCurrencyRounded();

            InsightsDetails.IncomeAfterExpenseMonthly.Value = pdfDataInsights.Income.TotalAfterExpenseMonthly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseMonthly.Description =
                $"The net earnings remaining after deducting {pdfDataInsights.Expense.TotalMonthly.ToCustomCurrencyRounded()} in monthly expenses from the total monthly income of {pdfDataInsights.Income.TotalMonthly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseYearly.Value = pdfDataInsights.Income.TotalAfterExpenseYearly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseYearly.Description =
                $"The total annual income after subtracting {pdfDataInsights.Expense.TotalYearly.ToCustomCurrencyRounded()} in yearly expenses from the total annual income of {pdfDataInsights.Income.TotalYearly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseWithLoanMonthly.Value = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyMonthly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseWithLoanMonthly.Description =
                $"The remaining income after deducting monthly expenses, loan repayments, and investment expenses from the total monthly income of {pdfDataInsights.Income.TotalMonthly.ToCustomCurrencyRounded()}.";

            InsightsDetails.IncomeAfterExpenseWithLoanYearly.Value = pdfDataInsights.Income.TotalAfterExpenseIncludingPropertyYearly.ToCustomCurrencyRounded();
            InsightsDetails.IncomeAfterExpenseWithLoanYearly.Description =
                $"The total annual income after subtracting yearly expenses, loan repayments, and investment expenses from the total annual income of {pdfDataInsights.Income.TotalYearly.ToCustomCurrencyRounded()}.";
            #endregion

            #region Expense

            InsightsDetails.ExpenseOverallTotalMonthly.Value = pdfDataInsights.Income.TotalExpenseIncludingPropertyMonthly.ToCustomCurrencyRounded();
            InsightsDetails.ExpenseOverallTotalYearly.Value = pdfDataInsights.Income.TotalExpenseIncludingPropertyYearly.ToCustomCurrencyRounded();

            InsightsDetails.ExpenseCostOfNewPropertyOwnershipMonthly.Value =
                pdfDataInsights.Loan.TotalMonthlyRunningExpense.ToCustomCurrencyRounded();
            InsightsDetails.ExpenseCostOfNewPropertyOwnershipYearly.Value = pdfDataInsights.Loan.TotalYearlyRunningExpense.ToCustomCurrencyRounded();

            InsightsDetails.ExpenseLoanFinancialCommitmentsMonthly.Value = pdfDataInsights.Loan.MonthlyRepaymentWithExpenses.ToCustomCurrencyRounded();
            InsightsDetails.ExpenseLoanFinancialCommitmentsYearly.Value = pdfDataInsights.Loan.YearlyRepaymentWithExpenses.ToCustomCurrencyRounded();

            InsightsDetails.ExpenseCurrentFinancialOutflowsMonthly.Value =
                pdfDataInsights.Expense.TotalMonthly.ToCustomCurrencyRounded();
            InsightsDetails.ExpenseCurrentFinancialOutflowsYearly.Value = pdfDataInsights.Expense.TotalYearly.ToCustomCurrencyRounded();

            #endregion


            InsightsDetails.ExpenseOverallTotalMonthly.Description =
                $"Your overall financial outflows within a given month, covering loan repayments and all other expenses. {Environment.NewLine}(Calculation: {InsightsDetails.RepaymentDetailMonthly.Value} repayment + {InsightsDetails.ExpenseCostOfNewPropertyOwnershipMonthly.Value} cost of ownership + {InsightsDetails.ExpenseCurrentFinancialOutflowsMonthly.Value} current monthly expenses)";


            InsightsDetails.ExpenseOverallTotalYearly.Description =
                $"Your overall financial outflows within a given year, covering loan repayments and all other expenses. {Environment.NewLine}(Calculation: {InsightsDetails.RepaymentDetailYearly.Value} repayment + {InsightsDetails.ExpenseCostOfNewPropertyOwnershipYearly.Value} cost of ownership + {InsightsDetails.ExpenseCurrentFinancialOutflowsYearly.Value} current yearly expenses)";


            UpdateInsightCharts();
        }

        #endregion

        #region Repayment Frequency

        [JsonIgnore] public ObservableCollection<SfSegmentItem> RepaymentFrequencyCollection { get; set; }

        public int RepaymentFrequencySelectedIndex
        {
            get
            {
                if (HomeLoanInfo?.HomeLoanRepaymentRequest?.TotalNumberPaymentPerYear == 12)
                {
                    return 0;
                }
                else if (HomeLoanInfo?.HomeLoanRepaymentRequest?.TotalNumberPaymentPerYear == 24)
                {
                    return 1;
                }
                else if (HomeLoanInfo?.HomeLoanRepaymentRequest?.TotalNumberPaymentPerYear == 52)
                {
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (HomeLoanInfo?.HomeLoanRepaymentRequest == null) return;

                if (value == 0)
                {
                    HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
                }
                else if (value == 1)
                {
                    HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 24;
                }
                else if (value == 2)
                {
                    HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 52;
                }
                else
                {
                    HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
                }

                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore]
        public string RepaymentFrequencySelected => RepaymentFrequencySelectedIndex == 0 ? " monthly" :
            RepaymentFrequencySelectedIndex == 1 ? " fortnightly" :
            RepaymentFrequencySelectedIndex == 2 ? " weekly" : " monthly";

        #endregion

        #region Amortisation


        [JsonIgnore]
        public List<PaymentAmortisationOutput>? PaymentAmortization => HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms ?? new List<PaymentAmortisationOutput>();

        // ── Schedule grouped into decades ──────────────────────────────────────
        //
        // Grouped off the same PaymentAmortization rows the grid used. Decade boundaries are taken
        // from the loan's own first year rather than from calendar decades (…2020, …2030), so the
        // first group starts at year one of the loan and every group holds ten of its years.
        [JsonIgnore]
        public List<AmortisationDecade> AmortisationDecades
        {
            get
            {
                var terms = PaymentAmortization;
                if (terms == null || terms.Count == 0) return new List<AmortisationDecade>();

                var sym = CurrencySymbol ?? "$";
                var firstYear = terms.Min(t => t.YearOfPayment);

                return terms
                    .GroupBy(t => (t.YearOfPayment - firstYear) / 10)
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        var years = g.OrderBy(t => t.YearOfPayment).ToList();
                        var from = years.First().YearOfPayment;
                        var to = years.Last().YearOfPayment;

                        return new AmortisationDecade(
                            periodLabel: from == to ? $"{from}" : $"{from}–{to}",
                            repaymentTotal: $"{sym}{years.Sum(t => t.PaymentAmount):N0}",
                            interestTotal: $"{sym}{years.Sum(t => t.InterestAmount):N0}",
                            principalTotal: $"{sym}{years.Sum(t => t.PrincipalAmount):N0}",
                            endBalance: $"{sym}{years.Last().BalanceAmount:N0}",
                            years: years,
                            isExpanded: g.Key == 0);
                    })
                    .ToList();
            }
        }

        [JsonIgnore] public bool HasAmortisationDecades => AmortisationDecades.Count > 0;

        /// <summary>
        /// Re-reads the decade list. Needed because <see cref="AmortisationDecades"/> is computed
        /// on every get from <see cref="PaymentAmortization"/> and raises nothing of its own, so a
        /// recompute of the underlying terms is otherwise invisible to the bound list.
        /// </summary>
        public void RaiseAmortisationDecadesChanged()
        {
            OnPropertyChanged(nameof(AmortisationDecades));
            OnPropertyChanged(nameof(HasAmortisationDecades));
        }

        [JsonIgnore] private ObservableCollection<ChartDataModel> _amortizationChartPrincipal = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _amortizationChartInterest = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _amortizationChartBalance = new();

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartPrincipalAmountAxis => _amortizationChartPrincipal;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartInterestAmountAxis => _amortizationChartInterest;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AmortizationChartBalanceAxis => _amortizationChartBalance;

        [JsonIgnore] public string AmortizationBalanceSubtitle =>
            $"Remaining loan balance over {LoanTermInYears} yr{(LoanTermInYears == 1 ? "" : "s")}";

        // ── "Where the money goes" (What If ▸ Answer) ──────────────────────────
        //
        // Same source as the two series above, but summed per year rather than left per payment
        // row. The Answer chart is a stacked column with one column per year, so feeding it the
        // per-row series would draw one thin column per repayment — 360 of them on a 30-year
        // monthly loan — instead of the twelve the prototype shows.
        [JsonIgnore] private ObservableCollection<ChartDataModel> _answerPrincipalByYear = new();
        [JsonIgnore] private ObservableCollection<ChartDataModel> _answerInterestByYear = new();

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AnswerPrincipalByYearAxis => _answerPrincipalByYear;

        [JsonIgnore]
        public ObservableCollection<ChartDataModel> AnswerInterestByYearAxis => _answerInterestByYear;

        [JsonIgnore] public bool HasAnswerChartData => _answerPrincipalByYear.Count > 0;

        // ── PDF export section list ────────────────────────────────────────────
        //
        // Describes what the report contains; it does not control it. See ExportSectionOption
        // for why these are read-only and the checkboxes bound to them are disabled.
        [JsonIgnore]
        public List<ExportSectionOption> ExportSections { get; } = new()
        {
            new("WhatIfExportSectionVerdict",   "Affordability verdict", "The headline answer and the three-box summary"),
            new("WhatIfExportSectionLoan",      "Loan & borrowing",      "Asset, deposit, loan amount, rate and term"),
            new("WhatIfExportSectionBudget",    "Income & expenses",     "Monthly and yearly totals, and the net surplus"),
            new("WhatIfExportSectionSchedule",  "Repayment schedule",    "Full year-by-year amortisation table"),
            new("WhatIfExportSectionProjection","Projection",            "Income and expense projection over time"),
            new("WhatIfExportSectionScenarios", "Scenarios",             "All nine what-if results"),
        };

        private void UpdateAmortizationCharts()
        {
            var terms = HomeLoanInfo?.PaymentSummary?.PaymentAmortizationTerms;
            if (terms == null)
            {
                _amortizationChartPrincipal.Clear();
                _amortizationChartInterest.Clear();
                _amortizationChartBalance.Clear();
                _answerPrincipalByYear.Clear();
                _answerInterestByYear.Clear();
                return;
            }

            var currentYear = DateTime.Now.Year;
            var filtered = terms.Where(f => f.YearOfPayment != currentYear).ToList();

            RefillCollection(_amortizationChartPrincipal,
                filtered.Select(f => new ChartDataModel(f.YearOfPayment.ToString(), f.PrincipalAmount)));

            RefillCollection(_amortizationChartInterest,
                filtered.Select(f => new ChartDataModel(f.YearOfPayment.ToString(), f.InterestAmount)));

            // Balance chart: one point per year using the last payment of that year
            var balanceByYear = filtered
                .GroupBy(f => f.YearOfPayment)
                .Select(g => new ChartDataModel(g.Key.ToString(), g.Last().BalanceAmount))
                .ToList();
            RefillCollection(_amortizationChartBalance, balanceByYear);

            // Answer chart: principal and interest summed per year, so the stacked columns show
            // the shift from mostly-interest to mostly-principal across the life of the loan.
            var byYear = filtered.GroupBy(f => f.YearOfPayment).OrderBy(g => g.Key).ToList();
            RefillCollection(_answerPrincipalByYear,
                byYear.Select(g => new ChartDataModel(g.Key.ToString(), g.Sum(f => f.PrincipalAmount))));
            RefillCollection(_answerInterestByYear,
                byYear.Select(g => new ChartDataModel(g.Key.ToString(), g.Sum(f => f.InterestAmount))));
        }

        // Called when currency changes — rebuilds chart data and notifies the grid
        // unconditionally (bypasses the IsAmortizationTabActive guard).
        public void ForceUpdateAmortizationCharts()
        {
            UpdateAmortizationCharts();
            OnPropertyChanged(nameof(PaymentAmortization));
            OnPropertyChanged(nameof(CurrencySymbol));
        }

        private static void RefillCollection(ObservableCollection<ChartDataModel> col, IEnumerable<ChartDataModel> source)
        {
            col.Clear();
            foreach (var item in source)
                col.Add(item);
        }

        #endregion

        #region Other expenses

        [JsonIgnore]
        public double StampDuty
        {
            get => HomeLoanInfo?.StampDuty?.StampDuty ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.StampDuty.StampDuty == value) return;

                isUpdating = true;

                HomeLoanInfo.StampDuty.StampDuty = value;
                HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        [JsonIgnore]
        public double MortgageCharges
        {
            get => HomeLoanInfo?.StampDuty?.MortgageCharges ?? 0;
            set
            {
                if (isUpdating || !HasInitialized) return;
                if (HomeLoanInfo.StampDuty.MortgageCharges == value) return;

                isUpdating = true;

                HomeLoanInfo.StampDuty.MortgageCharges = value;
                HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                TriggerPropertyChangedOnPropertyTab();

                isUpdating = false;
            }
        }

        [JsonIgnore]
        public double ConveyancerFee
        {
            get => HomeLoanInfo?.ConveyanceExpense?.ConveyancerFee ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    HomeLoanInfo.ConveyanceExpense.ConveyancerFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore]
        public double BankFee
        {
            get => HomeLoanInfo?.BankExpense?.BankSettlementFee ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    HomeLoanInfo.BankExpense.BankSettlementFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore]
        public double InspectionFee
        {
            get => HomeLoanInfo?.OtherExpense?.InspectionFee ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    HomeLoanInfo.OtherExpense.InspectionFee = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore]
        public double OtherExpenses
        {
            get => HomeLoanInfo?.OtherExpense?.OtherExpenses ?? 0;
            set
            {
                if (isUpdating == false && HasInitialized)
                {
                    isUpdating = true;

                    HomeLoanInfo.OtherExpense.OtherExpenses = value;
                    HomeLoanInfo.DepositAmountDirectInput = HomeLoanInfo.DepositAmountDirectInput;  // preserve the deposit, re-derive the loan — see note above

                    TriggerPropertyChangedOnPropertyTab();

                    isUpdating = false;
                }
            }
        }

        [JsonIgnore] public string OtherExpenseTotalAmount => $"{CurrencySymbol}{HomeLoanInfo?.OtherExpenseTotalAmount ?? 0:N0}";

        #endregion

        #region Expense

        #region Total Details

        [JsonIgnore]
        public string TotalMonthlyExpenseWithComma => TransactionRecords?.IncomeExpenseSummary?.TotalMonthlyWithComma;

        [JsonIgnore]
        public string TotalYearlyExpenseWithComma => TransactionRecords?.IncomeExpenseSummary?.TotalYearlyWithComma;


        [JsonIgnore] private IncomeViewModel _incomeSummary;

        [JsonIgnore]
        public IncomeViewModel IncomeSummary
        {
            get => _incomeSummary;
            set
            {
                _incomeSummary = value;
                OnPropertyChanged(nameof(IncomeSummary));
            }
        }

        [JsonIgnore] private ExpenseViewModel _expenseSummary;

        [JsonIgnore]
        public ExpenseViewModel ExpenseSummary
        {
            get => _expenseSummary;
            set
            {
                _expenseSummary = value;
                OnPropertyChanged(nameof(ExpenseSummary));
            }
        }

        [JsonIgnore] public string TotalMonthlyExistingExpense => $"{Math.Round(ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0, 0):N0}";

        // ── Answer-tab summary boxes (What If) ───────────────────────────────
        //
        // Three figures the prototype shows beneath the affordability hero. All three return the
        // placeholder until income and expenses exist, matching every other derived figure here.

        [JsonIgnore]
        private double MonthlyIncomeRaw => IncomeSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;

        [JsonIgnore]
        private double MonthlyExpenseRaw => ExpenseSummary?.TransactionRecords?.IncomeExpenseSummary?.TotalMonthly ?? 0;

        /// <summary>The loan repayment at the frequency the user selected.</summary>
        [JsonIgnore]
        public string AnswerRepaymentDisplay =>
            TermPaymentRoundedWithComma is null or "0" ? "--" : $"{CurrencySymbol}{TermPaymentRoundedWithComma}";

        /// <summary>Income less living expenses, before the loan repayment.</summary>
        /// <remarks>
        /// The sign goes BEFORE the symbol. Interpolating a negative straight after the symbol
        /// gives "$-41,295", which is what this did — the symbol and the minus have to be ordered
        /// explicitly because `:N0` puts the minus on the number, not on the currency.
        /// </remarks>
        [JsonIgnore]
        public string AnswerNetIncomeDisplay
        {
            get
            {
                if (!IsAffordabilityAvailable) return "--";

                var net = Math.Round(MonthlyIncomeRaw - MonthlyExpenseRaw, 0);
                return $"{(net < 0 ? "-" : string.Empty)}{CurrencySymbol}{Math.Abs(net):N0}";
            }
        }

        /// <summary>True when <see cref="AnswerNetIncomeDisplay"/> shows a negative figure.</summary>
        [JsonIgnore]
        public bool AnswerNetIncomeIsNegative =>
            IsAffordabilityAvailable && Math.Round(MonthlyIncomeRaw - MonthlyExpenseRaw, 0) < 0;

        /// <summary>
        /// The repayment as a share of gross monthly income — the standard payment-to-income
        /// ratio lenders quote.
        /// </summary>
        /// <remarks>
        /// <para>Defined deliberately, because the prototype's own figures do not reconcile: it
        /// shows "35% of income" beside a $2,792 repayment and $6,000 income, which is 46.5%.
        /// Gross income is the denominator (not net, and not income-after-expenses) so the number
        /// means the same thing a lender means by it.</para>
        ///
        /// <para><b>TermPaymentMonthly, not TermPayment.</b> <c>TermPayment</c> is the payment at
        /// whatever frequency the user selected, so dividing it by MONTHLY income only gave a
        /// payment-to-income ratio when that frequency happened to be monthly — a weekly payer saw
        /// roughly a quarter of the true ratio. The monthly-normalised payment is also what
        /// <see cref="AnswerVerdictCaption"/> compares the surplus against, so the two agreed about
        /// affordability while disagreeing about the payment.</para>
        ///
        /// <para>The display is capped. Past a few hundred percent the figure no longer measures
        /// anything the reader can use — it only says "the repayment dwarfs the income", which the
        /// verdict and the net-income box next to it already say — and four significant digits of
        /// it ("3,099%") read as a defect rather than a measurement.</para>
        /// </remarks>
        [JsonIgnore]
        public string AnswerRepaymentShareOfIncome
        {
            get
            {
                if (!IsAffordabilityAvailable || MonthlyIncomeRaw <= 0) return "--";
                var repayment = HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0;
                if (repayment <= 0) return "--";

                var share = repayment / MonthlyIncomeRaw * 100;
                return share > ShareOfIncomeDisplayCap
                    ? $">{ShareOfIncomeDisplayCap:N0}%"
                    : $"{Math.Round(share, 0):N0}%";
            }
        }

        /// <summary>
        /// The explanation card for the three figures beside the verdict, with this user's own
        /// numbers written into it.
        /// </summary>
        /// <remarks>
        /// <para>Built here rather than being static copy because the point is the figures: a
        /// reader asking what "of income" means is far better served by "your repayment of $3,812
        /// against the $17,810 a month you entered" than by a definition. The wording around them
        /// comes from <see cref="AnswerPanelExplanations"/> so the meaning of each figure is still
        /// stated in one place and unit-tested.</para>
        ///
        /// <para>Reuses the same <c>ScenarioExplanation</c> shape as the nine What If scenario
        /// cards, so the Answer tab gets the identical card rather than a second kind of
        /// explanation.</para>
        /// </remarks>
        [JsonIgnore]
        public ScenarioExplanation AnswerPanelsExplanation
        {
            get
            {
                var sym = CurrencySymbol;
                var income = MonthlyIncomeRaw;
                var expenses = MonthlyExpenseRaw;
                var monthlyRepayment = HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0;

                var repayment = AnswerPanelExplanations.For(AnswerPanel.Repayment);
                var netIncome = AnswerPanelExplanations.For(AnswerPanel.NetIncome);
                var share = AnswerPanelExplanations.For(AnswerPanel.ShareOfIncome);

                var hasBudget = IsAffordabilityAvailable && income > 0;

                var terms = new List<ExplanationTerm>
                {
                    new($"{repayment.Caption} — {AnswerRepaymentDisplay}",
                        repayment.Meaning
                        + (monthlyRepayment > 0
                            ? $"\n\nFor comparison, that works out at {sym}{monthlyRepayment:N0} a month."
                            : string.Empty)),

                    new($"{netIncome.Caption} — {AnswerNetIncomeDisplay}",
                        netIncome.Meaning
                        + (hasBudget
                            ? $"\n\nYours comes from the {sym}{income:N0} a month you entered as income, "
                              + $"less the {sym}{expenses:N0} a month of expenses you recorded."
                            : "\n\nYou have not recorded income and expenses yet, so there is nothing "
                              + "to work this out from.")),

                    new($"{share.Caption} — {AnswerRepaymentShareOfIncome}",
                        share.Meaning
                        + (hasBudget && monthlyRepayment > 0
                            ? $"\n\nYours is the {sym}{monthlyRepayment:N0} monthly repayment measured "
                              + $"against the {sym}{income:N0} a month coming in."
                            : string.Empty)),
                };

                return new ScenarioExplanation(
                    Title: "Understanding these numbers",
                    Purpose:
                        "The big figure at the top is the headline answer: what you would have left "
                        + "over each month, or be short by, once this loan and everything else is "
                        + "paid for. The three smaller boxes underneath show where it came from.",
                    ReadingTheNumbers: terms,
                    WatchFor:
                        "All of these are worked out from the figures you have entered, so they are "
                        + "only as good as those. They are estimates to help you think it through, "
                        + "not financial advice.");
            }
        }

        /// <summary>
        /// Highest payment-to-income ratio shown as a figure; above it the box reads "&gt;999%".
        /// </summary>
        /// <remarks>
        /// 999 rather than 100: a ratio over 100% is implausible but still informative (it says
        /// how far over), and percentages stay readable into the hundreds. Four digits are where
        /// they stop being readable, which is where the cap sits.
        /// </remarks>
        private const double ShareOfIncomeDisplayCap = 999;

        [JsonIgnore]
        public string TotalMonthlyOverallExpense =>
            $"{Math.Round(TransactionRecords?.IncomeExpenseSummary?.TotalMonthly + HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly ?? 0, 0):N0}";

        [JsonIgnore]
        public string TotalMonthlyOverallExpenseBreakdownWithComma
        {
            get
            {
                string expenses = System.Environment.NewLine;

                expenses += $"(${TransactionRecords?.IncomeExpenseSummary?.TotalMonthly.ToString("N0") ?? "0"}";

                if (HomeLoanInfo?.PaymentSummary?.Payment?.TermPaymentMonthly != null)
                {
                    expenses += $" + ${HomeLoanInfo.PaymentSummary.Payment.TermPaymentMonthly.ToString("N0")}";
                }

                expenses += ")";

                return expenses;
            }
        }


        #endregion

        #endregion

        private int _liveEditDepth;
        private bool _liveEditPassPending;

        /// <summary>
        /// True while a data-entry page is taking keystrokes and the broad notification pass is
        /// being coalesced.
        /// </summary>
        [JsonIgnore]
        public bool IsLiveEditing => _liveEditDepth > 0;

        /// <summary>
        /// Tells this view model that a modal data-entry page is about to take keystrokes, so the
        /// broad notification pass can be coalesced into one pass when editing ends.
        /// </summary>
        /// <remarks>
        /// <b>Why this exists.</b> Typing in the loan wizard or the upfront-costs page was
        /// sluggish, one character at a time. The arithmetic was never the problem — measured at
        /// 0.005 ms per change, because the payment schedule is no longer rebuilt on this path.
        /// The cost is <see cref="TriggerPropertyChangedOnPropertyTab"/> raising <b>48</b>
        /// PropertyChanged events across 47 properties for every keystroke. Each one makes MAUI
        /// re-evaluate every binding subscribed to it, on every page still alive — and both of
        /// these pages are modal, so the Loan page, the What If Answer tab and (once visited) the
        /// nine Insights panels are all sitting behind them, invisible, re-resolving their
        /// bindings per character.
        ///
        /// Notifying a page nobody can see, seven times while someone types "1234567", is the
        /// whole of the waste. So while live editing is in progress only the handful of properties
        /// the open page itself needs are raised, and the full pass is deferred to
        /// <see cref="EndLiveEdit"/>.
        ///
        /// Callers must pair this with <see cref="EndLiveEdit"/>. Balance it with a page-local
        /// flag rather than relying on appear/disappear being symmetric: an unmatched Begin leaves
        /// the app permanently coalescing, which presents as figures that never update anywhere.
        /// </remarks>
        public void BeginLiveEdit() => _liveEditDepth++;

        /// <summary>
        /// Ends a <see cref="BeginLiveEdit"/> scope and runs the deferred notification pass, if
        /// anything asked for one while it was open.
        /// </summary>
        public void EndLiveEdit()
        {
            if (_liveEditDepth > 0) _liveEditDepth--;

            // Still nested, so the outermost scope owns the flush.
            if (_liveEditDepth > 0) return;

            if (!_liveEditPassPending) return;

            _liveEditPassPending = false;
            TriggerPropertyChangedOnPropertyTab();
        }

        public void TriggerPropertyChangedOnPropertyTab()
        {
            if (PageHelper.IsFormLoading) return;
            if (SharedServiceCore.LoadSafe) return;

            // Deliberately BEFORE the live-edit check: the recalculation is what makes the derived
            // figures the open page displays correct, and it is effectively free. It is the
            // notification fan-out below that is expensive, not this.
            EventsTriggerPriceUpdate();

            if (_liveEditDepth > 0)
            {
                _liveEditPassPending = true;

                // The only properties either data-entry page consults while it is open:
                //  · LoanDetailsPage listens for these three in OnLoanViewModelPropertyChanged, to
                //    mirror the two figures the model re-derived back into its own entries;
                //  · UpfrontCostsPage binds OtherExpenseTotalAmount as its running total.
                // Adding a binding to one of these pages means adding the property here too, or it
                // will update only once the page closes.
                OnPropertyChanged(nameof(DepositAmountDirectInput));
                OnPropertyChanged(nameof(LoanAmountDirectInput));
                OnPropertyChanged(nameof(LoanAmountStrFormatted));
                OnPropertyChanged(nameof(OtherExpenseTotalAmount));

                // Saving is NOT deferred. It is already debounced, so it costs nothing per
                // keystroke, and deferring it would mean a dismissal route that somehow skipped
                // EndLiveEdit could lose what the user typed.
                ScheduleSave(() => SharedServiceCore.SaveData(this));
                return;
            }

            // Always notify — these drive the Asset tab which is always visible
            OnPropertyChanged(nameof(PropertyAmount));
            OnPropertyChanged(nameof(PropertyAmountFormatted));
            OnPropertyChanged(nameof(LoanTermInYears));
            OnPropertyChanged(nameof(InterestRate));
            OnPropertyChanged(nameof(StampDuty));
            OnPropertyChanged(nameof(DepositAmountDirectInput));
            OnPropertyChanged(nameof(LoanAmountDirectInput));
            OnPropertyChanged(nameof(LoanAmountPercentage));
            OnPropertyChanged(nameof(LoanAmountStrFormatted));
            OnPropertyChanged(nameof(DepositPercentage));
            OnPropertyChanged(nameof(LoanAmount));
            OnPropertyChanged(nameof(PropertyTotalAmount));
            OnPropertyChanged(nameof(ShowAssetTotalBreakdown));
            OnPropertyChanged(nameof(AssetTotalBreakdown));
            OnPropertyChanged(nameof(InterestRateFormatted));
            OnPropertyChanged(nameof(DepositAmountStrFormatted));
            OnPropertyChanged(nameof(OtherExpenseTotalAmount));
            OnPropertyChanged(nameof(MortgageCharges));
            OnPropertyChanged(nameof(ConveyancerFee));
            OnPropertyChanged(nameof(BankFee));
            OnPropertyChanged(nameof(InspectionFee));
            OnPropertyChanged(nameof(OtherExpenses));
            OnPropertyChanged(nameof(RepaymentFrequencySelected));
            OnPropertyChanged(nameof(HomeLoanInfo));
            OnPropertyChanged(nameof(TermPaymentRoundedWithComma));
            OnPropertyChanged(nameof(TotalPaymentRoundedWithComma));
            OnPropertyChanged(nameof(ChartInterestCategoryLabel));
            OnPropertyChanged(nameof(ChartTotalCostSubtitle));
            OnPropertyChanged(nameof(ChartInsightSubtitle));
            OnPropertyChanged(nameof(AmortizationBalanceSubtitle));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityNegative));
            OnPropertyChanged(nameof(AffordabilityTextDescription));

            // The What If Answer tab's figures. None of these were notified anywhere, so they held
            // whatever they evaluated to when the page's BindingContext was first set — correct on
            // arrival, stale if the loan then changed. The explanation card quotes the same figures,
            // so it is raised alongside them or it would describe numbers no longer on screen.
            OnPropertyChanged(nameof(AnswerVerdictCaption));
            OnPropertyChanged(nameof(AnswerSpareSuffix));
            OnPropertyChanged(nameof(AnswerSpareBasis));
            OnPropertyChanged(nameof(AnswerRepaymentDisplay));
            OnPropertyChanged(nameof(AnswerNetIncomeDisplay));
            OnPropertyChanged(nameof(AnswerNetIncomeIsNegative));
            OnPropertyChanged(nameof(AnswerRepaymentShareOfIncome));
            OnPropertyChanged(nameof(AnswerPanelsExplanation));

            if (IsAmortizationTabActive)
            {
                UpdateAmortizationCharts();
            }

            // Only update insight charts if the Insights tab is currently visible
            if (IsInsightsTabActive)
            {
                UpdateInsightCharts();
            }

            ScheduleSave(() => SharedServiceCore.SaveData(this));
            IsBusy = false;
        }
        public virtual void TriggerPropertyChangedOnAmortizationTab()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            UpdateAmortizationCharts();

            OnPropertyChanged(nameof(PaymentAmortization));
            OnPropertyChanged(nameof(AmortizationBalanceSubtitle));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityNegative));
            OnPropertyChanged(nameof(AffordabilityTextDescription));
            ScheduleSave(() => SharedServiceCore.SaveData(this));
        }

        public void RefreshExpenseTabPropertyChanged()
        {
            if (PageHelper.IsFormLoading || TransactionRecords == null) return;

            if (SharedServiceCore.LoadSafe) return;

            TransactionRecords.SumUpData();

            OnPropertyChanged(nameof(IncomeEntryName));
            OnPropertyChanged(nameof(HasErrorIncomeDescription));
            OnPropertyChanged(nameof(IncomeEntryAmount));
            OnPropertyChanged(nameof(IncomeEntryAmountText));
            OnPropertyChanged(nameof(HasErrorIncomeAmount));
            OnPropertyChanged(nameof(IsExpenseDataFormReadyToSubmit));
            OnPropertyChanged(nameof(TotalMonthlyExpenseWithComma));
            OnPropertyChanged(nameof(TotalYearlyExpenseWithComma));
            OnPropertyChanged(nameof(IncomeExpenseFrequencySelectedIndex));
            OnPropertyChanged(nameof(Transactions));
            OnPropertyChanged(nameof(FilteredTransactions));
            OnPropertyChanged(nameof(AutocompleteNameList));
            OnPropertyChanged(nameof(AutocompleteList));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(TotalMonthlyOverallExpense));
            OnPropertyChanged(nameof(TotalMonthlyExistingExpense));
            OnPropertyChanged(nameof(TotalMonthlyOverallExpenseBreakdownWithComma));
            OnPropertyChanged(nameof(AffordabilityCurrencySymbol));
            OnPropertyChanged(nameof(IsAffordabilityAvailable));
            OnPropertyChanged(nameof(Affordability));
            OnPropertyChanged(nameof(IsAffordabilityNegative));
            OnPropertyChanged(nameof(AffordabilityTextDescription));

            ScheduleSave(() => SharedServiceCore.SaveData(this));
        }
        public void RefreshInsightsTabPropertyChanged()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            BuildInsights();

            OnPropertyChanged(nameof(InsightsDetails));
        }

        public void UpdateAmortizationData()
        {
            if (PageHelper.IsFormLoading) return;

            HomeLoanCalculator.UpdateLoanPaymentAmortizationDataByYear(HomeLoanInfo.PaymentSummary);
        }

        public void SyncAmortization()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;

            UpdateAmortizationData();
            TriggerPropertyChangedOnAmortizationTab();
        }

        public void EventsTriggerStampDutyUpdate()
        {
            if (PageHelper.IsFormLoading || HomeLoanInfo == null) return;

            if (SharedServiceCore.LoadSafe) return;

            if (!ShowAustralianStateSelectorOnStampDuty) return;
            if (HomeLoanInfo.StampDuty.AustraliaStateSelected.HasValue)
            {
                HomeLoanInfo.StampDuty =
                    HomeLoanCalculator.StampDutyCalculator.CalculateStampDutyAustralia(
                        HomeLoanInfo.StampDuty.AustraliaStateSelected.Value, PropertyAmount);
            }
            else
            {
                // Optionally, handle the case where no state is selected.
                // For example, you could reset StampDuty or leave it unchanged.
                // HomeLoanInfo.StampDuty = new StampDutyOutput();
            }
            HomeLoanInfo.StampDuty.AutoUpdateMortgageCharges();
            OnPropertyChanged(nameof(StampDuty));
        }
        public void EventsTriggerPriceUpdate()
        {
            if (PageHelper.IsFormLoading || HomeLoanInfo == null) return;

            if (SharedServiceCore.LoadSafe) return;

            // Guard against a 0 payments-per-year (e.g. data saved before the frequency segment
            // was ever touched, or a wizard-only flow). Without this the payment calc returns 0
            // and the Asset tab shows "$0 monthly" even though a loan amount exists.
            if (HomeLoanInfo.HomeLoanRepaymentRequest != null &&
                HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear == 0)
            {
                HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
            }

            HomeLoanInfo.PaymentSummary =
                HomeLoanCalculator.CalculateHomeLoanPayments(HomeLoanInfo.LoanAmount,
                    HomeLoanInfo.HomeLoanRepaymentRequest);

            LiveEventsUpdate();
        }
        public void LiveEventsUpdate()
        {
            if (PageHelper.IsFormLoading || HomeLoanInfo == null) return;

            if (SharedServiceCore.LoadSafe) return;

            // Skipped while a modal data-entry page is taking keystrokes, where it is pure waste.
            // The sole consumer of this collection is a chart on LoanView (ItemsSource), and that
            // page is behind the modal. Handing a chart a brand-new ObservableCollection makes it
            // tear down and rebuild its series, which makes this the most expensive single item on
            // the per-keystroke path — for a result nobody can currently see.
            //
            // Not lost, just deferred: EndLiveEdit runs the flush pass, which reaches here again
            // through EventsTriggerPriceUpdate with coalescing off, so the chart is rebuilt exactly
            // once when the page closes.
            if (_liveEditDepth > 0) return;

            if (HomeLoanInfo.PropertyTotalAmount > 0 && HomeLoanInfo.PaymentSummary.Payment.TotalPaymentRounded > 0)
            {
                ChartPropertyValueWithInterestPayment = new ObservableCollection<DataModel>
                {
                    new DataModel
                    {
                        Category = "Loan", Value = HomeLoanInfo.LoanAmount,
                        ValueWithComma = $"{CurrencySymbol}{HomeLoanInfo.LoanAmount:N0}"
                    },
                    new DataModel
                    {
                        Category = ChartInterestCategoryLabel, Value = HomeLoanInfo.PaymentSummary.Payment.TotalInterestPayment,
                        ValueWithComma =
                            $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalInterestPaymentRoundedWithComma:N0}"
                    },
                    //new DataModel { Category = "Total", Value = HomeLoanInfo.PaymentSummary.Payment.TotalPayment, ValueWithComma = $"{CurrencySymbol}{HomeLoanInfo.PaymentSummary.Payment.TotalPaymentRounded:N2}" },
                };
            }
        }

        // Called once after data load to ensure SfSegmentedControl picks up its ItemsSource and SelectedIndex
        public void TriggerSegmentCollectionsRefresh()
        {
            OnPropertyChanged(nameof(RepaymentFrequencyCollection));
            OnPropertyChanged(nameof(RepaymentFrequencySelectedIndex));
            OnPropertyChanged(nameof(AustraliaStateCollection));
            OnPropertyChanged(nameof(AustraliaStateSelectedIndex));
        }

        public void TriggerPropertyChangedOnPageLevel()
        {
            if (PageHelper.IsFormLoading) return;

            if (SharedServiceCore.LoadSafe) return;
            OnPropertyChanged(nameof(PdfGenerator));
        }
    }
}
