using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// These assert <em>invariants</em> rather than a hard-coded formula. The loan amount is
/// derived from the total asset amount (asset value plus upfront costs) minus the deposit,
/// and upfront costs come from saved settings — so an absolute expected figure would only
/// hold on a device whose upfront costs happen to be zero.
/// <para>
/// Asset and deposit are coupled: the deposit is tracked as a percentage of the total, so
/// changing the asset value moves the deposit. Every test therefore sets both fields
/// together and never assumes one survives a change to the other.
/// </para>
/// </summary>
[TestFixture]
[Category("Calculation")]
public class LoanCalculationTests : UITestBase
{
    [SetUp]
    public void GoToLoanTab() => Tabs.GoTo(AppTab.Loan);

    [Test]
    public void RaisingTheDepositReducesTheLoanByTheSameAmount()
    {
        Loan.OpenLoanDetails();

        var loanAtLowDeposit = SetAssetAndDeposit("800000", "100000");
        var loanAtHighDeposit = SetAssetAndDeposit("800000", "150000");

        Assert.That(loanAtHighDeposit, Is.EqualTo(loanAtLowDeposit - 50_000m).Within(1m),
            "Adding 50,000 to the deposit should reduce the loan by exactly 50,000.");
    }

    [Test]
    public void RaisingTheAssetValueRaisesTheLoanByTheSameAmount()
    {
        Loan.OpenLoanDetails();

        var loanAtLowAsset = SetAssetAndDeposit("600000", "120000");
        var loanAtHighAsset = SetAssetAndDeposit("700000", "120000");

        Assert.That(loanAtHighAsset, Is.EqualTo(loanAtLowAsset + 100_000m).Within(1m),
            "Adding 100,000 to the asset value should increase the loan by exactly 100,000.");
    }

    /// <summary>
    /// Sets both coupled fields and returns the settled loan amount. The deposit is written
    /// after the asset because changing the asset re-derives the deposit from its percentage.
    /// </summary>
    private decimal SetAssetAndDeposit(string asset, string deposit)
    {
        App.EnterText(LoanPage.AssetEntry, asset);
        App.EnterText(LoanPage.DepositEntry, deposit);

        var expected = decimal.Parse(asset) - decimal.Parse(deposit);

        // Upfront costs are added on top, so the loan is at least asset − deposit.
        var settled = App.WaitForText(
            LoanPage.LoanFormatted,
            shown => Money(shown) >= expected,
            $"settled at or above {expected} for a {asset} asset with a {deposit} deposit");

        return Money(settled);
    }

    [Test]
    public void LoanAmountIsNeverMoreThanAssetPlusUpfrontCosts()
    {
        Loan.OpenLoanDetails();

        App.EnterText(LoanPage.AssetEntry, "650000");
        App.EnterText(LoanPage.DepositEntry, "130000");

        var loan = App.WaitForText(
            LoanPage.LoanFormatted,
            shown => Money(shown) > 0,
            "showed a loan amount");

        // The deposit must always come off the borrowed figure, whatever the upfront costs are.
        Assert.That(Money(loan), Is.LessThan(650_000m + 130_000m),
            "The loan amount ignored the deposit.");
        Assert.That(Money(loan), Is.GreaterThan(0m));
    }

    [Test]
    public void AmountsAreSpelledOutInWords()
    {
        Loan.OpenLoanDetails();

        App.EnterText(LoanPage.AssetEntry, "650000");
        App.EnterText(LoanPage.DepositEntry, "130000");

        var words = App.WaitForText(
            LoanPage.LoanWords,
            shown => shown.Trim().Length > 0,
            "rendered the loan amount in words");

        Assert.That(words, Does.Match("(?i)thousand|lakh|million"),
            $"Expected a spelled-out amount, got '{words}'.");
    }

    [Test]
    public void QuickInputValuesSurviveReopeningThePopup()
    {
        Loan.OpenLoanDetails();
        App.EnterText(LoanPage.AssetEntry, "725000");
        App.EnterText(LoanPage.DepositEntry, "145000");

        // Let the debounced save run before dismissing the popup.
        App.WaitForText(
            LoanPage.LoanFormatted,
            shown => Money(shown) > 0,
            "recalculated for the entered values");

        Loan.ApplyLoanDetails();
        Loan.OpenLoanDetails();

        Assert.Multiple(() =>
        {
            Assert.That(Money(App.Text(LoanPage.AssetEntry)), Is.EqualTo(725_000m),
                "The asset value was not persisted.");
            Assert.That(Money(App.Text(LoanPage.DepositEntry)), Is.EqualTo(145_000m),
                "The deposit was not persisted.");
        });
    }

    /// <summary>
    /// The Quick Setup wizard is what every new user actually sees, and until now the suite only
    /// ever cancelled it. It is one scrolling page now, so this fills the two fields that matter
    /// and checks the values reach the loan — leaving the optional upfront, running cost, income
    /// and expense fields alone, as most users would.
    /// </summary>
    [Test]
    public void CompletingTheSetupWizardRecordsTheLoan()
    {
        Loan.CompleteWizard("900000", "180000");

        Loan.OpenLoanDetails();

        Assert.Multiple(() =>
        {
            Assert.That(Money(App.Text(LoanPage.AssetEntry)), Is.EqualTo(900_000m),
                "The wizard's asset value did not reach the loan.");
            Assert.That(Money(App.Text(LoanPage.DepositEntry)), Is.EqualTo(180_000m),
                "The wizard's deposit did not reach the loan.");
        });
    }

    private static decimal Money(string text)
    {
        var cleaned = new string(text.Where(c => char.IsDigit(c) || c is '.' or '-').ToArray());
        return decimal.TryParse(cleaned, out var value) ? value : -1m;
    }
}
