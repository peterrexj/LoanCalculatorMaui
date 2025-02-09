// Services/ErrorHandlingService.cs
using System;
using Microsoft.Extensions.Logging;

namespace LoanCalculatorMaui.Services
{
    public interface IErrorHandlingService
    {
        void HandleException(Exception? ex, string message = null);
    }

    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger;
        }

        public void HandleException(Exception? ex, string message = null)
        {
            // Log the exception with detailed information
            _logger.LogError(ex, message ?? ex.Message);

            // Display an alert to the user
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("Error", message ?? "An unexpected error occurred.", "OK");
            });
        }
    }
}