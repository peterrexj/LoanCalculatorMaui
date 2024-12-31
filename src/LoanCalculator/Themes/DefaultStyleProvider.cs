using System.Reflection;
using LoanCalculator.Models.Enums;

namespace LoanCalculatorMaui.Themes
{
    public class DefaultStyleProvider
    {
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

            // Load the theme-specific styles
            var themeStyles = LoadResourceDictionary(themeFile);

            // Clear existing merged dictionaries
            Application.Current?.Resources.MergedDictionaries.Clear();

            // Add the common styles and theme-specific styles
            Application.Current?.Resources.MergedDictionaries.Add(commonStyles);
            Application.Current?.Resources.MergedDictionaries.Add(themeStyles);

            return resourceDictionary;
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

        private static double BoxCurrencyFontSize
        {
            get
            {
                if (Device.RuntimePlatform == Device.Android)
                {
                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        return 18;
                    }
                    else { return 24; }
                }
                if (Device.RuntimePlatform == Device.iOS)
                {
                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        return 18;
                    }
                    else { return 24; }
                }
                return 24;
            }
        }

        private static double BoxMainHighlightFontSize
        {
            get
            {
                if (Device.RuntimePlatform == Device.Android)
                {
                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        return 22;
                    }
                    else { return 30; }
                }
                if (Device.RuntimePlatform == Device.iOS)
                {
                    if (Device.Idiom == TargetIdiom.Phone)
                    {
                        return 22;
                    }
                    else { return 30; }
                }
                return 30;
            }
        }
    }
}
