param(
    [switch]$Tablet,
    [string]$Avd = ""
)

# Cross-platform Windows check ($IsWindows is undefined on Windows PowerShell 5.1,
# which instead sets $env:OS). Used throughout for path/exe/separator differences.
$isWin = [bool]($IsWindows -or $env:OS -eq "Windows_NT")

# Anchor to the script's own directory so the relative csproj/APK paths resolve
# no matter where the script is invoked from.
if ($PSScriptRoot) { Set-Location -LiteralPath $PSScriptRoot }

$Project   = "LoanCalculatorMaui.csproj"
$BundleId  = "com.pj.loan.afford.calc"

# Preferred AVD names if they happen to exist on this machine; otherwise we
# discover and pick one dynamically further below (see "Discovering AVDs").
$PreferredPhoneAvd  = "pixel_9_pro_-_api_36"
$PreferredTabletAvd = "tablet_h-dpi_13_5in_-_api_29_1"

# Android manifest merger requires a JDK >= 17. The system default may be older
# (e.g. Java 11), which fails with UnsupportedClassVersionError, so pick the first
# known JDK 17+ we can find. Set JAVA_HOME_OVERRIDE to force a specific JDK path.
# Candidates are OS-specific; entries may contain wildcards (expanded below).
$javaExe = if ($isWin) { "bin\java.exe" } else { "bin/java" }
$javaCandidates = @()
if ($env:JAVA_HOME_OVERRIDE) { $javaCandidates += $env:JAVA_HOME_OVERRIDE }
if ($isWin) {
    $javaCandidates += @(
        "$env:ProgramFiles\Android\Android Studio\jbr",
        "$env:ProgramFiles\Microsoft\jdk-21*",
        "$env:ProgramFiles\Microsoft\jdk-17*",
        "$env:ProgramFiles\Eclipse Adoptium\jdk-21*",
        "$env:ProgramFiles\Eclipse Adoptium\jdk-17*",
        "$env:ProgramFiles\Java\jdk-21*",
        "$env:ProgramFiles\Java\jdk-17*"
    )
} else {
    $javaCandidates += @(
        "/opt/homebrew/opt/openjdk@21/libexec/openjdk.jdk/Contents/Home",
        "/opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home",
        "/Applications/Android Studio.app/Contents/jbr/Contents/Home",
        "/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home",
        "/Library/Java/JavaVirtualMachines/temurin-21.jdk/Contents/Home",
        "/Library/Java/JavaVirtualMachines/temurin-27.jdk/Contents/Home"
    )
}
$javaHome = $null
foreach ($c in $javaCandidates) {
    # Expand any wildcard (e.g. jdk-17*) and take the first match with a java binary.
    foreach ($p in @(Resolve-Path -Path $c -ErrorAction SilentlyContinue | ForEach-Object { $_.Path })) {
        if (Test-Path (Join-Path $p $javaExe)) { $javaHome = $p; break }
    }
    if ($javaHome) { break }
}
if ($javaHome) {
    $env:JAVA_HOME = $javaHome
    $env:PATH = (Join-Path $javaHome "bin") + [IO.Path]::PathSeparator + $env:PATH
    Write-Host "==> Using Java: $javaHome"
} else {
    Write-Host "WARNING: No JDK 17+ found automatically; using system default (manifest merge may fail). Set JAVA_HOME_OVERRIDE to a JDK 17+ path." -ForegroundColor Yellow
}

$SdkRoot = $env:ANDROID_HOME
if (-not $SdkRoot) { $SdkRoot = $env:ANDROID_SDK_ROOT }
if (-not $SdkRoot) {
    $candidates = @(
        "$env:LOCALAPPDATA\Android\Sdk",
        "$env:USERPROFILE\AppData\Local\Android\Sdk",
        "$HOME/Library/Android/sdk",
        "$HOME/Android/Sdk"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { $SdkRoot = $c; break }
    }
}
if (-not $SdkRoot) { Write-Error "Android SDK not found. Set ANDROID_HOME or ANDROID_SDK_ROOT."; exit 1 }

$EmulatorExe = Join-Path $SdkRoot (Join-Path "emulator" "emulator")
$AdbExe      = Join-Path $SdkRoot (Join-Path "platform-tools" "adb")
if ($isWin) { $EmulatorExe += ".exe"; $AdbExe += ".exe" }

