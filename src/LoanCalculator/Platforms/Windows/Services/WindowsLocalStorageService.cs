using LoanCalculatorMaui.Services;
using Windows.Storage;

namespace LoanCalculatorMaui.Platforms.Windows.Services;

public class WindowsLocalStorageService()
    : LocalStorageService(ApplicationData.Current.LocalFolder.Path),
        ILocalStorage
{
    public async Task WriteTextAsync(string fileName, string text)
    {
        var folder = ApplicationData.Current.LocalFolder;
        var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
        await using var stream = await file.OpenStreamForWriteAsync();
        await using var streamWriter = new StreamWriter(stream);
        await streamWriter.WriteAsync(text);
    }
    public async Task<string> ReadTextAsync(string fileName)
    {
        var folder = ApplicationData.Current.LocalFolder;
        var file = await folder.GetFileAsync(fileName);
        await using var stream = await file.OpenStreamForReadAsync();
        using var streamReader = new StreamReader(stream);
        return await streamReader.ReadToEndAsync();
    }
}

