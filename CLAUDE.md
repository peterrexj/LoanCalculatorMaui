# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Build & Run Commands

All commands run from `src/LoanCalculator/` unless noted.

> **Do not build or run the app unless the user explicitly asks.** Builds here are slow
> (MAUI multi-target) and the user typically builds/runs themselves. After making code
> changes, stop and report what changed — do not kick off `dotnet build`, `dotnet run`,
> `run-ios.sh`, `run-android.ps1`, or deploy to a simulator/emulator on your own. Only do so
> when the user says to build, run, test, or deploy.

### Build

```bash
dotnet build LoanCalculatorMaui.csproj -f net9.0-ios18.0 -c Debug
dotnet build LoanCalculatorMaui.csproj -f net9.0-android36.0 -c Debug
dotnet build LoanCalculatorMaui.csproj -f net9.0-maccatalyst -c Debug
```

### Run on iOS Simulator

```bash
./run-ios.sh                          # iPhone 16 Pro (default)
./run-ios.sh --ipad                   # iPad Pro 13-inch (M4)
./run-ios.sh --device "iPad mini (A17 Pro)"
```

The script builds, installs to the simulator, launches with `--console-pty`, and filters log output to app-relevant lines only. `Console.WriteLine` output appears in the terminal.

### Run on Android

```bash
pwsh ./run-android.ps1                # Medium_Phone_API_36.1
pwsh ./run-android.ps1 -Tablet
pwsh ./run-android.ps1 -Avd <name>
```

### Run Tests

```bash
dotnet test src/Tests/LoanCalculator.UnitTests/LoanCalculator.UnitTests.csproj
```

Tests are `net9.0` only (no device required). Active test coverage: stamp duty bracket calculations, theme XAML key consistency.

### Playground (PDF generation dry-run, no device needed)

```bash
dotnet run --project src/LoanCalculator.Playground/LoanCalculator.Playground.csproj
```

---

## Project Structure

| Project | Purpose |
|---|---|
| `src/LoanCalculator/` | .NET MAUI app — views, platform services, DI wiring, shell |
| `src/LoanCalculator.Core/` | Class library — all business logic, ViewModels, models, service interfaces, PDF, theme handler |
| `src/Tests/LoanCalculator.UnitTests/` | NUnit tests targeting `LoanCalculator.Core` only |
| `src/LoanCalculator.Playground/` | Console sandbox for PDF generation |

Platform-specific implementations live in `src/LoanCalculator/Platforms/{Android,iOS,MacCatalyst,Windows}/`.

---

## Architecture

### Dependency Injection

DI is wired in `src/LoanCalculator/MauiProgram.cs`. Platform services are selected with `#if ANDROID / IOS / MACCATALYST / WINDOWS`. MacCatalyst reuses the iOS implementations.

In addition to constructor injection, `ServiceLocator` (in Core) is a static wrapper around `IServiceProvider` initialised in `App.xaml.cs`. It is used inside `SharedServiceCore` and Core classes where constructor injection is impractical.

All four primary ViewModels (`LoanViewModel`, `ExpenseViewModel`, `IncomeViewModel`, `SettingsViewModel`) are registered as **singletons** and shared across pages.

### ViewModel Hierarchy

```
BasePropertyChangeModel  (INotifyPropertyChanged)
  └── BaseViewModel      (IsBusy, IsPageBusy, IsActive, IsFree)
        └── ViewModelUiBase   (CurrencySymbol, ScheduleSave/FlushPendingSave 600ms debounce, isUpdating guard)
              └── ExpenseEntryViewBaseModel  (shared add/edit form fields, HasInitialized)
                    ├── LoanViewModel
                    ├── ExpenseViewModel
                    └── IncomeViewModel
```

Key conventions:
- Property setters guard with `if (!HasInitialized) return` during load and `isUpdating` for reentrancy.
- `MarkInitializationComplete()` is called after the first data load; only then do setters fire saves and recalculations.
- `ScheduleSave(() => SharedServiceCore.SaveData(this))` in trigger methods debounces disk writes. Call `FlushPendingSave(...)` in `OnDisappearing` and after explicit add/delete.

### Data Persistence

`SharedServiceCore.SaveData<T>` is fire-and-forget (`Task.Run`, no `.Wait()`). Reads are async via `SharedServiceCore.LoadDataFile<T>()`. JSON serialisation uses `System.Text.Json` with a custom `DoubleDefaultConverter`.

