using LoanCalculatorMaui.Services;

namespace LoanCalculatorMaui.Platforms.iOS.Services
{
    public class iOSLocalStorageService : ILocalStorage
    {
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

        public string HomeLoanDataFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "homeloandata.json");
        public string IncomeDataFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "incomedata.json");
        public string ExpenseDataFilePath => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "expensedata.json");
        public string DefaultDataFilePath => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "defaultdata.json");
        public string SettingsDataFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "settingsdata.json");
        public string NameValueDataFilePath =>
           Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "namevaluedata.json");

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