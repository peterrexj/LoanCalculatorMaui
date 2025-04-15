using LoanCalculator.Core.Models.ViewModels;

namespace LoanCalculator.Core.Services;

public interface INameValueDataService
{
    NameValueDataModel NameValueDataModel { get; }
    void SaveNameValueData(NameValueDataModel value = null);
}