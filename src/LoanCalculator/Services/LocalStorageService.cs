using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanCalculatorMaui.Services;

public abstract class LocalStorageService(string rootFolder)
{
    public string HomeLoanDataFilePath => Path.Combine(rootFolder, "homeloandata.json");
    public string IncomeDataFilePath => Path.Combine(rootFolder, "incomedata.json");
    public string ExpenseDataFilePath => Path.Combine(rootFolder, "expensedata.json");
    public string DefaultDataFilePath => Path.Combine(rootFolder, "defaultdata.json");
    public string SettingsDataFilePath => Path.Combine(rootFolder, "settingsdata.json");
    public string NameValueDataFilePath => Path.Combine(rootFolder, "namevaluedata.json");

    public string FilePathBasedOnType<T>() =>
        typeof(T).Name switch
        {
            "IncomeViewModel" => IncomeDataFilePath,
            "LoanViewModel" => HomeLoanDataFilePath,
            "ExpenseViewModel" => ExpenseDataFilePath,
            "SettingsViewModel" => SettingsDataFilePath,
            _ => DefaultDataFilePath
        };

    public async Task<T> GetData<T>()
    {
        var filePath = FilePathBasedOnType<T>();
        if (!File.Exists(filePath))
        {
            return default;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SaveData<T>(T data)
    {
        var options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        var filePath = FilePathBasedOnType<T>();
        var json = JsonSerializer.Serialize(data, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ClearData<T>()
    {
        var filePath = FilePathBasedOnType<T>();
        if (File.Exists(filePath))
        {
            await Task.Run(() => File.Delete(filePath));
        }
    }
}

