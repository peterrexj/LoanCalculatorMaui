using LoanCalculatorMaui.Services;

namespace LoanCalculatorMaui.Platforms.iOS.Services;

public class iOSLocalStorageService()
    : LocalStorageService(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
        ILocalStorage
{
    public void Initialize()
    {
        RootFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public async Task WriteTextAsync(string fileName, string text)
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var file = Path.Combine(documents, fileName);
        using (var streamWriter = new StreamWriter(file, false))
        {
            await streamWriter.WriteAsync(text);
        }
    }
    public async Task<string> ReadTextAsync(string fileName)
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var file = Path.Combine(documents, fileName);
        using (var streamReader = new StreamReader(file))
        {
            return await streamReader.ReadToEndAsync();
        }
    }
}
