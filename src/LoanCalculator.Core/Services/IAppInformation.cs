namespace LoanCalculatorMaui.Services
{
    public interface IAppInformation
    {
        string AppCentreAppKeyDroid { get; }
        string AdsBannerId { get; }
        string AdsInterstitialId { get; }
        int ShowFirstInterstitialAdOnClickLimit { get; }
        int ShowLaterInterstitialAdOnClickLimit { get; }
        string Country { get; }
        bool IsAustralia { get; }
    }
}
