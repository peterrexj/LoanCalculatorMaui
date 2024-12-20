using System.Text.Json.Serialization;
using LoanCalculator.Models;
using LoanCalculator.Models.BaseExtensions;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Services;
using Syncfusion.Maui.Popup;

namespace LoanCalculatorMaui.ViewModel
{
    public class ViewModelUiBase : BaseViewModel
    {
        public string CurrencySymbol { get; set; }
        public string NewLine { get; set; }
        protected bool isUpdating = false;

        protected readonly ISharedServices _sharedServices;

        public ViewModelUiBase()
        {
            _sharedServices = App.Services.GetService<ISharedServices>();

            CurrencySymbol = Helper.CurrencySymbol;
            NewLine = Environment.NewLine;
            this.AcceptAppLaunchDisclaimer = new Command<SfPopup>(AcceptAppLaunchDisclaimerAction);
        }

        public ViewModelUiBase(ISharedServices sharedServices)
        {
            _sharedServices = sharedServices;

            CurrencySymbol = Helper.CurrencySymbol;
            NewLine = Environment.NewLine;
            this.AcceptAppLaunchDisclaimer = new Command<SfPopup>(AcceptAppLaunchDisclaimerAction);
        }

        #region Disclaimers
        [JsonIgnore]
        public string AppLaunchDisclaimerData => _sharedServices.GetAppLaunchDisclaimerData();

        [JsonIgnore]
        public Command<SfPopup> AcceptAppLaunchDisclaimer { get; set; }

        public void ShowAppLaunchDisclaimer(SfPopup sfPopupLayout)
        {
            //if (Device.RuntimePlatform == Device.UWP)
            //{
            // Create a thread
            Thread backgroundThread = new Thread(() => ShowAppLaunchThreadedDisclaimer(sfPopupLayout));
            // Start thread
            backgroundThread.Start();
            //}
            //else
            //{
            //    LaunchAppDisclaimer(sfPopupLayout);
            //}
        }

        private void LaunchAppDisclaimer(SfPopup sfPopupLayout)
        {
            if (_sharedServices.ShouldShowAppLaunchDisclaimer())
            {
                //sfPopupLayout.PopupView.IsFullScreen = true;
                //sfPopupLayout.ClosePopupOnBackButtonPressed = false;

                sfPopupLayout.Show(true);
            }
        }
        private void ShowAppLaunchThreadedDisclaimer(SfPopup sfPopupLayout)
        {
            Thread.Sleep(2000);
            ViewHelper.RunOnAppDispatcher(() =>
            {
                LaunchAppDisclaimer(sfPopupLayout);
            });
        }

        private void AcceptAppLaunchDisclaimerAction(SfPopup sfPopupLayout)
        {
            _sharedServices.NameValueDataService.NameValueDataModel.HasShowAppLaunchDisclaimer = true;
            _sharedServices.NameValueDataService.SaveNameValueData();
            sfPopupLayout.Dismiss();
        }
        #endregion

        #region Save
        protected void SaveData<T>(T data)
        {
            if (PageHelper.IsFormLoading) { return; }

            Task.Run(async () =>
            {
                await _sharedServices.LocalStorage.SaveData<T>(data);
            }).Wait();
        }
        #endregion
    }
}
