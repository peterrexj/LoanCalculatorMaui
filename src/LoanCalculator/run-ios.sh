#!/bin/zsh

# Usage:
#   ./run-ios.sh                # iPhone 17 Pro (default, iOS 26.5), filtered logs
#   ./run-ios.sh --ipad         # iPad Pro 13-inch (M5)
#   ./run-ios.sh --device "iPad mini (A17 Pro)"
#   ./run-ios.sh --logs         # stream ALL app output (no filter)
#   ./run-ios.sh --nologs       # launch silently, no log streaming

PROJECT="LoanCalculatorMaui.csproj"
APP_BUNDLE="bin/Debug/net10.0-ios26.5/iossimulator-arm64/LoanCalculatorMaui.app"
BUNDLE_ID="com.pj.loan.afford.calc"

SIMULATOR_NAME="iPhone 17 Pro"
LOG_MODE="filtered"   # filtered | full | none

for arg in "$@"; do
  case "$arg" in
    --ipad)   SIMULATOR_NAME="iPad Pro 13-inch (M5)" ;;
    --logs)   LOG_MODE="full" ;;
    --nologs) LOG_MODE="none" ;;
    --device) ;;
  esac
done

for i in $(seq 1 $#); do
  if [ "${@[$i]}" = "--device" ] && [ $((i+1)) -le $# ]; then
    SIMULATOR_NAME="${@[$((i+1))]}"
  fi
done

cd "$(dirname "$0")"

echo "==> Finding simulator: $SIMULATOR_NAME..."
# Two traps here, both of which silently give you the wrong simulator:
#   1. The trailing " (" anchors the name — without it "iPhone 17 Pro" also matches
#      "iPhone 17 Pro Max".
#   2. The same name exists on every installed iOS runtime, and simctl lists them oldest-first,
#      so `head -1` picks the OLDEST. Track the runtime header and keep the newest match instead.
SIMULATOR_ID=$(xcrun simctl list devices available | awk -v name="$SIMULATOR_NAME" '
  /^-- iOS /           { split($3, v, "."); cur = v[1] * 1000 + v[2]; next }
  index($0, name " (") { if (cur >= best) { best = cur; line = $0 } }
  END { if (line != "") { match(line, /[0-9A-F-]{36}/); print substr(line, RSTART, RLENGTH) } }')

if [ -z "$SIMULATOR_ID" ]; then
  echo "ERROR: No simulator found matching '$SIMULATOR_NAME'"
  echo "Available simulators:"
  xcrun simctl list devices available | grep -v "^==" | grep -v "^--" | grep -v "^$" | grep "iPhone\|iPad"
  exit 1
fi

echo "    Found: $SIMULATOR_ID"

echo "==> Booting simulator..."
STATUS=$(xcrun simctl list devices | grep "$SIMULATOR_ID" | grep -o "Booted")
if [ "$STATUS" != "Booted" ]; then
  xcrun simctl boot "$SIMULATOR_ID"
  open -a Simulator
else
  echo "    Simulator already booted"
fi

echo "==> Building..."
dotnet build "$PROJECT" -f net10.0-ios26.5 -c Debug || exit 1

echo "==> Installing..."
xcrun simctl install "$SIMULATOR_ID" "$APP_BUNDLE" || exit 1

echo "==> Terminating existing instance..."
xcrun simctl terminate "$SIMULATOR_ID" "$BUNDLE_ID" 2>/dev/null || true

if [ "$LOG_MODE" = "none" ]; then
  echo "==> Launching (no logs)..."
  xcrun simctl launch "$SIMULATOR_ID" "$BUNDLE_ID"
  echo "==> Done. App running in simulator."
elif [ "$LOG_MODE" = "full" ]; then
  echo "==> Launching with FULL output (Ctrl+C to stop)..."
  xcrun simctl launch --console-pty "$SIMULATOR_ID" "$BUNDLE_ID" 2>&1 | grep -v "CoreFoundation\|UIKit\|RemoteTextInput\|RunningBoard\|Sentry.*timeout\|Sentry.*rate"
else
  echo "==> Launching with filtered output (Ctrl+C to stop)..."
  echo "    Tip: use --logs for full output, --nologs to launch silently"
  xcrun simctl launch --console-pty "$SIMULATOR_ID" "$BUNDLE_ID" 2>&1 | grep --line-buffered -E "\[CRASH\]|\[splash\]|\[Edit|\[AddOrUpdate\]|fail:|error|Error|exception|Exception|Unhandled|fatal|Fatal" | grep -v "Sentry\|NSURLError\|TaskCancel\|NU1608"
fi
