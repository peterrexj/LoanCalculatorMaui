using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    /// <summary>
    /// The loan term and interest rate must be bounded wherever they are written, not only in the
    /// +/- click handlers.
    /// </summary>
    /// <remarks>
    /// Those handlers bound what a user can step up to and nothing else. Two paths went around
    /// them: a value restored from <c>homeloandata.json</c>, because
    /// <c>CopyPropertiesFrom</c> assigns the model directly to get past the
    /// <c>HasInitialized</c> guards; and any code assigning the property. A 200-year loan in that
    /// file loaded and calculated, with the stepper only stopping the user making it longer.
    /// </remarks>
    [TestFixture]
    public class LoanTermBoundsTests
    {
        [OneTimeSetUp]
        public void GrantPremium() => SharedServiceCore.EntitlementOverrideForTests = true;

        [OneTimeTearDown]
        public void ClearPremium() => SharedServiceCore.EntitlementOverrideForTests = null;

        private static LoanViewModel BuildInitialized(int termYears = 30, double rate = 5.0)
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = rate,
                        LoanTermInYears = termYears,
                        TotalNumberPaymentPerYear = 12,
                    },
                    PropertyAmount = 650_000,
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] },
            };
            vm.MarkInitializationComplete();
            return vm;
        }

        /// <summary>A loan as it would arrive from disk, with an out-of-range term and rate.</summary>
        private static LoanViewModel BuildPersistedLoan(int termYears, double rate)
        {
            var onDisk = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = rate,
                        LoanTermInYears = termYears,
                        TotalNumberPaymentPerYear = 12,
                    },
                    PropertyAmount = 650_000,
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] },
            };

            var loaded = BuildInitialized();
            loaded.CopyPropertiesFrom(onDisk);
            return loaded;
        }

        // ── The property setter ───────────────────────────────────────────────

        [TestCase(51)]
        [TestCase(200)]
        [TestCase(int.MaxValue)]
        public void Term_AboveTheMaximum_IsClampedDownByTheSetter(int requested)
        {
            var vm = BuildInitialized();

            vm.LoanTermInYears = requested;

            Assert.That(vm.LoanTermInYears, Is.EqualTo(SharedServiceCore.GetMaxLoanTermYears()));
        }

        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(int.MinValue)]
        public void Term_BelowOne_IsClampedUpByTheSetter(int requested)
        {
            var vm = BuildInitialized();

            vm.LoanTermInYears = requested;

            Assert.That(vm.LoanTermInYears, Is.EqualTo(SharedServiceCore.MinLoanTermYears));
        }

        [Test]
        public void Term_WithinRange_IsAcceptedUnchanged()
        {
            var vm = BuildInitialized();

            vm.LoanTermInYears = 17;

            Assert.That(vm.LoanTermInYears, Is.EqualTo(17));
        }

        // ── The load path, which bypasses every setter ────────────────────────

        [Test]
        public void Term_RestoredFromDisk_IsBroughtIntoRange()
        {
            var vm = BuildPersistedLoan(termYears: 200, rate: 5.0);

            Assert.That(vm.LoanTermInYears, Is.EqualTo(SharedServiceCore.GetMaxLoanTermYears()),
                "a term from a restored or hand-edited file was trusted verbatim");
        }

        [Test]
        public void Term_RestoredFromDiskAsZero_IsBroughtIntoRange()
        {
            var vm = BuildPersistedLoan(termYears: 0, rate: 5.0);

            Assert.That(vm.LoanTermInYears, Is.EqualTo(SharedServiceCore.MinLoanTermYears));
        }

        [TestCase(999.0)]
        [TestCase(-3.0)]
        public void Rate_RestoredFromDisk_IsBroughtIntoRange(double storedRate)
        {
            var vm = BuildPersistedLoan(termYears: 30, rate: storedRate);

            Assert.That(vm.InterestRate,
                Is.InRange(SharedServiceCore.MinInterestRate, SharedServiceCore.MaxInterestRate));
        }

        [Test]
        public void ValidPersistedLoan_SurvivesTheLoadUntouched()
        {
            var vm = BuildPersistedLoan(termYears: 22, rate: 6.75);

            Assert.Multiple(() =>
            {
                Assert.That(vm.LoanTermInYears, Is.EqualTo(22));
                Assert.That(vm.InterestRate, Is.EqualTo(6.75));
            });
        }

        // ── The settings bounds ───────────────────────────────────────────────

        /// <summary>
        /// The settings getters now clamp as well as the setters, so a preference written by an
        /// older build or restored from a device backup cannot widen the range the steppers allow.
        /// </summary>
        /// <remarks>
        /// That clamp is NOT asserted here, and cannot be: <c>Preferences.Set</c> throws
        /// <c>NotImplementedInReferenceAssemblyException</c> without a MAUI host, which is why every
        /// getter in <c>SharedServiceCore</c> is wrapped in a try/catch returning its hard-coded
        /// default. Seeding a preference to an out-of-range value is therefore impossible from a
        /// unit test. Verifying it needs a device, or a Preferences seam that does not exist today.
        ///
        /// What IS covered is the part that bites: the property setter and the load path, both
        /// above. Those are where an out-of-range term actually reached the calculation.
        /// </remarks>
        [Test]
        public void GetMaxLoanTermYears_ReturnsAValueWithinTheAbsoluteBounds()
            => Assert.That(SharedServiceCore.GetMaxLoanTermYears(),
                Is.InRange(SharedServiceCore.MinConfigurableMaxLoanTermYears,
                           SharedServiceCore.AbsoluteMaxLoanTermYears));

        [Test]
        public void GetDefaultLoanTermYears_NeverExceedsTheConfiguredMaximum()
            => Assert.That(SharedServiceCore.GetDefaultLoanTermYears(),
                Is.InRange(SharedServiceCore.MinLoanTermYears, SharedServiceCore.GetMaxLoanTermYears()));

        [Test]
        public void TheCeiling_IsFifty()
            => Assert.That(SharedServiceCore.AbsoluteMaxLoanTermYears, Is.EqualTo(50),
                "the agreed hard limit on a loan term");
    }
}
