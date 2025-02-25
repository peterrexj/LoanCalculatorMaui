namespace LoanCalculatorMaui.Services
{
    public interface IAlertService
    {
        Task ShowAlertAsync(string title, string message, string okButton);
        Task<bool> ShowConfirmationAsync(string title, string message, string acceptButton, string cancelButton);
    }

    public class AlertService : IAlertService
    {
        public async Task ShowAlertAsync(string title, string message, string okButton)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current.MainPage.DisplayAlert(title, message, okButton);
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string acceptButton, string cancelButton)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () => await Application.Current.MainPage.DisplayAlert(title, message, acceptButton, cancelButton));
        }
    }
}
