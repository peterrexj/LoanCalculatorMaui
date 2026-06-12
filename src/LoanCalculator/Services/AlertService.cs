using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.Services
{
    public class AlertService : IAlertService
    {
        private static Page? CurrentPage =>
            Application.Current?.Windows.FirstOrDefault()?.Page;

        public async Task ShowAlertAsync(string title, string message, string okButton)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = CurrentPage;
                if (page != null)
                    await page.DisplayAlert(title, message, okButton);
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string acceptButton, string cancelButton)
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = CurrentPage;
                    return page != null && await page.DisplayAlert(title, message, acceptButton, cancelButton);
                });
            }
            catch
            {
                return true;
            }
        }
    }
}
