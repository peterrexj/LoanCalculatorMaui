# Shards the UI suite across several Android emulators, from Windows.
#
# Usage:
#   pwsh ./run-uitests-parallel-windows-android.ps1                         # 2 shards (the default)
#   pwsh ./run-uitests-parallel-windows-android.ps1 -Shards 3
#   pwsh ./run-uitests-parallel-windows-android.ps1 -Shards 2 -NoBuild
#   pwsh ./run-uitests-parallel-windows-android.ps1 -Filter "TestCategory=Smoke"
#
# This is the Windows/Android port of run-uitests-parallel.sh's --android path. There is no iOS
# equivalent here — iOS simulators do not exist on Windows, so --ios/--both are not offered.
#
# Requires PowerShell 7+ (pwsh). See the header comment in run-uitests-windows-android.ps1 for the
# empirically-verified quoting and native-exit-code gotchas this script and that one both guard
# against — this one was ported and reviewed the same way, not run on a real Windows machine.
#
# First-time setup: run run-uitests-windows-android.ps1 once by hand before using this script.
# Both scripts install Appium's node_modules/.appium on first use if missing; if two shards hit
# that check at the same moment, two concurrent `npm install`s can corrupt node_modules. The macOS
# original has the same latent race for the same reason — it is not new here.
#
# WHY SHARDING IS NOT THE FIRST LEVER, AND WHY THE DEFAULT IS 2
#
# See run-uitests-parallel.sh for the measured numbers: a degraded WebDriverAgent/driver session
# costs far more wall-clock than extra shards recover, and three-plus emulators on one host is a
# large load increase that can turn a stressed-animator slowdown into more flake, not just less
# wall-clock. Hence: default 2, measure, and only raise it if the host has headroom.
#
# WHAT EACH SHARD GETS ITS OWN COPY OF
#
# An Appium port, an artifacts directory (so shard.log is not overwritten), an emulator serial, and
# a shard index that offsets systemPort. Appium requires those ports to be unique per concurrent
# session; sharing them surfaces as random element-not-found failures rather than an obvious
# conflict.
#
# A shard must never share a device with another shard: the suite's budget data persists on the
# device and TestData latches its seeding in a static, so two shards on one device corrupt each
# other's starting state.

param(
    [int]$Shards = 2,
    [switch]$NoBuild,
    [string]$Filter = ""
)

if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Error "This script needs PowerShell 7+ (pwsh). You're running $($PSVersionTable.PSVersion) — install from https://aka.ms/powershell and re-run as 'pwsh -File run-uitests-parallel-windows-android.ps1 ...'."
    exit 1
}

$ErrorActionPreference = "Stop"
# See run-uitests-windows-android.ps1's header comment for why this is pinned explicitly rather
# than left to this pwsh install's default.
$PSNativeCommandUseErrorActionPreference = $false

if ($Shards -lt 1) {
    Write-Error "-Shards must be at least 1 (got $Shards)."
    exit 1
}

# Wraps a value for -ArgumentList. Start-Process joins ArgumentList entries with a bare space
# rather than passing them to the child process as a true argv array — confirmed empirically: an
# unquoted path containing a space (e.g. a typical "C:\Users\<name>\...\LoanCalculatorMaui" tree)
# is silently split into two arguments, and the spawned shard's pwsh fails before running a single
# test with a cryptic "is not recognized as the name of a script file" — not a test failure, a
# launch failure that looks like one. Every value placed into an -ArgumentList array below goes
# through this, including ones unlikely to contain a space, because the cost of being wrong once
# here is a shard that silently never runs.
function Protect-Arg([string]$Value) {
    if ($Value -match '\s') { return '"' + $Value + '"' }
    return $Value
}

$ScriptDir = $PSScriptRoot
$RepoRoot  = (Resolve-Path (Join-Path $ScriptDir "../../..")).Path
$AppDir    = Join-Path $RepoRoot "src/LoanCalculator"
$SingleRunner = Join-Path $ScriptDir "run-uitests-windows-android.ps1"
# Resolve the exact host executable this script is already running under (pwsh.exe's full path),
# rather than hoping a bare "pwsh" resolves on PATH the same way inside each spawned child.
$HostExe = (Get-Process -Id $PID).Path

# ── Partition the fixtures ────────────────────────────────────────────────────
#
# Shard by FIXTURE, not by test: fixtures hold per-class state (OneTimeSetUp, the TestData seeding
# latch) and splitting one across devices would re-seed and re-launch for no benefit.
#
# Read the fixtures out of the source rather than `dotnet test --list-tests`: that command prints
# bare METHOD names with no class or namespace, so there is nothing to group by. Every fixture here
# is one [TestFixture] class per file, so the source is the simpler source of truth and needs no
# build to enumerate.

