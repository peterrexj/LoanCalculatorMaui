namespace LoanCalculator.UITests.Infrastructure;

public enum TestPlatform
{
    Android,
    iOS,
}

/// <summary>
/// Every knob the suite needs, resolved once from environment variables with defaults
/// derived from this repo's layout. run-uitests.sh sets these; a bare `dotnet test` picks
/// up the defaults and targets the iOS simulator.
/// </summary>
public static class UITestConfig
{
    public const string BundleId = "com.pj.loan.afford.calc";
    public const string IosTargetFramework = "net10.0-ios26.5";
    public const string AndroidTargetFramework = "net10.0-android36.0";

    public static TestPlatform Platform { get; }
    public static string DeviceName { get; }
    public static string? Udid { get; }
    public static string? AvdName { get; }
    public static string AppPath { get; }
    public static Uri AppiumUri { get; }

    /// <summary>Wipe app data by reinstalling at session start. Off by default: reinstall costs ~30s.</summary>
    public static bool FreshInstall { get; }

    /// <summary>Terminate + relaunch the app before each test so navigation state never leaks between tests.</summary>
    public static bool RestartBetweenTests { get; }

    public static TimeSpan DefaultTimeout { get; }
    public static TimeSpan LaunchTimeout { get; }

    /// <summary>
    /// How long to wait for <c>POST /session</c> to come back. Deliberately separate from
    /// <see cref="LaunchTimeout"/>: that one budgets "the app reached its landing tab", this one
    /// budgets Appium standing up a driver session.
    /// </summary>
    /// <remarks>
    /// Must exceed the <c>wdaLaunchTimeout</c>/<c>wdaConnectionTimeout</c> handed to the iOS driver
    /// (240s), or the client hangs up while Appium is still legitimately waiting. The first session
    /// against a newly created simulator — or a freshly downloaded runtime — has to build and
    /// install WebDriverAgent, which takes minutes; later runs reuse it and connect in seconds.
    /// </remarks>
    public static TimeSpan SessionTimeout { get; }

    /// <summary>
    /// How long Appium waits for a reply to a <em>single</em> request it proxies to WebDriverAgent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Distinct from <see cref="SessionTimeout"/> and from <c>wdaLaunchTimeout</c>: this one is
    /// charged <b>per command</b>, so it is what a sick WDA costs you over and over.
    /// </para>
    /// <para>
    /// It defaulted to Appium's 240s, and that dominated a real run: WDA degraded in the back half
    /// of a 41-test suite and three tests hit five or six timeouts each, turning 90-second failures
    /// into ~20-minute ones — <b>65 of the run's 100 minutes went on three failures</b>. The fix is
    /// to fail fast per command; building WDA on a fresh simulator is still covered, because that
    /// is budgeted by <c>wdaLaunchTimeout</c>, not this.
    /// </para>
    /// <para>
    /// Raise it with <c>UITEST_WDA_CONNECTION_TIMEOUT_SECONDS</c> if a first-ever run against a
    /// brand-new simulator reports connection timeouts rather than a build failure.
    /// </para>
    /// </remarks>
    public static TimeSpan WdaConnectionTimeout { get; }

    /// <summary>
    /// Per-shard offsets so several devices can be driven at once. Appium needs a unique
    /// <c>wdaLocalPort</c> (iOS) / <c>systemPort</c> (Android) per concurrent session; sharing one
    /// makes sessions collide in ways that read as random element-not-found failures.
    /// </summary>
    public static int ShardIndex { get; }
    public static string RepoRoot { get; }
    public static string ArtifactDirectory { get; }

    /// <summary>Start an Appium server if one is not already listening on <see cref="AppiumUri"/>.</summary>
    public static bool AutoStartAppium { get; }

    /// <summary>
    /// The suite is opt-in: it needs a booted device and an installed build, so a
    /// solution-wide <c>dotnet test</c> skips it instead of failing. run-uitests.sh sets
    /// UITEST_PLATFORM, which is what turns it on.
    /// </summary>
    public static bool IsEnabled =>
        Environment.GetEnvironmentVariable("UITEST_PLATFORM") is { Length: > 0 };

