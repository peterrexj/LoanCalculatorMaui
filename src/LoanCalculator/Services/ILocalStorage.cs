namespace LoanCalculatorMaui.Services
{
    public interface ILocalStorage
    {
        bool IsInitialized { get; }
        void Initialize();
        Task WriteTextAsync(string fileName, string text);
        Task<string> ReadTextAsync(string fileName);

        string HomeLoanDataFilePath { get; }
        string IncomeDataFilePath { get; }
        string ExpenseDataFilePath { get; }
        string SettingsDataFilePath { get; }
        string DefaultDataFilePath { get; }
        string NameValueDataFilePath { get; }

        string FilePathBasedOnType<T>();
        Task<T> GetData<T>();
        Task SaveData<T>(T data);
        Task ClearData<T>();
    }
}
