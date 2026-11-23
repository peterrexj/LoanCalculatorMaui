using LoanCalculator.UITests.Infrastructure;
using LoanCalculator.UITests.Pages;

namespace LoanCalculator.UITests.Tests;

/// <summary>
/// Shared plumbing for the fixtures that exist to produce screenshots rather than to assert
/// (<see cref="StoreScreenshots"/>, <see cref="GuideCapture"/>). One unreachable screen must cost a
/// line in the report, never the rest of the run — the opposite of a normal test.
/// </summary>
public abstract class CaptureFixtureBase : UITestBase
{
    protected readonly List<string> Captured = [];
    protected readonly List<string> Missed = [];

    /// <summary>Report file written beside the PNGs, e.g. <c>store-report.txt</c>.</summary>
    protected abstract string ReportFileName { get; }

    [SetUp]
    public void ResetCaptureLog()
    {
        Captured.Clear();
        Missed.Clear();
    }

    /// <summary>
    /// Navigates, waits for animation to finish (a mid-animation frame looks broken in a listing),
    /// captures, and records the result. Returns whether it worked so callers can skip dependent
    /// steps, such as dismissing a page that never opened.
    /// </summary>
    protected bool Capture(string name, Action navigate, int settleMs = 1200)
    {
        try
        {
            navigate();
            Thread.Sleep(settleMs);
            Artifacts.Capture(App, name);
            Captured.Add(name);
            return true;
        }
        catch (Exception ex)
        {
            Missed.Add($"{name} — {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
            Recover();
            return false;
        }
    }

    /// <summary>
    /// After a miss, close whatever page or keypad is still open. Without this one failed step
    /// strands every later one behind a modal (a single stuck page once cost 38 shots), because an
    /// open page hides the tab bar the next step needs.
    /// </summary>
    private void Recover()
    {
        try { App.DismissKeyboard(); } catch { /* none showing */ }
        try { ModalPages.DismissAny(App); } catch { /* session may be gone */ }
    }

    /// <summary>Runs a step whose failure should be noted but must not stop the run.</summary>
    protected bool Try(string what, Action step)
    {
        try
        {
            step();
            return true;
        }
        catch (Exception ex)
        {
            Missed.Add($"{what} — {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
            Recover();
            return false;
        }
    }

    [TearDown]
    public void WriteCaptureReport()
    {
        var lines = new List<string>
        {
            $"{TestContext.CurrentContext.Test.Name} — captured {Captured.Count}, missed {Missed.Count}",
        };
        lines.AddRange(Captured.Select(n => $"  ok      {n}"));
        lines.AddRange(Missed.Select(m => $"  MISSED  {m}"));

        Directory.CreateDirectory(UITestConfig.ArtifactDirectory);
        File.AppendAllLines(Path.Combine(UITestConfig.ArtifactDirectory, ReportFileName), lines.Append(string.Empty));
    }
}
