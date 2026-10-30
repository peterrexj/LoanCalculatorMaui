using LoanCalculator.Core;
using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// Pins <see cref="LoanViewModel.ResetToDefaults"/>, which Settings' "delete loan data" calls.
    /// </summary>
    /// <remarks>
    /// Deleting loan data used to clear only <c>homeloandata.json</c>, so Settings reported the
    /// data gone while every other tab carried on calculating from the live view model — and a
    /// restart then showed different figures again. These tests assert the live model really is
    /// returned to its first-launch state.
    /// </remarks>
    [TestFixture]
    public class LoanResetToDefaultsTests
    {
        [OneTimeSetUp]
        public void GrantPremium() => SharedServiceCore.EntitlementOverrideForTests = true;

        [OneTimeTearDown]
        public void ClearPremium() => SharedServiceCore.EntitlementOverrideForTests = null;

        /// <summary>A loan with every field the user could have filled in carrying a real value.</summary>
        private static LoanViewModel BuildFullyPopulated()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = 7.25,
                        LoanTermInYears = 22,
                        TotalNumberPaymentPerYear = 12,
                    },
                    PropertyAmount = 650_000,
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] },
            };
            vm.MarkInitializationComplete();

            // A deposit typed directly — this is the split point that must not survive.
            vm.HomeLoanInfo.DepositAmountDirectInput = 130_000;

            vm.StampDuty = 24_000;
            vm.MortgageCharges = 900;
            vm.ConveyancerFee = 1_500;
            vm.BankFee = 600;
            vm.InspectionFee = 450;
            vm.OtherExpenses = 300;

            // A user-added asset running cost, on top of the five placeholders.
            vm.TransactionRecords!.Add("Strata fees", 450, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            vm.TransactionRecords.SumUpData();

            return vm;
        }

        [Test]
        public void Reset_RestoresTheFirstLaunchAssetAndDeposit()
        {
            var vm = BuildFullyPopulated();

            vm.ResetToDefaults();

            Assert.Multiple(() =>
            {
                Assert.That(vm.HomeLoanInfo.PropertyAmount, Is.EqualTo(1_000_000),
                    "asset value should be the sample figure a new install shows");
                Assert.That(vm.InterestRate, Is.EqualTo(SharedServiceCore.GetDefaultInterestRate()));
                Assert.That(vm.LoanTermInYears, Is.EqualTo(SharedServiceCore.GetDefaultLoanTermYears()));
            });
        }

        /// <summary>
        /// The ordering trap. <c>HomeLoanInformation.LoanAmount</c> consults
        /// <c>LoanAmountDirectInput</c> and then <c>DepositAmountDirectInput</c> <i>before</i> any
        /// percentage, so a reset that set <c>DepositPercentage = 10</c> while the old split point
        /// survived would be silently ignored and the figures would stay exactly as they were.
        /// </summary>
        /// <remarks>
        /// Asserts the split holds the DEFAULT-derived figures, not that it is zero:
        /// <c>ProcessDepositCalc</c> writes the computed deposit and loan back into the direct-input
        /// fields to keep all four in sync, so they are legitimately populated after a reset. What
        /// matters is whose numbers they are — 100,000 derived from the new 1,000,000 asset, not the
        /// 130,000 the user had typed.
        /// </remarks>
        [Test]
        public void Reset_ClearsTheSplitPointSoTheDefaultPercentageActuallyApplies()
        {
            var vm = BuildFullyPopulated();
            Assume.That(vm.HomeLoanInfo.DepositAmountDirectInput, Is.EqualTo(130_000),
                "the fixture must start with a directly-typed deposit for this to mean anything");

            vm.ResetToDefaults();

            Assert.Multiple(() =>
            {
                // 10% of the default 1,000,000 — the old 130,000 did not survive.
                Assert.That(vm.HomeLoanInfo.DepositAmountDirectInput, Is.EqualTo(100_000).Within(1),
                    "the deposit is still the user's old figure, so the default percentage was ignored");
                Assert.That(vm.HomeLoanInfo.LoanAmount, Is.EqualTo(900_000).Within(1),
                    "the loan should be 90% of the default asset value");
                Assert.That(vm.HomeLoanInfo.DepositPercentage, Is.EqualTo(10).Within(0.01));
            });
        }

        /// <summary>
        /// Upfront costs persist in the loan file but are not touched by <c>AddDefaultValues</c>,
        /// so a reset built on that alone would leave them behind — and they feed
        /// <c>PropertyTotalAmount</c>, so the loan would stay inflated.
        /// </summary>
        [Test]
        public void Reset_ClearsEveryUpfrontCost()
        {
            var vm = BuildFullyPopulated();
            Assume.That(vm.HomeLoanInfo.OtherExpenseTotalAmount, Is.GreaterThan(0));

            vm.ResetToDefaults();

            Assert.That(vm.HomeLoanInfo.OtherExpenseTotalAmount, Is.Zero,
                "stamp duty, mortgage, conveyance, bank, inspection and other fees should all be gone");
            Assert.That(vm.HomeLoanInfo.PropertyTotalAmount, Is.EqualTo(1_000_000),
                "with no upfront costs the total should equal the asset value");
        }

        /// <summary>
        /// Asset running costs live in the loan file, so leaving them meant Budget's Asset costs tab
        /// still listed figures whose file had been deleted.
        /// </summary>
        [Test]
        public void Reset_DropsUserAddedAssetCostsAndRestoresThePlaceholders()
        {
            var vm = BuildFullyPopulated();
            Assume.That(vm.TransactionRecords!.IncomeExpenseEntries!.Any(e => e.Name == "Strata fees"), Is.True);

            vm.ResetToDefaults();

            var entries = vm.TransactionRecords!.IncomeExpenseEntries!;

            Assert.Multiple(() =>
            {
                Assert.That(entries.Any(e => e.Name == "Strata fees"), Is.False,
                    "a user-entered running cost survived the delete");
                Assert.That(entries.All(e => e.Amount == 0), Is.True,
                    "the placeholders a new install gets are all zero");
                Assert.That(entries.Select(e => e.Name), Does.Contain("Maintenance cost"),
                    "the five first-launch placeholder rows should be back");
            });
        }

        /// <summary>
        /// Income and expenses belong to the Budget tab and have their own delete actions, so
        /// deleting the LOAN must not claim they are gone — the cached flag gates affordability
        /// across Loan and What If.
        /// </summary>
        [Test]
        public void Reset_LeavesTheIncomeAndExpenseFlagAlone()
        {
            var vm = BuildFullyPopulated();
            vm.HasIncomeExpensesRecorded = true;

            vm.ResetToDefaults();

            Assert.That(vm.HasIncomeExpensesRecorded, Is.True,
                "deleting the loan must not silently disable affordability on the other tabs");
        }

        /// <summary>
        /// The whole point: a reset must be indistinguishable from a fresh install, or the app shows
        /// different figures either side of a restart.
        /// </summary>
        [Test]
        public void Reset_MatchesAFreshlyInitialisedLoan()
        {
            var reset = BuildFullyPopulated();
            reset.ResetToDefaults();

            var fresh = new LoanViewModel();
            fresh.MarkInitializationComplete();
            fresh.InitializeViewData();
            fresh.AddDefaultToExpenses();
            fresh.AddDefaultValues();

            Assert.Multiple(() =>
            {
                Assert.That(reset.HomeLoanInfo.PropertyAmount, Is.EqualTo(fresh.HomeLoanInfo.PropertyAmount));
                Assert.That(reset.HomeLoanInfo.LoanAmount, Is.EqualTo(fresh.HomeLoanInfo.LoanAmount).Within(1));
                Assert.That(reset.HomeLoanInfo.OtherExpenseTotalAmount,
                    Is.EqualTo(fresh.HomeLoanInfo.OtherExpenseTotalAmount));
                Assert.That(reset.InterestRate, Is.EqualTo(fresh.InterestRate));
                Assert.That(reset.LoanTermInYears, Is.EqualTo(fresh.LoanTermInYears));
                Assert.That(reset.TransactionRecords!.IncomeExpenseEntries!.Count,
                    Is.EqualTo(fresh.TransactionRecords!.IncomeExpenseEntries!.Count));
            });
        }

        /// <summary>
        /// The prompt is one-shot: the wizard should open once after a delete, not on every
        /// subsequent visit to the Loan tab.
        /// </summary>
        [Test]
        public void LoanSetupPrompt_IsConsumedExactlyOnce()
        {
            // Drain anything a previous test armed.
            while (SharedServiceCore.ConsumeLoanSetupPrompt()) { }

            Assert.That(SharedServiceCore.ConsumeLoanSetupPrompt(), Is.False, "nothing requested yet");

            SharedServiceCore.RequestLoanSetupPrompt();

            Assert.That(SharedServiceCore.ConsumeLoanSetupPrompt(), Is.True, "the first visit should prompt");
            Assert.That(SharedServiceCore.ConsumeLoanSetupPrompt(), Is.False, "later visits must not");
        }
    }
}
