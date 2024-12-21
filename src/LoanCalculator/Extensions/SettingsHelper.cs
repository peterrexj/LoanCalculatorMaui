//using LoanCalculatorMaui.Services;
//using LoanCalculatorMaui.Themes;
//using LoanCalculatorMaui.ViewModel;

//namespace LoanCalculatorMaui.Extensions
//{
//    public class SettingsHelper
//    {
//        private SettingsViewModel _model;
//        public SettingsViewModel Model
//        {
//            get
//            {
//                if (_model == null)
//                {
//                    Refresh();
//                }
//                return _model;
//            }
//        }

//        public static void Refresh()
//        {
//            Task.Run(async () => _model = await SharedServices.LocalStorage.GetData<SettingsViewModel>()).Wait();

//            if (_model == null)
//            {
//                _model = DefaultValue;
//            }
//            _model.DefaultStyle = ThemeHelper.GetDefaultStyleTheme(_model.SelectedAppTheme);
//        }

//        private static SettingsViewModel DefaultValue
//        {
//            get
//            {
//                return new SettingsViewModel
//                {
//                    SelectedTheme = OSAppTheme.Light.ToString(),
//                };
//            }
//        }
//    }
//}
