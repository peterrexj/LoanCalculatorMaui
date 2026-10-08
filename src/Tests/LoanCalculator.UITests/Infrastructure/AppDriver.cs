using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Interactions;

namespace LoanCalculator.UITests.Infrastructure;

/// <summary>
/// The single API the tests and page objects talk to. Everything is addressed by MAUI
/// <c>AutomationId</c> so one test body drives both platforms.
/// <para>
/// Elements are never cached — every call re-resolves the locator. That costs a round trip
/// but removes StaleElementReferenceException, which is the main source of flake when a
/// MAUI page re-renders (theme change, binding update, list refresh) between two actions.
/// </para>
/// </summary>
public sealed class AppDriver : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    public AppiumDriver Raw { get; }
    public TestPlatform Platform { get; }

    private AppDriver(AppiumDriver raw, TestPlatform platform)
    {
        Raw = raw;
        Platform = platform;

        // Our own polling loops handle waiting, and they can only be fast if the server
        // does not also block for an implicit wait on every failed lookup.
        Raw.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
    }

    // ── Session ──────────────────────────────────────────────────────────────────

    public static AppDriver Create()
    {
        var options = UITestConfig.Platform == TestPlatform.iOS
            ? BuildIosOptions()
            : BuildAndroidOptions();

        try
        {
            // SessionTimeout, not LaunchTimeout: this call must outlast Appium's own WDA budget.
            AppiumDriver raw = UITestConfig.Platform == TestPlatform.iOS
                ? new IOSDriver(UITestConfig.AppiumUri, options, UITestConfig.SessionTimeout)
                : new AndroidDriver(UITestConfig.AppiumUri, options, UITestConfig.SessionTimeout);

            return new AppDriver(raw, UITestConfig.Platform);
        }
        catch (WebDriverException ex)
        {
            // A session that cannot start fails OneTimeSetUp, so NUnit repeats the same wall of
            // stack trace under every single test. Translate the causes we recognise into one
            // actionable line first.
            throw new InvalidOperationException(DescribeSessionFailure(ex), ex);
        }
    }

    private static string DescribeSessionFailure(WebDriverException ex)
    {
        var message = ex.Message;

        if (message.Contains("ANDROID_HOME", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ANDROID_SDK_ROOT", StringComparison.OrdinalIgnoreCase))
        {
            return
                "The Appium server cannot see the Android SDK, so no Android session can start.\n" +
                "\n" +
                "This almost always means the suite attached to a server you started by hand — the\n" +
                "run log will say \"attaching to existing server\" — and that server was launched\n" +
                "without ANDROID_HOME. UiAutomator2 needs it to find adb. run-uitests.sh exports it\n" +
                "for servers it starts itself, which is why an auto-started run works.\n" +
                "\n" +
                "Fix: restart your server with `npm run start` (it now exports ANDROID_HOME), or stop\n" +
                "it and let run-uitests.sh start its own.\n" +
                "\n" +
                $"Original driver error: {message.Split('\n')[0]}";
        }

        if (message.Contains("not installed", StringComparison.OrdinalIgnoreCase))
        {
            return
                $"The app is not installed on the device, so the session cannot start.\n" +
                $"run-uitests.sh installs it; a bare `dotnet test` does not.\n" +
                $"\nOriginal driver error: {message.Split('\n')[0]}";
        }

        return
            $"Could not start an Appium session on {UITestConfig.Platform}.\n" +
            $"Check the device is booted and the server at {UITestConfig.AppiumUri} is healthy " +
            $"(TestResults/uitest-artifacts/appium-server.log).\n" +
            $"\nOriginal driver error: {message.Split('\n')[0]}";
    }

    private static AppiumOptions BuildIosOptions()
    {
        var options = new AppiumOptions
        {
            AutomationName = "XCUITest",
            PlatformName = "iOS",
            DeviceName = UITestConfig.DeviceName,
        };

        if (UITestConfig.Udid is not null)
            options.AddAdditionalAppiumOption("udid", UITestConfig.Udid);

        // Deliberately no `app` capability. run-uitests.sh installs the freshly built binary
        // itself, because when Appium owns installation `noReset` wins over
        // `enforceAppInstall` and the suite silently tests whatever build is already there.
        options.AddAdditionalAppiumOption("bundleId", UITestConfig.BundleId);
        options.AddAdditionalAppiumOption("noReset", true);
        options.AddAdditionalAppiumOption("newCommandTimeout", 300);
        // Launch budget stays generous: a fresh simulator has to build and install WDA.
        options.AddAdditionalAppiumOption("wdaLaunchTimeout", 240_000);

        // Per-command budget, deliberately much shorter — see UITestConfig.WdaConnectionTimeout.
        // At Appium's 240s default, a degraded WDA cost 20 minutes per failing test.
        options.AddAdditionalAppiumOption(
            "wdaConnectionTimeout", (int)UITestConfig.WdaConnectionTimeout.TotalMilliseconds);

        // Unique per shard so several simulators can be driven concurrently.
        options.AddAdditionalAppiumOption("wdaLocalPort", 8100 + UITestConfig.ShardIndex);
        options.AddAdditionalAppiumOption("showXcodeLog", false);

        // Force the software keyboard: with a connected hardware keyboard, SendKeys on a
        // MAUI Entry can land on the host instead of the field.
        options.AddAdditionalAppiumOption("connectHardwareKeyboard", false);
        options.AddAdditionalAppiumOption("forceSimulatorSoftwareKeyboardPresence", true);

        // Shortens the splash/page transitions the waits below have to sit through.
        options.AddAdditionalAppiumOption("reduceMotion", true);

        return options;
    }

    private static AppiumOptions BuildAndroidOptions()
    {
        var options = new AppiumOptions
        {
            AutomationName = "UiAutomator2",
            PlatformName = "Android",
            DeviceName = UITestConfig.DeviceName,
        };

        if (UITestConfig.Udid is not null)
            options.AddAdditionalAppiumOption("udid", UITestConfig.Udid);
        if (UITestConfig.AvdName is not null)
            options.AddAdditionalAppiumOption("avd", UITestConfig.AvdName);

        // See BuildIosOptions: installation is the run script's job, not Appium's.
        options.AddAdditionalAppiumOption("appPackage", UITestConfig.BundleId);

        // MAUI emits a hash-named launcher activity (crc64….MainActivity), so the exact
        // name changes between builds. Let Appium read it from the manifest and only tell
        // it what to wait for.
        options.AddAdditionalAppiumOption("appWaitActivity", "*.MainActivity");
        options.AddAdditionalAppiumOption("appWaitForLaunch", true);

        options.AddAdditionalAppiumOption("noReset", true);
        options.AddAdditionalAppiumOption("autoGrantPermissions", true);
        options.AddAdditionalAppiumOption("newCommandTimeout", 300);

        // Deliberately NOT setting `disableWindowAnimation`. It looks like free flake
        // reduction, but Appium implements it by zeroing all three global animation scales —
        // including `animator_duration_scale`, which drives MAUI's animation framework. With
        // that at 0, `FadeTo`/`ScaleTo` never signal completion, so SplashPage.RunSplashAsync
        // never finishes, NavigateToShell() never runs, and the app sits on the native splash
        // forever while the dots loop spins. The app never renders and every lookup fails with
        // "Timed out waiting for the root AccessibilityNodeInfo".
        //
        // run-uitests.sh zeroes only window_animation_scale and transition_animation_scale,
        // which are safe, and restores them afterwards.

        // UiAutomator2 blocks on the window becoming "idle" before it will hand over the
        // accessibility tree. This app animates more or less continuously (splash dots,
        // Syncfusion charts), so with the default idle timeout every lookup fails with
        // "Timed out waiting for the root AccessibilityNodeInfo". Capping the idle wait makes
        // the tree available immediately; our own explicit waits handle settling instead.
        options.AddAdditionalAppiumOption("settings[waitForIdleTimeout]", 100);
        options.AddAdditionalAppiumOption("settings[actionAcknowledgmentTimeout]", 500);

        // MAUI nests layouts deeply and Android marks many of those containers unimportant
        // for accessibility; keeping them means AutomationIds on wrappers stay findable.
        options.AddAdditionalAppiumOption("settings[ignoreUnimportantViews]", false);

        // Appium's adbExecTimeout defaults to 20s, but this app's cold start was measured at
        // 23.1s in Debug on a calm machine (`am start -W` TotalTime) — XAML inflation of the
        // Syncfusion-heavy pages dominates. So `am start-activity -W` reliably exceeds the default
        // and the session fails before a single test runs. Give it room; a loaded host or a cold
        // emulator is slower still.
        options.AddAdditionalAppiumOption("adbExecTimeout", 180_000);

        // Same reasoning for how long to wait for the launched activity to appear.
        options.AddAdditionalAppiumOption("appWaitDuration", 120_000);

        options.AddAdditionalAppiumOption("androidInstallTimeout", 180_000);
        options.AddAdditionalAppiumOption("uiautomator2ServerInstallTimeout", 180_000);
        options.AddAdditionalAppiumOption("uiautomator2ServerLaunchTimeout", 120_000);
        options.AddAdditionalAppiumOption("avdLaunchTimeout", 300_000);
        options.AddAdditionalAppiumOption("avdReadyTimeout", 300_000);
        options.AddAdditionalAppiumOption("ignoreHiddenApiPolicyError", true);

        // Unique per shard so several emulators can be driven concurrently. UiAutomator2 forwards
        // its server over this port; sharing one across sessions makes them fight for the same
        // forward and surfaces as random element-not-found failures.
        options.AddAdditionalAppiumOption("systemPort", 8200 + UITestConfig.ShardIndex);

        if (UITestConfig.Udid is not null)
            options.AddAdditionalAppiumOption("udid", UITestConfig.Udid);

        return options;
    }

    /// <summary>Terminate and relaunch, so each test starts on the app's landing tab.</summary>
    public void RestartApp()
    {
        Raw.TerminateApp(UITestConfig.BundleId);
        Raw.ActivateApp(UITestConfig.BundleId);
    }

    // ── Locating ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// MAUI maps <c>AutomationId</c> to <c>AccessibilityIdentifier</c> on iOS and to
    /// <c>ContentDescription</c> on Android, so accessibility-id covers both. The XPath
    /// fallback catches controls whose handler puts the value on resource-id or name
    /// instead — several Syncfusion wrappers do.
    /// </summary>
    private By[] LocatorsFor(string automationId)
    {
        if (Platform == TestPlatform.Android)
        {
            // MAUI maps AutomationId to the Android view's *resource-id*, and UiAutomator2
            // reports it package-prefixed: "com.pj.loan.afford.calc:id/DisclaimerAcceptButton".
            // It does NOT set contentDescription, so accessibility-id matches nothing here —
            // it is kept last purely as a safety net for controls that do set it.
            return
            [
                MobileBy.Id($"{UITestConfig.BundleId}:id/{automationId}"),
                MobileBy.XPath(
                    $"//*[@resource-id='{UITestConfig.BundleId}:id/{automationId}'" +
                    $" or @resource-id='{automationId}'" +
                    $" or @content-desc='{automationId}']"),
                MobileBy.AccessibilityId(automationId),
            ];
        }

        // iOS: AutomationId becomes accessibilityIdentifier, surfaced as @name.
        return
        [
            MobileBy.AccessibilityId(automationId),
            MobileBy.XPath($"//*[@name='{automationId}' or @label='{automationId}']"),
        ];
    }

    public AppiumElement Find(string automationId, TimeSpan? timeout = null)
    {
        var element = Resolve(automationId, timeout ?? UITestConfig.DefaultTimeout, out var outcome);
        if (element is not null) return element;

        // Present but off-screen: scroll it into view and take it. This is the most common
        // spurious failure — popup content and long pages do not all fit on a smaller screen,
        // and Android has noticeably less usable height than iOS for the same layout.
        if (outcome is { SawHiddenMatch: true, LastError: null })
        {
            try
            {
                return ScrollTo(automationId);
            }
            catch (AssertionException)
            {
                // Could not reach it; fall through to the descriptive failure below.
            }
        }

        // An unhandled app exception puts a native alert over everything, so every subsequent
        // lookup fails. Report the alert rather than a misleading "element not found".
        ThrowIfAppErrorDialogShowing();

        var detail = outcome switch
        {
            { LastError: not null } => "Every lookup failed on the driver side, so the element " +
                $"may well exist. Last driver error:\n  {outcome.LastError.Message.Split('\n')[0]}",
            { SawHiddenMatch: true } => "It IS in the element tree but is not visible, so it is " +
                "obscured rather than missing. Likely causes, in the order worth checking: the " +
                "software keyboard is covering it (anything in the lower third of the screen after " +
                "EnterText — call DismissKeyboard() first), it is scrolled off-screen (ScrollTo()), " +
                "or it is on an inactive tab (navigate there). The captured .xml artifact gives the " +
                "element's x/y/height — compare it against the XCUIElementTypeKeyboard bounds.",
            _ => "If the control exists, it may be missing an AutomationId in XAML.",
        };

        throw new AssertionException(
            $"No visible element with AutomationId '{automationId}' appeared within " +
            $"{(timeout ?? UITestConfig.DefaultTimeout).TotalSeconds:0}s on {Platform}.\n{detail}");
    }

    /// <summary>
    /// The app surfaces unhandled exceptions as a native alert ("An unexpected error
    /// occurred"), which is modal and hides the whole UI. Without this, the first test to hit
    /// it reports a 20-second "element not found" and every test after it fails the same way,
    /// hiding the actual crash completely.
    /// </summary>
    private void ThrowIfAppErrorDialogShowing()
    {
        const string marker = "unexpected error";
        try
        {
            var alerts = Raw.FindElements(MobileBy.XPath(
                    $"//*[contains(@text,'{marker}') or contains(@label,'{marker}') or contains(@name,'{marker}')]"))
                .Cast<AppiumElement>()
                .Where(element => element.Displayed)
                .ToList();

            if (alerts.Count == 0) return;

            var message = alerts
                .Select(a => string.IsNullOrWhiteSpace(a.Text) ? a.GetAttribute("label") : a.Text)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)) ?? marker;

            throw new AssertionException(
                $"The app raised an unhandled exception and is showing its error dialog on {Platform}:\n" +
                $"  {message.Trim()}\n" +
                "Everything is blocked behind that alert, so this is the real failure. " +
                "The screenshot and page-source artifacts show it.");
        }
        catch (WebDriverException)
        {
            // Could not inspect the tree; let the caller report its own failure.
        }
    }

    /// <summary>
    /// True only when the element is on screen. Presence alone is not enough: Syncfusion's
    /// SfTabView keeps every tab's content in the element tree, so a present-but-hidden match
    /// would make "am I on this tab?" checks — and therefore tab navigation — silently pass.
    /// </summary>
    public bool Exists(string automationId, TimeSpan? timeout = null) =>
        Resolve(automationId, timeout ?? TimeSpan.FromSeconds(2), out _) is not null;

    private readonly record struct ResolveOutcome(bool SawHiddenMatch, WebDriverException? LastError);

    /// <summary>
    /// Polls every locator strategy until one yields a <em>visible</em> element or time runs
    /// out. The outcome distinguishes "never found", "found but hidden" and "the driver kept
    /// erroring", because otherwise all three present identically and are awful to debug.
    /// </summary>
    private AppiumElement? Resolve(string automationId, TimeSpan timeout, out ResolveOutcome outcome)
    {
        var deadline = DateTime.UtcNow + timeout;
        var locators = LocatorsFor(automationId);
        var sawHidden = false;
        WebDriverException? lastError = null;

        while (true)
        {
            foreach (var by in locators)
            {
                try
                {
                    foreach (var element in Raw.FindElements(by).Cast<AppiumElement>())
                    {
                        if (element.Displayed)
                        {
                            outcome = new ResolveOutcome(sawHidden, null);
                            return element;
                        }

                        sawHidden = true;
                    }

                    lastError = null;
                }
                catch (WebDriverException ex)
                {
                    // Usually the tree being rebuilt mid-query — transient, so retry. But if
                    // every attempt fails this way it is a real fault, and Find reports it.
                    lastError = ex;
                }
            }

            if (DateTime.UtcNow >= deadline)
            {
                outcome = new ResolveOutcome(sawHidden, lastError);
                return null;
            }

            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>Finds an element by its visible text — for Shell tabs and other chrome MAUI does not tag.</summary>
    public AppiumElement FindByText(string text, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? UITestConfig.DefaultTimeout);
        var locators = new By[]
        {
            MobileBy.AccessibilityId(text),
            MobileBy.XPath(
                $"//*[@text='{text}' or @label='{text}' or @name='{text}' or @content-desc='{text}']"),
        };

        while (true)
        {
            foreach (var by in locators)
            {
                try
                {
                    foreach (var element in Raw.FindElements(by).Cast<AppiumElement>())
                        if (element.Displayed) return element;
                }
                catch (WebDriverException) { /* transient tree rebuild */ }
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new AssertionException(
                    $"No element showing text '{text}' appeared within " +
                    $"{(timeout ?? UITestConfig.DefaultTimeout).TotalSeconds:0}s on {Platform}.");
            }

            Thread.Sleep(PollInterval);
        }
    }

    // ── Waiting ──────────────────────────────────────────────────────────────────

    public void WaitUntilVisible(string automationId, TimeSpan? timeout = null) =>
        Find(automationId, timeout ?? UITestConfig.DefaultTimeout);

    public void WaitUntilGone(string automationId, TimeSpan? timeout = null)
    {
        var limit = timeout ?? UITestConfig.DefaultTimeout;
        var deadline = DateTime.UtcNow + limit;

        while (DateTime.UtcNow < deadline)
        {
            if (Resolve(automationId, TimeSpan.Zero, out _) is null) return;
            Thread.Sleep(PollInterval);
        }

        throw new AssertionException(
            $"Element '{automationId}' was still present after {limit.TotalSeconds:0}s.");
    }

    /// <summary>Waits for a value to settle — for labels recalculated by a debounced save.</summary>
    public string WaitForText(
        string automationId,
        Func<string, bool> predicate,
        string describeExpectation,
        TimeSpan? timeout = null)
    {
        var limit = timeout ?? UITestConfig.DefaultTimeout;
        var deadline = DateTime.UtcNow + limit;
        var lastSeen = "<never resolved>";

        while (true)
        {
            try
            {
                lastSeen = Text(automationId, TimeSpan.FromSeconds(1));
                if (predicate(lastSeen)) return lastSeen;
            }
            catch (AssertionException)
            {
                // Not rendered yet — keep waiting.
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new AssertionException(
                    $"'{automationId}' never {describeExpectation} within {limit.TotalSeconds:0}s. " +
                    $"Last value: '{lastSeen}'.");
            }

            Thread.Sleep(PollInterval);
        }
    }

    // ── Reading ──────────────────────────────────────────────────────────────────

    public string Text(string automationId, TimeSpan? timeout = null)
    {
        var element = Find(automationId, timeout);
        var text = element.Text;
        if (!string.IsNullOrEmpty(text)) return text;

        // A MAUI Entry surfaces its content as `value` on iOS when Text comes back empty. Android
        // has no `value` attribute at all — UiAutomator2 throws NotImplementedException and lists
        // the attributes it does support — so asking for it there is not a fallback, it is a crash.
        if (Platform == TestPlatform.Android) return string.Empty;

        return element.GetAttribute("value") ?? string.Empty;
    }

    /// <summary>
    /// All text found on an element and inside it, joined with spaces. For composite controls
    /// whose content is assembled from <c>Span</c>s — which cannot carry an AutomationId — so the
    /// only way to assert on them is to read the whole block and look for what you expect.
    /// </summary>
    public string TextWithin(string automationId, TimeSpan? timeout = null)
    {
        var element = Find(automationId, timeout);

        var textNodes = Platform == TestPlatform.iOS
            ? ".//XCUIElementTypeStaticText | .//XCUIElementTypeTextField"
            : ".//android.widget.TextView | .//android.widget.EditText";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(element.Text)) parts.Add(element.Text);

        try
        {
            parts.AddRange(element.FindElements(MobileBy.XPath(textNodes))
                .Select(child => child.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text)));
        }
        catch (WebDriverException)
        {
            // Subtree changed mid-query; return whatever was gathered.
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Waits for text to disappear, but does not fail if it is already gone — for dismissing a
    /// dialog whose teardown may outrun the next query.
    /// </summary>
    public void WaitUntilTextGoneIfPresent(string text, TimeSpan? timeout = null)
    {
        var limit = timeout ?? UITestConfig.DefaultTimeout;
        var deadline = DateTime.UtcNow + limit;

        while (DateTime.UtcNow < deadline)
        {
            if (VisibleTextCount(text) == 0) return;
            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>How many visible elements currently show exactly this text.</summary>
    public int VisibleTextCount(string text)
    {
        try
        {
            return Raw.FindElements(MobileBy.XPath(
                    $"//*[@text='{text}' or @label='{text}' or @name='{text}' or @content-desc='{text}']"))
                .Cast<AppiumElement>()
                .Count(element => element.Displayed);
        }
        catch (WebDriverException)
        {
            // Tree rebuilding mid-query — report "unknown" as non-zero so callers keep waiting.
            return -1;
        }
    }

    /// <summary>Polls an arbitrary condition. For state that is not a single element's text.</summary>
    public void WaitUntil(Func<bool> condition, string describeExpectation, TimeSpan? timeout = null)
    {
        var limit = timeout ?? UITestConfig.DefaultTimeout;
        var deadline = DateTime.UtcNow + limit;

        while (true)
        {
            try
            {
                if (condition()) return;
            }
            catch (Exception) when (DateTime.UtcNow < deadline)
            {
                // The condition may read elements that are still appearing — keep trying.
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new AssertionException(
                    $"Timed out after {limit.TotalSeconds:0}s waiting for {describeExpectation}.");
            }

            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>Parses a displayed currency/number label — strips symbols, separators and suffixes.</summary>
    public decimal Number(string automationId, TimeSpan? timeout = null)
    {
        var raw = Text(automationId, timeout);
        var digits = new string(raw.Where(c => char.IsDigit(c) || c is '.' or '-').ToArray());

        if (decimal.TryParse(digits, out var value)) return value;

        throw new AssertionException(
            $"Could not read a number from '{automationId}'. Displayed text was '{raw}'.");
    }

    // ── Acting ───────────────────────────────────────────────────────────────────

    public void Tap(string automationId, TimeSpan? timeout = null)
    {
        // One retry: a tap can land exactly as the page re-renders and invalidates the node.
        try
        {
            Find(automationId, timeout).Click();
        }
        catch (StaleElementReferenceException)
        {
            Find(automationId, timeout).Click();
        }
    }

    public void TapText(string text, TimeSpan? timeout = null)
    {
        try
        {
            FindByText(text, timeout).Click();
        }
        catch (StaleElementReferenceException)
        {
            FindByText(text, timeout).Click();
        }
    }

    /// <summary>
    /// Taps the first visible element whose text <em>contains</em> the given fragment. For option
    /// rows whose full label carries decoration a test should not have to spell out — currency reads
    /// "US Dollar (USD) $", so matching on "US Dollar" is both clearer and less brittle.
    /// Deliberately not the default: tab titles need exact matching to stay unambiguous.
    /// </summary>
    public void TapTextContaining(string fragment, TimeSpan? timeout = null)
    {
        var limit = timeout ?? UITestConfig.DefaultTimeout;
        var deadline = DateTime.UtcNow + limit;
        var by = MobileBy.XPath(
            $"//*[contains(@text,'{fragment}') or contains(@label,'{fragment}')" +
            $" or contains(@name,'{fragment}') or contains(@content-desc,'{fragment}')]");

        while (true)
        {
            try
            {
                var match = Raw.FindElements(by).Cast<AppiumElement>().FirstOrDefault(e => e.Displayed);
                if (match is not null)
                {
                    match.Click();
                    return;
                }
            }
            catch (WebDriverException) { /* tree rebuilding — retry */ }

            if (DateTime.UtcNow >= deadline)
            {
                throw new AssertionException(
                    $"No visible element containing '{fragment}' appeared within " +
                    $"{limit.TotalSeconds:0}s on {Platform}.");
            }

            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>
    /// Taps the <paramref name="index"/>th of <paramref name="count"/> equal-width segments inside a
    /// control, using the control's <em>own</em> bounds to work out where.
    /// <para>
    /// Needed because Syncfusion draws <c>SfSegmentedControl</c>'s labels: they are plainly visible
    /// on screen but carry no findable text, so they cannot be tapped by name. The control itself is
    /// addressable and its segments are evenly spaced, so the position is derived rather than
    /// hard-coded — no absolute screen coordinates are involved.
    /// </para>
    /// </summary>
    public void TapSegment(string automationId, int index, int count)
    {
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Segment {index} is outside 0..{count - 1}.");

        var element = ScrollTo(automationId);
        var origin = element.Location;
        var size = element.Size;

        var x = origin.X + (int)(size.Width * ((index + 0.5) / count));
        var y = origin.Y + (size.Height / 2);

        TapAt(x, y);
    }

    /// <summary>A single tap at a viewport coordinate. Prefer Tap(automationId) wherever possible.</summary>
    public void TapAt(int x, int y)
    {
        var finger = new PointerInputDevice(PointerKind.Touch, "finger");
        var sequence = new ActionSequence(finger);
        sequence.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, x, y, TimeSpan.Zero));
        sequence.AddAction(finger.CreatePointerDown(MouseButton.Left));
        sequence.AddAction(finger.CreatePointerUp(MouseButton.Left));

        Raw.PerformActions([sequence]);
    }

    /// <summary>Taps an element a number of times — for the app's −/+ stepper buttons.</summary>
    public void TapRepeatedly(string automationId, int times)
    {
        for (var i = 0; i < times; i++) Tap(automationId);
    }

    /// <summary>
    /// Replaces the contents of a text field, dismisses the keyboard so the value is
    /// committed, then verifies the field really holds what was typed. The app binds its
    /// numeric fields to string properties via a plain <c>Entry</c>, so text changes
    /// propagate per keystroke; the keyboard dismissal is what makes any
    /// focus-change-driven recalculation fire.
    /// <para>
    /// The read-back is not paranoia: dropped or misdirected keystrokes are the most common
    /// mobile UI-test flake, and without it a partial entry shows up much later as a
    /// nonsensical assertion failure instead of a clear one here.
    /// </para>
    /// </summary>
    public void EnterText(string automationId, string text)
    {
        // Close any keyboard left open by a previous field first: on Android it covers the
        // lower half of the screen, which hides the next field and makes it unfindable.
        DismissKeyboard();

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            ClearText(automationId);
            Find(automationId).SendKeys(text);
            DismissKeyboard();

            if (DigitsOf(Text(automationId)) == DigitsOf(text)) return;
        }

        throw new AssertionException(
            $"'{automationId}' still reads '{Text(automationId)}' after typing '{text}' twice. " +
            "The keystrokes are landing somewhere else, or the field reformats its input " +
            "in a way this comparison does not expect.");
    }

    /// <summary>Digits only, so typed "700000" compares equal to displayed "700,000".</summary>
    private static string DigitsOf(string text) =>
        new(text.Where(char.IsDigit).ToArray());

    public void ClearText(string automationId)
    {
        var element = Find(automationId);
        element.Click();
        element.Clear();

        var remaining = element.Text;
        if (string.IsNullOrEmpty(remaining)) return;

        // Clear() is unreliable on some MAUI/Syncfusion fields — fall back to backspaces.
        element.SendKeys(string.Concat(Enumerable.Repeat(Keys.Backspace, remaining.Length + 2)));
    }

    public void DismissKeyboard()
    {
        // Not on Android: Appium's hideKeyboard there can fall back to pressing BACK, which
        // dismisses the SfPopup the field lives in. The symptom is brutal to diagnose — the
        // first field in a popup accepts text, then the next one is "not found" because the
        // whole popup silently closed. Android's plain Entry updates its binding per keystroke
        // anyway, so no blur is needed, and Find() scrolls past a covering keyboard.
        if (Platform == TestPlatform.Android) return;

        try
        {
            Raw.HideKeyboard();
        }
        catch (WebDriverException)
        {
            // No keyboard on screen, or the platform refuses to hide it. Not worth failing a test.
        }
    }

    // ── Scrolling ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Swipes the page until the element is on screen. Uses W3C pointer actions rather than
    /// a platform-specific scroll command so one implementation covers both drivers.
    /// </summary>
    public AppiumElement ScrollTo(string automationId, int maxSwipes = 12, bool up = false)
    {
        // Resolve only ever returns visible elements, so a non-null result means "already there".
        var existing = Resolve(automationId, TimeSpan.FromSeconds(1), out _);
        if (existing is not null) return existing;

        for (var i = 0; i < maxSwipes; i++)
        {
            SwipePage(towardsTop: !up);

            var found = Resolve(automationId, TimeSpan.FromMilliseconds(400), out _);
            if (found is not null) return found;
        }

        throw new AssertionException(
            $"Scrolled {maxSwipes} times without bringing '{automationId}' into view on {Platform}.");
    }

    /// <summary>
    /// Top edge of the on-screen keyboard, or <c>null</c> when none is showing.
    /// </summary>
    /// <remarks>
    /// iOS only: the keyboard is a real element (<c>XCUIElementTypeKeyboard</c>) with a frame.
    /// Android's IME is a separate window and does not appear in the accessibility tree, so there
    /// is nothing to measure — see the note in <see cref="DismissKeyboard"/>.
    /// </remarks>
    private int? KeyboardTop()
    {
        if (Platform != TestPlatform.iOS) return null;

        try
        {
            var keyboard = Raw.FindElements(By.XPath("//XCUIElementTypeKeyboard"))
                .FirstOrDefault();
            if (keyboard?.Displayed != true) return null;

            var top = keyboard.Location.Y;

            // XCUIElementTypeKeyboard is NOT the top of the keyboard assembly. The numeric pad's
            // input accessory ("Done" toolbar) is a SIBLING sitting above it, with the keyboard's
            // own inputView container spanning the gap between them. Measured on iPhone 17 Pro:
            // toolbar 522..566, inputView 566..874, keyboard 583..816. Using the keyboard's 583
            // put the swipe start at 571 — inside that stack, where the drag is swallowed and
            // scrolls nothing at all. That is not a slow scroll, it is zero scroll: ScrollTo
            // burned all 12 swipes with the content still at offset 0, and then reported the
            // target as un-scrollable-to, which reads exactly like a broken locator.
            foreach (var bar in Raw.FindElements(By.XPath("//XCUIElementTypeToolbar")))
            {
                if (!bar.Displayed) continue;

                var barTop = bar.Location.Y;
                var barBottom = barTop + bar.Size.Height;

                // Only the accessory directly above the keyboard — not app chrome elsewhere.
                if (barTop < top && barBottom >= top - 80) top = barTop;
            }

            return top;
        }
        catch (WebDriverException)
        {
            // No keyboard, or the driver would not answer. Swipe with the default band.
            return null;
        }
    }

    /// <summary>One page-ish swipe. <paramref name="towardsTop"/> drags content up, i.e. scrolls down.</summary>
    public void SwipePage(bool towardsTop = true)
    {
        var size = Raw.Manage().Window.Size;
        var x = size.Width / 2;

        // Stay well inside the safe area: starting near the top edge opens the notification
        // shade on Android, and near the bottom it grabs the home indicator on iOS.
        var high = (int)(size.Height * 0.25);
        var low = (int)(size.Height * 0.75);

        // Keep the entire drag above the software keyboard. On iOS it is forced present for the
        // whole session (forceSimulatorSoftwareKeyboardPresence), so it covers the bottom ~27%
        // even with no field focused — and 0.75 * height lands squarely on the keypad. The
        // gesture then TYPES instead of scrolling: a swipe down the middle of a numeric pad
        // presses 2 and 5, silently corrupting whichever field still has focus while the
        // element you wanted never comes into view.
        if (KeyboardTop() is int keyboardTop && keyboardTop > 120)
        {
            low = Math.Min(low, keyboardTop - 12);
            // Preserve a usable drag distance if that squeezed the usable band.
            if (low - high < 120) high = Math.Max(1, low - 120);
        }

        var (startY, endY) = towardsTop ? (low, high) : (high, low);

        var finger = new PointerInputDevice(PointerKind.Touch, "finger");
        var sequence = new ActionSequence(finger);
        sequence.AddAction(finger.CreatePointerMove(
            CoordinateOrigin.Viewport, x, startY, TimeSpan.Zero));
        sequence.AddAction(finger.CreatePointerDown(MouseButton.Left));
        sequence.AddAction(finger.CreatePointerMove(
            CoordinateOrigin.Viewport, x, endY, TimeSpan.FromMilliseconds(400)));
        sequence.AddAction(finger.CreatePointerUp(MouseButton.Left));

        Raw.PerformActions([sequence]);
    }

    // ── Diagnostics ──────────────────────────────────────────────────────────────

    public byte[] Screenshot() => Raw.GetScreenshot().AsByteArray;

    public string PageSource => Raw.PageSource;

    public void Dispose()
    {
        try
        {
            Raw.Quit();
        }
        catch (WebDriverException)
        {
            // The session may already be gone (server restarted, device reset). Nothing to do.
        }
        finally
        {
            Raw.Dispose();
        }
    }
}
