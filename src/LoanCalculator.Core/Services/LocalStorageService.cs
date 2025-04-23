using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanCalculator.Core.Services;

public abstract class LocalStorageService
{
    public string RootFolder { get; set; }
    private readonly JsonSerializerOptions _serializerOptions;

    protected LocalStorageService(string rootFolder)
    {
        RootFolder = rootFolder;
        _serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DoubleDefaultConverter() },
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
    }

    public bool IsInitialized => !string.IsNullOrEmpty(RootFolder);

    public string HomeLoanDataFilePath => Path.Combine(RootFolder, "homeloandata.json");
    public string IncomeDataFilePath => Path.Combine(RootFolder, "incomedata.json");
    public string ExpenseDataFilePath => Path.Combine(RootFolder, "expensedata.json");
    public string DefaultDataFilePath => Path.Combine(RootFolder, "defaultdata.json");
    public string SettingsDataFilePath => Path.Combine(RootFolder, "settingsdata.json");
    public string NameValueDataFilePath => Path.Combine(RootFolder, "namevaluedata.json");
    public string ThemeSelectDataFilePath => Path.Combine(RootFolder, "themeselectdata.json");

    public string FilePathBasedOnType<T>() =>
        typeof(T).Name switch
        {
            "IncomeViewModel" => IncomeDataFilePath,
            "LoanViewModel" => HomeLoanDataFilePath,
            "ExpenseViewModel" => ExpenseDataFilePath,
            "SettingsViewModel" => SettingsDataFilePath,
            "NameValueDataModel" => NameValueDataFilePath,
            "ThemeSelect" => ThemeSelectDataFilePath,
            _ => DefaultDataFilePath
        };

    public async Task<T> GetData<T>()
    {
        EnsureRootFolderIsSet();
        var filePath = FilePathBasedOnType<T>();
        if (!File.Exists(filePath))
        {
            return default;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<T>(json, _serializerOptions);
    }

    public async Task SaveData<T>(T data)
    {
        EnsureRootFolderIsSet();

        var filePath = FilePathBasedOnType<T>();
        var json = JsonSerializer.Serialize(data, _serializerOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ClearData<T>()
    {
        EnsureRootFolderIsSet();
        var filePath = FilePathBasedOnType<T>();
        if (File.Exists(filePath))
        {
            await Task.Run(() => File.Delete(filePath));
        }
    }

    private void EnsureRootFolderIsSet()
    {
        if (string.IsNullOrEmpty(RootFolder))
        {
            throw new InvalidOperationException("Root folder is not set.");
        }
    }

    public virtual async Task SaveFileToFileSystem(string fileName, MemoryStream stream)
    {
        // Save the stream as a file in the device and invoke it for viewing
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, stream.ToArray());
        // Invoke the file for viewing
        await Launcher.OpenAsync(new OpenFileRequest
        {
            File = new ReadOnlyFile(filePath)
        });
    }

    public virtual async Task<Stream> LoadFileFromFileSystem(string path)
    {
        try
        {
            // Open the file from the app package
            var imageStream = await FileSystem.OpenAppPackageFileAsync(path);
            return imageStream;
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"File '{path}' not found.", ex);
        }
    }
}

