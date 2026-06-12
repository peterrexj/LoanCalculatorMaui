#!/bin/zsh

# Usage:
#   ./run-ios.sh                # iPhone 16 Pro (default), filtered logs
#   ./run-ios.sh --ipad         # iPad Pro 13-inch (M4)
#   ./run-ios.sh --device "iPad mini (A17 Pro)"
#   ./run-ios.sh --logs         # stream ALL app output (no filter)
#   ./run-ios.sh --nologs       # launch silently, no log streaming

PROJECT="LoanCalculatorMaui.csproj"
APP_BUNDLE="bin/Debug/net9.0-ios18.0/iossimulator-arm64/LoanCalculatorMaui.app"
BUNDLE_ID="com.pj.loan.afford.calc"

SIMULATOR_NAME="iPhone 16 Pro"
LOG_MODE="filtered"   # filtered | full | none

for arg in "$@"; do
  case "$arg" in
    --ipad)   SIMULATOR_NAME="iPad Pro 13-inch (M4)" ;;
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
SIMULATOR_ID=$(xcrun simctl list devices available | grep "$SIMULATOR_NAME" | grep -v "unavailable" | head -1 | sed 's/.*(\([A-F0-9-]*\)).*/\1/')

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
dotnet build "$PROJECT" -f net9.0-ios18.0 -c Debug || exit 1

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
  xcrun simctl launch --console-pty "$SIMULATOR_ID" "$BUNDLE_ID" 2>&1 | grep --line-buffered -E "\[CRASH\]|\[Edit|\[AddOrUpdate\]|error|Error|exception|Exception|Unhandled|fatal|Fatal" | grep -v "Sentry\|NSURLError\|TaskCancel\|NU1608"
fi
