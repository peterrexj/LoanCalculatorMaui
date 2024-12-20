namespace LoanCalculatorMaui.Extensions
{
    public class ViewHelper
    {
        public static async Task RunOnAppDispatcher(Action action)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    action(); // Execute the provided action on the UI thread
                });
            }
            catch (Exception ex)
            {
                // Capture and handle exceptions
                //ExceptionHandler.CaptureException(ex);
            }
        }
    }
}
