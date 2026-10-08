using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculator.UnitTests.Models.ViewModels.PrimaryModels
{
    [TestFixture]
    public class LoanViewModelTests
    {
        private LoanViewModel _vm;

        /// <summary>
        /// Builds a fully-initialized LoanViewModel using public API only.
        /// PageHelper / SharedServiceCore guards must be off so property triggers run.
        /// </summary>
        private static LoanViewModel BuildInitialized(double propertyAmount = 1_000_000, double depositDirect = 100_000,
            double interestRate = 5.0, int termYears = 30)
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();

            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    {
                        InterestRate = interestRate,
                        LoanTermInYears = termYears,
                        TotalNumberPaymentPerYear = 12
                    },
                    PropertyAmount = propertyAmount
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.HomeLoanInfo.DepositAmountDirectInput = depositDirect;
            vm.MarkInitializationComplete();
            return vm;
        }

        [SetUp]
        public void Setup()
        {
            PageHelper.PageLoadingComplete();
            SharedServiceCore.LoadSafeOff();
            _vm = BuildInitialized();
        }

        // ── Cost increases must grow the loan, never the deposit ──────────────
        //
        // The deposit is money the user has; the loan is what they must borrow. So when the asset
        // price or any upfront cost rises, the deposit is unchanged and the loan absorbs the
        // difference. The model stores a total plus one split point, so something has to give —
        // these tests pin WHICH.
        //
        // This was backwards: PropertyAmount and all six expense setters used to self-assign
        // HomeLoanInfo.LoanAmountDirectInput, which preserves the loan and re-derives the deposit
        // (ProcessDepositCalc branch 3). The observed effect was that the loan NEVER changed and
        // every cost increase was quietly credited to the user's savings:
        //   asset 600k, deposit 100k, loan 500k  →  +20k upfront  →  deposit 120k, loan 500k
        //                                        →  +50k asset    →  deposit 170k, loan 500k
        // They now self-assign the deposit instead (branch 4: loan = total - deposit).

        [Test]
        public void RaisingUpfrontCosts_GrowsTheLoanAndLeavesTheDepositAlone()
        {
            var vm = BuildInitialized(propertyAmount: 600_000, depositDirect: 100_000);

            vm.OtherExpenses = 20_000;

            Assert.Multiple(() =>
            {
                Assert.That(vm.DepositAmountDirectInput, Is.EqualTo(100_000).Within(1),
                    "upfront costs must not be added to the deposit");
                Assert.That(vm.HomeLoanInfo.LoanAmountDirectInput, Is.EqualTo(520_000).Within(1),
                    "the loan must absorb the extra cost");
            });
        }

        [Test]
        public void RaisingTheAssetPrice_GrowsTheLoanAndLeavesTheDepositAlone()
        {
            var vm = BuildInitialized(propertyAmount: 600_000, depositDirect: 100_000);

            vm.PropertyAmount = 650_000;

            Assert.Multiple(() =>
            {
                Assert.That(vm.DepositAmountDirectInput, Is.EqualTo(100_000).Within(1),
                    "a higher asset price must not inflate the deposit");
                Assert.That(vm.HomeLoanInfo.LoanAmountDirectInput, Is.EqualTo(550_000).Within(1));
            });
        }

        [Test]
        public void RaisingCostsRepeatedly_KeepsTheDepositFixed()
        {
            var vm = BuildInitialized(propertyAmount: 600_000, depositDirect: 100_000);

            vm.OtherExpenses = 20_000;
            vm.PropertyAmount = 650_000;
            vm.BankFee = 5_000;

            Assert.Multiple(() =>
            {
                Assert.That(vm.DepositAmountDirectInput, Is.EqualTo(100_000).Within(1));
                Assert.That(vm.HomeLoanInfo.LoanAmountDirectInput,
                    Is.EqualTo(vm.HomeLoanInfo.PropertyTotalAmount - 100_000).Within(1),
                    "the loan is always the total minus the untouched deposit");
            });
        }

        [TearDown]
        public void TearDown()
        {
            SharedServiceCore.LoadSafeOff();
            PageHelper.PageLoadingComplete();
        }

        // ── NumberToWordsPublic ───────────────────────────────────────────────

        [Test]
        public void NumberToWordsPublic_Zero_ReturnsEmpty()
            => Assert.That(LoanViewModel.NumberToWordsPublic(0), Is.Empty);

        [Test]
        public void NumberToWordsPublic_Negative_ReturnsEmpty()
            => Assert.That(LoanViewModel.NumberToWordsPublic(-1), Is.Empty);

        [Test]
        public void NumberToWordsPublic_One_ReturnsOne()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1), Is.EqualTo("One"));

        [Test]
        public void NumberToWordsPublic_Fifteen_ReturnsFifteen()
            => Assert.That(LoanViewModel.NumberToWordsPublic(15), Is.EqualTo("Fifteen"));

        [Test]
        public void NumberToWordsPublic_TwentyOne_ReturnsTwentyOne()
            => Assert.That(LoanViewModel.NumberToWordsPublic(21), Is.EqualTo("Twenty One"));

        [Test]
        public void NumberToWordsPublic_OneHundred_ReturnsOneHundred()
            => Assert.That(LoanViewModel.NumberToWordsPublic(100), Is.EqualTo("One Hundred"));

        [Test]
        public void NumberToWordsPublic_OneThousand_ReturnsOneThousand()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1_000), Is.EqualTo("One Thousand"));

        [Test]
        public void NumberToWordsPublic_OneMillion_ReturnsOneMillion()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1_000_000), Is.EqualTo("One Million"));

        [Test]
        public void NumberToWordsPublic_FiveHundredThousand_ContainsHundredThousand()
            => Assert.That(LoanViewModel.NumberToWordsPublic(500_000), Does.Contain("Hundred").And.Contain("Thousand"));

        [Test]
        public void NumberToWordsPublic_OneBillion_ReturnsOneBillion()
            => Assert.That(LoanViewModel.NumberToWordsPublic(1_000_000_000), Is.EqualTo("One Billion"));

        [Test]
        public void NumberToWordsPublic_750000_IncludesSevenHundredFifty()
            => Assert.That(LoanViewModel.NumberToWordsPublic(750_000), Does.Contain("Seven").And.Contain("Fifty"));

        // ── InterestRateFormatted ─────────────────────────────────────────────

        [Test]
        public void InterestRateFormatted_WholeNumber_OmitsDecimals()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate = 5.0;
            Assert.That(_vm.InterestRateFormatted, Is.EqualTo("5%"));
        }

        [Test]
        public void InterestRateFormatted_WithDecimal_ShowsTwoDecimals()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate = 5.25;
            Assert.That(_vm.InterestRateFormatted, Is.EqualTo("5.25%"));
        }

        [Test]
        public void InterestRateFormatted_HalfPercent_ShowsTwoDecimals()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate = 6.5;
            Assert.That(_vm.InterestRateFormatted, Is.EqualTo("6.50%"));
        }

        // ── RepaymentFrequencySelectedIndex ────────────────────────────────────

        [Test]
        public void RepaymentFrequencySelectedIndex_12PerYear_ReturnsZero()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
            Assert.That(_vm.RepaymentFrequencySelectedIndex, Is.EqualTo(0));
        }

        [Test]
        public void RepaymentFrequencySelectedIndex_24PerYear_ReturnsOne()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 24;
            Assert.That(_vm.RepaymentFrequencySelectedIndex, Is.EqualTo(1));
        }

        [Test]
        public void RepaymentFrequencySelectedIndex_52PerYear_ReturnsTwo()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 52;
            Assert.That(_vm.RepaymentFrequencySelectedIndex, Is.EqualTo(2));
        }

        [Test]
        public void RepaymentFrequencySelectedIndex_Unknown_ReturnsZero()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 26;
            Assert.That(_vm.RepaymentFrequencySelectedIndex, Is.EqualTo(0));
        }

        [Test]
        public void RepaymentFrequencySelectedIndex_SetZero_StoresMonthly()
        {
            _vm.RepaymentFrequencySelectedIndex = 0;
            Assert.That(_vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear, Is.EqualTo(12));
        }

        [Test]
        public void RepaymentFrequencySelectedIndex_SetOne_StoresFortnightly()
        {
            _vm.RepaymentFrequencySelectedIndex = 1;
            Assert.That(_vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear, Is.EqualTo(24));
        }

        [Test]
        public void RepaymentFrequencySelectedIndex_SetTwo_StoresWeekly()
        {
            _vm.RepaymentFrequencySelectedIndex = 2;
            Assert.That(_vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear, Is.EqualTo(52));
        }

        // ── RepaymentFrequencySelected ─────────────────────────────────────────

        [Test]
        public void RepaymentFrequencySelected_Monthly_ContainsMonthly()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 12;
            Assert.That(_vm.RepaymentFrequencySelected, Does.Contain("monthly"));
        }

        [Test]
        public void RepaymentFrequencySelected_Fortnightly_ContainsFortnightly()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 24;
            Assert.That(_vm.RepaymentFrequencySelected, Does.Contain("fortnightly"));
        }

        [Test]
        public void RepaymentFrequencySelected_Weekly_ContainsWeekly()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.TotalNumberPaymentPerYear = 52;
            Assert.That(_vm.RepaymentFrequencySelected, Does.Contain("weekly"));
        }

        // ── Amortization balance subtitle ─────────────────────────────────────

        [Test]
        public void AmortizationBalanceSubtitle_ContainsLoanTermInYears()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears = 25;
            Assert.That(_vm.AmortizationBalanceSubtitle, Does.Contain("25"));
        }

        [Test]
        public void AmortizationBalanceSubtitle_SingularYear_NoTrailingS()
        {
            _vm.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears = 1;
            Assert.That(_vm.AmortizationBalanceSubtitle, Does.Contain("1 yr").And.Not.Contain("yrs"));
        }
        // ── TotalMonthlyOverallExpense does not throw ─────────────────────────

        [Test]
        public void TotalMonthlyOverallExpense_NoPaymentSummary_DoesNotThrow()
        {
            _vm.HomeLoanInfo.PaymentSummary = null;
            _vm.TransactionRecords = new Incomes { IncomeExpenseEntries = [] };
            _vm.TransactionRecords.SumUpData();
            Assert.DoesNotThrow(() => { var _ = _vm.TotalMonthlyOverallExpense; });
        }

        // ── IsStampDutyToggleEnabled ──────────────────────────────────────────

        [Test]
        public void IsStampDutyToggleEnabled_AustralianModeOff_IsTrue()
        {
            _vm.IsAustralianModeEnabled = false;
            Assert.That(_vm.IsStampDutyToggleEnabled, Is.True);
        }

        [Test]
        public void IsStampDutyToggleEnabled_AustralianModeOn_IsFalse()
        {
            _vm.IsAustralianModeEnabled = true;
            Assert.That(_vm.IsStampDutyToggleEnabled, Is.False);
        }

        [Test]
        public void ShowAustralianStateSelectorOnStampDuty_FollowsAustralianModeEnabled()
        {
            _vm.IsAustralianModeEnabled = true;
            Assert.That(_vm.ShowAustralianStateSelectorOnStampDuty, Is.True);

            _vm.IsAustralianModeEnabled = false;
            Assert.That(_vm.ShowAustralianStateSelectorOnStampDuty, Is.False);
        }

        [Test]
        public void ShowStampDutyInput_AustralianModeOn_IsTrue()
        {
            _vm.IsAustralianModeEnabled = true;
            _vm.IsStampDutyEnabled = false;
            Assert.That(_vm.ShowStampDutyInput, Is.True);
        }

        [Test]
        public void ShowStampDutyInput_StampDutyEnabledOnly_IsTrue()
        {
            _vm.IsAustralianModeEnabled = false;
            _vm.IsStampDutyEnabled = true;
            Assert.That(_vm.ShowStampDutyInput, Is.True);
        }

        [Test]
        public void ShowStampDutyInput_BothFalse_IsFalse()
        {
            _vm.IsAustralianModeEnabled = false;
            _vm.IsStampDutyEnabled = false;
            Assert.That(_vm.ShowStampDutyInput, Is.False);
        }

        // ── AustraliaStateSelectedIndex (nullable int?) ───────────────────────

        [Test]
        public void AustraliaStateSelectedIndex_DefaultIsNull()
        {
            // No state selected on a fresh VM — index should be null, not 0
            Assert.That(_vm.AustraliaStateSelectedIndex, Is.Null);
        }

        [Test]
        public void AustraliaStateSelectedIndex_SetNull_IsNoOp()
        {
            // Null assignment must not throw and must leave the stamp duty unchanged
            var stampDutyBefore = _vm.HomeLoanInfo.StampDuty.AustraliaStateSelected;
            Assert.DoesNotThrow(() => _vm.AustraliaStateSelectedIndex = null);
            Assert.That(_vm.HomeLoanInfo.StampDuty.AustraliaStateSelected, Is.EqualTo(stampDutyBefore));
        }

        [Test]
        public void AustraliaStateSelectedIndex_SetNull_FiresNoPropertyChanged()
        {
            var changed = new List<string>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            _vm.AustraliaStateSelectedIndex = null;
            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void AustraliaStateSelectedIndex_SetValidIndex_UpdatesStateSelected()
        {
            // Index 0 maps to the first AustralianStatesEnum value
            _vm.IsAustralianModeEnabled = true;
            _vm.AustraliaStateSelectedIndex = 0;
            Assert.That(_vm.HomeLoanInfo.StampDuty.AustraliaStateSelected, Is.Not.Null);
        }

        [Test]
        public void EventsTriggerStampDutyUpdate_NoStateSelected_DoesNotThrow()
        {
            // AustraliaStateSelected is null by default — calling the trigger must not throw
            _vm.HomeLoanInfo.StampDuty.AustraliaStateSelected = null;
            Assert.DoesNotThrow(() => _vm.EventsTriggerStampDutyUpdate());
        }

        // ── CopyPropertiesFrom ────────────────────────────────────────────────

        [Test]
        public void CopyPropertiesFrom_Null_ThrowsArgumentNullException()
        {
            var vm = new LoanViewModel();
            Assert.Throws<ArgumentNullException>(() => vm.CopyPropertiesFrom(null!));
        }

        [Test]
        public void CopyPropertiesFrom_CopiesHomeLoanInfoReference()
        {
            var source = BuildInitialized(800_000, 80_000);
            var target = new LoanViewModel();
            target.CopyPropertiesFrom(source);
            Assert.That(target.HomeLoanInfo, Is.SameAs(source.HomeLoanInfo));
        }

        // ── AddDefaultValues ──────────────────────────────────────────────────

        [Test]
        public void AddDefaultValues_SetsPropertyAmountToOneMillion()
        {
            var vm = BuildInitialized(0, 0);
            vm.AddDefaultValues();
            Assert.That(vm.HomeLoanInfo.PropertyAmount, Is.EqualTo(1_000_000));
        }

        [Test]
        public void AddDefaultValues_SetsInterestRateTo5()
        {
            var vm = BuildInitialized(0, 0);
            vm.AddDefaultValues();
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.InterestRate, Is.EqualTo(5.0));
        }

        // ── AddDefaultToExpenses ──────────────────────────────────────────────

        [Test]
        public void AddDefaultToExpenses_AddsExpectedDefaultEntries()
        {
            _vm.AddDefaultToExpenses();
            Assert.That(_vm.TransactionRecords!.IncomeExpenseEntries!.Count, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void AddDefaultToExpenses_ContainsMaintenanceCost()
        {
            _vm.AddDefaultToExpenses();
            Assert.That(_vm.TransactionRecords!.Exists("Maintenance cost"), Is.True);
        }

        // ── DepositAmountStrFormatted / LoanAmountStrFormatted ─────────────────

        [Test]
        public void DepositAmountStrFormatted_ReflectsDepositAmountDirectInput()
        {
            _vm.HomeLoanInfo.DepositAmountDirectInput = 150_000;
            Assert.That(_vm.DepositAmountStrFormatted, Does.Contain("150"));
        }

        [Test]
        public void LoanAmountStrFormatted_ReflectsLoanAmountDirectInput()
        {
            _vm.HomeLoanInfo.LoanAmountDirectInput = 850_000;
            Assert.That(_vm.LoanAmountStrFormatted, Does.Contain("850"));
        }

        // ── PropertyAmountWords / LoanAmountWords ─────────────────────────────

        [Test]
        public void PropertyAmountWords_ZeroPropertyAmount_ReturnsEmpty()
        {
            _vm.HomeLoanInfo.PropertyAmount = 0;
            Assert.That(_vm.PropertyAmountWords, Is.Empty);
        }

        [Test]
        public void PropertyAmountWords_OneMillion_ContainsMillionWord()
        {
            _vm.HomeLoanInfo.PropertyAmount = 1_000_000;
            Assert.That(_vm.PropertyAmountWords, Does.Contain("Million"));
        }

        [Test]
        public void LoanAmountWords_HalfMillion_ContainsHundredThousand()
        {
            _vm.HomeLoanInfo.LoanAmountDirectInput = 500_000;
            Assert.That(_vm.LoanAmountWords, Does.Contain("Thousand"));
        }

        // ── PropertyAmountFormatted ───────────────────────────────────────────

        [Test]
        public void PropertyAmountFormatted_IncludesCurrencySymbol()
        {
            _vm.HomeLoanInfo.PropertyAmount = 750_000;
            Assert.That(_vm.PropertyAmountFormatted, Does.Contain("750"));
        }

        // ── AffordabilityTextDescription ──────────────────────────────────────

        [Test]
        public void AffordabilityTextDescription_NoIncomeExpenses_MentionsRecord()
        {
            _vm.HasIncomeExpensesRecorded = false;
            Assert.That(_vm.AffordabilityTextDescription, Does.Contain("record"));
        }

        [Test]
        public void AffordabilityTextDescription_WithIncomeExpenses_QualifiesAsEstimate()
        {
            // The caption is deliberately worded as an estimate rather than an
            // "affordability status" verdict (Play Store advisory-safe reframe).
            _vm.HasIncomeExpensesRecorded = true;
            Assert.That(_vm.AffordabilityTextDescription, Does.Contain("estimate"));
        }

        // ── PropertyChanged on key loan properties ─────────────────────────────

        [Test]
        public void IsStampDutyEnabled_Set_FiresShowStampDutyInputChanged()
        {
            var changed = new List<string>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _vm.IsStampDutyEnabled = true;

            Assert.That(changed, Contains.Item(nameof(_vm.ShowStampDutyInput)));
        }

        [Test]
        public void IsAustralianModeEnabled_Set_FiresMultipleRelatedProperties()
        {
            var changed = new List<string>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _vm.IsAustralianModeEnabled = true;

            Assert.That(changed, Contains.Item(nameof(_vm.ShowStampDutyInput)));
            Assert.That(changed, Contains.Item(nameof(_vm.IsStampDutyToggleEnabled)));
            Assert.That(changed, Contains.Item(nameof(_vm.ShowAustralianStateSelectorOnStampDuty)));
        }
        // ── AddDefaultValues ── LoanTermInYears ───────────────────────────────

        [Test]
        public void AddDefaultValues_SetsLoanTermTo30Years()
        {
            var vm = BuildInitialized(0, 0);
            vm.AddDefaultValues();
            Assert.That(vm.HomeLoanInfo.HomeLoanRepaymentRequest.LoanTermInYears, Is.EqualTo(30));
        }

        // ── IsDepositPercentageSliderEnabled ──────────────────────────────────

        [Test]
        public void IsDepositPercentageSliderEnabled_DefaultFalse()
        {
            var vm = new LoanViewModel();
            Assert.That(vm.IsDepositPercentageSliderEnabled, Is.False);
        }

        [Test]
        public void IsDepositPercentageSliderEnabled_SetTrue_FiresPropertyChanged()
        {
            var changed = new List<string>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _vm.IsDepositPercentageSliderEnabled = true;

            Assert.That(changed, Contains.Item(nameof(_vm.IsDepositPercentageSliderEnabled)));
        }
        // ── New popup / wizard visibility properties ──────────────────────────

        [Test]
        public void IsUpfrontInputVisible_DefaultFalse()
        {
            Assert.That(_vm.IsUpfrontInputVisible, Is.False);
        }

        [Test]
        public void IsUpfrontInputVisible_SetTrue_FiresPropertyChanged()
        {
            var changed = new List<string>();
            _vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            _vm.IsUpfrontInputVisible = true;
            Assert.That(changed, Does.Contain(nameof(_vm.IsUpfrontInputVisible)));
            Assert.That(_vm.IsUpfrontInputVisible, Is.True);
        }
    }
}
