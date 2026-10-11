# Useful Commands

Every command that runs this repo, sectioned by what it does, with the combinations each one
takes. Run everything from `src/LoanCalculator/` unless the section says otherwise. `.sh` scripts
are zsh and need macOS (iOS tooling, `xcrun simctl`); `.ps1` scripts run on Windows PowerShell and
on macOS/Linux PowerShell (pwsh) alike. Where a `.sh` has no `.ps1` counterpart, that's called out
explicitly — don't assume one exists.

> **Note on this file:** generated from the scripts' own `--help`/usage headers and CLAUDE.md as of
> 2026-10-09. If a script's usage comment changes, this file can drift — check the script itself
> when in doubt, especially for the exact flag spelling.

---

## 1. Build

```bash
cd src/LoanCalculator
dotnet build LoanCalculatorMaui.csproj -f net10.0-ios26.5 -c Debug
dotnet build LoanCalculatorMaui.csproj -f net10.0-android36.0 -c Debug
dotnet build LoanCalculatorMaui.csproj -f net10.0-maccatalyst -c Debug
```

Swap `-c Debug` for `-c Release` for a release build of any target.

---

## 2. Run the app

### 2.1 iOS Simulator — `run-ios.sh` (macOS only, no `.ps1` equivalent — iOS simulators don't exist on Windows)

```bash
cd src/LoanCalculator
./run-ios.sh                                    # iPhone 17 Pro (default, iOS 26.5), filtered logs
./run-ios.sh --ipad                             # iPad Pro 13-inch (M5)
./run-ios.sh --device "iPad mini (A17 Pro)"     # any named simulator
./run-ios.sh --logs                             # stream ALL app output, no filter
./run-ios.sh --nologs                           # launch silently, no log streaming
```

