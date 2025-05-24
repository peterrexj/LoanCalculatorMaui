namespace LoanCalculatorMaui.Services
{
    public interface IAppInformation
    {
        string Country { get; }
        bool IsAustralia { get; }
        string ApplicationTitle { get; }
        string InAppProductId { get; }
        string AppShareLink { get; }
        string RateAppLink { get; }
    }
}
