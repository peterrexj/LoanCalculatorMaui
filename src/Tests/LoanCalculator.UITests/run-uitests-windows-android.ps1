# Runs the cross-platform UI suite against one Android device at a time, from Windows.
#
# Usage:
#   pwsh ./run-uitests-windows-android.ps1                                   # first available AVD
#   pwsh ./run-uitests-windows-android.ps1 -Avd Medium_Phone_API_36.1
#   pwsh ./run-uitests-windows-android.ps1 -NoBuild                          # reuse existing APK
#   pwsh ./run-uitests-windows-android.ps1 -Filter "TestCategory=Smoke"
#   pwsh ./run-uitests-windows-android.ps1 -Fresh                            # reinstall first, empty app data
#
# This is the Windows/Android port of run-uitests.sh (macOS, iOS+Android). There is no iOS
# equivalent — iOS simulators do not exist on Windows. To shard across several emulators, see
# run-uitests-parallel-windows-android.ps1.
#
# Requires PowerShell 7+ (pwsh). Several fixes below (quoting, native-command exit-code handling)
# were verified empirically against pwsh's actual behaviour — see the comments inline — because
# this script could not be tested on a real Windows/Android machine before being written; it was
# ported from run-uitests.sh's Android branch logic rather than run interactively on the target.
#
# Env overrides (used by the parallel runner, usable by hand too):
#   UITEST_SERIAL        target one specific Android emulator (required if several are running)
#   UITEST_APPIUM_URL    e.g. http://127.0.0.1:4733 — each concurrent run needs its own port
#   UITEST_ARTIFACTS     per-run artifact dir, so shards do not overwrite each other's logs
#   UITEST_SHARD_INDEX   offsets systemPort so concurrent sessions do not collide
#
# Ported from run-uitests.sh's Android branch — see that file for the macOS/iOS half and for the
# reasoning behind each of the animation-scale and stale-build checks below. Keep the two in sync
# when either changes; nothing here should drift from it on purpose.

param(
    [string]$Avd = "",
    [switch]$NoBuild,
    [switch]$Fresh,
    [string]$Filter = ""
)

if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Error "This script needs PowerShell 7+ (pwsh). You're running $($PSVersionTable.PSVersion) — install from https://aka.ms/powershell and re-run as 'pwsh -File run-uitests-windows-android.ps1 ...'."
    exit 1
}

$ErrorActionPreference = "Stop"
# Pin this explicitly rather than trust whichever default this pwsh version ships with — it has
# changed across PowerShell 7.x releases. False means a non-zero exit code from a native command
# (adb, dotnet, npm, emulator) does NOT throw; every exit code this script cares about is already
# checked by hand via $LASTEXITCODE immediately after the call, and every "best effort, ignore
# failure" call (uninstall, animation-scale writes) depends on that NOT throwing.
$PSNativeCommandUseErrorActionPreference = $false

# Wraps a value for -ArgumentList. Start-Process joins ArgumentList entries with a bare space
# rather than passing them to the child process as a true argv array (confirmed empirically: an
# unquoted path containing a space is silently split into two arguments and the child process
# fails to find its own script file). Any value that can plausibly contain a space — a path under
# a Windows user profile, in particular — must go through this before being added to an argument
# list, including this script's own future use of Start-Process for the app under test if that
# ever changes.
function Protect-Arg([string]$Value) {
    if ($Value -match '\s') { return '"' + $Value + '"' }
    return $Value
}

# Reduces any native command's captured output (0, 1, or many lines) to a single trimmed string
# without ever throwing — a bare `(cmd).Trim()` throws "cannot call a method on a null-valued
# expression" the moment a poll returns nothing (confirmed empirically; this is exactly what
# happens transiently while an emulator is still coming up), and `.ToString()` on a multi-line
# result returns the literal text "System.Object[]" instead of the lines (also confirmed). Both
# are real failure modes for the exact adb calls below, not theoretical.
function ConvertTo-SingleLine($RawOutput) {
    if ($null -eq $RawOutput) { return "" }
    return ($RawOutput | Out-String).Trim()
}

$ScriptDir = $PSScriptRoot
$RepoRoot  = (Resolve-Path (Join-Path $ScriptDir "../../..")).Path
$AppDir    = Join-Path $RepoRoot "src/LoanCalculator"
$BundleId  = "com.pj.loan.afford.calc"

