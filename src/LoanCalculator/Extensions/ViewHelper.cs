namespace LoanCalculatorMaui.Extensions
{
    public class ViewHelper
    {
        public static async Task RunOnAppDispatcherAsync(Action action)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(action);
            }
            catch (Exception ex)
            {
                // Capture and handle exceptions
                //ExceptionHandler.CaptureException(ex);
            }
        }
    }
}
