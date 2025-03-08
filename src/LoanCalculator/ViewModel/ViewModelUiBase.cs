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
        public bool IsUpdating
        {
            get => isUpdating;
            set
            {
                isUpdating = value;
            }
        }

        public ViewModelUiBase()
        {
            CurrencySymbol = Helper.CurrencySymbol;
            NewLine = Environment.NewLine;
        }
    }
}