# ── First-run setup ───────────────────────────────────────────────────────────

if (-not (Test-Path (Join-Path $ScriptDir "node_modules")) -or -not (Test-Path (Join-Path $ScriptDir ".appium"))) {
    Write-Host "==> Installing Appium and its drivers (first run only)..."
    Push-Location $ScriptDir
    try {
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
        npm run drivers
        if ($LASTEXITCODE -ne 0) { throw "npm run drivers failed" }
    } finally { Pop-Location }
}

$env:UITEST_PLATFORM = "android"

# Mirror UITestConfig's default so this script and the tests agree on the endpoint.
if (-not $env:UITEST_APPIUM_URL) { $env:UITEST_APPIUM_URL = "http://127.0.0.1:4723" }
$env:UITEST_FRESH_INSTALL = if ($Fresh) { "true" } else { "false" }

# ── Locate Android SDK and Java ───────────────────────────────────────────────

$SdkRoot = $env:ANDROID_HOME
if (-not $SdkRoot) { $SdkRoot = $env:ANDROID_SDK_ROOT }
if (-not $SdkRoot) {
    foreach ($c in @("$env:LOCALAPPDATA\Android\Sdk", "$env:USERPROFILE\AppData\Local\Android\Sdk")) {
        if (Test-Path $c) { $SdkRoot = $c; break }
    }
}
if (-not $SdkRoot -or -not (Test-Path $SdkRoot)) {
    Write-Error "Android SDK not found. Set ANDROID_HOME or ANDROID_SDK_ROOT."
    exit 1
}
$env:ANDROID_HOME = $SdkRoot

$Adb      = Join-Path $SdkRoot "platform-tools\adb.exe"
$Emulator = Join-Path $SdkRoot "emulator\emulator.exe"
if (-not (Test-Path $Adb)) { Write-Error "adb.exe not found at $Adb — check ANDROID_HOME/ANDROID_SDK_ROOT."; exit 1 }
if (-not (Test-Path $Emulator)) { Write-Error "emulator.exe not found at $Emulator — check ANDROID_HOME/ANDROID_SDK_ROOT."; exit 1 }

# The .NET Android manifest merger needs a JDK 17+ — mirror run-android.ps1's search.
if (-not $env:JAVA_HOME) {
    $javaCandidates = @(
        "$env:ProgramFiles\Android\Android Studio\jbr",
        "$env:ProgramFiles\Microsoft\jdk-21*",
        "$env:ProgramFiles\Microsoft\jdk-17*",
        "$env:ProgramFiles\Eclipse Adoptium\jdk-21*",
        "$env:ProgramFiles\Eclipse Adoptium\jdk-17*",
        "$env:ProgramFiles\Java\jdk-21*",
        "$env:ProgramFiles\Java\jdk-17*"
    )
    foreach ($c in $javaCandidates) {
        foreach ($p in @(Resolve-Path -Path $c -ErrorAction SilentlyContinue | ForEach-Object { $_.Path })) {
            if (Test-Path (Join-Path $p "bin\java.exe")) { $env:JAVA_HOME = $p; break }
        }
        if ($env:JAVA_HOME) { break }
    }
}

# ── Start or reuse an emulator ────────────────────────────────────────────────

$running = & $Adb devices | Select-String "emulator" | Select-String "device$"
if (-not $running) {
    $EmulatorName = $Avd
    if (-not $EmulatorName) {
        $EmulatorName = (& $Emulator -list-avds 2>$null | Select-Object -First 1)
    }
    if (-not $EmulatorName) {
        Write-Error "No AVDs found. Create one in Android Studio first."
        exit 1
    }
    Write-Host "==> Starting emulator '$EmulatorName'..."
    Start-Process -FilePath $Emulator `
        -ArgumentList (Protect-Arg "-avd"), (Protect-Arg $EmulatorName), (Protect-Arg "-no-snapshot-save") `
        -WindowStyle Hidden
    & $Adb wait-for-device

    # ConvertTo-SingleLine: an empty poll while the emulator is still coming up must retry, not
    # crash — see the function's own comment for the exact failure this avoids.
    $booted = ""
    $bootWaitSeconds = 0
    $bootWaitLimitSeconds = 300
    while ($booted -ne "1") {
        if ($bootWaitSeconds -ge $bootWaitLimitSeconds) {
            Write-Error "Emulator did not finish booting within $bootWaitLimitSeconds seconds."
            exit 1
        }
        Start-Sleep -Seconds 2
        $bootWaitSeconds += 2
        $raw = & $Adb shell getprop sys.boot_completed 2>$null
        $booted = ConvertTo-SingleLine $raw
    }
    Write-Host "    Emulator ready."
} else {
    Write-Host "==> Using the emulator that is already running."
}

