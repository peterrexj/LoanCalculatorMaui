// Services/ErrorHandlingService.cs
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LoanCalculatorMaui.Services
{
    public interface IErrorHandlingService
    {
        void HandleException(Exception? ex, string message = null);
    }

    public class ErrorHandlingService(ILogger<ErrorHandlingService> logger) : IErrorHandlingService
    {
        public void HandleException(Exception? ex, string message = null)
        {
            //enabling this will log the exception to the console & Sentry will capture and send the exception
            // Log the exception with detailed information
            //logger.LogError(ex, message ?? ex.Message);

            if (ex is JsonException)
            {
                //this usually happens when there are changes to model or the value of the type
            }

            // Display an alert to the user
            MainThread.BeginInvokeOnMainThread(async () =>
            {
#if !DEBUG
                SentrySdk.CaptureException(ex);
#endif
                await Application.Current?.MainPage.DisplayAlert("Error", message ?? "An unexpected error occurred.", "OK");
            });
        }
    }
}