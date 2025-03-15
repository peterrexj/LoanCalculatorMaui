using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.ViewModel;
using Pj.Library;

namespace LoanCalculatorMaui.Themes
{
    public class StyleProvider
    {
        public static async Task<AppThemes?> GetCurrentThemeAsync()
        {
            try
            {
                var data = await SharedServices.LoadDataFile<ThemeSelect>();
                return data == null || data.Theme == null ? null : LoanCalculator.Models.Enums.EnumHelper<AppThemes>.FromString(data.Theme.ToString());
            }
            catch (Exception e)
            {
                // ignored
            }

            return null;
        }

        public static void LoadDefaultStyle()
        {
            AppThemes? currentTheme = null;
            Task.Run(async () => currentTheme = await GetCurrentThemeAsync()).Wait();
            currentTheme ??= AppThemes.Light;
            LoadDefaultStyle(currentTheme.Value);
        }

        public static void LoadDefaultStyle(AppThemes appTheme)
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
                    case AppThemes.FireBreather:
                        themeFile = "Theme.FireBreather.xaml";
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

        private static ResourceDictionary? LoadResourceDictionary(string resourcePath)
        {
            try
            {
                var xaml = PjUtility.Runtime.GetAssembly("LoanCalculatorMaui").GetEmbeddedResourceAsText($"LoanCalculatorMaui.Extensions.Data.{resourcePath}");

                var resourceDictionary = new ResourceDictionary();
                resourceDictionary.LoadFromXaml(xaml);

                return resourceDictionary;
            }
            catch (Exception e)
            {
                return null;
            }

            #region Using resources

            //var assembly = Assembly.GetExecutingAssembly();
            //var resourceName = $"{assembly.GetName().Name}.{resourcePath.Replace("/", ".")}";
            //using var stream = assembly.GetManifestResourceStream(resourceName);
            //if (stream == null)
            //{
            //    throw new FileNotFoundException("Resource not found", resourceName);
            //}

            //using var reader = new StreamReader(stream);
            //var xaml = reader.ReadToEnd();

            #endregion
        }
    }
}
