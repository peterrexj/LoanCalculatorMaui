using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.Services
{
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
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () => await Application.Current?.MainPage?.DisplayAlert(title, message, acceptButton, cancelButton));
            }
            catch (Exception e)
            {
                return true;
            }
        }
    }
}
