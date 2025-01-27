using System.Text.Json.Serialization;
using System.Windows.Input;
using LoanCalculator.Models;
using LoanCalculator.Models.BaseExtensions;
using LoanCalculator.Models.Enums;
using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Services;
using Syncfusion.Maui.Core.Carousel;
using Syncfusion.Maui.Popup;

namespace LoanCalculatorMaui.ViewModel
{
    public class ViewModelUiBase : BaseViewModel
    {
        public string CurrencySymbol { get; set; }
        public string NewLine { get; set; }
        protected bool isUpdating = false;

        public ViewModelUiBase()
        {
            CurrencySymbol = Helper.CurrencySymbol;
            NewLine = Environment.NewLine;
        }

        #region Data files
        public async Task<T?> LoadDataFile<T>()
        {
            T? data = default;

            try
            {
                SharedServices.LocalStorage!.Initialize();
                data = await SharedServices.LocalStorage.GetData<T>().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Log or handle the exception as needed
                // ExceptionHandler.CaptureException(e);
            }

            return data;
        }

        protected Task SaveData<T>(T data)
        {
            if (PageHelper.IsFormLoading)
            {
                return Task.CompletedTask;
            }

            Task.Run(async () =>
            {
                if (SharedServices.LocalStorage == null) { return; }

                await SharedServices.LocalStorage.SaveData<T>(data).ConfigureAwait(false);
            }).Wait();
            return Task.CompletedTask;
        }

        #endregion

        
    }
}
