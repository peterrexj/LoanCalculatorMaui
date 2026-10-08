using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Infrastructure;

/// <summary>
/// Owns the app's data for a run so no test depends on another having gone first.
/// <para>
/// The app persists to disk, so data survives the per-test relaunch. Seeding through the UI is
/// slow (~20 s), so each dataset is seeded <b>at most once per run</b> and every later request is
/// a no-op. Any test that wipes the device must call <see cref="MarkWiped"/>, which makes the next
/// request re-seed.
/// </para>
/// <para>
/// The alternative — writing the JSON files onto the device directly — would be faster still, but
/// couples the suite to the on-disk schema and skips the very UI under test. Seeding through the UI
/// keeps the tests honest; caching keeps them affordable.
/// </para>
/// </summary>
internal static class TestData
{
    /// <summary>Fixed figures so failures are reproducible and comparable across runs.</summary>
    internal const string Asset = "650000";
    internal const string Deposit = "130000";
    internal const string IncomeAmount = "6000";
    internal const string ExpenseAmount = "1500";

    private static bool _loanSeeded;
    private static bool _budgetSeeded;

    /// <summary>Call after deleting app data, so the next request re-seeds instead of trusting the cache.</summary>
    internal static void MarkWiped()
    {
        _loanSeeded = false;
        _budgetSeeded = false;
    }

    /// <summary>Guarantees a loan exists. Cheap after the first call in a run.</summary>
    internal static void EnsureLoan(ShellTabs tabs, LoanPage loan)
    {
        if (_loanSeeded) return;

        tabs.GoTo(AppTab.Loan);
        loan.EnterAssetAndDeposit(Asset, Deposit);
        _loanSeeded = true;
    }

    /// <summary>
    /// Guarantees the budget has at least one income and one expense — the precondition for the
    /// Summary totals and the affordability stress test.
    /// </summary>
    internal static void EnsureBudget(ShellTabs tabs, BudgetPage budget)
    {
        if (_budgetSeeded) return;

        tabs.GoTo(AppTab.Budget);
        if (!budget.HasSummaryTotals())
        {
            budget.AddIncome(UniqueName("SeedIncome"), IncomeAmount);
            budget.AddExpense(UniqueName("SeedExpense"), ExpenseAmount);
        }

        _budgetSeeded = true;
    }

    /// <summary>
    /// Restores both datasets after a destructive test, so a wipe cannot strand whatever runs next
    /// regardless of fixture order.
    /// </summary>
    internal static void RestoreBaseline(ShellTabs tabs, LoanPage loan, BudgetPage budget)
    {
        MarkWiped();
        EnsureLoan(tabs, loan);
        EnsureBudget(tabs, budget);
    }

    /// <summary>
    /// Unique, space-free names. Space-free because row action AutomationIds embed the name;
    /// unique so repeated runs never collide in the income/expense lists.
    /// </summary>
    internal static string UniqueName(string stem) => $"{stem}{DateTime.Now:HHmmssfff}";
}
