using System.Text.Json;
using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Measures how much work one user gesture costs, so the Loan screen's responsiveness has a
    /// regression guard rather than relying on someone noticing it feels slow again.
    /// </summary>
    /// <remarks>
    /// Dragging the deposit slider and backspacing in the asset field were both sluggish. Every
    /// change to any loan figure runs <c>EventsTriggerPriceUpdate</c>, which rebuilt the full
    /// payment-by-payment schedule — one object per payment across the term, so 360 for a monthly
    /// 30-year loan and 1,560 for a weekly one — and then discarded it, because its only consumer
    /// is the amortisation builder reached from the Schedule page and the PDF.
    /// </remarks>
    [TestFixture]
    public class LoanHotPathCostTests
    {
        [OneTimeSetUp]
        public void GrantPremium() => SharedServiceCore.EntitlementOverrideForTests = true;

        [OneTimeTearDown]
        public void ClearPremium() => SharedServiceCore.EntitlementOverrideForTests = null;

        /// <summary>A weekly loan — the worst case, at 52 payments a year.</summary>
        private static LoanViewModel BuildWeeklyLoan()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = 5.0,
                        LoanTermInYears = 30,
                        TotalNumberPaymentPerYear = 52,
                    },
                    PropertyAmount = 650_000,
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] },
            };
            vm.MarkInitializationComplete();

            // A percentage split, not a direct deposit: a direct amount takes precedence over the
            // percentage in HomeLoanInformation.LoanAmount, so the tests below would be changing a
            // value the loan does not consult.
            vm.HomeLoanInfo.DepositPercentage = 20;

            // Establish a real PaymentSummary, as a page load does. Without it the summary is the
            // empty one HomeLoanInformation's constructor makes, whose Payment is null — and then a
            // test asserting "the schedule was not built" would pass because nothing was ever
            // calculated at all.
            vm.EventsTriggerPriceUpdate();
            return vm;
        }

        /// <summary>
        /// The gesture the user complained about: one drag step on the deposit slider.
        /// </summary>
        [Test]
        public void ChangingTheDeposit_DoesNotBuildThePaymentSchedule()
        {
            var vm = BuildWeeklyLoan();

            vm.DepositPercentage = 25;

            Assert.That(vm.HomeLoanInfo.PaymentSummary?.PaymentTerms, Is.Empty,
                "a slider step rebuilt 1,560 schedule rows that nothing on screen reads");
        }

        /// <summary>And one keystroke in the asset field.</summary>
        [Test]
        public void ChangingTheAssetValue_DoesNotBuildThePaymentSchedule()
        {
            var vm = BuildWeeklyLoan();

            vm.PropertyAmount = 655_000;

            Assert.That(vm.HomeLoanInfo.PaymentSummary?.PaymentTerms, Is.Empty,
                "a keystroke rebuilt 1,560 schedule rows that nothing on screen reads");
        }

        /// <summary>
        /// The figures must still be right — this is a deferral, not a removal.
        /// </summary>
        [Test]
        public void ChangingTheDeposit_StillProducesTheRepaymentFigures()
        {
            var vm = BuildWeeklyLoan();

            vm.DepositPercentage = 25;

            Assert.Multiple(() =>
            {
                Assert.That(vm.HomeLoanInfo.PaymentSummary?.Payment?.TermPayment, Is.GreaterThan(0));
                Assert.That(vm.HomeLoanInfo.LoanAmount, Is.EqualTo(487_500).Within(1),
                    "75% of the 650,000 asset");
            });
        }

        /// <summary>
        /// The schedule still gets built when something genuinely needs it.
        /// </summary>
        [Test]
        public void AskingForTheAmortisationData_BuildsTheSchedule()
        {
            var vm = BuildWeeklyLoan();
            vm.DepositPercentage = 25;
            Assume.That(vm.HomeLoanInfo.PaymentSummary?.PaymentTerms, Is.Empty);

            vm.UpdateAmortizationData();

            Assert.That(vm.HomeLoanInfo.PaymentSummary?.PaymentTerms, Is.Not.Empty,
                "the Schedule page and the PDF depend on this being built on demand");
        }

        /// <summary>
        /// Saving must not write the derived schedule. It is up to ~230 KB of JSON for a weekly
        /// loan, and a save is triggered by every change to any loan figure.
        /// </summary>
        [Test]
        public void SavedLoanJson_DoesNotContainThePaymentSchedule()
        {
            var vm = BuildWeeklyLoan();
            vm.UpdateAmortizationData();     // force the schedule to exist in memory

            Assume.That(vm.HomeLoanInfo.PaymentSummary?.PaymentTerms, Is.Not.Empty,
                "the schedule must be present in memory for this test to prove anything");

            var json = JsonSerializer.Serialize(vm.HomeLoanInfo);

            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Not.Contain("PaymentTerms"),
                    "the per-payment schedule is derived data and must not be persisted");
                Assert.That(json, Does.Not.Contain("PaymentAmortizationTerms"),
                    "the by-year amortisation is derived from the schedule and equally should not be");
                Assert.That(json, Does.Contain("PaymentSummary"),
                    "the figures themselves should still be saved");

                // A size ceiling as well as the name checks: it catches the schedule returning
                // under a different property name, and states the cost plainly. With the schedule
                // included this payload was over 200 KB, written on every save.
                Assert.That(json.Length, Is.LessThan(20_000),
                    $"the saved loan is {json.Length / 1024.0:F1} KB — derived data has crept back in");
            });
        }

        /// <summary>
        /// A changed loan must not serve the previous loan's schedule — the deferral only works
        /// because each recalculation produces a fresh, empty summary.
        /// </summary>
        [Test]
        public void AfterTheLoanChanges_TheScheduleIsRebuiltRatherThanStale()
        {
            var vm = BuildWeeklyLoan();
            vm.UpdateAmortizationData();
            var before = vm.HomeLoanInfo.PaymentSummary!.PaymentTerms!.Count;
            var beforePayment = vm.HomeLoanInfo.PaymentSummary.PaymentTerms[0].PaymentAmount;

            vm.LoanTermInYears = 15;          // halves the number of payments
            vm.UpdateAmortizationData();

            var after = vm.HomeLoanInfo.PaymentSummary!.PaymentTerms!.Count;

            Assert.Multiple(() =>
            {
                Assert.That(after, Is.Not.EqualTo(before), "the schedule did not follow the new term");
                Assert.That(after, Is.EqualTo(15 * 52));
                Assert.That(vm.HomeLoanInfo.PaymentSummary.PaymentTerms[0].PaymentAmount,
                    Is.Not.EqualTo(beforePayment).Within(0.001),
                    "a shorter term means a bigger payment, so a stale schedule would show here");
            });
        }

        // ── Live-edit coalescing ──────────────────────────────────────────────
        //
        // Typing in the loan wizard and the upfront-costs page was sluggish per character. The
        // arithmetic was not the cause: measured at 0.005 ms per change once the schedule stopped
        // being rebuilt (the tests above). The cause was TriggerPropertyChangedOnPropertyTab
        // raising 48 PropertyChanged events across 47 properties for every keystroke, each one
        // making MAUI re-resolve every binding subscribed to it — on pages sitting behind the
        // modal, which nobody can see. BeginLiveEdit/EndLiveEdit coalesce that into one pass.

        /// <summary>Typing "1234567" into the asset field, one keystroke at a time.</summary>
        private static int CountNotificationsWhileTyping(LoanViewModel vm, bool coalesce)
        {
            double[] keystrokes = [1, 12, 123, 1234, 12345, 123456, 1234567];

            var events = 0;
            vm.PropertyChanged += (_, _) => events++;

            if (coalesce) vm.BeginLiveEdit();
            foreach (var value in keystrokes) vm.PropertyAmount = value;

            return events;
        }

        [Test]
        public void TypingWhileLiveEditing_RaisesFarFewerNotifications()
        {
            var withoutCoalescing = CountNotificationsWhileTyping(BuildWeeklyLoan(), coalesce: false);
            var withCoalescing = CountNotificationsWhileTyping(BuildWeeklyLoan(), coalesce: true);

            Assert.Multiple(() =>
            {
                Assert.That(withoutCoalescing, Is.GreaterThan(300),
                    "the un-coalesced path should still be raising the full pass per keystroke");
                Assert.That(withCoalescing, Is.LessThan(withoutCoalescing / 4),
                    "coalescing should cut the per-keystroke fan-out by much more than a quarter");
            });
        }

        /// <summary>
        /// The chart collection is the most expensive single item on the per-keystroke path: it is
        /// a fresh ObservableCollection handed to a chart's ItemsSource, which rebuilds its series.
        /// </summary>
        [Test]
        public void LiveEditing_DoesNotRebuildTheChartCollection()
        {
            var vm = BuildWeeklyLoan();

            var chartRebuilds = 0;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(LoanViewModel.ChartPropertyValueWithInterestPayment))
                    chartRebuilds++;
            };

            vm.BeginLiveEdit();
            vm.PropertyAmount = 700_000;
            vm.PropertyAmount = 710_000;

            Assert.That(chartRebuilds, Is.Zero, "the chart was rebuilt behind a modal page");

            vm.EndLiveEdit();

            Assert.That(chartRebuilds, Is.EqualTo(1),
                "closing the page must rebuild it exactly once, or the chart shows the old loan");
        }

        /// <summary>
        /// Coalescing defers the notification, never the value — the open page reads these back to
        /// show what the model accepted.
        /// </summary>
        [Test]
        public void LiveEditing_StillWritesTheModel()
        {
            var vm = BuildWeeklyLoan();

            vm.BeginLiveEdit();
            vm.PropertyAmount = 1_234_567;

            Assert.That(vm.PropertyAmount, Is.EqualTo(1_234_567),
                "the model write must be immediate even though the notification is not");
        }

        [Test]
        public void EndLiveEdit_RunsTheDeferredPass()
        {
            var vm = BuildWeeklyLoan();

            vm.BeginLiveEdit();
            vm.PropertyAmount = 700_000;

            var afterFlush = 0;
            vm.PropertyChanged += (_, _) => afterFlush++;
            vm.EndLiveEdit();

            Assert.That(afterFlush, Is.GreaterThan(20),
                "the pages behind the modal never heard about anything that was typed");
        }

        [Test]
        public void EndLiveEdit_WithNothingChanged_RaisesNothing()
        {
            var vm = BuildWeeklyLoan();
            vm.BeginLiveEdit();

            var events = 0;
            vm.PropertyChanged += (_, _) => events++;
            vm.EndLiveEdit();

            Assert.That(events, Is.Zero,
                "opening and closing a data-entry page without typing should cost nothing");
        }

        /// <summary>
        /// Nesting must not let an inner scope flush early, or the outer one stops coalescing.
        /// </summary>
        [Test]
        public void NestedLiveEdit_OnlyTheOutermostScopeFlushes()
        {
            var vm = BuildWeeklyLoan();

            vm.BeginLiveEdit();
            vm.BeginLiveEdit();
            vm.PropertyAmount = 700_000;

            var events = 0;
            vm.PropertyChanged += (_, _) => events++;

            vm.EndLiveEdit();
            Assert.That(events, Is.Zero, "the inner scope flushed while the outer one was still open");
            Assert.That(vm.IsLiveEditing, Is.True);

            vm.EndLiveEdit();
            Assert.That(events, Is.GreaterThan(20), "the outermost scope did not flush");
            Assert.That(vm.IsLiveEditing, Is.False);
        }
    }
}
