using LoanCalculator.Core.Services;
using System.Diagnostics;

namespace LoanCalculator.Playground;

public static class MockServiceProvider
{
    public static IServiceProvider CreateMockServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IAlertService, MockAlertService>();
        services.AddSingleton<IErrorHandlingService, MockErrorHandlingService>();
        services.AddSingleton<ILocalStorage, PlaygroundAppStorageService>();

        //services.AddSingleton(mockAlertService.Object);

        // Add other services as needed
        // services.AddSingleton<IMyService, MyMockService>();

        return services.BuildServiceProvider();
    }
}

public class MockErrorHandlingService : IErrorHandlingService
{
    public Exception? LastHandledException { get; private set; }
    public string? LastHandledMessage { get; private set; }

    public void HandleException(Exception? ex, string message = null)
    {
        // Store the exception and message for testing purposes
        LastHandledException = ex;
        LastHandledMessage = message;

        // Optionally, log to the console for debugging
        Console.WriteLine($"Mock HandleException called. Message: {message}, Exception: {ex?.Message}");
    }
}

public class MockAlertService : IAlertService
{
    public string LastAlertTitle { get; private set; }
    public string LastAlertMessage { get; private set; }
    public string LastAlertOkButton { get; private set; }

    public string LastConfirmationTitle { get; private set; }
    public string LastConfirmationMessage { get; private set; }
    public string LastConfirmationAcceptButton { get; private set; }
    public string LastConfirmationCancelButton { get; private set; }
    public bool ConfirmationResult { get; set; } = true; // Default result for confirmation dialogs

    public Task ShowAlertAsync(string title, string message, string okButton)
    {
        // Store the alert details for testing purposes
        LastAlertTitle = title;
        LastAlertMessage = message;
        LastAlertOkButton = okButton;

        // Optionally log the alert for debugging
        Console.WriteLine($"Mock Alert: Title='{title}', Message='{message}', OkButton='{okButton}'");

        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message, string acceptButton, string cancelButton)
    {
        // Store the confirmation dialog details for testing purposes
        LastConfirmationTitle = title;
        LastConfirmationMessage = message;
        LastConfirmationAcceptButton = acceptButton;
        LastConfirmationCancelButton = cancelButton;

        // Optionally log the confirmation for debugging
        Console.WriteLine($"Mock Confirmation: Title='{title}', Message='{message}', AcceptButton='{acceptButton}', CancelButton='{cancelButton}'");

        // Return the predefined result
        return Task.FromResult(ConfirmationResult);
    }
}

public class PlaygroundAppStorageService()
    : LocalStorageService(@"e:\temp\delete\"),
        ILocalStorage
{
    public void Initialize()
    {
        RootFolder = @"e:\temp\delete\";
    }

    public async Task WriteTextAsync(string fileName, string text)
    {
        var file = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), fileName);
        await using var streamWriter = new StreamWriter(file, false);
        await streamWriter.WriteAsync(text);
    }
    public async Task<string> ReadTextAsync(string fileName)
    {
        var file = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), fileName);
        using var streamReader = new StreamReader(file);
        return await streamReader.ReadToEndAsync();
    }

    public override async Task SaveFileToFileSystem(string fileName, MemoryStream stream)
    {
        try
        {
            // Save the file to the MyDocuments folder
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            Task.Run(async () =>
            {
                await File.WriteAllBytesAsync(filePath, stream.ToArray());
            }).Wait();

            // Open the file with the default PDF viewer
            var processStartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true // Ensures the file is opened with the default application
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening file: {ex.Message}");
            throw;
        }
    }

    public override async Task<Stream> LoadFileFromFileSystem(string fileName)
    {
        try
        {
            var parentPath = "D:\\Pro\\LoanCalculatorMaui\\src\\LoanCalculator\\";
            var filePath = Path.Combine(parentPath, fileName);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return stream;

            // Open the file from the app package
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file: {ex.Message}");
            throw;
        }
    }

}