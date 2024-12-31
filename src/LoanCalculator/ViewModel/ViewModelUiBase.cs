using System.Text.Json.Serialization;
using System.Windows.Input;
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

        public ViewModelUiBase()
        {
            CurrencySymbol = Helper.CurrencySymbol;
            NewLine = Environment.NewLine;
        }

        #region Save
        protected void SaveData<T>(T data)
        {
            if (PageHelper.IsFormLoading) { return; }

            Task.Run(async () =>
            {
                if (SharedServices.LocalStorage == null) { return; }

                await SharedServices.LocalStorage.SaveData<T>(data).ConfigureAwait(false);
            }).Wait();
        }
        #endregion
    }
}