# An explicit serial wins. With more than one emulator up (a sharded run), picking the first one
# arbitrarily means the animation-scale writes below — which are per-device GLOBAL settings that
# survive reboots — can land on the wrong device.
if ($env:UITEST_SERIAL) {
    $Serial = $env:UITEST_SERIAL
} else {
    $devices = @(& $Adb devices | Select-String "emulator" | Select-String "device$")
    if ($devices.Count -eq 0) {
        Write-Error "No emulator device is visible to adb even after starting/checking one. Run 'adb devices' by hand to see what adb sees."
        exit 1
    }
    $Serial = $devices[0].ToString().Split("`t")[0].Trim()
    if ($devices.Count -gt 1) {
        Write-Host "    WARNING: $($devices.Count) emulators are running and no UITEST_SERIAL was set;"
        Write-Host "             picking '$Serial'. Set UITEST_SERIAL to target one deliberately."
    }
}
Write-Host "    Serial: $Serial"

# ── Animation scales ───────────────────────────────────────────────────────────
#
# Reduce tap-timing flake by removing SYSTEM animations — but never touch
# animator_duration_scale. That one drives MAUI's animation framework: at 0, FadeTo/ScaleTo never
# signal completion, so SplashPage never hands off to the Shell and the app sits on the native
# splash forever. These settings are global and survive app reinstalls and emulator reboots, so a
# stray 0 here looks like a permanently broken app days later on a totally unrelated run. Self-heal
# rather than trust a captured "original" value — see run-uitests.sh for the incident where a crashed
# run with a zeroed scale got captured as the baseline and every later run "restored" the corruption.

$animScales = @("window_animation_scale", "transition_animation_scale")

foreach ($scale in @("animator_duration_scale") + $animScales) {
    $raw = & $Adb -s $Serial shell settings get global $scale 2>$null
    $current = ConvertTo-SingleLine $raw
    if ($current -in @("0", "0.0", "", "null")) {
        Write-Host "    NOTE: $scale was '$(if ($current) { $current } else { 'unset' })' — repairing to 1.0."
        if ($scale -eq "animator_duration_scale") {
            Write-Host "          At 0 the app hangs on the native splash and every test fails at launch."
        }
        & $Adb -s $Serial shell settings put global $scale 1.0 2>$null | Out-Null
    }
}

foreach ($scale in $animScales) {
    & $Adb -s $Serial shell settings put global $scale 0 2>$null | Out-Null
}

# Always restore 1.0 — not a captured value — on normal completion, a handled failure (every
# early `exit` below is inside this try), or an unhandled terminating error. (Verified
# empirically: `exit` inside a try still runs an enclosing `finally` before the process actually
# terminates, and the exit code set by `exit $LASTEXITCODE` later in this script still reaches the
# caller afterwards.)
#
# What this does NOT catch: Ctrl+C while blocked inside a synchronous native call (adb/dotnet),
# or the process being killed outright (Task Manager, a parent script giving up on it). The bash
# original covers that with `trap ... EXIT INT TERM`; there is no equivalent verified here — a
# PowerShell engine-exiting event was tried and its behaviour under a real Windows Ctrl+C could
# not be confirmed from this environment, so it was left out rather than shipped half-verified.
# If a run is killed before finishing, the two system animation scales are left at 0 on that
# emulator. Recover by hand:
#   adb -s <serial> shell settings put global window_animation_scale 1.0
#   adb -s <serial> shell settings put global transition_animation_scale 1.0
$restoreScales = {
    foreach ($scale in $animScales) {
        & $Adb -s $Serial shell settings put global $scale 1.0 2>$null | Out-Null
    }
}

