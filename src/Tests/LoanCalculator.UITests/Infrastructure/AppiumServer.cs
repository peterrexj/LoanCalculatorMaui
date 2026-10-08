using System.Diagnostics;
using System.Net.Http;

namespace LoanCalculator.UITests.Infrastructure;

/// <summary>
/// Attaches to an Appium server that is already listening, or starts the repo-local one
/// (<c>npm run start</c> in this project) and owns its lifetime. Nothing is killed that we
/// did not start, so a server you launched by hand in another terminal survives the run.
/// </summary>
public sealed class AppiumServer : IDisposable
{
    private readonly Uri _uri;
    private Process? _ownedProcess;

    private AppiumServer(Uri uri) => _uri = uri;

    public static AppiumServer StartOrAttach(Uri uri, bool autoStart)
    {
        var server = new AppiumServer(uri);

        if (server.IsUp())
        {
            TestContext.Progress.WriteLine($"[appium] attaching to existing server at {uri}");
            return server;
        }

        if (!autoStart)
        {
            throw new InvalidOperationException(
                $"No Appium server at {uri} and UITEST_AUTOSTART_APPIUM is off. " +
                $"Start one with: cd src/Tests/LoanCalculator.UITests && npm run start");
        }

        server.Launch();
        return server;
    }

    private void Launch()
    {
        var projectDir = Path.Combine(
            UITestConfig.RepoRoot, "src", "Tests", "LoanCalculator.UITests");

        var appiumHome = Path.Combine(projectDir, ".appium");
        if (!Directory.Exists(appiumHome))
        {
            throw new InvalidOperationException(
                $"Appium drivers are not installed at {appiumHome}. Run: " +
                $"cd src/Tests/LoanCalculator.UITests && npm install && npm run drivers");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "npm",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("start");

        // Pass the configured port through to appium. Without this the npm script's own default
        // wins and UITEST_APPIUM_URL is quietly ignored — which is fine for a single run, but
        // means every shard of a parallel run tries to bind the same port and they collide.
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(UITestConfig.AppiumUri.Port.ToString());

        startInfo.Environment["APPIUM_HOME"] = appiumHome;

        var logPath = Path.Combine(UITestConfig.ArtifactDirectory, "appium-server.log");
        Directory.CreateDirectory(UITestConfig.ArtifactDirectory);
        var log = new StreamWriter(logPath, append: false) { AutoFlush = true };

        TestContext.Progress.WriteLine($"[appium] starting server; log -> {logPath}");

        _ownedProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the Appium server process.");

        _ownedProcess.OutputDataReceived += (_, e) => { if (e.Data is not null) log.WriteLine(e.Data); };
        _ownedProcess.ErrorDataReceived += (_, e) => { if (e.Data is not null) log.WriteLine(e.Data); };
        _ownedProcess.BeginOutputReadLine();
        _ownedProcess.BeginErrorReadLine();

        WaitUntilUp(TimeSpan.FromSeconds(90), logPath);
        TestContext.Progress.WriteLine($"[appium] server ready at {_uri}");
    }

    private void WaitUntilUp(TimeSpan timeout, string logPath)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_ownedProcess?.HasExited == true)
            {
                throw new InvalidOperationException(
                    $"The Appium server exited with code {_ownedProcess.ExitCode} during startup. " +
                    $"See {logPath}.");
            }

            if (IsUp()) return;
            Thread.Sleep(500);
        }

        throw new TimeoutException(
            $"Appium server did not become ready at {_uri} within {timeout.TotalSeconds:0}s. See {logPath}.");
    }

    private bool IsUp()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = http.GetAsync(new Uri(_uri, "/status")).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            // Connection refused / DNS / timeout all mean "not listening yet".
            return false;
        }
    }

    public void Dispose()
    {
        if (_ownedProcess is null || _ownedProcess.HasExited)
        {
            _ownedProcess?.Dispose();
            return;
        }

        TestContext.Progress.WriteLine("[appium] stopping server we started");
        try
        {
            // Kill the whole tree: `npm run start` spawns node as a child.
            _ownedProcess.Kill(entireProcessTree: true);
            _ownedProcess.WaitForExit(10_000);
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[appium] could not stop server cleanly: {ex.Message}");
        }
        finally
        {
            _ownedProcess.Dispose();
        }
    }
}