Combinable: `--device <name>` with `--logs`/`--nologs` (`--ipad` is really a canned `--device`, so
don't combine it with `--device`). Builds, installs, launches with `--console-pty`, filters log
output to app-relevant lines by default. Plain `Console.WriteLine` shows up in the terminal.

### 2.2 Android emulator — `run-android.ps1` (Windows, macOS, Linux via `pwsh`)

```powershell
cd src/LoanCalculator
pwsh ./run-android.ps1                          # Medium_Phone_API_36.1 / first available phone AVD
pwsh ./run-android.ps1 -Tablet                  # prefer a tablet-shaped AVD
pwsh ./run-android.ps1 -Avd <name>              # a specific AVD by name (overrides -Tablet)
```

`-Avd` and `-Tablet` are mutually exclusive in effect — pass one or the other, not both. If an
emulator is already running, this script reuses it regardless of which flags you pass. Discovers
the Android SDK (`ANDROID_HOME`/`ANDROID_SDK_ROOT`, else common install paths) and a JDK 17+
(`JAVA_HOME_OVERRIDE` to force one) automatically; builds, installs, and launches the app.

**This is a deploy-and-launch script, not a test runner** — it has no equivalent to
`run-uitests.sh`/`run-uitests-parallel.sh` below. See §4 for why that matters on Windows.

### 2.3 MacCatalyst

No run script — build with `-f net10.0-maccatalyst` (§1) and launch the produced `.app` directly,
or run from Visual Studio/Rider.

---

## 3. Tests — unit tests (no device needed)

```bash
dotnet test src/Tests/LoanCalculator.UnitTests/LoanCalculator.UnitTests.csproj
```

Runs everywhere (Windows, macOS, Linux) — no `.sh`/`.ps1` wrapper exists or is needed. Active
coverage: stamp duty bracket calculations, theme XAML key consistency, loan/affordability
guardrails. A solution-wide `dotnet test` runs this project but **skips** the UI suite in §4 (it's
opt-in via `UITEST_PLATFORM`), so it won't fail a CI run that never set up Appium.

---

## 4. Tests — UI tests (Appium, drives a real simulator/emulator)

One C# suite drives both platforms; tests address the UI by `AutomationId`, resolved per-platform
in `Infrastructure/`. The original runners (`run-uitests.sh`, `run-uitests-parallel.sh`) are macOS
zsh scripts covering both iOS and Android. **§4.1a/4.2a are their Android-only PowerShell ports**,
for running the Android half of the suite from Windows — there is still no way to run the iOS half
anywhere but macOS (iOS simulators don't exist on Windows). `run-android.ps1` (§2.2) is unrelated
to both: it only deploys the app, it doesn't start Appium or run tests.

### 4.1 One device at a time — `run-uitests.sh` (macOS, iOS + Android)

```bash
cd src/Tests/LoanCalculator.UITests
npm install && npm run drivers                              # first time only

./run-uitests.sh --ios                                       # iPhone 17 Pro simulator (iOS 26.5)
./run-uitests.sh --ios --device "iPad Pro 13-inch (M5)"       # named iOS simulator
./run-uitests.sh --android                                   # first available AVD
./run-uitests.sh --android --avd Medium_Phone_API_36.1        # named AVD
./run-uitests.sh --ios --no-build                             # reuse existing app bundle (fast path)
./run-uitests.sh --ios --filter "TestCategory=Smoke"           # only the Smoke category
./run-uitests.sh --ios --fresh                                # reinstall first, empty app data
./run-uitests.sh --android --no-build --filter "TestCategory=Smoke"
./run-uitests.sh --android --avd Medium_Phone_API_36.1 --fresh --filter "TestCategory=Smoke"
```

Combinable: `--device`/`--avd` + `--no-build` + `--filter` + `--fresh`, in any mix (device/avd is
platform-specific — don't pass `--device` with `--android` or `--avd` with `--ios`). `--shards` and
`--both` belong to the parallel runner (§4.2) and are rejected here with a pointer to it.

**`--no-build` tests whatever was built last** — see CLAUDE.md's stale-build warning before
re-diagnosing a failure that looks like the bug you just fixed. Compare mtimes:
```bash
stat -f '%Sm %N' -t '%F %T' src/LoanCalculator/bin/Debug/net10.0-android36.0/*-Signed.apk
stat -f '%Sm %N' -t '%F %T' <the file you changed>
```

Env overrides (mainly for `run-uitests-parallel.sh`, usable by hand too):

```bash
UITEST_SERIAL=emulator-5556 ./run-uitests.sh --android      # target one of several running emulators
UITEST_APPIUM_URL=http://127.0.0.1:4733 ./run-uitests.sh --ios   # non-default Appium port
UITEST_ARTIFACTS=/tmp/myrun ./run-uitests.sh --ios          # custom artifacts dir
UITEST_SHARD_INDEX=1 ./run-uitests.sh --android             # offset systemPort/wdaLocalPort
```

### 4.1a One Android emulator at a time, from Windows — `run-uitests-windows-android.ps1`

```powershell
cd src/Tests/LoanCalculator.UITests
pwsh ./run-uitests-windows-android.ps1                                   # first available AVD
pwsh ./run-uitests-windows-android.ps1 -Avd Medium_Phone_API_36.1         # named AVD
pwsh ./run-uitests-windows-android.ps1 -NoBuild                          # reuse existing APK (fast path)
pwsh ./run-uitests-windows-android.ps1 -Filter "TestCategory=Smoke"        # only the Smoke category
pwsh ./run-uitests-windows-android.ps1 -Fresh                            # reinstall first, empty app data
pwsh ./run-uitests-windows-android.ps1 -Avd Medium_Phone_API_36.1 -NoBuild -Fresh -Filter "TestCategory=Smoke"
```

Combinable: `-Avd` + `-NoBuild` + `-Fresh` + `-Filter`, in any mix. Direct Android port of §4.1 —
same build/install/animation-scale/stale-build logic, same env var overrides (`UITEST_SERIAL`,
`UITEST_APPIUM_URL`, `UITEST_ARTIFACTS`, `UITEST_SHARD_INDEX`), same `-NoBuild` staleness warning.
Runs via `pwsh` (PowerShell 7+) on Windows; needs Android SDK + emulator + a JDK 17+ (same
discovery search as `run-android.ps1`) and Node for the first-run Appium/driver install.

### 4.2 Sharded across several devices — `run-uitests-parallel.sh` (macOS, iOS + Android)

```bash
cd src/Tests/LoanCalculator.UITests
./run-uitests-parallel.sh --ios                              # 2 iOS shards (the default)
./run-uitests-parallel.sh --ios --shards 3
./run-uitests-parallel.sh --android                          # 2 Android shards
./run-uitests-parallel.sh --android --shards 2
./run-uitests-parallel.sh --both                              # iOS and Android concurrently, 2 shards each
./run-uitests-parallel.sh --ios --shards 2 --no-build
./run-uitests-parallel.sh --both --shards 3 --filter "TestCategory=Smoke"
```

Combinable: `--ios`/`--android`/`--both` + `--shards <n>` + `--no-build` + `--filter`. Must pick one
of `--ios`/`--android`/`--both`. Shards by **fixture**, not by test (fixtures hold per-class
`OneTimeSetUp` state); if `--shards` exceeds the fixture count it's capped down automatically.

Default is 2 shards deliberately — see the long comment at the top of the script: a stressed
simulator stops completing animations before it helps to add more, and the first lever
(`UITEST_WDA_CONNECTION_TIMEOUT_SECONDS`, 60s) already recovers more wall-clock than 3× sharding.
Measure at 2 before raising it.

- iOS: clones `iPhone 17 Pro` (or `UITEST_DEVICE`) once per shard, named `LoanCalc-Shard<N>`.
- Android: boots several instances of one AVD (`UITEST_AVD`, else the first available) with
  `-read-only`, one serial per shard.
- Tail a specific shard's log while it runs: `tail -f TestResults/uitest-<platform>-shard<N>/shard.log`

### 4.2a Sharded across several Android emulators, from Windows — `run-uitests-parallel-windows-android.ps1`

```powershell
cd src/Tests/LoanCalculator.UITests
pwsh ./run-uitests-parallel-windows-android.ps1                          # 2 shards (the default)
pwsh ./run-uitests-parallel-windows-android.ps1 -Shards 3
pwsh ./run-uitests-parallel-windows-android.ps1 -Shards 2 -NoBuild
pwsh ./run-uitests-parallel-windows-android.ps1 -Filter "TestCategory=Smoke"
pwsh ./run-uitests-parallel-windows-android.ps1 -Shards 3 -NoBuild -Filter "TestCategory=Smoke"
```

Combinable: `-Shards <n>` + `-NoBuild` + `-Filter`, in any mix. Android-only port of §4.2 — no
`-Both`/iOS option, since there is nothing to shard it against on Windows. Same fixture-discovery,
build-once, and per-shard-serial logic as the macOS script; launches each shard as its own `pwsh`
child process (via `Start-Process`, one real OS process per shard — not a PowerShell job) so each
shard's actual `dotnet test` exit code is what determines PASS/FAIL.

Tail a specific shard's log while it runs:
```powershell
Get-Content TestResults\uitest-android-shard0\shard.log -Wait
```

### 4.3 Reading results

- `src/Tests/LoanCalculator.UITests/README.md` — setup, running, architecture, writing a test
- `src/Tests/LoanCalculator.UITests/TEST-CASES.md` — every test case: objective, steps, expected
  result, how to run it alone on each platform, what to check when it fails
- `src/Tests/LoanCalculator.UITests/TROUBLESHOOTING.md` — **read before debugging a failure**;
  organised by symptom

---

## 5. Playground (PDF generation dry-run, no device needed)

```bash
dotnet run --project src/LoanCalculator.Playground/LoanCalculator.Playground.csproj
```

---

## 6. Publish / release builds

The two **current** publish scripts live under `src/LoanCalculator/` and resolve their target
framework from the csproj dynamically, so they track whatever version CLAUDE.md's build commands
use without edits. The `Build/` scripts are older (`net9.0-ios`, `net9.0-windows10.0.19041.0`,
hardcoded signing identity) — treat those as legacy/reference, not as the current release path,
unless you've specifically updated them to match the current `net10.0` targets.

### 6.1 Android — `publish-android-loancalc.ps1` (current, cross-platform via `pwsh`)

```powershell
cd src/LoanCalculator
pwsh ./publish-android-loancalc.ps1
```

No flags — discovers the Android target framework from the csproj, locates Java and the Android
SDK, runs `dotnet publish -c Release`, and prints the path to the signed `.aab` for the Play Store.

### 6.2 iOS — `publish-ios-loancalc.sh` (current, macOS only — iOS publishing requires a Mac)

```bash
cd src/LoanCalculator
./publish-ios-loancalc.sh
```

No flags — discovers the iOS target framework from the csproj, verifies Xcode command-line tools,
runs `dotnet publish -c Release`.

### 6.3 Legacy / reference scripts under `Build/`

```bash
Build/publish-ios.sh                      # older iOS publish; hardcoded signing identity/profile — net9.0-ios
pwsh Build/package-uwp-app.ps1            # UWP packaging via msbuild — net9.0-windows10.0.19041.0
pwsh Build/publish-ios-from-windows.ps1 -MacUsername "<name>" -MacIP "<ip>" -AppleIDUsername "<apple-id>"
pwsh Build/publish-ios-from-windows.ps1 -MacUsername "<name>" -MacIP "<ip>" -AppleIDUsername "<apple-id>" -Stages "Build"
pwsh Build/publish-ios-from-windows.ps1 -MacUsername "<name>" -MacIP "<ip>" -AppleIDUsername "<apple-id>" -Stages "Copy","Deploy"
```

`publish-ios-from-windows.ps1` builds the IPA on Windows, copies it to a Mac over SSH, then runs
`Build/mac-upload-script.sh <ipa-path> <apple-id> <password>` on the Mac to upload via `altool`.
Needs: OpenSSH client on Windows, Remote Login enabled on the Mac, and (for passwordless use)
SSH keys set up between the two machines. `-Stages` takes any subset of `Build`, `Copy`, `Deploy`
to resume a partial run.

---

## 7. Known gaps (as of 2026-10-09)

- **No way to run the iOS half of the UI suite from Windows** — not fixable, iOS simulators don't
  exist outside macOS. The Android half now has Windows PowerShell ports (§4.1a, §4.2a).
- The two Windows ports (`run-uitests-windows-android.ps1`, `run-uitests-parallel-windows-android.ps1`)
  were written by porting the macOS scripts' Android-only logic line-for-line, but have not been
  run on an actual Windows machine yet — exercise them there before trusting a failure as a real
  regression rather than a porting bug.
- `Build/publish-ios.sh` and `Build/package-uwp-app.ps1` target `net9.0-*`, one version behind the
  `net10.0-*` targets the rest of the repo now builds against (see `global.json`).