Write-Host "==> Discovering fixtures..."
$testsDir = Join-Path $ScriptDir "Tests"
if (-not (Test-Path $testsDir)) {
    Write-Error "Tests directory not found at $testsDir."
    exit 1
}
$fixtures = @()
Get-ChildItem $testsDir -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
    $text = Get-Content $_.FullName -Raw
    if ($text -match '\[TestFixture\]') {
        $m = [regex]::Match($text, 'class\s+([A-Za-z0-9_]+)')
        if ($m.Success) { $fixtures += $m.Groups[1].Value }
    }
}
$fixtures = @($fixtures | Sort-Object -Unique)

if ($fixtures.Count -eq 0) {
    Write-Error "Could not discover any fixtures under $testsDir. Is the test project building?"
    exit 1
}

Write-Host "    $($fixtures.Count) fixtures across $Shards shard(s)"
if ($Shards -gt $fixtures.Count) {
    Write-Host "    Only $($fixtures.Count) fixtures, so capping shards at $($fixtures.Count)."
    $Shards = $fixtures.Count
}

function Get-ShardFilter([int]$Idx) {
    $parts = @()
    for ($i = 0; $i -lt $fixtures.Count; $i++) {
        if (($i % $Shards) -eq $Idx) { $parts += "FullyQualifiedName~$($fixtures[$i])" }
    }
    if ($Filter -and $parts.Count -gt 0) {
        # dotnet test has no grouping syntax, so an extra filter cannot be AND-ed across an OR
        # list. Honour the explicit filter alone rather than silently applying it to one shard only.
        return $Filter
    }
    return ($parts -join "|")
}

# ── Build once, not per shard ────────────────────────────────────────────────