Write-Host "==> Discovering AVDs..."
$avds = @(& $EmulatorExe -list-avds 2>$null | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" })

Write-Host "==> Checking emulator state..."
$running = & $AdbExe devices | Select-String "emulator" | Select-String "device$"

if ($running) {
    # Any already-running emulator is fine — no need to match a specific AVD name.
    Write-Host "    Emulator already running; using it."
} else {
    if ($avds.Count -eq 0) {
        Write-Error "No AVDs found. Create one in Android Studio (Device Manager) or with 'avdmanager', then re-run."
        exit 1
    }

    # Resolve which AVD to launch: an explicit -Avd wins; otherwise prefer a known
    # name if it exists, then a name matching the requested form factor, and finally
    # just fall back to the first available AVD so it always runs somewhere.
    if ($Avd -ne "") {
        if ($avds -notcontains $Avd) {
            Write-Host "ERROR: Requested AVD '$Avd' not found." -ForegroundColor Red
            Write-Host "Available AVDs:"
            $avds | ForEach-Object { Write-Host "    $_" }
            exit 1
        }
        $EmulatorName = $Avd
    } else {
        if ($Tablet) {
            $preferred = @($PreferredTabletAvd) + @($avds | Where-Object { $_ -match 'tab' })
        } else {
            $preferred = @($PreferredPhoneAvd) + @($avds | Where-Object { $_ -match 'phone|pixel|nexus' })
        }
        $EmulatorName = $preferred | Where-Object { $avds -contains $_ } | Select-Object -First 1
        if (-not $EmulatorName) { $EmulatorName = $avds[0] }
        Write-Host "    Selected AVD: $EmulatorName"
        Write-Host "    (available: $($avds -join ', '))"
    }

    Write-Host "    Starting emulator '$EmulatorName'..."
    $startArgs = @{ FilePath = $EmulatorExe; ArgumentList = "-avd", $EmulatorName }
    if ($isWin) { $startArgs["WindowStyle"] = "Hidden" }
    Start-Process @startArgs
    Write-Host "    Waiting for device to come online..."
    & $AdbExe wait-for-device
    $booted = ""
    while ($booted -ne "1") {
        Start-Sleep -Seconds 2
        $booted = & $AdbExe shell getprop sys.boot_completed 2>$null
        $booted = $booted.Trim()
        Write-Host "    Boot status: $booted"
    }
    Write-Host "    Emulator ready."
}

$serial = (& $AdbExe devices | Select-String "emulator" | Select-String "device$" | Select-Object -First 1).ToString().Split("`t")[0].Trim()
Write-Host "    Serial: $serial"

Write-Host "==> Building..."
$javaSdkArg = if ($javaHome) { "-p:JavaSdkDirectory=$javaHome" } else { $null }
dotnet build $Project -f net10.0-android36.0 -c Debug -p:AndroidSdkDirectory=$SdkRoot -p:EmbedAssembliesIntoApk=true $javaSdkArg
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "==> Finding APK..."
$outDir = Join-Path "bin" (Join-Path "Debug" "net10.0-android36.0")
$apk = Get-ChildItem $outDir -Filter "*-Signed.apk" -Recurse | Select-Object -First 1
if (-not $apk) { $apk = Get-ChildItem $outDir -Filter "*.apk" -Recurse | Select-Object -First 1 }
if (-not $apk) { Write-Error "APK not found under $outDir"; exit 1 }
Write-Host "    APK: $($apk.FullName)"

Write-Host "==> Stopping existing app..."
& $AdbExe -s $serial shell am force-stop $BundleId

Write-Host "==> Installing..."
& $AdbExe -s $serial install -r $apk.FullName
if ($LASTEXITCODE -ne 0) {
    Write-Host "    Install failed - uninstalling old app and retrying..." -ForegroundColor Yellow
    & $AdbExe -s $serial uninstall $BundleId
    & $AdbExe -s $serial install $apk.FullName
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

Write-Host "==> Launching..."
$dumpOutput = & $AdbExe -s $serial shell pm dump $BundleId 2>$null
$mainActivity = $dumpOutput | Select-String "MainActivity filter" | Select-Object -First 1 |
                ForEach-Object { $_.Line.Trim() -replace '\s+', ' ' } |
                ForEach-Object { ($_ -split ' ')[1] }
if (-not $mainActivity) { Write-Error "Could not determine MainActivity for $BundleId"; exit 1 }
Write-Host "    Activity: $mainActivity"
& $AdbExe -s $serial shell am start -n $mainActivity

Write-Host "==> Done"
