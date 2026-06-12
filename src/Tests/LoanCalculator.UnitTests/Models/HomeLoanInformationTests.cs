using LoanCalculator.Core.Models;

namespace LoanCalculator.UnitTests.Models
{
    [TestFixture]
    public class HomeLoanInformationTests
    {
        private HomeLoanInformation _info;

        [SetUp]
        public void Setup()
        {
            _info = new HomeLoanInformation();
            _info.PropertyAmount = 500000;
        }

        // ── LoanAmount computed getter ────────────────────────────────────────

        [Test]
        public void LoanAmount_ZeroPropertyAmount_ReturnsZero()
        {
            _info.PropertyAmount = 0;
            Assert.That(_info.LoanAmount, Is.EqualTo(0));
        }

        [Test]
        public void LoanAmount_NoInputs_EqualsTotalPropertyAmount()
        {
            // No deposit or loan inputs set — defaults to full property amount
            Assert.That(_info.LoanAmount, Is.EqualTo(_info.PropertyTotalAmount));
        }

        [Test]
        public void LoanAmount_LoanDirectInputSet_ReturnsDirectInput()
        {
            _info.LoanAmountDirectInput = 400000;
            Assert.That(_info.LoanAmount, Is.EqualTo(400000));
        }

        [Test]
        public void LoanAmount_DepositDirectInputSet_IsPropertyMinusDeposit()
        {
            _info.DepositAmountDirectInput = 100000;
            Assert.That(_info.LoanAmount, Is.EqualTo(_info.PropertyTotalAmount - 100000));
        }

        [Test]
        public void LoanAmount_LoanPercentageSet_IsPercentageOfTotal()
        {
            _info.LoanAmountPercentage = 80;
            double expected = (_info.PropertyTotalAmount * 80) / 100;
            Assert.That(_info.LoanAmount, Is.EqualTo(expected).Within(0.01));
        }

        [Test]
        public void LoanAmount_DepositPercentageSet_IsPropertyMinusDepositPercentage()
        {
            _info.DepositPercentage = 20;
            double expected = _info.PropertyTotalAmount - ((_info.PropertyTotalAmount * 20) / 100);
            Assert.That(_info.LoanAmount, Is.EqualTo(expected).Within(0.01));
        }

        // ── LoanAmountDirectInput setter ─────────────────────────────────────

        [Test]
        public void LoanAmountDirectInput_ZeroValue_LoanIsZeroDepositIsTotal()
        {
            // When zero is set, byLoanDirectOnZero=true path:
            // _loanAmountDirectInput = 0, _depositAmountDirectInput = PropertyTotalAmount - 0 = total
            _info.LoanAmountDirectInput = 400000;
            _info.LoanAmountDirectInput = 0;

            Assert.That(_info.LoanAmountDirectInput, Is.EqualTo(0));
            Assert.That(_info.DepositAmountDirectInput, Is.EqualTo(_info.PropertyTotalAmount));
        }

        [Test]
        public void LoanAmountDirectInput_NegativeValue_ClearsAllFields()
        {
            _info.LoanAmountDirectInput = -100;
            Assert.That(_info.LoanAmountDirectInput, Is.EqualTo(0));
        }

        [Test]
        public void LoanAmountDirectInput_ExceedsTotal_ClampsToTotal()
        {
            double total = _info.PropertyTotalAmount;
            _info.LoanAmountDirectInput = total + 999999;

            Assert.That(_info.LoanAmountDirectInput, Is.EqualTo(total));
        }

        [Test]
        public void LoanAmountDirectInput_ValidValue_UpdatesDepositDirectInput()
        {
            _info.LoanAmountDirectInput = 400000;
            double expectedDeposit = _info.PropertyTotalAmount - 400000;

            Assert.That(_info.DepositAmountDirectInput, Is.EqualTo(expectedDeposit).Within(0.01));
        }

        [Test]
        public void LoanAmountDirectInput_ValidValue_UpdatesPercentages()
        {
            _info.LoanAmountDirectInput = 400000;
            double total = _info.PropertyTotalAmount;

            double expectedLoanPct = 100 - ((total - 400000) / total * 100);
            Assert.That(_info.LoanAmountPercentage, Is.EqualTo(expectedLoanPct).Within(0.01));
        }

        // ── DepositAmountDirectInput setter ──────────────────────────────────

