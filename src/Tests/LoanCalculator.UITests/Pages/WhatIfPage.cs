using LoanCalculator.UITests.Infrastructure;

namespace LoanCalculator.UITests.Pages;

public sealed class WhatIfPage(AppDriver app) : PageBase(app)
{
    public const string PageTitle = "WhatIfPageTitle";
    public const string NoDataPanel = "WhatIfNoDataPanel";

    public const string RateDeltaMinus = "WhatIfRateDeltaMinus";
    public const string RateDeltaValue = "WhatIfRateDeltaValue";
    public const string RateDeltaPlus = "WhatIfRateDeltaPlus";
    public const string NewRate = "WhatIfRateNewRate";
    public const string NewMonthly = "WhatIfRateNewMonthly";

    /// <summary>
    /// Swipe budget for the two stress-test probes. One of them is *meant* to return false, so a
    /// full-page hunt would spend 12 swipes to learn what a short look already tells us.
    /// </summary>
    private const int ProbeSwipes = 6;

    public const string StressUnavailablePanel = "WhatIfStressUnavailablePanel";
    public const string StressResultsPanel = "WhatIfStressResultsPanel";

    /// <summary>Every scenario card is hidden until the Loan tab has data.</summary>
    public bool ShowsNoDataPlaceholder() => App.Exists(NoDataPanel, TimeSpan.FromSeconds(3));

    // The stress test needs income and expenses as well as a loan, so it shows a prompt until
    // the Budget tab has data. Both panels sit far down the page, hence the scroll.

    public bool StressTestResultsShowing()
    {
        try
        {
            App.ScrollTo(StressResultsPanel, maxSwipes: ProbeSwipes);
            return true;
        }
        catch (AssertionException)
        {
            return false;
        }
    }

    public bool StressTestPromptShowing()
    {
        try
        {
            App.ScrollTo(StressUnavailablePanel, maxSwipes: ProbeSwipes);
            return true;
        }
        catch (AssertionException)
        {
            return false;
        }
    }

    public string RateDelta() => App.ScrollTo(RateDeltaValue).Text;

    public void IncreaseRate(int steps = 1)
    {
        App.ScrollTo(RateDeltaPlus);
        App.TapRepeatedly(RateDeltaPlus, steps);
    }

    public void DecreaseRate(int steps = 1)
    {
        App.ScrollTo(RateDeltaMinus);
        App.TapRepeatedly(RateDeltaMinus, steps);
    }

    public decimal NewMonthlyRepayment()
    {
        App.ScrollTo(NewMonthly);
        return App.Number(NewMonthly);
    }

    public string NewInterestRate() => App.ScrollTo(NewRate).Text;
}
