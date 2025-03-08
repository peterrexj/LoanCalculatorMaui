using System.Reflection;
using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.ViewModel;

namespace LoanCalculatorMaui.Themes
{
    public class StyleProvider
    {
        public static async Task<AppThemes?> GetCurrentThemeAsync()
        {
            try
            {
                var data = await SharedServices.LoadDataFile<ThemeSelect>();
                return data == null || data.Theme == null ? null : EnumHelper<AppThemes>.FromString(data.Theme.ToString());
            }
            catch (Exception e)
            {
                // ignored
            }

            return null;
        }

        public static ResourceDictionary LoadDefaultStyle()
        {
            AppThemes? currentTheme = null;
            Task.Run(async () => currentTheme = await GetCurrentThemeAsync()).Wait();
            currentTheme ??= AppThemes.Light;
            return LoadDefaultStyle(currentTheme.Value);
        }

        public static ResourceDictionary LoadDefaultStyle(AppThemes appTheme)
        {
            var resourceDictionary = new ResourceDictionary();
            string themeFile;

            switch (appTheme)
            {
                case AppThemes.Dark:
                    themeFile = "Themes/DarkTheme.xaml";
                    break;
                case AppThemes.Light:
                    themeFile = "Themes/LightTheme.xaml";
                    break;
                case AppThemes.FireBreather:
                    themeFile = "Themes/FireBreatherTheme.xaml";
                    break;
                default:
                    throw new ArgumentException("Unsupported theme");
            }

            // Load the common styles
            var commonStyles = LoadResourceDictionary("Themes/CommonStyles.xaml");
            var commonDataGridStyles = LoadResourceDictionary("Themes/CommonDataGridStyles.xaml");

            // Load the theme-specific styles
            var themeStyles = LoadResourceDictionary(themeFile);

            // Clear existing merged dictionaries
            Application.Current?.Resources.MergedDictionaries.Clear();

            ClearAllResources("LoanApp");

            // Add the common styles and theme-specific styles to the application's merged dictionaries
            Application.Current?.Resources.MergedDictionaries.Add(commonStyles);
            Application.Current?.Resources.MergedDictionaries.Add(commonDataGridStyles);
            Application.Current?.Resources.MergedDictionaries.Add(themeStyles);

            UpdateResources("LoanApp");

            return resourceDictionary;
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

        private static ResourceDictionary LoadResourceDictionary(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.{resourcePath.Replace("/", ".")}";
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
    }
}
