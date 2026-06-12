using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Services;
using Pj.Library;
using System.Collections.ObjectModel;

namespace LoanCalculator.Core.Themes
{
    public class ThemeHandler : IThemeHandler
    {
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

        public void LoadDefaultStyle()
        {
            // Called from the App constructor on the main thread — must use Task.Run to
            // avoid a sync-context deadlock on Android/iOS when the file read completes.
            var currentTheme = Task.Run(() => GetCurrentThemeAsync()).GetAwaiter().GetResult()
                               ?? SharedServiceCore.DefaultAppTheme;
            LoadDefaultStyle(currentTheme);
        }

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
                    case AppThemes.Warm:
                        themeFile = "Theme.Warm.xaml";
                        break;
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

                #region Clear Dictionary
                // This does not work in the release mode as the resources are empty and fails from native code
                // Even though the null check is done, it still fails

                // Clear existing merged dictionaries
                //if (Application.Current?.Resources == null)
                //{
                //    Application.Current.Resources = new ResourceDictionary();
                //}

                //if (Application.Current?.Resources?.MergedDictionaries?.Any() == true)
                //{
                //    Application.Current.Resources.MergedDictionaries.Clear();
                //}
                //Application.Current?.Resources.MergedDictionaries.Clear();
                #endregion

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

        static void ClearAllResources(string prefix)
        {
            try
            {
                var resourceKeys = Application.Current?.Resources.Keys.Cast<string>().Where(k => k.StartsWith(prefix)).ToList() ?? new List<string>();
                var mergedDictionaryKeys = Application.Current?.Resources.MergedDictionaries.SelectMany(f => f.Keys.Cast<string>()).Where(k => k.StartsWith(prefix)).ToList() ?? new List<string>();

                var keys = resourceKeys.Union(mergedDictionaryKeys).ToList();

                foreach (var key in keys)
                {
                    if (!Application.Current?.Resources.ContainsKey(key) ?? true)
                    {
                        continue;
                    }
                    Application.Current.Resources[key] = null;
                    Application.Current.Resources.Remove(key);
                }
            }
            catch (Exception e)
            {
            }
        }

        static void UpdateResources(string prefix)
        {
            try
            {
                var resourceKeys = Application.Current?.Resources.Keys.Cast<string>().Where(k => k.StartsWith(prefix)).ToList() ?? new List<string>();
                var mergedDictionaryKeys = Application.Current?.Resources.MergedDictionaries.SelectMany(f => f.Keys.Cast<string>()).Where(k => k.StartsWith(prefix)).ToList() ?? new List<string>();

                var keys = resourceKeys.Union(mergedDictionaryKeys).ToList();

                foreach (var key in keys)
                {
                    var temp = Application.Current.Resources[key];
                    Application.Current.Resources[key] = null;
                    Application.Current.Resources[key] = temp;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private ResourceDictionary? LoadResourceDictionary(string resourcePath)
        {
            try
            {
                // Use the entry assembly (the MAUI app) which contains the embedded theme resources.
                // This is reliable on both iOS (AOT) and Android — unlike PjUtility.Runtime.GetAssembly()
                // which uses stack-walking that fails silently when frame metadata is stripped on iOS.
                var assembly = System.Reflection.Assembly.GetEntryAssembly()
                    ?? AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == "LoanCalculatorMaui");

                if (assembly == null) return null;

                using var stream = assembly.GetManifestResourceStream($"LoanCalculatorMaui.Extensions.Data.{resourcePath}");
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ThemeHandler] Resource not found: LoanCalculatorMaui.Extensions.Data.{resourcePath}");
                    return null;
                }
                using var reader = new System.IO.StreamReader(stream);
                var xaml = reader.ReadToEnd();

                var resourceDictionary = new ResourceDictionary();
                resourceDictionary.LoadFromXaml(xaml);

                return resourceDictionary;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeHandler] Failed to load '{resourcePath}': {e.Message}");
                return null;
            }
        }

        public ObservableCollection<Brush> GetChartColors()
        {
            var appResources = Application.Current?.Resources;
            if (appResources != null)
            {
                return
                [
                    new SolidColorBrush((Color)appResources["LoanAppChartColor1"]),
                    new SolidColorBrush((Color)appResources["LoanAppChartColor2"]),
                    new SolidColorBrush((Color)appResources["LoanAppChartColor3"])
                ];
            }
            else
            {
                return
                [
                    new SolidColorBrush(Color.FromArgb("#d7bde2")),
                    new SolidColorBrush(Color.FromArgb("#d6eaf8")),
                    new SolidColorBrush(Color.FromArgb("#fdebd0"))
                ];
            }
        }
    }
}
