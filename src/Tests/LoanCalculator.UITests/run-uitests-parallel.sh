#!/bin/zsh
#
# Shards the UI suite across several devices, and optionally runs both platforms at once.
#
# Usage:
#   ./run-uitests-parallel.sh --ios                    # 2 iOS shards (the default)
#   ./run-uitests-parallel.sh --ios --shards 3
#   ./run-uitests-parallel.sh --android --shards 2
#   ./run-uitests-parallel.sh --both                   # iOS and Android concurrently
#   ./run-uitests-parallel.sh --ios --shards 2 --no-build
#
# WHY SHARDING IS NOT THE FIRST LEVER, AND WHY THE DEFAULT IS 2
#
# A measured 41-test iOS run took 100 minutes, but 65 of those were three failures: a degraded
# WebDriverAgent cost 240s per wedged command (Appium's default) and each failure hit five or six
# of them. Capping that per-command budget — UITEST_WDA_CONNECTION_TIMEOUT_SECONDS, now 60s by
# default — recovers more wall clock than 3x sharding does, and it is why that change landed first.
#
# The remaining ~36 minutes of healthy run time is what sharding attacks: ~15 minutes at 2 shards.
#
# Shard with care, though. The failure mode this suite actually suffers from is load-sensitive:
# a stressed simulator stops completing animations, which is what strands the app on its splash.
# Three simulators plus three WebDriverAgents plus three Appium servers on one Mac is a large load
# increase, so more shards can mean more flake, not just less wall clock. Pulling the other way,
# degradation here is cumulative — in that 100-minute run nothing failed in the first 44 minutes —
# and sharding cuts restarts per WDA from 41 to ~14, which should reduce exactly that. Both effects
# are real. Hence: default 2, measure, and only raise it if the host has headroom.
#
# WHAT EACH SHARD GETS ITS OWN COPY OF
#
# Appium port, artifacts directory (so appium-server.log is not overwritten), device, and a shard
# index that offsets wdaLocalPort (iOS) / systemPort (Android). Appium requires those ports to be
# unique per concurrent session; sharing them surfaces as random element-not-found failures rather
# than as an obvious conflict.
#
# A shard must never share a device with another shard: the suite's budget data persists on the
# device and TestData latches its seeding in a static, so two shards on one device corrupt each
# other's starting state.

set -e

SCRIPT_DIR="${0:A:h}"
REPO_ROOT="${SCRIPT_DIR:h:h:h}"
APP_DIR="$REPO_ROOT/src/LoanCalculator"

SHARDS=2
RUN_IOS=0
RUN_ANDROID=0
BUILD=1
EXTRA_FILTER=""

while [ $# -gt 0 ]; do
  case "$1" in
    --ios)      RUN_IOS=1 ;;
    --android)  RUN_ANDROID=1 ;;
    --both)     RUN_IOS=1; RUN_ANDROID=1 ;;
    --shards)   SHARDS="$2"; shift ;;
    --filter)   EXTRA_FILTER="$2"; shift ;;
    --no-build) BUILD=0 ;;
    -h|--help)  sed -n '2,40p' "$0"; exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
  shift
done

if [ "$RUN_IOS" = "0" ] && [ "$RUN_ANDROID" = "0" ]; then
  echo "Pick --ios, --android or --both." >&2
  exit 1
fi

# ── Partition the fixtures ────────────────────────────────────────────────────
#
# Shard by FIXTURE, not by test: fixtures hold per-class state (OneTimeSetUp, the TestData seeding
# latch) and splitting one across devices would re-seed and re-launch for no benefit.
#
# Round-robin over a list discovered at runtime, so a fixture added later is picked up without
# anyone remembering to update a hard-coded list here.

