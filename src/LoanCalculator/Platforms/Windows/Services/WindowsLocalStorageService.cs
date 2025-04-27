using LoanCalculatorMaui.Services;
using Windows.Storage;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.Platforms.Windows.Services;

public class WindowsLocalStorageService()
    : LocalStorageService(string.Empty),
        ILocalStorage
{


    public void Initialize()
    {
        //RootFolder = ApplicationData.Current.LocalFolder.Path;
        RootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LoanCalculator");
        Directory.CreateDirectory(RootFolder); // Ensure the directory exists
    }

    public async Task WriteTextAsync(string fileName, string text)
    {
        Directory.CreateDirectory(RootFolder); // Ensure the directory exists

        var filePath = Path.Combine(RootFolder, fileName);
        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var streamWriter = new StreamWriter(stream);
        await streamWriter.WriteAsync(text);
    }

    public async Task<string> ReadTextAsync(string fileName)
    {
        var filePath = Path.Combine(RootFolder, fileName);

        // Ensure the file exists before attempting to read
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{fileName}' does not exist in the directory '{RootFolder}'.");
        }

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var streamReader = new StreamReader(stream);
        return await streamReader.ReadToEndAsync();
    }
}

