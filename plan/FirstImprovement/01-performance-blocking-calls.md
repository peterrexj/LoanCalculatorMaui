# 01 — Performance: Blocking `.Wait()` on Async Calls

**Priority:** Critical  
**Status:** ✅ DONE  
**Symptoms:** App freezes on tab switch or startup, especially on iOS (strict thread affinity); potential deadlocks on the main thread.

---

## What Was Done

- `SharedServiceCore.SaveData<T>` converted to fire-and-forget (no `.Wait()`, no lock).
- All `GetData` calls converted to `async Task<T>` methods (`GetExpenseSummaryAsync`, `GetIncomeSummaryAsync`, `GetLoanViewModelAsync`).
- `IsPremiumUserAsync`, `IsCurrentDayAsync`, `HasAlertedUserForDataWipeAsync` all converted to `async Task<bool>` — all callers `await` them.
- `ThemeHandler.LoadDefaultStyle` no longer wraps embedded resource reads in `Task.Run(...).Wait()`.
- One remaining sync `SecureStorage.GetAsync(...).GetAwaiter().GetResult()` in `IsPremiumUser()` (the sync overload) — see note below.

### Remaining: `IsPremiumUser()` sync overload (SharedServiceCore.cs:193)

`IsPremiumUser()` still calls `.GetAwaiter().GetResult()` to satisfy synchronous callers (`IsTrialUser`, `App.xaml.cs` constructor). As long as `TESTING_PREMIUM_OVERRIDE = true` in dev, this never hits the SecureStorage path. For release builds this is a latent deadlock risk on iOS. Convert `IsTrialUser` to an async property or remove the sync overload before disabling the override.

---

## Problem

Multiple locations use `Task.Run(async () => ...).Wait()` or `.Result` to force async work synchronously. On iOS the main thread dispatcher can deadlock when a background thread tries to complete work that needs to re-enter the main thread (e.g. MAUI bindings firing during deserialization). Even when it doesn't deadlock, it blocks the calling thread for the full duration of a disk read, causing noticeable freezes.

### Affected locations

| File | Line(s) | Pattern |
|------|---------|---------|
| `src/LoanCalculator.Core/Services/SharedServiceCore.cs` | 70–73 | `Task.Run(async () => await LocalStorage.SaveData(data)).Wait()` |
| `src/LoanCalculator.Core/Services/SharedServiceCore.cs` | 89 | `Task.Run(async () => temp = await LocalStorage.GetData<TViewModel>()).Wait()` |
| `src/LoanCalculator.Core/Services/SharedServiceCore.cs` | 106 | `Task.Run(async () => temp = await LocalStorage.GetData<ExpenseViewModel>()).Wait()` |
| `src/LoanCalculator.Core/Services/SharedServiceCore.cs` | 121 | `Task.Run(async () => temp = await LocalStorage.GetData<IncomeViewModel>()).Wait()` |
| `src/LoanCalculator.Core/Services/SharedServiceCore.cs` | 138 | `Task.Run(async () => temp = await LocalStorage.GetData<LoanViewModel>()).Wait()` |
| `src/LoanCalculator.Core/Services/SharedServiceCore.cs` | 225, 239, 257, 267, 284, 294 | `Task.Run(() => SecureStorage.*).Result` / `.Wait()` |
| `src/LoanCalculator.Core/Themes/ThemeHandler.cs` | (LoadDefaultStyle) | `Task.Run(async () => ...).Wait()` |
| `src/LoanCalculator/Services/NameValueDataService.cs` | ~14 | `Task.Run(async () => ...).Wait()` |

---

## Fix

### `SharedServiceCore.SaveData<T>`

Convert to a true fire-and-forget with a guard — callers already check `PageHelper.IsFormLoading`. No caller awaits the return value today (they all do `SharedServiceCore.SaveData(this)` without `await`).

```csharp
// Before
public static Task SaveData<T>(T data)
{
    lock (_saveDataLock)
    {
        Task.Run(async () =>
        {
            await LocalStorage.SaveData(data).ConfigureAwait(false);
        }).Wait();
    }
    return Task.CompletedTask;
}

// After
public static void SaveData<T>(T data)
{
    if (_loadSafe || PageHelper.IsFormLoading) return;
    // Fire-and-forget: errors go to Sentry via ErrorHandlingService
    _ = Task.Run(async () =>
    {
        try
        {
            await LocalStorage.SaveData(data).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            ErrorHandlingService.HandleException(e);
        }
    });
}
```

> The `lock` is removed because the new pattern is non-blocking and each save overwrites the same file anyway (last write wins is correct for this use case).

### `SharedServiceCore.ExpenseSummary` / `IncomeSummary` / `GetLoanViewModel`

These are called from `LoanView.LoadDataSet()` already inside `Task.Run(...)`. Convert the properties to async methods so callers can properly await them.

```csharp
// Before — synchronous property with internal .Wait()
public static ExpenseViewModel ExpenseSummary { get { ... Task.Run(...).Wait(); } }

// After — async method
public static async Task<ExpenseViewModel> GetExpenseSummaryAsync()
{
    var temp = await LocalStorage.GetData<ExpenseViewModel>().ConfigureAwait(false);
    if (temp == null) return new ExpenseViewModel();
    temp.TransactionRecords?.SumUpData();
    return temp;
}
```

Update callers in `LoanView.xaml.cs` (line ~109–125) and `IncomeView.xaml.cs` / `ExpenseView.xaml.cs` accordingly — they already `await Task.WhenAll(...)` so this is a drop-in change.

### `SharedServiceCore.IsPremiumUser` / `IsCurrentDay` / `HasAlertedUserForDataWipe`

These are called from `OnAppearing` (which is already async). Convert each to `async Task<bool>` and await them.

```csharp
public static async Task<bool> IsPremiumUserAsync()
{
    try
    {
        if (AppInformation is { IsFullyPaidApplication: true }) return true;
        var value = await SecureStorage.GetAsync("IsPremium").ConfigureAwait(false);
        return value == "true";
    }
    catch { return false; }
}
```

The synchronous `IsTrialUser` property can be kept temporarily for code paths that cannot easily be made async (e.g., XAML-bound computed properties) but it must not be called on the main thread in hot paths. Mark it with a `// NOTE: only call off main thread` comment.

### `ThemeHandler.LoadDefaultStyle`

Called from `App` constructor (synchronous context). The theme load reads an embedded resource stream — this is synchronous by nature (no I/O). The `Task.Run(...).Wait()` wrapper exists only to run it off the current thread. Since it's called once at startup from the constructor (not the main thread dispatch queue), replacing it with a direct synchronous call is correct:

```csharp
// Remove Task.Run wrapper — embedded resource reads are synchronous
public void LoadDefaultStyle()
{
    var themeFile = GetThemeFile(); // reads Preferences — already sync on MAUI
    ClearAllResources("LoanApp");
    LoadResourceDictionary("Theme.CommonStyles.xaml");
    LoadResourceDictionary("Theme.CommonDataGridStyles.xaml");
    LoadResourceDictionary(themeFile);
    UpdateResources("LoanApp");
}
```

---

## Verification

1. Run on a real iOS device. Tab switches should no longer feel "locked up" for 200–500ms.
2. Rapid typing in the Property Amount field should not freeze the UI.
3. Run the unit tests in `LoanCalculator.UnitTests` — no regressions.
4. Check Sentry for any new `InvalidOperationException` or `DeadlockException` after deploying.