# Read the fixtures out of the source rather than from `dotnet test --list-tests`: that command
# prints bare METHOD names with no class or namespace, so there is nothing to group by. Every
# fixture here is one [TestFixture] class per file, which makes the source the simpler source of
# truth — and it needs no build to enumerate.
echo "==> Discovering fixtures..."
FIXTURES=$(grep -l '\[TestFixture\]' "$SCRIPT_DIR"/Tests/*.cs 2>/dev/null \
           | xargs -I{} sh -c "grep -hoE 'class [A-Za-z0-9_]+' {} | head -1" \
           | sed -E 's/^class //' \
           | sort -u)

if [ -z "$FIXTURES" ]; then
  echo "    Could not discover any fixtures. Is the test project building?" >&2
  exit 1
fi

FIXTURE_COUNT=$(echo "$FIXTURES" | wc -l | tr -d ' ')
echo "    $FIXTURE_COUNT fixtures across $SHARDS shard(s)"

if [ "$SHARDS" -gt "$FIXTURE_COUNT" ]; then
  echo "    Only $FIXTURE_COUNT fixtures, so capping shards at $FIXTURE_COUNT."
  SHARDS=$FIXTURE_COUNT
fi

# shard_filter <index> -> a --filter expression for that shard's fixtures
shard_filter() {
  local idx="$1" i=0 expr=""
  while IFS= read -r fixture; do
    if [ $((i % SHARDS)) -eq "$idx" ]; then
      [ -n "$expr" ] && expr="$expr|"
      expr="${expr}FullyQualifiedName~${fixture}"
    fi
    i=$((i + 1))
  done <<< "$FIXTURES"

  if [ -n "$EXTRA_FILTER" ] && [ -n "$expr" ]; then
    # dotnet test has no grouping syntax, so an extra filter cannot be AND-ed across an OR list.
    # Honour the explicit filter alone rather than silently applying it to one shard only.
    echo "$EXTRA_FILTER"
  else
    echo "$expr"
  fi
}

# ── Build once, not per shard ────────────────────────────────────────────────

if [ "$BUILD" = "1" ]; then
  [ "$RUN_IOS" = "1" ] && {
    echo "==> Building iOS once for all shards..."
    dotnet build "$APP_DIR/LoanCalculatorMaui.csproj" -f net10.0-ios26.5 -c Debug --nologo -v q
  }
  [ "$RUN_ANDROID" = "1" ] && {
    echo "==> Building Android once for all shards..."
    dotnet build "$APP_DIR/LoanCalculatorMaui.csproj" -f net10.0-android36.0 -c Debug --nologo -v q
  }
fi

# ── iOS: one cloned simulator per shard ──────────────────────────────────────
#
# Clones of the SAME model, created on demand. Using different models instead (an iPad for shard 2)
# would change layout and fail tests written for a phone, so the shards must be identical devices.

BASE_IOS_DEVICE="${UITEST_DEVICE:-iPhone 17 Pro}"

ensure_ios_shard_device() {
  local idx="$1"
  local name="LoanCalc-Shard$idx"

  local existing
  existing=$(xcrun simctl list devices available \
             | grep -F "$name (" | head -1 | sed -E 's/.*\(([0-9A-F-]{36})\).*/\1/')
  if [ -n "$existing" ]; then
    echo "$name"
    return
  fi

  # Create on the same runtime as the newest available base device, so shards match the SDK the
  # app is built against. simctl lists oldest-first, hence the tail.
  local runtime
  runtime=$(xcrun simctl list devices available -j \
            | python3 -c "
import json,sys
d=json.load(sys.stdin)['devices']
rts=[rt for rt,devs in d.items() if any('$BASE_IOS_DEVICE'==x['name'] for x in devs)]
print(sorted(rts)[-1] if rts else '')")

  if [ -z "$runtime" ]; then
    echo "    No runtime hosts '$BASE_IOS_DEVICE'." >&2
    exit 1
  fi

  xcrun simctl create "$name" "$BASE_IOS_DEVICE" "$runtime" >/dev/null
  echo "$name"
}

# ── Android: several instances of one AVD ────────────────────────────────────
#
# One AVD can only boot once normally; -read-only lifts that, which is how several instances of the
# same image run side by side. Each gets its own serial (emulator-5554, -5556, ...).

start_android_shards() {
  local wanted="$1"
  local sdk="${ANDROID_HOME:-$HOME/Library/Android/sdk}"
  local adb="$sdk/platform-tools/adb"
  local emulator="$sdk/emulator/emulator"
  local avd="${UITEST_AVD:-$("$emulator" -list-avds | head -1)}"

  local running
  running=$("$adb" devices | grep -c "emulator.*device$" || true)

  local i=$running
  while [ "$i" -lt "$wanted" ]; do
    # stderr: this function's STDOUT is captured as the serial list by the caller.
    echo "    Starting emulator instance $((i + 1))/$wanted from AVD '$avd' (read-only)..." >&2
    "$emulator" -avd "$avd" -read-only -no-snapshot-save -no-boot-anim >/dev/null 2>&1 &
    sleep 25
    i=$((i + 1))
  done

  "$adb" wait-for-device
  sleep 10
  "$adb" devices | grep "emulator.*device$" | cut -f1 | head -"$wanted"
}

# ── Launch the shards ────────────────────────────────────────────────────────

PIDS=()
LABELS=()
LOGS=()

launch_shard() {
  local platform="$1" idx="$2" device="$3" serial="$4"
  local filter; filter=$(shard_filter "$idx")
  local label="$platform-shard$idx"
  local artifacts="$REPO_ROOT/TestResults/uitest-$label"
  local log="$artifacts/shard.log"

  if [ -z "$filter" ]; then
    echo "    $label has no fixtures assigned, skipping."
    return
  fi

  mkdir -p "$artifacts"
  echo "    $label -> port $((4723 + idx)) | artifacts TestResults/uitest-$label"

  # An ARRAY, not a packed string: zsh performs no field splitting on "$var", so
  # "--device My Sim" would reach run-uitests.sh as a single unknown option.
  local -a device_args
  [ -n "$device" ] && device_args=(--device "$device")

  (
    export UITEST_APPIUM_URL="http://127.0.0.1:$((4723 + idx))"
    export UITEST_ARTIFACTS="$artifacts"
    export UITEST_SHARD_INDEX="$idx"
    [ -n "$serial" ] && export UITEST_SERIAL="$serial"

    # Already built above; a per-shard rebuild would serialise the whole point of this script.
    "$SCRIPT_DIR/run-uitests.sh" --"$platform" --no-build "${device_args[@]}" --filter "$filter"
  ) >"$log" 2>&1 &

  PIDS+=($!)
  LABELS+=("$label")
  LOGS+=("$log")
}

if [ "$RUN_IOS" = "1" ]; then
  echo "==> Preparing $SHARDS iOS simulator(s)..."
  for idx in $(seq 0 $((SHARDS - 1))); do
    name=$(ensure_ios_shard_device "$idx")
    launch_shard ios "$idx" "$name" ""
  done
fi

if [ "$RUN_ANDROID" = "1" ]; then
  echo "==> Preparing $SHARDS Android emulator(s)..."
  SERIALS=($(start_android_shards "$SHARDS"))
  idx=0
  for serial in "${SERIALS[@]}"; do
    # Offset so Android never shares a shard index — and therefore a systemPort — with iOS when
    # --both is used.
    launch_shard android "$((idx + SHARDS))" "" "$serial"
    idx=$((idx + 1))
  done
fi

echo ""
echo "==> ${#PIDS[@]} shard(s) running. Tailing is per shard: tail -f TestResults/uitest-*/shard.log"
echo ""

FAILED=0
for i in {1..${#PIDS[@]}}; do
  if wait "${PIDS[$i]}"; then
    echo "PASS  ${LABELS[$i]}"
  else
    echo "FAIL  ${LABELS[$i]}  (${LOGS[$i]})"
    FAILED=1
  fi
done

echo ""
if [ "$FAILED" = "1" ]; then
  echo "==> Some shards failed."
  for i in {1..${#LOGS[@]}}; do
    shard_summary=$(grep -hE 'error TESTERROR|Test summary' "${LOGS[$i]}" 2>/dev/null || true)
    echo ""
    echo "--- ${LABELS[$i]} ---"
    if [ -n "$shard_summary" ]; then
      echo "$shard_summary" | sed 's|^|  |'
    else
      # No test ever ran: the shard died in setup. Show the tail, or the cause stays invisible —
      # which is exactly what happened when a packed --device argument was rejected.
      echo "  (no test results — shard failed before running. Last lines:)"
      tail -12 "${LOGS[$i]}" 2>/dev/null | sed 's|^|  |'
    fi
  done
  exit 1
fi

echo "==> All shards passed."