        [Test]
        public void DepositAmountDirectInput_ZeroValue_DepositIsZeroLoanIsTotal()
        {
            // When zero is set, byDepositDirectOnZero=true path:
            // _depositAmountDirectInput = 0, _loanAmountDirectInput = PropertyTotalAmount - 0 = total
            _info.DepositAmountDirectInput = 100000;
            _info.DepositAmountDirectInput = 0;

            Assert.That(_info.DepositAmountDirectInput, Is.EqualTo(0));
            Assert.That(_info.LoanAmountDirectInput, Is.EqualTo(_info.PropertyTotalAmount));
        }

        [Test]
        public void DepositAmountDirectInput_ExceedsTotal_ClampsToTotal()
        {
            double total = _info.PropertyTotalAmount;
            _info.DepositAmountDirectInput = total + 1;
            Assert.That(_info.DepositAmountDirectInput, Is.EqualTo(total));
        }

        [Test]
        public void DepositAmountDirectInput_ValidValue_UpdatesLoanDirectInput()
        {
            _info.DepositAmountDirectInput = 100000;
            double expectedLoan = _info.PropertyTotalAmount - 100000;
            Assert.That(_info.LoanAmountDirectInput, Is.EqualTo(expectedLoan).Within(0.01));
        }

        [Test]
        public void DepositAmountDirectInput_ValidValue_UpdatesPercentages()
        {
            _info.DepositAmountDirectInput = 100000;
            double total = _info.PropertyTotalAmount;
            double expectedDepositPct = (100000 / total) * 100;
            Assert.That(_info.DepositPercentage, Is.EqualTo(expectedDepositPct).Within(0.01));
        }

        // ── LoanAmountPercentage setter ───────────────────────────────────────

        [Test]
        public void LoanAmountPercentage_ZeroValue_LoanPercentIsZeroDepositPercentIs100()
        {
            // byLoanPercentOnZero=true path: loanPercent=0 → _loanAmountPercentage=0,
            // _depositPercentage = 100 - 0 = 100
            _info.LoanAmountPercentage = 80;
            _info.LoanAmountPercentage = 0;

            Assert.That(_info.LoanAmountPercentage, Is.EqualTo(0));
            Assert.That(_info.DepositPercentage, Is.EqualTo(100));
        }

        [Test]
        public void LoanAmountPercentage_ExcedsHundred_ClampsTo100()
        {
            _info.LoanAmountPercentage = 150;
            Assert.That(_info.LoanAmountPercentage, Is.EqualTo(100));
        }

        [Test]
        public void LoanAmountPercentage_80Percent_DepositPercentageIs20()
        {
            _info.LoanAmountPercentage = 80;
            Assert.That(_info.DepositPercentage, Is.EqualTo(20).Within(0.001));
        }

        [Test]
        public void LoanAmountPercentage_SetsLoanAndDepositDirectValues()
        {
            double total = _info.PropertyTotalAmount;
            _info.LoanAmountPercentage = 80;

            double expectedLoan = total - ((total * 20) / 100);
            Assert.That(_info.LoanAmountDirectInput, Is.EqualTo(expectedLoan).Within(0.01));
        }

        // ── DepositPercentage setter ──────────────────────────────────────────

        [Test]
        public void DepositPercentage_ZeroValue_DepositPercentIsZeroLoanPercentIs100()
        {
            // byDepositPercentOnZero=true path: depositPercent=0 → _depositPercentage=0,
            // _loanAmountPercentage = 100 - 0 = 100
            _info.DepositPercentage = 20;
            _info.DepositPercentage = 0;

            Assert.That(_info.DepositPercentage, Is.EqualTo(0));
            Assert.That(_info.LoanAmountPercentage, Is.EqualTo(100));
        }

        [Test]
        public void DepositPercentage_ExceedsHundred_ClampsTo100()
        {
            _info.DepositPercentage = 120;
            Assert.That(_info.DepositPercentage, Is.EqualTo(100));
        }

        [Test]
        public void DepositPercentage_20Percent_LoanPercentageIs80()
        {
            _info.DepositPercentage = 20;
            Assert.That(_info.LoanAmountPercentage, Is.EqualTo(80).Within(0.001));
        }

        // ── Loan + Deposit always sum to PropertyTotalAmount ─────────────────

        [Test]
        public void LoanAndDeposit_ViaDirectInput_SumToPropertyTotal()
        {
            _info.LoanAmountDirectInput = 350000;
            double total = _info.PropertyTotalAmount;

            Assert.That(_info.LoanAmountDirectInput + _info.DepositAmountDirectInput,
                Is.EqualTo(total).Within(0.01));
        }

