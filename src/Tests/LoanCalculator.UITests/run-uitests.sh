#!/bin/zsh
#
# Runs the cross-platform UI suite against one platform at a time.
#
# Usage:
#   ./run-uitests.sh --ios                      # iPhone 17 Pro simulator (iOS 26.5)
#   ./run-uitests.sh --ios --device "iPad Pro 13-inch (M5)"
#   ./run-uitests.sh --android                  # first available AVD
#   ./run-uitests.sh --android --avd Medium_Phone_API_36.1
#   ./run-uitests.sh --ios --no-build           # reuse the existing app bundle
#   ./run-uitests.sh --ios --filter Smoke       # only the Smoke category
#   ./run-uitests.sh --ios --fresh              # reinstall first, so app data starts empty
#
# One device at a time. To shard across several devices, or to run both platforms at once:
#   ./run-uitests-parallel.sh --ios --shards 2
#   ./run-uitests-parallel.sh --both
#
# Both platforms run the same C# tests; only the driver and device differ.
#
# Env overrides (used by run-uitests-parallel.sh, useful by hand too):
#   UITEST_SERIAL        target one specific Android emulator (required if several are running)
#   UITEST_APPIUM_URL    e.g. http://127.0.0.1:4733 — each concurrent run needs its own port
#   UITEST_ARTIFACTS     per-run artifact dir, so shards do not overwrite each other's logs
#   UITEST_SHARD_INDEX   offsets wdaLocalPort/systemPort so concurrent sessions do not collide

set -e

SCRIPT_DIR="${0:A:h}"
REPO_ROOT="${SCRIPT_DIR:h:h:h}"
APP_DIR="$REPO_ROOT/src/LoanCalculator"

PLATFORM=""
DEVICE=""
AVD=""
BUILD=1
FRESH=0
FILTER=""

while [ $# -gt 0 ]; do
  case "$1" in
    --ios)      PLATFORM="ios" ;;
    --android)  PLATFORM="android" ;;
    --device)   DEVICE="$2"; shift ;;
    --avd)      AVD="$2"; shift ;;
    --filter)   FILTER="$2"; shift ;;
    --no-build) BUILD=0 ;;
    --fresh)    FRESH=1 ;;
    -h|--help)  sed -n '2,20p' "$0"; exit 0 ;;
    # Options that belong to the sharded runner. Worth naming explicitly: the two scripts differ
    # by one word, and a bare "Unknown option" sends you reading argument parsing instead.
    --shards|--both)
      echo "'$1' belongs to the sharded runner, not this script." >&2
      echo "" >&2
      echo "  ./run-uitests-parallel.sh --ios --shards 2     # shard across 2 simulators" >&2
      echo "  ./run-uitests-parallel.sh --both              # iOS and Android concurrently" >&2
      echo "" >&2
      echo "This script runs one device at a time. See run-uitests-parallel.sh --help." >&2
      exit 1
      ;;
    *) echo "Unknown option: $1" >&2
       echo "Run '$0 --help', or see run-uitests-parallel.sh for sharded runs." >&2
       exit 1 ;;
  esac
  shift
done

if [ -z "$PLATFORM" ]; then
  echo "ERROR: pass --ios or --android." >&2
  exit 1
fi

if [ ! -d "$SCRIPT_DIR/node_modules" ] || [ ! -d "$SCRIPT_DIR/.appium" ]; then
  echo "==> Installing Appium and its drivers (first run only)..."
  (cd "$SCRIPT_DIR" && npm install && npm run drivers)
fi

export UITEST_PLATFORM="$PLATFORM"

# Mirror UITestConfig's default so the shell and the tests agree on the endpoint, and so the
# pre-flight check below has something to probe.
export UITEST_APPIUM_URL="${UITEST_APPIUM_URL:-http://127.0.0.1:4723}"
export UITEST_FRESH_INSTALL=$([ "$FRESH" = "1" ] && echo true || echo false)

