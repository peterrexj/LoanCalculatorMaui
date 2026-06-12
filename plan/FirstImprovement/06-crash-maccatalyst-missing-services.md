# 06 — Crashes: MacCatalyst Missing DI Registrations

**Priority:** High  
**Status:** ✅ DONE  
**Symptoms:** App crashes at startup on Mac (MacCatalyst) with `NullReferenceException` or `InvalidOperationException` when any service tries to resolve `ILocalStorage` or `IAppInformation`.

---

## What Was Done

- `#elif MACCATALYST` block in `MauiProgram.cs` now registers `iOSAppInformation` and `iOSLocalStorageService` — same as the iOS block (Option A from the plan).

---

## Problem

In `MauiProgram.cs`, the platform-conditional DI registration block has an empty `#elif MACCATALYST` branch:

```csharp
// src/LoanCalculator/MauiProgram.cs, lines 65–66
#elif MACCATALYST
    // <--- NOTHING REGISTERED
#elif WINDOWS
    builder.Services.AddSingleton<IAppInformation, WindowsAppInformation>();
    builder.Services.AddSingleton<ILocalStorage, WindowsLocalStorageService>();
#endif
```

Any code path that calls `SharedServiceCore.LocalStorage` or `SharedServiceCore.AppInformation` on Mac will get `null` back from `ServiceLocator.GetService<T>()`, which then causes a `NullReferenceException` when the property is dereferenced.

Similarly, `App.xaml.cs` calls `SharedServiceCore.IsTrialUser` at startup, which calls `ServiceLocator.GetService<IAppInformation>()` — `null` on Mac.

---

## Fix

### Option A — Reuse the iOS implementations (simplest, correct)

MacCatalyst is iOS running on Mac hardware. The iOS implementations of both services use iOS-standard APIs (`NSFileManager`, `NSUserDefaults`, etc.) which are all available on MacCatalyst. Register the same iOS classes:

```csharp
#elif MACCATALYST
    builder.Services.AddSingleton<IAppInformation, LoanCalculatorMaui.Platforms.iOS.Services.iOSAppInformation>();
    builder.Services.AddSingleton<ILocalStorage, LoanCalculatorMaui.Platforms.iOS.Services.iOSLocalStorageService>();
```

Verify that the iOS service implementations do not have any iOS-simulator-only checks that would fail on a real Mac.

### Option B — Create dedicated MacCatalyst implementations

If `iOSAppInformation.IsAustralia` or similar properties need to return different values on Mac (e.g. the App Store listing for Mac is different), create:

- `src/LoanCalculator/Platforms/MacCatalyst/Services/MacAppInformation.cs`
- `src/LoanCalculator/Platforms/MacCatalyst/Services/MacLocalStorageService.cs`

These can inherit from or delegate to the iOS implementations with only the differing properties overridden.

**Recommendation:** Start with Option A. It unblocks the MacCatalyst build immediately with zero new code.

---

## Additional Guard

Add null guards in `SharedServiceCore` for the services that can be null on unsupported platforms:

```csharp
// SharedServiceCore.cs — property getters
public static ILocalStorage LocalStorage =>
    _localStorage ??= ServiceLocator.GetService<ILocalStorage>()
    ?? throw new InvalidOperationException("ILocalStorage is not registered for this platform.");
```

This converts a silent `NullReferenceException` 10 frames deep into a clear error at the point of missing registration.

---

## Verification

1. Build and run on MacCatalyst target — no startup crash.
2. Navigate through all 4 tabs — data loads and saves correctly.
3. Theme switching works (relies on `ILocalStorage`).
