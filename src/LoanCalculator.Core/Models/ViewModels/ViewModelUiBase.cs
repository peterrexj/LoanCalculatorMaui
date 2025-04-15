using LoanCalculator.Core.Models.BaseExtensions;

namespace LoanCalculator.Core.Models.ViewModels
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
