using LoanCalculatorMaui.Services;

namespace LoanCalculatorMaui.Platforms.Android.Services;
public class AndroidLocalStorageService()
    : LocalStorageService(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)),
        ILocalStorage
{
    public void Initialize()
    {
        RootFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
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
}
