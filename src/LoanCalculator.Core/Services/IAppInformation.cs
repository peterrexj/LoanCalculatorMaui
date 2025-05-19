namespace LoanCalculatorMaui.Services
{
    public interface IAppInformation
    {
        string Country { get; }
        bool IsAustralia { get; }
        string InAppProductId { get; }
        string AppShareLink { get; }
    }
}
