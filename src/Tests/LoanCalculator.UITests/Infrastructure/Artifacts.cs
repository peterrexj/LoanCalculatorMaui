using System.Text.RegularExpressions;

namespace LoanCalculator.UITests.Infrastructure;

/// <summary>
/// Dumps a screenshot and the element tree when a test fails. The element tree is what
/// tells you whether a missing element is genuinely absent or just untagged, so it is worth
/// far more than the screenshot when a locator stops matching after a XAML change.
/// </summary>
public static class Artifacts
{
    public static void CaptureFailure(AppDriver app, string testName)
    {
        var stem = Path.Combine(
            UITestConfig.ArtifactDirectory,
            $"{UITestConfig.Platform}-{Sanitise(testName)}-{DateTime.Now:HHmmss}");

        Directory.CreateDirectory(UITestConfig.ArtifactDirectory);

        TrySave($"{stem}.png", () => File.WriteAllBytes($"{stem}.png", app.Screenshot()));
        TrySave($"{stem}.xml", () => File.WriteAllText($"{stem}.xml", app.PageSource));
    }

    private static void TrySave(string path, Action save)
    {
        try
        {
            save();
            TestContext.Progress.WriteLine($"[artifact] {path}");
        }
        catch (Exception ex)
        {
            // The session may be dead, which is often *why* the test failed. Never mask
            // the original failure with a capture error.
            TestContext.Progress.WriteLine($"[artifact] could not write {path}: {ex.Message}");
        }
    }

    private static string Sanitise(string name) =>
        Regex.Replace(name, @"[^\w\-]+", "_").Trim('_');
}