Platform storage paths:
- iOS/MacCatalyst: `Environment.SpecialFolder.MyDocuments`
- Android: `Environment.SpecialFolder.LocalApplicationData`
- Windows: `%LOCALAPPDATA%/LoanCalculator/`

Named JSON files: `homeloandata.json` (Loan), `incomedata.json`, `expensedata.json`, `settingsdata.json`, `namevaluedata.json`, `themeselectdata.json`.

### Cross-Tab Data Coordination

`SharedServiceCore` holds dirty flags (`IsIncomeDirty`, `IsExpenseDirty`, `IsLoanDirty`). Each view sets its flag in `OnDisappearing`. `LoanView.OnAppearing` checks flags and calls `RefreshCrossTabSummaries()` (targeted reload from live singleton VMs) instead of a full reload. First-ever load is detected with a `_hasLoadedOnce` flag per view.

### Premium / Trial

`SharedServiceCore.TESTING_PREMIUM_OVERRIDE = true` currently bypasses all trial restrictions. **Comment this out before any App Store release.** Premium status is stored in `SecureStorage` under key `"IsPremium"`. `IsPremiumUser()` has both a sync overload (uses `.GetAwaiter().GetResult()` — safe only when the override is active) and `IsPremiumUserAsync()`.

### Theme System

Four themes: `Dark` (default), `Light`, `Forest`, `Warm`. Theme XAML files are **EmbeddedResources** in the main app assembly at `src/LoanCalculator/Extensions/Data/`. `ThemeHandler.LoadDefaultStyle()` loads `Theme.CommonStyles.xaml`, `Theme.CommonDataGridStyles.xaml`, and the selected theme file from embedded streams, clears all `LoanApp`-prefixed keys from `Application.Current.Resources`, and adds the new dictionaries. All views reference theme resources with `{DynamicResource LoanApp...}`.

The unit test `ThemeFiles_ShouldHave_EqualKeys` enforces that all four theme files define identical resource key sets — run it after adding any new theme key.

### Navigation & Shell

Four-tab Shell: Loan, Income, Expense, Settings. No push navigation. Startup: `App` sets `MainPage = SplashPage`; after the Lottie animation (or 3 s timeout) `SplashPage` swaps to `AppShell`.

### SfPopup ContentTemplate Binding

`SfPopup.ContentTemplate` DataTemplate does **not** inherit the page's `BindingContext` automatically. Always set it explicitly on the root element inside the template:

```xml
<sfPopup:SfPopup.ContentTemplate>
    <DataTemplate>
        <VerticalStackLayout
            BindingContext="{Binding Source={x:Reference thisPage}, Path=BindingContext}">
```

This requires `x:Name="thisPage"` on the `ContentPage`. Without this, `SfComboBox` and interactive controls inside the popup will not bind correctly.

`SfComboBox` inside `SfTextInputLayout` inside `SfPopup` does not render its selected value text — use `SfComboBox` directly (with a plain `Label` above it) instead. Also set `AutoSizeMode="Height"` on the `SfPopup` so content is not clipped.

### SfNumericEntry Quirk

`SfNumericEntry.ValueChanged` fires on Enter, spin button, or **focus change only** — not on every keystroke. Inside `SfPopup`, tapping Save before unfocusing the field means the new value is never written back. Use a plain `Entry` with `Keyboard="Numeric"` and bind to a string property (`IncomeEntryAmountText`) that parses to `double` on set. This is already in place for the add/edit forms.

---

## Key Files Quick Reference

| File | What it does |
|---|---|
| `src/LoanCalculator/MauiProgram.cs` | DI registration, Sentry, Syncfusion license |
| `src/LoanCalculator/App.xaml.cs` | Global exception handlers, purchase restore, splash handoff |
| `src/LoanCalculator.Core/Services/SharedServiceCore.cs` | Static helpers: SaveData, LoadDataFile, premium check, dirty flags, currency list |
| `src/LoanCalculator.Core/Models/ViewModels/ExpenseEntryViewBaseModel.cs` | Shared add/edit form state (entry fields, frequency, amount text, add/update logic) |
| `src/LoanCalculator.Core/Themes/ThemeHandler.cs` | Theme loading from embedded resources |
| `src/LoanCalculator/Extensions/Data/Theme.CommonStyles.xaml` | Shared styles + typography token system (`FontSizeSmall/Body/Medium/Large/Heading`, `MarginSection`) |
| `src/LoanCalculator/run-ios.sh` | iOS simulator build + launch script |