    static UITestConfig()
    {
        RepoRoot = FindRepoRoot();
        Platform = ParsePlatform(Env("UITEST_PLATFORM"));

        AppiumUri = new Uri(Env("UITEST_APPIUM_URL") ?? "http://127.0.0.1:4723");
        AutoStartAppium = Flag("UITEST_AUTOSTART_APPIUM", @default: true);
        FreshInstall = Flag("UITEST_FRESH_INSTALL", @default: false);
        RestartBetweenTests = Flag("UITEST_RESTART_PER_TEST", @default: true);
        DefaultTimeout = TimeSpan.FromSeconds(Number("UITEST_TIMEOUT_SECONDS", 20));
        // Android gets a much larger launch budget than iOS. This app's Debug cold start measured
        // 23.1s on an unloaded emulator (`am start -W` TotalTime) — LoanView's XAML inflation alone
        // was 19.4s — and the app is relaunched before every test. Under any host load that
        // comfortably exceeds 90s, which fails BaseSetUp and takes the whole fixture with it.
        var launchDefault = Platform == TestPlatform.Android ? 180 : 90;
        LaunchTimeout = TimeSpan.FromSeconds(Number("UITEST_LAUNCH_TIMEOUT_SECONDS", launchDefault));

        // 300s > the 240s wdaLaunchTimeout in BuildIosOptions, with headroom.
        SessionTimeout = TimeSpan.FromSeconds(Number("UITEST_SESSION_TIMEOUT_SECONDS", 300));

        WdaConnectionTimeout =
            TimeSpan.FromSeconds(Number("UITEST_WDA_CONNECTION_TIMEOUT_SECONDS", 60));

        ShardIndex = (int)Number("UITEST_SHARD_INDEX", 0);

        Udid = Env("UITEST_UDID");
        AvdName = Env("UITEST_AVD");

        DeviceName = Env("UITEST_DEVICE")
            // iPhone 17 Pro lives on the iOS 26.5 runtime, which matches the SDK the app is built
            // against (net10.0-ios26.5). The previous default, iPhone 16 Pro, only existed on an
            // iOS 18.4 runtime — so the suite was running 8 major versions below its build SDK.
            ?? (Platform == TestPlatform.iOS ? "iPhone 17 Pro" : "Android Emulator");

        AppPath = Env("UITEST_APP") ?? DefaultAppPath(Platform);

        ArtifactDirectory = Env("UITEST_ARTIFACTS")
            ?? Path.Combine(RepoRoot, "TestResults", "uitest-artifacts");
    }

    private static string DefaultAppPath(TestPlatform platform)
    {
        var appDir = Path.Combine(RepoRoot, "src", "LoanCalculator", "bin", "Debug");

        if (platform == TestPlatform.iOS)
        {
            return Path.Combine(
                appDir, IosTargetFramework, "iossimulator-arm64", "LoanCalculatorMaui.app");
        }

        var androidDir = Path.Combine(appDir, AndroidTargetFramework);
        if (!Directory.Exists(androidDir))
            return Path.Combine(androidDir, $"{BundleId}-Signed.apk");

        // Prefer the signed APK; an unsigned one cannot be installed.
        var signed = Directory
            .EnumerateFiles(androidDir, "*-Signed.apk", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        return signed ?? Path.Combine(androidDir, $"{BundleId}-Signed.apk");
    }

    private static TestPlatform ParsePlatform(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        null or "" => OperatingSystem.IsMacOS() ? TestPlatform.iOS : TestPlatform.Android,
        "ios" => TestPlatform.iOS,
        "android" => TestPlatform.Android,
        var other => throw new ArgumentException(
            $"UITEST_PLATFORM must be 'ios' or 'android', got '{other}'."),
    };

    /// <summary>Walks up from the test binary to the directory holding the solution file.</summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LoanCalculatorMaui.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate LoanCalculatorMaui.sln above the test binary. " +
            "Set UITEST_APP to the built .app/.apk path explicitly.");
    }

    private static string? Env(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool Flag(string key, bool @default) => Env(key) switch
    {
        null => @default,
        var v => v.Equals("1", StringComparison.OrdinalIgnoreCase)
              || v.Equals("true", StringComparison.OrdinalIgnoreCase)
              || v.Equals("yes", StringComparison.OrdinalIgnoreCase),
    };

    private static int Number(string key, int @default) =>
        int.TryParse(Env(key), out var parsed) ? parsed : @default;

    public static string Describe() =>
        $"""
         Platform ........... {Platform}
         Device ............. {DeviceName}{(Udid is null ? "" : $" (udid {Udid})")}{(AvdName is null ? "" : $" (avd {AvdName})")}
         App ................ {AppPath}
         Appium ............. {AppiumUri}
         Fresh install ...... {FreshInstall}
         Restart per test ... {RestartBetweenTests}
         Element timeout .... {DefaultTimeout.TotalSeconds:0}s
         Launch timeout ..... {LaunchTimeout.TotalSeconds:0}s
         Session timeout .... {SessionTimeout.TotalSeconds:0}s (first run builds WebDriverAgent)
         WDA per-command .... {WdaConnectionTimeout.TotalSeconds:0}s
         Shard index ........ {ShardIndex}
         Artifacts .......... {ArtifactDirectory}
         """;
}
