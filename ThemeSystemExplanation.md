# Theme System in LoanCalculatorMaui

This document provides a detailed explanation of the theme system implemented in the LoanCalculatorMaui application. It covers how themes are defined, loaded, managed, and applied throughout the application.

## Table of Contents

1. [Theme Architecture Overview](#theme-architecture-overview)
2. [Theme Definition](#theme-definition)
3. [Theme Files Structure](#theme-files-structure)
4. [Theme Loading and Application](#theme-loading-and-application)
5. [Theme Persistence](#theme-persistence)
6. [Theme Switching](#theme-switching)
7. [Resource Usage in XAML](#resource-usage-in-xaml)
8. [Embedded Resources](#embedded-resources)
9. [Implementation Best Practices](#implementation-best-practices)
10. [Implementing a Similar Theme System](#implementing-a-similar-theme-system)

## Theme Architecture Overview

The theme system in LoanCalculatorMaui is built around a combination of:

1. **Theme Enumeration**: Defined in `AppThemes.cs`
2. **Theme Handler Interface**: Defined in `IThemeHandler.cs`
3. **Theme Handler Implementation**: Implemented in `ThemeHandler.cs`
4. **Theme XAML Files**: Located in `src/LoanCalculator/Extensions/Data/`
5. **Theme Selection Model**: Implemented in `ThemeSelect.cs`
6. **Theme Selection UI**: Implemented in `SettingsViewModel.cs`
7. **Base Application Styles**: Defined in `Colors.xaml` and `Styles.xaml`

This architecture allows for a flexible, maintainable, and extensible theming system that can be easily modified or expanded. The theme system is designed to work alongside the base MAUI styles while providing app-specific theming capabilities.

## Theme Definition

Themes are defined as an enumeration in `AppThemes.cs`:

```csharp
public enum AppThemes
{
    Dark,
    Light,
    Forest,
    //Warm
}
```

Each enum value corresponds to a specific theme that can be applied to the application. The `Warm` theme is commented out, indicating it might be under development or temporarily disabled.

## Theme Files Structure

The theme system uses several XAML files to define the visual appearance:

### Common Style Files

1. **Theme.CommonStyles.xaml**: Contains styles that are shared across all themes, including:
   - Font families and global styles for common controls
   - Border styles
   - Expander styles
   - Tab view styles
   - Input text styles
   - Segment styles
   - Button styles
   - Chart styles
   - AutoComplete and ComboBox styles

2. **Theme.CommonDataGridStyles.xaml**: Contains styles specific to data grids that are shared across all themes:
   - Header cell styles
   - Data cell styles with different color variations
   - Border styles for data grid cells

### Theme-Specific Files

Each theme has its own XAML file that defines colors and resources specific to that theme:

1. **Theme.Dark.xaml**: Dark theme with predominantly dark backgrounds and light text
2. **Theme.Light.xaml**: Light theme with light backgrounds and dark text
3. **Theme.Forest.xaml**: Forest theme with green-tinted colors
4. **Theme.Warm.xaml**: (Commented out in the enum but file exists) Warm theme with warm colors

Each theme file defines a comprehensive set of colors and resources, including:

- Base colors (background, foreground)
- Tab bar colors
- Input field colors
- Segment control colors
- Slider colors
- Data grid colors
- Navigation button colors
- ComboBox colors
- Switch colors
- AutoComplete colors
- AppShell colors
- Notification colors
- List colors
- Button colors
- Chart colors
- Border colors
- Expander colors
- Checkbox colors
- Disclaimer colors
- Progress indicator colors
- Purchase UI colors

Additionally, each theme file defines various brushes (LinearGradientBrush, RadialGradientBrush) that use the theme colors to create gradients for different UI elements.

## Theme Loading and Application

### Theme Handler Interface

The theme system loads and applies themes through the `ThemeHandler` class, which implements the `IThemeHandler` interface:

```csharp
public interface IThemeHandler
{
    Task<AppThemes?> GetCurrentThemeAsync();
    void LoadDefaultStyle();
    void LoadDefaultStyle(AppThemes appTheme);
    ObservableCollection<Brush> GetChartColors();
}
```

The `ThemeHandler` class handles:

1. **Theme Retrieval**: Getting the current theme from storage
2. **Theme Loading**: Loading the appropriate theme files
3. **Theme Application**: Applying the theme to the application

### Theme Loading Process

The key method for loading themes is `LoadDefaultStyle(AppThemes appTheme)`, which:

1. Determines the correct theme file based on the provided `AppThemes` enum value
2. Loads the common styles (Theme.CommonStyles.xaml and Theme.CommonDataGridStyles.xaml)
3. Loads the theme-specific styles (e.g., Theme.Dark.xaml)
4. Clears existing resources with the "LoanApp" prefix
5. Adds the loaded resource dictionaries to the application's merged dictionaries
6. Updates resources with the "LoanApp" prefix

Here's the implementation of this method:

```csharp
public void LoadDefaultStyle(AppThemes appTheme)
{
    try
    {
        string themeFile;

        switch (appTheme)
        {
            case AppThemes.Dark:
                themeFile = "Theme.Dark.xaml";
                break;
            case AppThemes.Light:
                themeFile = "Theme.Light.xaml";
                break;
            case AppThemes.Forest:
                themeFile = "Theme.Forest.xaml";
                break;
            //case AppThemes.Warm:
            //    themeFile = "Theme.Warm.xaml";
            //    break;
            default:
                throw new ArgumentException("Unsupported theme");
        }

        // Load the common styles
        var commonStyles = LoadResourceDictionary("Theme.CommonStyles.xaml");
        var commonDataGridStyles = LoadResourceDictionary("Theme.CommonDataGridStyles.xaml");

        // Load the theme-specific styles
        var themeStyles = LoadResourceDictionary(themeFile);

        if (commonDataGridStyles == null || commonStyles == null || themeStyles == null)
        {
            return;
        }

        ClearAllResources("LoanApp");

        // Add the common styles and theme-specific styles to the application's merged dictionaries
        Application.Current?.Resources.MergedDictionaries.Add(commonStyles);
        Application.Current?.Resources.MergedDictionaries.Add(commonDataGridStyles);
        Application.Current?.Resources.MergedDictionaries.Add(themeStyles);

        UpdateResources("LoanApp");
    }
    catch (Exception e)
    {
        throw new Exception($"Exception thrown from the style provider {e}");
    }
}
```

### Theme Initialization

The theme is initialized when the application starts in `App.xaml.cs`:

```csharp
public App(IServiceProvider serviceProvider)
{
    InitializeComponent();

    ServiceLocator.ServiceProvider = serviceProvider;

    AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
    TaskScheduler.UnobservedTaskException += HandleTaskSchedulerException;

    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("...");

    ServiceLocator.GetService<IThemeHandler>().LoadDefaultStyle();

    // Other initialization code...
}
```

The `LoadDefaultStyle()` method without parameters calls the overloaded version with the current or default theme:

```csharp
public void LoadDefaultStyle()
{
    AppThemes? currentTheme = null;
    Task.Run(async () => currentTheme = await GetCurrentThemeAsync()).Wait();
    currentTheme ??= SharedServiceCore.DefaultAppTheme;
    LoadDefaultStyle(currentTheme.Value);
}
```

This ensures that the application always starts with the correct theme, either the previously selected theme or the default theme if none has been selected.

## Theme Persistence

### Theme Selection Model

Themes are persisted using the `ThemeSelect` class and the application's local storage system:

```csharp
public class ThemeSelect
{
    private AppThemes? _theme;
    public AppThemes? Theme { get; set; }
}
```

This simple class stores the selected theme as an `AppThemes` enum value.

### Saving Theme Selection

The `SettingsViewModel` handles saving the selected theme:

```csharp
private async Task SaveAndApplyApplicationThemeAsync(AppThemes theme)
{
    await SharedServiceCore.SaveData(new ThemeSelect { Theme = theme });
    await ApplyApplicationThemeAsync(theme);
}
```

The `SharedServiceCore.SaveData` method saves the `ThemeSelect` object to local storage, allowing the theme selection to persist across application restarts.

### Loading Saved Theme

When the application starts or when the theme needs to be retrieved, the `ThemeHandler.GetCurrentThemeAsync()` method is called:

```csharp
public async Task<AppThemes?> GetCurrentThemeAsync()
{
    try
    {
        var data = await SharedServiceCore.LoadDataFile<ThemeSelect>();
        return data?.Theme == null ? null : LoanCalculator.Core.Models.Enums.EnumHelper<AppThemes>.FromString(data.Theme.ToString());
    }
    catch (Exception e)
    {
        // ignored
    }

    return null;
}
```

This method:
1. Loads the `ThemeSelect` object from local storage
2. Returns the stored theme or `null` if no theme is stored or an error occurs

### Default Theme

If no theme is stored or an error occurs when loading the theme, the application falls back to the default theme defined in `SharedServiceCore`:

```csharp
public const AppThemes DefaultAppTheme = AppThemes.Dark;
```

This ensures that the application always has a valid theme, even if the saved theme cannot be loaded.

## Theme Switching

Theme switching is handled by the `SettingsViewModel` class, which provides a UI for selecting themes:

```csharp
public string? SelectedTheme
{
    get
    {
        if (_selectedTheme == null)
        {
            _themeHandler.GetCurrentThemeAsync().ContinueWith(task =>
            {
                _selectedTheme = task.Result != null ?
                    Themes.FirstOrDefault(t => t == task.Result.ToString()) :
                    Themes.FirstOrDefault(t => t == SharedServiceCore.DefaultAppTheme.ToString());
            });
        }
        return _selectedTheme;
    }
    set
    {
        if (value == null) return;
        if (isUpdating) return;
        if (_selectedTheme == value) return;

        _selectedTheme = value;

        // Await the theme change operation
        MainThread.BeginInvokeOnMainThread(async void () =>
        {
            IsBusy = true; // Show spinner
            IsUpdating = true;

            await Task.Delay(500);
            await ChangeThemeAsync(_selectedTheme);
        });
    }
}
```

### Theme Switching Process

When a user selects a new theme:

1. The `SelectedTheme` property setter is triggered
2. The `ChangeThemeAsync` method is called on the main thread
3. The theme is converted from a string to an `AppThemes` enum value
4. The theme is saved and applied using `SaveAndApplyApplicationThemeAsync`
5. The UI is updated to reflect the new theme

The actual theme change process involves several steps:

```csharp
private async Task ChangeThemeAsync(string selectedTheme)
{
    try
    {
        var appTheme = EnumHelper<AppThemes>.FromString(selectedTheme);
        await SaveAndApplyApplicationThemeAsync(appTheme);

        OnPropertyChanged(nameof(SelectedTheme)); // Notify UI of the change
    }
    catch (Exception ex)
    {
        _errorHandlingService.HandleException(ex); // Handle any errors
    }
    finally
    {
        IsUpdating = false;
        IsBusy = false; // Hide spinner
    }
}

private async Task SaveAndApplyApplicationThemeAsync(AppThemes theme)
{
    await SharedServiceCore.SaveData(new ThemeSelect { Theme = theme });
    await ApplyApplicationThemeAsync(theme);
}

private async Task ApplyApplicationThemeAsync(AppThemes theme)
{
    await MainThread.InvokeOnMainThreadAsync(() => _themeHandler.LoadDefaultStyle(theme));
}
```

### UI Update Mechanism

When a theme is changed, the UI updates automatically due to the use of `DynamicResource` bindings throughout the application. The process works as follows:

1. The `ThemeHandler.LoadDefaultStyle(AppThemes appTheme)` method is called
2. It loads the appropriate theme resources and adds them to the application's merged dictionaries
3. The MAUI framework automatically updates all UI elements that use `DynamicResource` bindings
4. Elements using `StaticResource` bindings are not updated (which is why `DynamicResource` is used for theme-related resources)

The application shows a spinner during the theme change process to indicate to the user that the operation is in progress. After a short delay (500ms), the theme is applied, and the spinner is hidden.

## Resource Usage in XAML

The theme resources are used throughout the application's XAML files using the `DynamicResource` markup extension:

```xml
<Label TextColor="{DynamicResource LoanAppTabTextColorSelected}" />
```

Using `DynamicResource` instead of `StaticResource` ensures that the UI updates automatically when the theme changes.

### Examples of Resource Usage

Here are some examples of how theme resources are used in the application:

#### Border Styles

```xml
<Style x:Key="BorderTopHighlightBox" TargetType="Border">
    <Setter Property="StrokeThickness" Value="2" />
    <Setter Property="Stroke" Value="{DynamicResource BorderTopGradientBrush}" />
    <Setter Property="Background" Value="{DynamicResource BorderTopBackgroundGradientBrush}" />
    <Setter Property="HorizontalOptions" Value="FillAndExpand" />
    <Setter Property="StrokeShape" Value="RoundRectangle 12,12,12,12" />
    <Setter Property="VerticalOptions" Value="FillAndExpand" />
</Style>
```

#### Tab View Styles

```xml
<Style TargetType="tabView:SfTabView">
    <Setter Property="HeaderHorizontalTextAlignment" Value="Center" />
    <Setter Property="IndicatorCornerRadius" Value="1" />
    <Setter Property="IndicatorStrokeThickness" Value="5" />
    <Setter Property="IndicatorPlacement" Value="Bottom" />
    <Setter Property="IndicatorWidthMode" Value="Stretch" />
    <Setter Property="IndicatorBackground" Value="{DynamicResource LoanAppTabIndicatorStrokeColor}" />
    <Setter Property="TabBarBackground" Value="{DynamicResource TabBarBackgroundGradientBrush}" />
</Style>
```

#### Chart Styles

```xml
<Style x:Key="InsightsChartDataLabelStyle" TargetType="chart:ChartDataLabelStyle">
    <Setter Property="FontAttributes" Value="Bold" />
    <Setter Property="FontFamily" Value="{DynamicResource DefaultFontFamily}" />
    <Setter Property="FontSize" Value="{OnIdiom Phone=10, Tablet=15, Desktop=15, TV=15, Watch=8, Default=10}" />
    <Setter Property="LabelFormat" Value="c" />
    <Setter Property="TextColor" Value="{DynamicResource LoanAppChartDataMarkerTextColor}" />
</Style>
```

### Resource Naming Convention

The application follows a consistent naming convention for theme resources:

1. **Color Resources**: Named with a descriptive prefix and purpose
   - Example: `LoanAppTabTextColorSelected`, `LoanAppButtonBgColor`

2. **Brush Resources**: Named to indicate the type of brush and purpose
   - Example: `BorderTopGradientBrush`, `TabBarBackgroundGradientBrush`

3. **Style Resources**: Named to indicate the control type and purpose
   - Example: `BorderTopHighlightBox`, `ExpanderHeaderLabelStyle`

This naming convention makes it easy to understand the purpose of each resource and maintain consistency across the application.

## Embedded Resources

The theme XAML files are embedded resources in the application. This means they are compiled into the application assembly rather than being separate files that need to be deployed alongside the application.

### How Resources are Embedded

In the project file (`.csproj`), the theme XAML files are marked with the `EmbeddedResource` build action. This tells the compiler to include these files in the compiled assembly.

### Loading Embedded Resources

The `ThemeHandler` loads these resources using the `PjUtility` helper class:

```csharp
var xaml = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui")
    .GetEmbeddedResourceAsText($"LoanCalculatorMaui.Extensions.Data.{resourcePath}");

var resourceDictionary = new ResourceDictionary();
resourceDictionary.LoadFromXaml(xaml);
```

The process works as follows:

1. `GetAssembly("LoanCalculatorMaui")` retrieves the assembly containing the embedded resources
2. `GetEmbeddedResourceAsText()` extracts the XAML content as text using the fully qualified resource name
3. `LoadFromXaml()` parses the XAML text and creates a `ResourceDictionary` object

This approach allows the theme files to be bundled with the application and loaded at runtime without requiring external files.

### Resource Naming Convention

Embedded resources follow a specific naming convention:

- The fully qualified name includes the assembly name, followed by the folder path with dots instead of slashes
- For example, a file at `Extensions/Data/Theme.Dark.xaml` in the `LoanCalculatorMaui` assembly is accessed as `LoanCalculatorMaui.Extensions.Data.Theme.Dark.xaml`

### Other Embedded Resources

The application also uses embedded resources for other content, such as disclaimer data:

```csharp
var disclaimerData = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui")
    .GetEmbeddedResourceAsText(
        "LoanCalculatorMaui.Extensions.DisclaimerData.AppLaunchDisclaimerData.html")
    .Replace("{{AppName}}", AppInformation?.ApplicationTitle ?? "Loan Affordability Calculator");
```

This allows HTML content to be embedded in the application and dynamically modified at runtime.

## Implementation Best Practices

The theme system in LoanCalculatorMaui follows several best practices:

1. **Separation of Common and Theme-Specific Styles**: Common styles are defined once and shared across themes, while theme-specific styles are defined in separate files.

2. **Dynamic Resource Usage**: Using `DynamicResource` ensures that the UI updates automatically when the theme changes.

3. **Theme Persistence**: Saving the selected theme allows the user's preference to persist across application restarts.

4. **Asynchronous Theme Loading**: Theme loading is performed asynchronously to avoid blocking the UI thread.

5. **Resource Naming Convention**: All theme-specific resources follow a naming convention (prefixed with "LoanApp") to make them easily identifiable and manageable.

6. **Gradient Brushes**: Using gradient brushes creates a more visually appealing UI than flat colors.

7. **Device Adaptation**: Many styles include device-specific adaptations using the `OnIdiom` markup extension to ensure the UI looks good on different device types and screen sizes.

8. **Default Theme**: A default theme (Dark) is defined in `SharedServiceCore.DefaultAppTheme` to ensure the application always has a theme even if none is selected.

9. **Theme Initialization at Startup**: The theme is loaded at application startup in `App.xaml.cs` to ensure the correct theme is applied from the beginning.

10. **Resource Cleanup**: When changing themes, existing resources are cleared to prevent resource conflicts or leaks.

By following these best practices, the theme system in LoanCalculatorMaui provides a robust, flexible, and maintainable way to customize the application's appearance.

## Relationship with Base MAUI Styles

The theme system in LoanCalculatorMaui works alongside the base MAUI styles defined in `Colors.xaml` and `Styles.xaml`. Here's how they interact:

### Base MAUI Styles

The application starts with standard MAUI styles defined in:

1. **Colors.xaml**: Defines base colors like `Primary`, `Secondary`, and various shades of gray
2. **Styles.xaml**: Defines default styles for MAUI controls using `AppThemeBinding` for light/dark mode support

These styles are loaded in `App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
            <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
        </ResourceDictionary.MergedDictionaries>
        <localConverters:StringToColorConverter x:Key="StringToBrushConverter" />
    </ResourceDictionary>
</Application.Resources>
```

### Theme System Override

When the theme system loads a theme, it adds three resource dictionaries to the application's merged dictionaries:

1. Common styles (Theme.CommonStyles.xaml)
2. Common data grid styles (Theme.CommonDataGridStyles.xaml)
3. Theme-specific styles (e.g., Theme.Dark.xaml)

These dictionaries contain resources with keys prefixed with "LoanApp" (e.g., `LoanAppDefaultForegroundColor`), which are different from the keys used in the base MAUI styles.

This approach allows the application to:

1. Use the base MAUI styles for standard controls and behaviors
2. Use the custom theme system for app-specific styling and branding
3. Switch between custom themes without affecting the underlying MAUI styles

The theme system effectively creates a layer of app-specific styling on top of the base MAUI styling system.

## Implementing a Similar Theme System

To implement a similar theme system in another .NET MAUI project, follow these steps:

### 1. Define Theme Enumeration

Create an enumeration to define the available themes:

```csharp
public enum AppThemes
{
    Dark,
    Light,
    Custom1,
    Custom2
}
```

### 2. Create Theme Handler Interface and Implementation

Define an interface for the theme handler:

```csharp
public interface IThemeHandler
{
    Task<AppThemes?> GetCurrentThemeAsync();
    void LoadDefaultStyle();
    void LoadDefaultStyle(AppThemes appTheme);
}
```

Implement the theme handler:

```csharp
public class ThemeHandler : IThemeHandler
{
    public async Task<AppThemes?> GetCurrentThemeAsync()
    {
        try
        {
            // Load theme selection from storage
            var data = await YourStorageService.LoadDataAsync<ThemeSelect>();
            return data?.Theme;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void LoadDefaultStyle()
    {
        AppThemes? currentTheme = null;
        Task.Run(async () => currentTheme = await GetCurrentThemeAsync()).Wait();
        currentTheme ??= YourDefaultTheme;
        LoadDefaultStyle(currentTheme.Value);
    }

    public void LoadDefaultStyle(AppThemes appTheme)
    {
        try
        {
            string themeFile;

            // Map enum values to file names
            switch (appTheme)
            {
                case AppThemes.Dark:
                    themeFile = "Theme.Dark.xaml";
                    break;
                case AppThemes.Light:
                    themeFile = "Theme.Light.xaml";
                    break;
                // Add cases for your custom themes
                default:
                    throw new ArgumentException("Unsupported theme");
            }

            // Load resource dictionaries
            var commonStyles = LoadResourceDictionary("Theme.CommonStyles.xaml");
            var themeStyles = LoadResourceDictionary(themeFile);

            // Clear existing resources
            ClearThemeResources();

            // Add new resources
            Application.Current?.Resources.MergedDictionaries.Add(commonStyles);
            Application.Current?.Resources.MergedDictionaries.Add(themeStyles);
        }
        catch (Exception e)
        {
            // Handle exceptions
        }
    }

    private ResourceDictionary LoadResourceDictionary(string resourcePath)
    {
        // Load embedded resource
        var assembly = GetType().Assembly;
        var resourceName = $"YourNamespace.Themes.{resourcePath}";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException("Resource not found", resourceName);
        }

        using var reader = new StreamReader(stream);
        var xaml = reader.ReadToEnd();

        var resourceDictionary = new ResourceDictionary();
        resourceDictionary.LoadFromXaml(xaml);

        return resourceDictionary;
    }

    private void ClearThemeResources()
    {
        // Clear existing theme resources
        // Implementation depends on your naming convention
    }
}
```

### 3. Create Theme XAML Files

Create XAML files for your themes:

1. **Theme.CommonStyles.xaml**: Common styles shared across all themes
2. **Theme.Dark.xaml**, **Theme.Light.xaml**, etc.: Theme-specific styles

Make sure to set the build action to "EmbeddedResource" for these files.

### 4. Create Theme Selection Model

Create a class to store the selected theme:

```csharp
public class ThemeSelect
{
    public AppThemes? Theme { get; set; }
}
```

### 5. Implement Theme Switching in ViewModel

Create a view model to handle theme switching:

```csharp
public class SettingsViewModel : BaseViewModel
{
    private readonly IThemeHandler _themeHandler;
    private string? _selectedTheme;
    
    public ObservableCollection<string> Themes { get; }

    public string? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value) return;
            _selectedTheme = value;
            
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                IsBusy = true;
                await ChangeThemeAsync(_selectedTheme);
                IsBusy = false;
            });
            
            OnPropertyChanged();
        }
    }

    public SettingsViewModel(IThemeHandler themeHandler)
    {
        _themeHandler = themeHandler;
        Themes = new ObservableCollection<string>(Enum.GetNames(typeof(AppThemes)));
        InitializeSelectedTheme();
    }

    private void InitializeSelectedTheme()
    {
        var currentTheme = Task.Run(() => _themeHandler.GetCurrentThemeAsync()).Result;
        _selectedTheme = currentTheme != null
            ? currentTheme.ToString()
            : YourDefaultTheme.ToString();
    }

    private async Task ChangeThemeAsync(string selectedTheme)
    {
        try
        {
            var appTheme = Enum.Parse<AppThemes>(selectedTheme);
            await SaveThemeAsync(appTheme);
            await ApplyThemeAsync(appTheme);
        }
        catch (Exception ex)
        {
            // Handle exceptions
        }
    }

    private async Task SaveThemeAsync(AppThemes theme)
    {
        await YourStorageService.SaveDataAsync(new ThemeSelect { Theme = theme });
    }

    private async Task ApplyThemeAsync(AppThemes theme)
    {
        await MainThread.InvokeOnMainThreadAsync(() => _themeHandler.LoadDefaultStyle(theme));
    }
}
```

### 6. Register Services in DI Container

Register the theme handler in your dependency injection container:

```csharp
// In MauiProgram.cs
builder.Services.AddSingleton<IThemeHandler, ThemeHandler>();
```

### 7. Initialize Theme at Startup

Initialize the theme when the application starts:

```csharp
// In App.xaml.cs
public App(IServiceProvider serviceProvider)
{
    InitializeComponent();
    
    // Other initialization code
    
    serviceProvider.GetService<IThemeHandler>().LoadDefaultStyle();
    
    // More initialization code
}
```

### 8. Use Dynamic Resources in XAML

Use dynamic resources in your XAML files:

```xml
<Label TextColor="{DynamicResource YourAppTextColor}" />
<Button BackgroundColor="{DynamicResource YourAppButtonColor}" />
```

### 9. Create a Theme Selection UI

Create a UI for selecting themes:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="YourNamespace.SettingsPage">
    <StackLayout>
        <Label Text="Select Theme" />
        <Picker ItemsSource="{Binding Themes}"
                SelectedItem="{Binding SelectedTheme}" />
    </StackLayout>
</ContentPage>
```

By following these steps, you can implement a similar theme system in your own .NET MAUI project. Adjust the implementation details as needed to fit your project's structure and requirements.