if (-not $NoBuild) {
    Write-Host "==> Building Android once for all shards..."
    dotnet build (Join-Path $AppDir "LoanCalculatorMaui.csproj") -f net10.0-android36.0 -c Debug --nologo -v q
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

# ── Several instances of one AVD ──────────────────────────────────────────────
#
# One AVD can only boot once normally; -read-only lifts that, which is how several instances of
# the same image run side by side. Each gets its own serial (emulator-5554, -5556, ...).

function Start-AndroidShards([int]$Wanted) {
    $sdkRoot = $env:ANDROID_HOME
    if (-not $sdkRoot) { $sdkRoot = $env:ANDROID_SDK_ROOT }
    if (-not $sdkRoot) {
        foreach ($c in @("$env:LOCALAPPDATA\Android\Sdk", "$env:USERPROFILE\AppData\Local\Android\Sdk")) {
            if (Test-Path $c) { $sdkRoot = $c; break }
        }
    }
    if (-not $sdkRoot -or -not (Test-Path $sdkRoot)) {
        Write-Error "Android SDK not found. Set ANDROID_HOME or ANDROID_SDK_ROOT."
        exit 1
    }
    $adb = Join-Path $sdkRoot "platform-tools\adb.exe"
    $emulator = Join-Path $sdkRoot "emulator\emulator.exe"
    if (-not (Test-Path $adb)) { Write-Error "adb.exe not found at $adb — check ANDROID_HOME/ANDROID_SDK_ROOT."; exit 1 }
    if (-not (Test-Path $emulator)) { Write-Error "emulator.exe not found at $emulator — check ANDROID_HOME/ANDROID_SDK_ROOT."; exit 1 }

    $avd = $env:UITEST_AVD
    if (-not $avd) { $avd = (& $emulator -list-avds 2>$null | Select-Object -First 1) }
    if (-not $avd) {
        Write-Error "No AVDs found. Create one in Android Studio first."
        exit 1
    }

    $runningCount = @(& $adb devices | Select-String "emulator" | Select-String "device$").Count

    $i = $runningCount
    while ($i -lt $Wanted) {
        Write-Host "    Starting emulator instance $($i + 1)/$Wanted from AVD '$avd' (read-only)..."
        Start-Process -FilePath $emulator `
            -ArgumentList (Protect-Arg "-avd"), (Protect-Arg $avd), (Protect-Arg "-read-only"), (Protect-Arg "-no-snapshot-save"), (Protect-Arg "-no-boot-anim") `
            -WindowStyle Hidden
        Start-Sleep -Seconds 25
        $i++
    }

    & $adb wait-for-device
    Start-Sleep -Seconds 10
    return @(& $adb devices | Select-String "emulator" | Select-String "device$" |
        ForEach-Object { $_.ToString().Split("`t")[0].Trim() } | Select-Object -First $Wanted)
}

# ── Launch the shards ────────────────────────────────────────────────────────

$procs = @()
$labels = @()
$logs = @()

function Start-Shard([int]$Idx, [string]$Serial) {
    $filter = Get-ShardFilter $Idx
    $label = "android-shard$Idx"
    $artifacts = Join-Path $RepoRoot "TestResults\uitest-$label"
    $log = Join-Path $artifacts "shard.log"
    $errLog = Join-Path $artifacts "shard.err.log"

    if (-not $filter) {
        Write-Host "    $label has no fixtures assigned, skipping."
        return
    }

    New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
    $port = 4723 + $Idx
    Write-Host "    $label -> port $port | artifacts TestResults\uitest-$label"

    # Captured at spawn time, same as the bash version's subshell export — each child process
    # snapshots the environment when it is created, so setting these immediately before
    # Start-Process (rather than once up front) is what keeps shards from colliding.
    $env:UITEST_APPIUM_URL = "http://127.0.0.1:$port"
    $env:UITEST_ARTIFACTS = $artifacts
    $env:UITEST_SHARD_INDEX = "$Idx"
    $env:UITEST_SERIAL = $Serial

    $psArgs = @(
        (Protect-Arg "-NoProfile"),
        (Protect-Arg "-File"),
        (Protect-Arg $SingleRunner),
        (Protect-Arg "-NoBuild")
    )
    if ($filter) { $psArgs += @((Protect-Arg "-Filter"), (Protect-Arg $filter)) }

    # Already built above; a per-shard rebuild would serialise the whole point of this script.
    $proc = Start-Process -FilePath $HostExe -ArgumentList $psArgs `
        -RedirectStandardOutput $log -RedirectStandardError $errLog `
        -WindowStyle Hidden -PassThru

    $script:procs += $proc
    $script:labels += $label
    $script:logs += $log
}

Write-Host "==> Preparing $Shards Android emulator(s)..."
$serials = Start-AndroidShards $Shards
if ($serials.Count -eq 0) {
    Write-Error "No emulator serials came back after starting/checking $Shards emulator(s). Run 'adb devices' by hand to see what adb sees — nothing was launched."
    exit 1
}
if ($serials.Count -lt $Shards) {
    Write-Host "    WARNING: only $($serials.Count) of $Shards emulator(s) are up; running that many shards instead."
}
for ($idx = 0; $idx -lt $serials.Count; $idx++) {
    Start-Shard $idx $serials[$idx]
}

if ($procs.Count -eq 0) {
    Write-Error "No shard actually launched (every shard had no fixtures assigned, or starting it failed). Nothing ran."
    exit 1
}

Write-Host ""
Write-Host "==> $($procs.Count) shard(s) running. Tailing is per shard, e.g.:"
Write-Host "    Get-Content TestResults\uitest-android-shard0\shard.log -Wait"
Write-Host ""

$failed = $false
for ($i = 0; $i -lt $procs.Count; $i++) {
    $procs[$i].WaitForExit()
    if ($procs[$i].ExitCode -eq 0) {
        Write-Host "PASS  $($labels[$i])"
    } else {
        Write-Host "FAIL  $($labels[$i])  ($($logs[$i]))"
        $failed = $true
    }
}

Write-Host ""
if ($failed) {
    Write-Host "==> Some shards failed."
    for ($i = 0; $i -lt $logs.Count; $i++) {
        $content = if (Test-Path $logs[$i]) { Get-Content $logs[$i] } else { @() }
        $summary = $content | Select-String -Pattern "error TESTERROR|Test summary"
        Write-Host ""
        Write-Host "--- $($labels[$i]) ---"
        if ($summary) {
            $summary | ForEach-Object { Write-Host "  $_" }
        } else {
            # No test ever ran: the shard died in setup. Show the tail, or the cause stays
            # invisible — which is exactly what happened on macOS when a packed argument was
            # rejected before any test ran, and exactly what an unquoted path with a space (see
            # Protect-Arg above) would do here if it had not been caught.
            Write-Host "  (no test results — shard failed before running. Last lines:)"
            $content | Select-Object -Last 12 | ForEach-Object { Write-Host "  $_" }
            $errLogPath = Join-Path (Split-Path $logs[$i] -Parent) "shard.err.log"
            if (Test-Path $errLogPath) {
                Get-Content $errLogPath | Select-Object -Last 12 | ForEach-Object { Write-Host "  $_" }
            }
        }
    }
    exit 1
}

Write-Host "==> All shards passed."