try {

    # ── Build ──────────────────────────────────────────────────────────────────

    if (-not $NoBuild) {
        Write-Host "==> Building Android app..."
        $javaSdkArg = if ($env:JAVA_HOME) { "-p:JavaSdkDirectory=$($env:JAVA_HOME)" } else { $null }
        dotnet build (Join-Path $AppDir "LoanCalculatorMaui.csproj") `
            -f net10.0-android36.0 -c Debug `
            -p:AndroidSdkDirectory=$SdkRoot -p:EmbedAssembliesIntoApk=true $javaSdkArg
        if ($LASTEXITCODE -ne 0) { exit 1 }
    }

    $outDir = Join-Path $AppDir "bin\Debug\net10.0-android36.0"
    $apk = Get-ChildItem $outDir -Filter "*-Signed.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $apk) {
        Write-Error "No signed APK found under $outDir. Run without -NoBuild."
        exit 1
    }

    # Install here rather than letting Appium do it: with noReset the driver skips installation
    # when the bundle id is already present, which silently tests a stale build.
    if ($Fresh) {
        Write-Host "==> Uninstalling for a clean data directory..."
        & $Adb -s $Serial uninstall $BundleId 2>$null | Out-Null
    }
    Write-Host "==> Installing app..."
    & $Adb -s $Serial install -r $apk.FullName
    if ($LASTEXITCODE -ne 0) {
        Write-Host "    Install failed — uninstalling and retrying..." -ForegroundColor Yellow
        & $Adb -s $Serial uninstall $BundleId 2>$null | Out-Null
        & $Adb -s $Serial install $apk.FullName
        if ($LASTEXITCODE -ne 0) { exit 1 }
    }

    $env:UITEST_DEVICE = "Android Emulator"
    $env:UITEST_UDID = $Serial
    $env:UITEST_APP = $apk.FullName
    if ($Avd) { $env:UITEST_AVD = $Avd }

    # -NoBuild is the fast path, but it tests whatever was built last. Two ways that bites, both
    # indistinguishable from a real regression: a test looking for an AutomationId that only
    # exists in newer XAML fails as "element not found", and an app fix that only exists in newer
    # C# fails as the very bug it fixes. Warn loudly rather than let either eat an afternoon.
    if ($NoBuild) {
        $apkTime = $apk.LastWriteTimeUtc
        $staleXaml = Get-ChildItem (Join-Path $AppDir "View"), (Join-Path $AppDir "Controls") `
            -Filter "*.xaml" -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTimeUtc -gt $apkTime }
        $staleCs = Get-ChildItem (Join-Path $AppDir "View"), (Join-Path $AppDir "Controls"), (Join-Path $RepoRoot "src\LoanCalculator.Core") `
            -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' -and $_.LastWriteTimeUtc -gt $apkTime }
        if ($staleXaml -or $staleCs) {
            Write-Host ""
            Write-Host "WARNING: -NoBuild, but these sources are NEWER than the app you are about to test:" -ForegroundColor Yellow
            $staleXaml | ForEach-Object { Write-Host "         $($_.Name)" }
            $staleCs   | ForEach-Object { Write-Host "         $($_.Name)" }
            Write-Host "         A new AutomationId will not be found, and any app fix in these files is NOT"
            Write-Host "         in this build. Re-run without -NoBuild before trusting a failure."
            Write-Host ""
        }
    }

    # An Appium server started by hand does not inherit the ANDROID_HOME exported above, and every
    # Android session then fails with "Neither ANDROID_HOME nor ANDROID_SDK_ROOT ... was exported".
    try {
        $probe = Invoke-WebRequest -Uri "$($env:UITEST_APPIUM_URL)/status" -TimeoutSec 3 -ErrorAction SilentlyContinue
        if ($probe) {
            Write-Host ""
            Write-Host "NOTE: attaching to an Appium server that is already running."
            Write-Host "      If it was started by hand, it must have been started with ANDROID_HOME set —"
            Write-Host "      'npm run start' does that. Otherwise every Android test will fail in OneTimeSetUp."
            Write-Host ""
        }
    } catch { }

    Write-Host "==> Running UI tests on android"
    Write-Host "    App: $($env:UITEST_APP)"

    $testArgs = @("test", (Join-Path $ScriptDir "LoanCalculator.UITests.csproj"), "--nologo")
    if ($Filter) { $testArgs += @("--filter", $Filter) }

    dotnet @testArgs
    exit $LASTEXITCODE

} finally {
    & $restoreScales
}
