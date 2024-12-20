using LoanCalculatorMaui.Services;
using Windows.Storage;

namespace LoanCalculatorMaui.Platforms.Windows.Services
{
    public class WindowsLocalStorageService : ILocalStorage
    {
        public async Task WriteTextAsync(string fileName, string text)
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                using (var streamWriter = new StreamWriter(stream))
                {
                    await streamWriter.WriteAsync(text);
                }
            }
        }
        public async Task<string> ReadTextAsync(string fileName)
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.GetFileAsync(fileName);
            using (var stream = await file.OpenStreamForReadAsync())
            {
                using (var streamReader = new StreamReader(stream))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
        }

        public string HomeLoanDataFilePath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "homeloandata.json");
        public string IncomeDataFilePath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "incomedata.json");
        public string ExpenseDataFilePath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "expensedata.json");
        public string DefaultDataFilePath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "defaultdata.json");
        public string SettingsDataFilePath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "settingsdata.json");
        public string NameValueDataFilePath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "namevaluedata.json");

        public string FilePathBasedOnType<T>()
        {
            switch (typeof(T).Name)
            {
                case "IncomeViewModel":
                    return IncomeDataFilePath;
                case "LoanViewModel":
                    return HomeLoanDataFilePath;
                case "ExpenseViewModel":
                    return ExpenseDataFilePath;
                case "SettingsViewModel":
                    return SettingsDataFilePath;
                default:
                    return DefaultDataFilePath;
            }
        }
        public async Task<T> GetData<T>()
        {
            return await Task.Run(() =>
            {
                T viewModel;
                if (File.Exists(FilePathBasedOnType<T>()) == false)
                {
                    return Task.FromResult<T>(default(T));
                }
                else
                {
                    viewModel = Pj.Library.SerializationHelper.DeSerializeFromJsonFile<T>(FilePathBasedOnType<T>());
                }

                return Task.FromResult(viewModel);
            });
        }
        public async Task SaveData<T>(T data)
        {
            await Task.Run(() => {
                Pj.Library.SerializationHelper.SerializeToJson<T>(data, FilePathBasedOnType<T>());
                return Task.CompletedTask;
            });
        }
        public async Task ClearData<T>()
        {
            await Task.Run(() => {
                File.Delete(FilePathBasedOnType<T>());
                return Task.CompletedTask;
            });
        }
    }
}