# ── iOS ──────────────────────────────────────────────────────────────────────
if [ "$PLATFORM" = "ios" ]; then
  SIMULATOR_NAME="${DEVICE:-iPhone 17 Pro}"

  echo "==> Finding simulator: $SIMULATOR_NAME"
  # Resolve on the NEWEST installed iOS runtime. The same device name exists on every runtime you
  # have, and simctl lists them oldest-first — so a plain `head -1` silently picks the oldest. That
  # is how this suite ended up running on iOS 18.4 while the app was built against the 26.5 SDK.
  SIMULATOR_ID=$(xcrun simctl list devices available | awk -v name="$SIMULATOR_NAME" '
    /^-- iOS /           { split($3, v, "."); cur = v[1] * 1000 + v[2]; rt = $3; next }
    index($0, name " (") { if (cur >= best) { best = cur; line = $0; brt = rt } }
    END { if (line != "") { match(line, /[0-9A-F-]{36}/)
            print substr(line, RSTART, RLENGTH) > "/dev/stdout"
            printf "    (iOS %s)\n", brt > "/dev/stderr" } }')

  if [ -z "$SIMULATOR_ID" ]; then
    echo "ERROR: no simulator matching '$SIMULATOR_NAME'. Available:" >&2
    xcrun simctl list devices available | grep -E "iPhone|iPad" >&2
    exit 1
  fi
  echo "    $SIMULATOR_ID"

  if ! xcrun simctl list devices | grep "$SIMULATOR_ID" | grep -q Booted; then
    echo "==> Booting simulator..."
    xcrun simctl boot "$SIMULATOR_ID"
    open -a Simulator
  fi
  xcrun simctl bootstatus "$SIMULATOR_ID" -b >/dev/null 2>&1 || true

  # Cuts animation-timing flake for the whole run.
  xcrun simctl spawn "$SIMULATOR_ID" \
    defaults write com.apple.Accessibility ReduceMotionEnabled -int 1 2>/dev/null || true

  if [ "$BUILD" = "1" ]; then
    echo "==> Building iOS app..."
    dotnet build "$APP_DIR/LoanCalculatorMaui.csproj" -f net10.0-ios26.5 -c Debug
  fi

  APP_BUNDLE="$APP_DIR/bin/Debug/net10.0-ios26.5/iossimulator-arm64/LoanCalculatorMaui.app"
  if [ ! -d "$APP_BUNDLE" ]; then
    echo "ERROR: no app bundle at $APP_BUNDLE. Run without --no-build." >&2
    exit 1
  fi

  # Install here rather than letting Appium do it: with noReset the driver skips
  # installation when the bundle id is already present, which silently tests a stale build.
  if [ "$FRESH" = "1" ]; then
    echo "==> Uninstalling for a clean data directory..."
    xcrun simctl uninstall "$SIMULATOR_ID" com.pj.loan.afford.calc 2>/dev/null || true
  fi
  echo "==> Installing app..."
  xcrun simctl install "$SIMULATOR_ID" "$APP_BUNDLE"

  export UITEST_DEVICE="$SIMULATOR_NAME"
  export UITEST_UDID="$SIMULATOR_ID"
  export UITEST_APP="$APP_BUNDLE"
fi

# ── Android ──────────────────────────────────────────────────────────────────
if [ "$PLATFORM" = "android" ]; then
  SDK_ROOT="${ANDROID_HOME:-${ANDROID_SDK_ROOT:-$HOME/Library/Android/sdk}}"
  if [ ! -d "$SDK_ROOT" ]; then
    echo "ERROR: Android SDK not found. Set ANDROID_HOME." >&2
    exit 1
  fi
  ADB="$SDK_ROOT/platform-tools/adb"
  EMULATOR="$SDK_ROOT/emulator/emulator"
  export ANDROID_HOME="$SDK_ROOT"

  # The .NET Android manifest merger needs a JDK 17+; mirror run-android.ps1's choice.
  for candidate in \
    /opt/homebrew/opt/openjdk@21/libexec/openjdk.jdk/Contents/Home \
    /opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home \
    "/Applications/Android Studio.app/Contents/jbr/Contents/Home"; do
    if [ -x "$candidate/bin/java" ]; then export JAVA_HOME="$candidate"; break; fi
  done

  if ! "$ADB" devices | grep -q "emulator.*device$"; then
    EMULATOR_NAME="${AVD:-$("$EMULATOR" -list-avds | head -1)}"
    if [ -z "$EMULATOR_NAME" ]; then
      echo "ERROR: no AVDs found. Create one in Android Studio first." >&2
      exit 1
    fi
    echo "==> Starting emulator '$EMULATOR_NAME'..."
    "$EMULATOR" -avd "$EMULATOR_NAME" -no-snapshot-save >/dev/null 2>&1 &
    "$ADB" wait-for-device
    until [ "$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ]; do
      sleep 2
    done
    echo "    Emulator ready."
  else
    echo "==> Using the emulator that is already running."
  fi

  # An explicit serial wins. With more than one emulator up — which is exactly what a sharded
  # run does — `head -1` picks an arbitrary one, and since the animation-scale writes below are
  # per-device *global* settings that survive reboots, targeting the wrong emulator leaves a
  # stray scale behind on a device nobody is looking at.
  if [ -n "$UITEST_SERIAL" ]; then
    SERIAL="$UITEST_SERIAL"
  else
    SERIAL=$("$ADB" devices | grep "emulator.*device$" | head -1 | cut -f1)
    RUNNING_COUNT=$("$ADB" devices | grep -c "emulator.*device$")
    if [ "$RUNNING_COUNT" -gt 1 ]; then
      echo "    WARNING: $RUNNING_COUNT emulators are running and no UITEST_SERIAL was set;"
      echo "             picking '$SERIAL'. Set UITEST_SERIAL to target one deliberately."
    fi
  fi
  echo "    Serial: $SERIAL"

  # Reduce tap-timing flake by removing *system* animations — but never touch
  # animator_duration_scale. That one drives MAUI's animation framework: at 0, FadeTo/ScaleTo
  # never signal completion, so SplashPage never hands off to the Shell and the app sits on
  # the native splash forever. These settings are global and survive app reinstalls and
  # emulator reboots, so a stray 0 here looks like a permanently broken app. Restore on exit.
  # Animation scales are a recurring foot-gun here, so this block is deliberately self-healing.
  # It defends against two distinct failure modes:
  #
  #  1. animator_duration_scale = 0 stops MAUI's FadeTo/ScaleTo ever signalling completion, so
  #     SplashPage never hands off to the Shell and the app hangs on the NATIVE splash forever.
  #     Nothing here ever sets that scale — we only repair it when we find it zeroed.
  #
  #  2. The restore-on-exit used to latch. It captured the CURRENT value as "the original", so
  #     after a run that died without its trap firing — SIGKILL, closed terminal, or an emulator
  #     snapshot taken mid-run — the two system scales were already 0, and the next run dutifully
  #     "restored" 0. Once corrupted it stayed corrupted, and every later run re-confirmed it.
  #     A zero is never worth preserving on a dev machine, so we always restore 1.0.
  #
  # These settings are global: they survive app reinstalls, uninstalls and emulator reboots, which
  # is why a stray 0 presents as a permanently broken app rather than as a device setting.
  for scale in animator_duration_scale window_animation_scale transition_animation_scale; do
    current=$("$ADB" -s "$SERIAL" shell settings get global "$scale" 2>/dev/null | tr -d '\r')
    case "$current" in
      0 | 0.0 | "" | null)
        echo "    NOTE: $scale was '${current:-unset}' — repairing to 1.0."
        [ "$scale" = "animator_duration_scale" ] && \
          echo "          At 0 the app hangs on the native splash and every test fails at launch."
        "$ADB" -s "$SERIAL" shell settings put global "$scale" 1.0 2>/dev/null || true
        ;;
    esac
  done

  # Only the two SYSTEM scales are zeroed for the run, to cut tap-timing flake.
  # animator_duration_scale is deliberately left alone — see failure mode 1 above.
  ANIM_SCALES=(window_animation_scale transition_animation_scale)
  for scale in "${ANIM_SCALES[@]}"; do
    "$ADB" -s "$SERIAL" shell settings put global "$scale" 0 2>/dev/null || true
  done

  # Restore a known-good 1.0 rather than a captured value — see failure mode 2 above.
  restore_animation_scales() {
    for scale in "${ANIM_SCALES[@]}"; do
      "$ADB" -s "$SERIAL" shell settings put global "$scale" 1.0 2>/dev/null || true
    done
  }
  trap restore_animation_scales EXIT INT TERM

  if [ "$BUILD" = "1" ]; then
    echo "==> Building Android app..."
    dotnet build "$APP_DIR/LoanCalculatorMaui.csproj" -f net10.0-android36.0 -c Debug \
      -p:AndroidSdkDirectory="$SDK_ROOT" -p:EmbedAssembliesIntoApk=true \
      ${JAVA_HOME:+-p:JavaSdkDirectory=$JAVA_HOME}
  fi

  APK=$(find "$APP_DIR/bin/Debug/net10.0-android36.0" -name "*-Signed.apk" 2>/dev/null | head -1)
  if [ -z "$APK" ]; then
    echo "ERROR: no signed APK found. Run without --no-build." >&2
    exit 1
  fi

  # As on iOS, install here so the suite can never run against a stale build.
  if [ "$FRESH" = "1" ]; then
    echo "==> Uninstalling for a clean data directory..."
    "$ADB" -s "$SERIAL" uninstall com.pj.loan.afford.calc 2>/dev/null || true
  fi
  echo "==> Installing app..."
  if ! "$ADB" -s "$SERIAL" install -r "$APK"; then
    echo "    Install failed — uninstalling and retrying..."
    "$ADB" -s "$SERIAL" uninstall com.pj.loan.afford.calc 2>/dev/null || true
    "$ADB" -s "$SERIAL" install "$APK"
  fi

  export UITEST_DEVICE="Android Emulator"
  export UITEST_UDID="$SERIAL"
  export UITEST_APP="$APK"
  [ -n "$AVD" ] && export UITEST_AVD="$AVD"
fi

# --no-build is the fast path, but it tests whatever was built last. Two ways that bites, both
# indistinguishable from a real regression: a test looking for an AutomationId that only exists
# in newer XAML fails as "element not found", and an app fix that only exists in newer C# fails
# as the very bug it fixes — sending you off to re-diagnose something you already fixed. Warn
# loudly rather than let either eat an afternoon.
if [ "$BUILD" = "0" ]; then
  STALE_XAML=$(find "$APP_DIR/View" "$APP_DIR/Controls" \
      -name "*.xaml" -newer "$UITEST_APP" 2>/dev/null)
  # Core carries the ViewModels, so a behaviour fix usually lands there rather than in the app.
  STALE_CS=$(find "$APP_DIR/View" "$APP_DIR/Controls" "$REPO_ROOT/src/LoanCalculator.Core" \
      -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" -newer "$UITEST_APP" 2>/dev/null)
  if [ -n "$STALE_XAML" ] || [ -n "$STALE_CS" ]; then
    echo ""
    echo "WARNING: --no-build, but these sources are NEWER than the app you are about to test:"
    [ -n "$STALE_XAML" ] && echo "$STALE_XAML" | sed 's|.*/|         |'
    [ -n "$STALE_CS" ]   && echo "$STALE_CS"   | sed 's|.*/|         |'
    echo "         A new AutomationId will not be found, and any app fix in these files is NOT"
    echo "         in this build. Re-run without --no-build before trusting a failure."
    echo ""
  fi
fi

# An Appium server started by hand does not inherit the ANDROID_HOME exported above, and every
# Android session then fails with "Neither ANDROID_HOME nor ANDROID_SDK_ROOT ... was exported".
# We cannot read another process's environment, so warn whenever we are about to attach to one.
if [ "$PLATFORM" = "android" ] && curl -s -m 3 "$UITEST_APPIUM_URL/status" >/dev/null 2>&1; then
  echo ""
  echo "NOTE: attaching to an Appium server that is already running."
  echo "      If it was started by hand, it must have been started with ANDROID_HOME set —"
  echo "      'npm run start' does that. Otherwise every Android test will fail in OneTimeSetUp."
  echo ""
fi

echo "==> Running UI tests on $PLATFORM"
echo "    App: $UITEST_APP"

TEST_ARGS=(test "$SCRIPT_DIR/LoanCalculator.UITests.csproj" --nologo)
[ -n "$FILTER" ] && TEST_ARGS+=(--filter "$FILTER")

dotnet "${TEST_ARGS[@]}"
