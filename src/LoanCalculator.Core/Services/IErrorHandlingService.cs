// Services/ErrorHandlingService.cs

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LoanCalculator.Core.Services
{
    public interface IErrorHandlingService
    {
        void HandleException(Exception? ex, string message = null);
    }

    public class ErrorHandlingService(ILogger<ErrorHandlingService> logger) : IErrorHandlingService
    {
        public void HandleException(Exception? ex, string message = null)
        {
            if (ex == null) return;

            logger.LogError(ex, message ?? ex.Message);

#if !DEBUG
            SentrySdk.CaptureException(ex);
#endif

            // JSON errors are usually stale saved data after a model change — not user-facing.
            if (ex is JsonException) return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                    if (page == null) return;

#if DEBUG
                    await page.DisplayAlert("Error", $"{message ?? "An unexpected error occurred."}\n\n{ex.GetType().Name}: {ex.Message}", "OK");
#else
                    await page.DisplayAlert("Something went wrong", "We hit an unexpected error. The app will continue but some data may not have saved correctly.", "OK");
#endif
                }
                catch { /* alert itself failed — nothing more we can do */ }
            });
        }
    }
}