        [Test]
        public void LoanAndDeposit_ViaPercentage_SumToPropertyTotal()
        {
            _info.LoanAmountPercentage = 75;
            double total = _info.PropertyTotalAmount;

            Assert.That(_info.LoanAmountDirectInput + _info.DepositAmountDirectInput,
                Is.EqualTo(total).Within(0.01));
        }

        [Test]
        public void LoanAndDepositPercentage_AlwaysSumTo100()
        {
            _info.DepositPercentage = 30;
            Assert.That(_info.LoanAmountPercentage + _info.DepositPercentage, Is.EqualTo(100).Within(0.001));

            _info.LoanAmountPercentage = 60;
            Assert.That(_info.LoanAmountPercentage + _info.DepositPercentage, Is.EqualTo(100).Within(0.001));
        }

        // ── IsLoanAmountByPercentage / IsDepositByPercentage ──────────────────

        [Test]
        public void IsLoanAmountByPercentage_WhenLoanPctSet_IsTrue()
        {
            _info.LoanAmountPercentage = 80;
            Assert.That(_info.IsLoanAmountByPercentage, Is.True);
        }

        [Test]
        public void IsLoanAmountByPercentage_WhenNotSet_IsFalse()
        {
            Assert.That(_info.IsLoanAmountByPercentage, Is.False);
        }

        [Test]
        public void IsDepositByPercentage_WhenDepositPctSet_IsTrue()
        {
            _info.DepositPercentage = 20;
            Assert.That(_info.IsDepositByPercentage, Is.True);
        }

        // ── OtherExpenseTotalAmount & PropertyTotalAmount ─────────────────────

        [Test]
        public void PropertyTotalAmount_NoExpenses_EqualsPropertyAmount()
        {
            Assert.That(_info.PropertyTotalAmount, Is.EqualTo(_info.PropertyAmount));
        }

        [Test]
        public void LoanPercentage_WhenZeroPropertyAmount_ReturnsZero()
        {
            _info.PropertyAmount = 0;
            Assert.That(_info.LoanPercentage, Is.EqualTo(0));
        }

        [Test]
        public void LoanPercentage_WhenLoanAmountSet_ReturnsCorrectPercentage()
        {
            _info.LoanAmountDirectInput = 400000;
            double expected = 400000.0 / _info.PropertyTotalAmount * 100;
            Assert.That(_info.LoanPercentage, Is.EqualTo(expected).Within(0.01));
        }
    }

    // ── StampDutyOutput — nullable AustraliaStateSelected / AustraliaStateIndex ──

    [TestFixture]
    public class StampDutyOutputTests
    {
        [Test]
        public void AustraliaStateSelected_DefaultIsNull()
        {
            var sdo = new StampDutyOutput();
            Assert.That(sdo.AustraliaStateSelected, Is.Null);
        }

        [Test]
        public void AustraliaStateIndex_WhenNoStateSelected_IsNull()
        {
            var sdo = new StampDutyOutput();
            Assert.That(sdo.AustraliaStateIndex, Is.Null);
        }

        [Test]
        public void AustraliaStateIndex_WhenStateSet_ReturnsCorrectIndex()
        {
            var sdo = new StampDutyOutput();
            sdo.SetState(LoanCalculator.Core.Models.Enums.AustralianStatesEnum.NSW);
            Assert.That(sdo.AustraliaStateIndex, Is.Not.Null);
            Assert.That(sdo.AustraliaStateIndex, Is.EqualTo(
                StampDutyOutput.AustraliaStateToIndex(LoanCalculator.Core.Models.Enums.AustralianStatesEnum.NSW)));
        }

        [Test]
        public void SetState_ThenClearViaNull_IndexIsNull()
        {
            var sdo = new StampDutyOutput();
            sdo.SetState(LoanCalculator.Core.Models.Enums.AustralianStatesEnum.VIC);
            Assert.That(sdo.AustraliaStateIndex, Is.Not.Null);

            sdo.AustraliaStateSelected = null;
            Assert.That(sdo.AustraliaStateIndex, Is.Null);
        }

        [Test]
        public void SumUpData_DoesNotThrow_WhenNoStateSelected()
        {
            var sdo = new StampDutyOutput();
            Assert.DoesNotThrow(() => sdo.SumUpData());
        }
    }
}
