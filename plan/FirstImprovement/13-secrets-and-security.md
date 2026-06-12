# 13 — Security: Hardcoded Credentials and Keys

**Priority:** Medium  
**Status:** ❌ NOT DONE — Android signing password still hardcoded in `.csproj`  
**Risk:** Exposed secrets in source control can be scraped. The Android signing password is the highest risk — it allows anyone with repo access to sign APKs as your identity.

---

## Current State

- `CatholicSaintsPassword@01` still hardcoded in `LoanCalculatorMaui.csproj` (lines 85–86 and 92–93) for both debug and release configurations.
- Syncfusion license key still hardcoded in `App.xaml.cs` (line 22).
- Sentry DSN still hardcoded in `MauiProgram.cs` (line 25).

None of the `local.props` / environment variable migration from the fix plan has been applied.

**Action required before making this repo public or sharing with new contributors:** Follow the fix steps below to move all three secrets to environment variables / `local.props`.

---

## What's Exposed

| Location | What | Risk Level |
|----------|------|-----------|
| `src/LoanCalculator/LoanCalculatorMaui.csproj`, lines 85–93 | Android keystore password (`AndroidSigningStorePass`, `AndroidSigningKeyPass`) — value: `CatholicSaintsPassword@01` | **High** — anyone can sign APKs as this publisher |
| `src/LoanCalculator/MauiProgram.cs`, line 25 | Sentry DSN URL | Medium — allows sending fake error events to your Sentry project |
| `src/LoanCalculator/App.xaml.cs`, line 22 | Syncfusion license key | Low — community license, but should not be in source |

---

## Fix

### Android Signing Credentials

Move signing credentials out of the project file and into environment variables or a local secrets file that is `.gitignore`d.

**Step 1 — Remove from `.csproj`:**

```xml
<!-- Remove these lines from LoanCalculatorMaui.csproj -->
<AndroidSigningStorePass>CatholicSaintsPassword@01</AndroidSigningStorePass>
<AndroidSigningKeyPass>CatholicSaintsPassword@01</AndroidSigningKeyPass>
```

**Step 2 — Use environment variables instead:**

```xml
<!-- LoanCalculatorMaui.csproj — read from env var -->
<AndroidSigningStorePass>$(ANDROID_KEYSTORE_PASS)</AndroidSigningStorePass>
<AndroidSigningKeyPass>$(ANDROID_KEY_PASS)</AndroidSigningKeyPass>
```

**Step 3 — For local development**, create a `local.props` file:

```xml
<!-- local.props — add to .gitignore -->
<Project>
  <PropertyGroup>
    <AndroidSigningStorePass>CatholicSaintsPassword@01</AndroidSigningStorePass>
    <AndroidSigningKeyPass>CatholicSaintsPassword@01</AndroidSigningKeyPass>
  </PropertyGroup>
</Project>
```

Import it in the csproj:

```xml
<Import Project="local.props" Condition="Exists('local.props')" />
```

**Step 4 — Add `local.props` to `.gitignore`:**

```
# Local secrets
local.props
```

**Step 5 — For CI/CD:** Set `ANDROID_KEYSTORE_PASS` and `ANDROID_KEY_PASS` as secrets in GitHub Actions / Azure DevOps / your CI provider.

### Sentry DSN

The DSN is a project-specific ingest URL. It's not a password but it allows others to pollute your Sentry project with fake events. Move to an environment variable or build configuration:

```csharp
// MauiProgram.cs
options.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN")
    ?? "https://16ef66602415c605019107b0a5bd0978@o4508789158445056.ingest.de.sentry.io/4508789160280144";
```

For local development the fallback keeps things working. In production CI, set `SENTRY_DSN` as a secret.

Alternatively, embed it in a build-time constant via a `Directory.Build.props` or `appsettings.json` approach — acceptable for a mobile app DSN (it's semi-public by nature).

### Syncfusion License Key

The community license key is not a secret in the traditional sense (it's tied to a free account), but it should not live in version-controlled source:

```csharp
// App.xaml.cs
var sfKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE")
    ?? "Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXpfd3RQR2VZUUFwWERWYEo=";
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(sfKey);
```

---

## Rotate the Android Signing Password

Since the password is already in source control history, simply removing it from the file is not enough — it's in `git log`. Options:

1. **Change the keystore password** — use `keytool -storepasswd` to change it, update local secrets.
2. **Rewrite git history** — use `git filter-repo` to remove it from all commits (requires coordination with all repo contributors).
3. **Accept the risk** — if the repo is private and has limited contributors, the immediate risk is low. But document that the password has been rotated.

**Recommendation:** Change the password and update `local.props`. Don't rewrite history unless the repo has been public.

---

## Verification

1. `git grep "CatholicSaintsPassword"` returns no results after the change.
2. Android release build succeeds using the environment variable path.
3. `.gitignore` includes `local.props` — `git status` does not show it after creation.
