# 05 — Crashes: async void + OnPropertyChanged from Background Threads

**Priority:** Critical  
**Status:** ✅ DONE — Background thread marshalling fixed; `async void` converted to `async Task`  
**Symptoms:** `InvalidOperationException: "Collection was modified"` or silent binding failures; Sentry capturing `System.Threading.SynchronizationLockException`; charts not updating after data load.

---

## What Was Done

- **Fix B (thread marshalling) — DONE:** `UpdateProjectionDataAsync` in both `IncomeViewModel` and `ExpenseViewModel` now does CPU work in `Task.Run`, then uses `MainThread.BeginInvokeOnMainThread` to fire all `OnPropertyChanged` calls. No binding notifications from background threads.
- **Fix C (LoadDataSet awaiting) — DONE:** `LoanView.OnAppearing` now directly `await`s `LoadDataSet()` inside its `try` block; the `finally` block correctly runs after loading completes. `Dispatcher.Dispatch` wrapper removed.
- **Fix A (`async void` → `async Task`) — DONE:** All 4 methods converted in both `IncomeViewModel` and `ExpenseViewModel`:
  - `UpdateProjectionDataAsync()` → `async Task`
  - `RefreshIncomePropertyChangedAsync()` → `async Task`
  - All 8 call sites (inside property setters) updated to `_ = MethodAsync()` fire-and-forget pattern.

---

## Problem

### Issue A — `async void` in ViewModels

`async void` methods swallow exceptions — any unhandled exception inside them terminates the app on mobile without going through the normal exception handler, so Sentry may not capture them.

Affected methods:
- `IncomeViewModel.UpdateProjectionDataAsync()` — declared `async void`
- `ExpenseViewModel.UpdateProjectionDataAsync()` — declared `async void`
- `IncomeViewModel.RefreshIncomePropertyChangedAsync()` — declared `async void`
- `ExpenseViewModel.RefreshExpensePropertyChangedAsync()` — declared `async void`

### Issue B — `OnPropertyChanged` called from background thread

Inside these `async void` methods, `Task.Run(...)` fires `OnPropertyChanged(...)` from a thread pool thread. MAUI's binding engine and Syncfusion controls are **not thread-safe** — modifying bound properties from a non-UI thread causes:

- Silent binding failures (UI never updates)
- `InvalidOperationException` on collections (charts, DataGrid)
- Occasional native crashes on iOS

Example in `ExpenseViewModel` (similar pattern in `IncomeViewModel`):

```csharp
// Current — fires OnPropertyChanged from a background thread
private async void UpdateProjectionDataAsync()
{
    await Task.Run(() =>
    {
        // calculates data...
        OnPropertyChanged(nameof(ProjectionData));  // <-- NOT on UI thread
    });
}
```

### Issue C — `Dispatcher.Dispatch(async () => ...)` loses exceptions

In `LoanView.OnAppearing` (line 51–54):

```csharp
Dispatcher.Dispatch(async () =>
{
    await LoadDataSet();
});
```

`Dispatch` does not await the returned `Task` — any exception thrown after the first `await` inside `LoadDataSet` is silently swallowed (the `try/catch` inside `LoadDataSet` catches internal exceptions, but the outer `finally` in `OnAppearing` runs before `LoadDataSet` completes, resetting `IsPageBusy = false` prematurely).

---

## Fix

### Fix A — Convert `async void` to `async Task`

```csharp
// Before
private async void UpdateProjectionDataAsync() { ... }

// After
private async Task UpdateProjectionDataAsync()
{
    try { ... }
    catch (Exception ex) { _errorHandlingService.HandleException(ex); }
}
```

Update all callers to `await` (or fire-and-forget safely with `_ = UpdateProjectionDataAsync()`).

### Fix B — Marshal `OnPropertyChanged` to the UI thread

Wrap all `OnPropertyChanged` calls that happen inside `Task.Run` with a UI thread dispatch. Add a helper to `BasePropertyChangeModel` or `ViewModelUiBase`:

```csharp
// In ViewModelUiBase.cs
protected void NotifyOnUiThread(string propertyName)
{
    if (MainThread.IsMainThread)
        OnPropertyChanged(propertyName);
    else
        MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(propertyName));
}
```

Then in `UpdateProjectionDataAsync`:

```csharp
private async Task UpdateProjectionDataAsync()
{
    var result = await Task.Run(() => CalculateProjection());
    // Now back on UI thread context (if called from UI thread originally)
    // Or explicitly marshal:
    MainThread.BeginInvokeOnMainThread(() =>
    {
        ProjectionData = result;
        OnPropertyChanged(nameof(ProjectionData));
        OnPropertyChanged(nameof(ProjectionSummary));
    });
}
```

**Better pattern:** Use `await Task.Run(...)` to do CPU work off the UI thread, then update ViewModel properties *after* the await (which resumes on the captured SynchronizationContext = UI thread) rather than calling `OnPropertyChanged` inside the `Task.Run` lambda.

```csharp
private async Task UpdateProjectionDataAsync()
{
    var result = await Task.Run(() => CalculateProjection()).ConfigureAwait(false);

    // ConfigureAwait(false) means we might be on a thread pool thread here.
    // Use MainThread.BeginInvokeOnMainThread to be explicit:
    MainThread.BeginInvokeOnMainThread(() =>
    {
        ProjectionData = result;
        NotifyProjectionChanged();
    });
}
```

### Fix C — Properly await `LoadDataSet` from `OnAppearing`

```csharp
// Before
protected override async void OnAppearing()
{
    await Task.Delay(100);
    await Task.Yield();
    Dispatcher.Dispatch(async () => { await LoadDataSet(); });
    // ... finally block runs here, before LoadDataSet completes
}

// After
protected override async void OnAppearing()
{
    try
    {
        base.OnAppearing();
        await Task.Delay(100);
        await LoadDataSet();  // awaited directly — finally block runs after completion
    }
    catch (Exception ex)
    {
        _errorHandlingService.HandleException(ex);
    }
    finally
    {
        PageHelper.PageLoadingComplete();
        _viewModel.IsUpdating = false;
        _viewModel.IsBusy = false;
        _viewModel.IsActive = true;
        _viewModel.IsPageBusy = false;
    }
}
```

The `Dispatcher.Dispatch` wrapper was compensating for a threading issue — once `OnPropertyChanged` calls are properly marshalled (Fix B), this workaround is no longer needed.

---

## Verification

1. Navigate between all 4 tabs rapidly 10 times — no `InvalidOperationException` in Sentry.
2. Open Income tab, add several entries, switch to Loan tab and back — projection chart should update correctly.
3. Check Sentry for any `System.Threading.*` exceptions post-deploy.
4. Test on iOS simulator and real device — iOS is strictest about thread affinity violations.
