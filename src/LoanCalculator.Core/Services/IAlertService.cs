namespace LoanCalculator.Core.Services
{
    public interface IAlertService
    {
        Task ShowAlertAsync(string title, string message, string okButton);
        Task<bool> ShowConfirmationAsync(string title, string message, string acceptButton, string cancelButton);
    }
}
