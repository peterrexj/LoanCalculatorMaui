// Services/ErrorHandlingService.cs
using System;
using Microsoft.Extensions.Logging;

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