using System.Reflection;
using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.ViewModel;
using Microsoft.Maui.Controls.Xaml;


namespace LoanCalculatorMaui.Extensions
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

            // Load the ResourceDictionary from the specified URI
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.{themeFile.Replace("/", ".")}";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Resource not found", resourceName);
                }

                using (var reader = new StreamReader(stream))
                {
                    var xaml = reader.ReadToEnd();
                    var themeDictionary = new ResourceDictionary();
                    themeDictionary.LoadFromXaml(xaml);

                    // Clear existing merged dictionaries
                    Application.Current.Resources.MergedDictionaries.Clear();

                    // Add the new theme dictionary
                    Application.Current.Resources.MergedDictionaries.Add(themeDictionary);
                }
            }

            return resourceDictionary;
        }





        public static StyleModelDefault LoadDefaultStyle1(AppThemes appTheme)
        {
            switch (appTheme)
            {
                case AppThemes.Dark:
                    return new StyleModelDefault
                    {
                        //Background
                        DefaultForegroundColor = "White",
                        DefaultTabStrokeColor = "#85929E",
                        DefaultTabHeadTextColor = "#EAEDED",

                        //Expander
                        ExpanderHeaderIconExpandColor = "#BB8FCE",
                        ExpanderHeaderIconCollapseColor = "#2C3E50",
                        ExpanderHeaderTextColor = "#212F3C",

                        //Box
                        BoxBorderColor = "#F5EEF8",

                        //Top Highlight boxes for summary
                        HighlightBoxBorderColor = "#D2B4DE",
                        HighlightFgColor = "#1B2631",

                        //Chart
                        ChartBorderColor = "#616A6B",
                        ChartAxisColor = "Red",
                        ChartLegendColor = "Green",
                        ChartTitleColor = "Pink",
                        ChartDataMarkerColor = "Brown",
                        ChartColor1 = "#3498DB",
                        ChartColor2 = "#48C9B0",
                        ChartColor3 = "#884EA0",

                        //InputText
                        InputTextFgColor = "#BFC9CA",
                        InputTextWhenFocusedColor = "#A9DFBF",
                        InputTextWhenUnFocusedColor = "#808B96",
                        InputErrorTextFgColor = "#F5B7B1",

                        //Segments
                        SegBgColor = "#424949",
                        SegBorderColor = "#34495E",
                        SegTextFgColor = "#808B96",
                        SegSelectedTextFgColor = "#212F3C",
                        SegSelectedBgColor = "#D2B4DE",

                        //Ranger Slider
                        RangeSliderKnobColor = "#34495E",
                        RangeSliderTrackColor = "#EC7063",
                        RangeSliderTrackSelectionColor = "#4A235A",
                        RangeSliderLabelTextColor = "White", //This can be same as the foreground color

                        //DataGrid
                        DataGridHeaderBackgroundColor = "#2E4053",
                        DataGridHeaderForegroundColor = "#EAECEE",
                        DataGridGridCellBorderColor = "#7F8C8D",
                        DataGridRowBackgroundColor = "#ABB2B9",
                        DataGridRowForegroundColor = "#212F3D",

                        //Combobox
                        ComboDropDownBackgroundColor = "#34495E",
                        ComboDropDownTextColor = "#BFC9CA", //InputTextFgColor
                        ComboHighlightedTextColor = "#212F3D",
                        ComboSelectedDropDownItemColor = "#E5E7E9",
                        ComboTextColor = "#BFC9CA", //InputTextFgColor

                        //List
                        LstForegroundColor = "#CCD1D1",

                        //Buttons
                        ButtonFgColor = "Black",

                        //Fonts
                        //DefaultFontFamily = "Scratch"
                        DefaultFontFamily = "Calibri",
                        BoxItemCurrencyFontSize = BoxCurrencyFontSize,
                        BoxItemHighlightNumberFontSize = BoxMainHighlightFontSize,

                        //SfSwitch
                        SwitchBusyIndicatorColorON = "Transparent",
                        SwitchThumbBorderColorON = "Transparent",
                        SwitchThumbColorON = "#D7BDE2", //  -- 884EA0
                        SwitchTrackBorderColorON = "Transparent",
                        SwitchTrackColorON = "#884EA0",  //  -- EBDEF0

                        SwitchBusyIndicatorColorOFF = "Transparent",
                        SwitchThumbBorderColorOFF = "Transparent",
                        SwitchThumbColorOFF = "#884EA0",
                        SwitchTrackBorderColorOFF = "Transparent",
                        SwitchTrackColorOFF = "#EBDEF0",

                        //Notification
                        NotificationBgColor = "#EBDEF0",

                        //AutoComplete
                        AutoCompleteBackgroundColor = "#424949",
                        AutoCompleteClearButtonColor = "#BFC9CA",
                        AutoCompleteDropdownBackgroundColor = "#424949",
                        AutoCompleteDropdownBorderColor = "#424949",
                        AutoCompleteDropdownTextColor = "#BFC9CA",
                        AutoCompleteHighlightedTextColor = "#148F77",
                        AutoCompleteNoResultsFoundTextColor = "#BFC9CA",
                        AutoCompleteTextColor = "#BFC9CA",
                        AutoCompleteWaterColor = "#707B7C",

                        //App Theme Base
                        AppShellBgColor = "#1B2631",
                        AppShellFgColor = "#B3B6B7",
                        AppShellTitleColor = "",
                        AppShellDisabledColor = "",
                        AppShellUnselectedColor = "",
                        AppShellTabBarBackgroundColor = "#1B2631",
                        AppShellTabBarForegroundColor = "#B3B6B7",
                        AppShellTabBarUnselectedColor = "",
                        AppShellTabBarDisabledColor = "",
                        AppShellTabBarTitleColor = "",
                    };
                case AppThemes.Light:
                    return new StyleModelDefault
                    {
                        DefaultForegroundColor = "Black",

                        //Tab
                        DefaultTabStrokeColor = "#1C2833",
                        DefaultTabHeadTextColor = "#2E4053",

                        //Expander
                        ExpanderHeaderIconExpandColor = "#EBF5FB",
                        ExpanderHeaderIconCollapseColor = "#D5D8DC",
                        ExpanderHeaderTextColor = "#212F3C",

                        //Box
                        BoxBorderColor = "#F5EEF8",

                        //Top Highlight boxes for summary
                        HighlightBoxBorderColor = "#D6EAF8",
                        HighlightFgColor = "White",

                        //Chart
                        ChartBorderColor = "#616A6B",
                        ChartAxisColor = "#2E4053",
                        ChartLegendColor = "#2E4053",
                        ChartTitleColor = "#1B2631",
                        ChartDataMarkerColor = "#2E4053",
                        ChartColor1 = "#F5B041",
                        ChartColor2 = "#73C6B6",
                        ChartColor3 = "#BB8FCE",

                        //InputText
                        InputTextFgColor = "#17202A",
                        InputTextWhenFocusedColor = "#566573",
                        InputTextWhenUnFocusedColor = "#17202A",
                        InputErrorTextFgColor = "#CA6F1E",

                        //Segments
                        SegBgColor = "#F7F9F9",
                        SegBorderColor = "#34495E",
                        SegTextFgColor = "#808B96",
                        SegSelectedTextFgColor = "#212F3C",
                        SegSelectedBgColor = "#AED6F1",

                        //Ranger Slider
                        RangeSliderKnobColor = "#154360",
                        RangeSliderTrackColor = "#AED6F1",
                        RangeSliderTrackSelectionColor = "#2980B9",
                        RangeSliderLabelTextColor = "#17202A", //This can be same as the foreground color

                        //DataGrid
                        DataGridHeaderBackgroundColor = "#85929E",
                        DataGridHeaderForegroundColor = "#17202A",
                        DataGridGridCellBorderColor = "#7F8C8D",
                        DataGridRowBackgroundColor = "#EAECEE",
                        DataGridRowForegroundColor = "#212F3D",

                        //Combobox
                        ComboDropDownBackgroundColor = "#BDC3C7",
                        ComboDropDownTextColor = "#17202A", //InputTextFgColor
                        ComboHighlightedTextColor = "#EBEDEF",
                        ComboSelectedDropDownItemColor = "#5D6D7E",
                        ComboTextColor = "#17202A", //InputTextFgColor

                        //List
                        LstForegroundColor = "#17202A",

                        //Buttons
                        ButtonFgColor = "#EBEDEF",

                        //Fonts
                        //DefaultFontFamily = "Scratch"
                        DefaultFontFamily = "Calibri",
                        BoxItemCurrencyFontSize = BoxCurrencyFontSize,
                        BoxItemHighlightNumberFontSize = BoxMainHighlightFontSize,

                        //SfSwitch
                        SwitchBusyIndicatorColorON = "Transparent",
                        SwitchThumbBorderColorON = "Transparent",
                        SwitchThumbColorON = "#D6EAF8", //  -- 884EA0
                        SwitchTrackBorderColorON = "Transparent",
                        SwitchTrackColorON = "#1A5276",  //  -- EBDEF0

                        SwitchBusyIndicatorColorOFF = "Transparent",
                        SwitchThumbBorderColorOFF = "Transparent",
                        SwitchThumbColorOFF = "#1A5276",
                        SwitchTrackBorderColorOFF = "Transparent",
                        SwitchTrackColorOFF = "#D6EAF8",

                        //Notification
                        NotificationBgColor = "#F2F3F4",

                        //AutoComplete
                        AutoCompleteBackgroundColor = "#BFC9CA",
                        AutoCompleteClearButtonColor = "#5D6D7E",
                        AutoCompleteDropdownBackgroundColor = "#BFC9CA",
                        AutoCompleteDropdownBorderColor = "#BFC9CA",
                        AutoCompleteDropdownTextColor = "#212F3C",
                        AutoCompleteHighlightedTextColor = "#117864",
                        AutoCompleteNoResultsFoundTextColor = "#212F3C",
                        AutoCompleteTextColor = "#212F3C",
                        AutoCompleteWaterColor = "#505050",

                        //App Theme Base
                        AppShellBgColor = "#B3B6B7",
                        AppShellFgColor = "#283747",
                        AppShellTitleColor = "#283747",
                        AppShellDisabledColor = "#979A9A", //does not Works
                        AppShellUnselectedColor = "#5D6D7E",
                        AppShellTabBarBackgroundColor = "#B3B6B7", //background
                        AppShellTabBarForegroundColor = "#283747", //does not Works
                        AppShellTabBarUnselectedColor = "#5D6D7E",
                        AppShellTabBarDisabledColor = "#979A9A", //does not Works
                        AppShellTabBarTitleColor = "#21618C", //selected foreground color
                    };
                case AppThemes.FireBreather:
                    return new StyleModelDefault
                    {
                        DefaultForegroundColor = "#78281F",

                        //Tab
                        DefaultTabStrokeColor = "#85929E",
                        DefaultTabHeadTextColor = "#EAEDED",

                        //Expander
                        ExpanderHeaderIconExpandColor = "#BB8FCE",
                        ExpanderHeaderIconCollapseColor = "#2C3E50",
                        ExpanderHeaderTextColor = "#212F3C",

                        //Box
                        BoxBorderColor = "#F5EEF8",

                        //Top Highlight boxes for summary
                        HighlightBoxBorderColor = "Transparent",
                        HighlightFgColor = "#AAB7B8",

                        //Chart
                        ChartBorderColor = "#616A6B",
                        ChartAxisColor = "Red",
                        ChartLegendColor = "Green",
                        ChartTitleColor = "Pink",
                        ChartDataMarkerColor = "Brown",
                        ChartColor1 = "#3498DB",
                        ChartColor2 = "#48C9B0",
                        ChartColor3 = "#884EA0",

                        //InputText
                        InputTextFgColor = "#BFC9CA",
                        InputTextWhenFocusedColor = "#A9DFBF",
                        InputTextWhenUnFocusedColor = "#808B96",
                        InputErrorTextFgColor = "#F5B7B1",

                        //Segments
                        SegBgColor = "#424949",
                        SegBorderColor = "#34495E",
                        SegTextFgColor = "#808B96",
                        SegSelectedTextFgColor = "#212F3C",
                        SegSelectedBgColor = "#D2B4DE",

                        //Ranger Slider
                        RangeSliderKnobColor = "#34495E",
                        RangeSliderTrackColor = "#EC7063",
                        RangeSliderTrackSelectionColor = "#4A235A",
                        RangeSliderLabelTextColor = "White", //This can be same as the foreground color

                        //DataGrid
                        DataGridHeaderBackgroundColor = "#2E4053",
                        DataGridHeaderForegroundColor = "#EAECEE",
                        DataGridGridCellBorderColor = "#7F8C8D",
                        DataGridRowBackgroundColor = "#ABB2B9",
                        DataGridRowForegroundColor = "#212F3D",

                        //Combobox
                        ComboDropDownBackgroundColor = "#34495E",
                        ComboDropDownTextColor = "#BFC9CA", //InputTextFgColor
                        ComboHighlightedTextColor = "#212F3D",
                        ComboSelectedDropDownItemColor = "#E5E7E9",
                        ComboTextColor = "#BFC9CA", //InputTextFgColor

                        //List
                        LstForegroundColor = "#CCD1D1",

                        //Buttons
                        ButtonFgColor = "Black",

                        //Fonts
                        //DefaultFontFamily = "Scratch"
                        DefaultFontFamily = "Calibri",
                        BoxItemCurrencyFontSize = BoxCurrencyFontSize,
                        BoxItemHighlightNumberFontSize = BoxMainHighlightFontSize,

                        //SfSwitch
                        SwitchBusyIndicatorColorON = "Transparent",
                        SwitchThumbBorderColorON = "Transparent",
                        SwitchThumbColorON = "#D7BDE2", //  -- 884EA0
                        SwitchTrackBorderColorON = "Transparent",
                        SwitchTrackColorON = "#884EA0",  //  -- EBDEF0

                        SwitchBusyIndicatorColorOFF = "Transparent",
                        SwitchThumbBorderColorOFF = "Transparent",
                        SwitchThumbColorOFF = "#884EA0",
                        SwitchTrackBorderColorOFF = "Transparent",
                        SwitchTrackColorOFF = "#EBDEF0",

                        //Notification
                        NotificationBgColor = "#EBDEF0",

                        //App Theme Base
                        AppShellBgColor = "#B3B6B7",
                        AppShellFgColor = "#2C3E50",
                        AppShellTitleColor = "",
                        AppShellDisabledColor = "",
                        AppShellUnselectedColor = "",
                        AppShellTabBarBackgroundColor = "#B3B6B7",
                        AppShellTabBarForegroundColor = "#2C3E50",
                        AppShellTabBarUnselectedColor = "",
                        AppShellTabBarDisabledColor = "",
                        AppShellTabBarTitleColor = "",
                    };
                default:
                    return null;
            }
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
