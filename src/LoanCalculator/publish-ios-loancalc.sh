#!/bin/bash
set -e

PROJECT="LoanCalculatorMaui.csproj"
BUNDLE_ID="com.pj.loan.afford.calc"

# ── Resolve iOS target framework from csproj ─────────────────────────────────
IOS_TF=$(grep -oE 'net[0-9]+\.[0-9]+-ios[0-9.]+' "$PROJECT" | head -1)
if [ -z "$IOS_TF" ]; then
  echo "ERROR: Could not determine iOS TargetFramework from $PROJECT" >&2; exit 1
fi
echo "==> Target framework: $IOS_TF"

OUT_DIR="bin/Release/$IOS_TF/ios-arm64/publish"

# ── Verify running on Mac ─────────────────────────────────────────────────────
if [[ "$(uname)" != "Darwin" ]]; then
  echo "ERROR: iOS publishing requires macOS." >&2; exit 1
fi

# ── Check Xcode command line tools ───────────────────────────────────────────
if ! xcode-select -p &>/dev/null; then
  echo "ERROR: Xcode command line tools not found. Run: xcode-select --install" >&2; exit 1
fi
echo "==> Xcode: $(xcode-select -p)"

# ── Publish ──────────────────────────────────────────────────────────────────
echo "==> Publishing release IPA..."
dotnet publish "$PROJECT" -f "$IOS_TF" -c Release
if [ $? -ne 0 ]; then echo "ERROR: Publish failed" >&2; exit 1; fi

# ── Locate output IPA ────────────────────────────────────────────────────────
IPA=$(find "$OUT_DIR" -name "*.ipa" 2>/dev/null | head -1)
if [ -z "$IPA" ]; then
  echo "ERROR: IPA not found under $OUT_DIR" >&2; exit 1
fi

echo ""
echo "==> Done. Upload this file to App Store Connect:"
echo "    $IPA"
