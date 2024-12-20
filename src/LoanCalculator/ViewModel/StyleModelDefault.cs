using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using LoanCalculator.Models.BaseExtensions;

namespace LoanCalculatorMaui.ViewModel
{
    public class StyleModelDefault : BasePropertyChangeModel
    {
        public string DefaultForegroundColor { get; set; }
        //public IGradientSource DefaultMgPageBgSource { get; set; }

        //Tab
        public string DefaultTabStrokeColor { get; set; }
        public string DefaultTabHeadTextColor { get; set; }
        //public IGradientSource DefaultMgTabHeadBgSource { get; set; }

        //Expander
        //public IGradientSource MgExpanderHeadBgSource { get; set; }
        public string ExpanderHeaderIconExpandColor { get; set; }
        public string ExpanderHeaderIconCollapseColor { get; set; }
        public string ExpanderHeaderTextColor { get; set; }

        //Box
        //public IGradientSource BoxBgSource { get; set; }
        public string BoxBorderColor { get; set; }

        //Highlight Box
        //public IGradientSource HighlightBgSource { get; set; }
        public string HighlightBoxBorderColor { get; set; }
        public string HighlightFgColor { get; set; }


        //Chart
        //public IGradientSource ChartBgSource { get; set; }
        public string ChartBorderColor { get; set; }
        public string ChartAxisColor { get; set; }
        public string ChartLegendColor { get; set; }
        public string ChartTitleColor { get; set; }
        public string ChartDataMarkerColor { get; set; }
        public string ChartColor1 { get; set; }
        public string ChartColor2 { get; set; }
        public string ChartColor3 { get; set; }


        [JsonIgnore]
        public ObservableCollection<Color> CustomPaletteColors =>
            new System.Collections.ObjectModel.ObservableCollection<Color> { Color.FromArgb(ChartColor1), Color.FromArgb(ChartColor2), Color.FromArgb(ChartColor3) };

        //Input Text
        public string InputTextFgColor { get; set; }
        public string InputTextWhenFocusedColor { get; set; }
        public string InputTextWhenUnFocusedColor { get; set; }
        public string InputErrorTextFgColor { get; set; }

        //SegmentedControl
        public string SegTextFgColor { get; set; }
        public string SegSelectedTextFgColor { get; set; }
        public string SegBorderColor { get; set; }
        public string SegBgColor { get; set; }
        public string SegSelectedBgColor { get; set; }

        //RangeSlider
        public string RangeSliderKnobColor { get; set; }
        public string RangeSliderTrackColor { get; set; }
        public string RangeSliderTrackSelectionColor { get; set; }
        public string RangeSliderLabelTextColor { get; set; }

        //DataGrid
        public string DataGridHeaderBackgroundColor { get; set; }
        public string DataGridHeaderForegroundColor { get; set; }
        public string DataGridGridCellBorderColor { get; set; }
        public string DataGridRowBackgroundColor { get; set; }
        public string DataGridRowForegroundColor { get; set; }

        //Combobox
        public string ComboDropDownBackgroundColor { get; set; }
        public string ComboDropDownTextColor { get; set; }
        public string ComboHighlightedTextColor { get; set; }
        public string ComboSelectedDropDownItemColor { get; set; }
        public string ComboTextColor { get; set; }

        //ListView
        //public IGradientSource LstBackgroundSource { get; set; }
        public string LstForegroundColor { get; set; }


        //Button
        //public IGradientSource ButtonBgSource { get; set; }
        public string ButtonFgColor { get; set; }


        //Fonts
        public string DefaultFontFamily { get; set; }
        public double BoxItemCurrencyFontSize { get; set; }
        public double BoxItemHighlightNumberFontSize { get; set; }

        //SfSwitch
        public string SwitchBusyIndicatorColorON { get; set; }
        public string SwitchThumbBorderColorON { get; set; }
        public string SwitchThumbColorON { get; set; }
        public string SwitchTrackBorderColorON { get; set; }
        public string SwitchTrackColorON { get; set; }

        public string SwitchBusyIndicatorColorOFF { get; set; }
        public string SwitchThumbBorderColorOFF { get; set; }
        public string SwitchThumbColorOFF { get; set; }
        public string SwitchTrackBorderColorOFF { get; set; }
        public string SwitchTrackColorOFF { get; set; }

        //Notification
        //public IGradientSource NotificationAlertHighBackgroundSource { get; set; }
        //public IGradientSource NotificationBackgroundSource { get; set; }
        public string NotificationBgColor { get; set; }

        //App Theme Base
        public string AppShellBgColor { get; set; }
        public string AppShellFgColor { get; set; }
        public string AppShellTitleColor { get; set; }
        public string AppShellDisabledColor { get; set; }
        public string AppShellUnselectedColor { get; set; }
        public string AppShellTabBarBackgroundColor { get; set; }
        public string AppShellTabBarForegroundColor { get; set; }
        public string AppShellTabBarUnselectedColor { get; set; }
        public string AppShellTabBarDisabledColor { get; set; }
        public string AppShellTabBarTitleColor { get; set; }

        //AutoComplete
        public string AutoCompleteBackgroundColor { get; set; }
        public string AutoCompleteClearButtonColor { get; set; }
        public string AutoCompleteDropdownBackgroundColor { get; set; }
        public string AutoCompleteDropdownBorderColor { get; set; }
        public string AutoCompleteDropdownTextColor { get; set; }
        public string AutoCompleteHighlightedTextColor { get; set; }
        public string AutoCompleteNoResultsFoundTextColor { get; set; }
        public string AutoCompleteTextColor { get; set; }
        public string AutoCompleteWaterColor { get; set; }
    }


}
