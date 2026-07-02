# Release Checklist

## Already Good

- `TESTING_PREMIUM_OVERRIDE = false` — premium gates are active
- `ITSAppUsesNonExemptEncryption = false` — correct for App Store
- Sentry `Debug` is gated by `#if DEBUG` — won't log in release builds
- App icon and splash screen are set up via `MauiIcon` / `MauiSplashScreen`
- iOS `CFBundleDisplayName = "Loan Affordability"` — fixed

---

## Known Issues / Needs Attention

### Version numbers are out of sync

- `csproj`: `ApplicationDisplayVersion = 1.12.0`, `ApplicationVersion = 1`
- iOS `Info.plist`: `CFBundleShortVersionString = 1.26`
- Android `AndroidManifest.xml`: `versionCode = 34`, `versionName = "Release 34"`

iOS `Info.plist` should match the `csproj` version, or vice versa — pick one source of truth.
For iOS, MAUI uses the `csproj` values at build time and they override `Info.plist`, so `1.26`
in `Info.plist` may already be a non-issue — but confirm the `csproj` has the right next version number.

**Android `versionName = "Release 34"`** — this shows in the Play Store listing.
Consider whether it should be a human-readable version like `1.12.0` instead. Not changed — left as-is intentionally.

---

## Pre-Submission Checklist

| Item | iOS | Android |
|---|---|---|
| Version number incremented from last release | Bump `ApplicationDisplayVersion` + `ApplicationVersion` in `csproj` | Bump `android:versionCode` + `android:versionName` in `AndroidManifest.xml` |
| Store screenshots updated | App Store Connect | Play Console |
| "What's New" release notes written | App Store Connect | Play Console |
| App tested on real device (not just simulator) | iPhone + iPad | Phone + Tablet |
| In-app purchase product IDs verified live | TestFlight | Internal test track |
| Sentry DSN is the production project | Already set in `MauiProgram.cs` | Already set in `MauiProgram.cs` |
| Privacy policy URL up to date | App Store Connect | Play Console |

---

## Key File Locations

| What | File |
|---|---|
| App version (primary source of truth) | `src/LoanCalculator/LoanCalculatorMaui.csproj` — `ApplicationDisplayVersion`, `ApplicationVersion` |
| iOS display name + bundle version | `src/LoanCalculator/Platforms/iOS/Info.plist` — `CFBundleDisplayName`, `CFBundleShortVersionString` |
| Android version | `src/LoanCalculator/Platforms/Android/AndroidManifest.xml` — `android:versionCode`, `android:versionName` |
| Premium gate bypass flag | `src/LoanCalculator.Core/Services/SharedServiceCore.cs` — `TESTING_PREMIUM_OVERRIDE` (must be `false` for release) |
| Sentry config | `src/LoanCalculator/MauiProgram.cs` — `options.Dsn` |
| App icon | `src/LoanCalculator/Resources/AppIcon/appicon.svg` + `appiconfg.png` |
| Splash screen | `src/LoanCalculator/Resources/AppIcon/appiconfg.png` (Color `#0A1A20`) |
