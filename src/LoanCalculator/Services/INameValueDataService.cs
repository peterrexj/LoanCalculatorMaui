using LoanCalculatorMaui.ViewModel;

namespace LoanCalculatorMaui.Services;

public interface INameValueDataService
{
    NameValueDataModel NameValueDataModel { get; }
    void SaveNameValueData(NameValueDataModel value = null);
}