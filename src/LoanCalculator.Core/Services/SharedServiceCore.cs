using LoanCalculator.Core.Helper;
using LoanCalculatorMaui.Services;

namespace LoanCalculator.Core.Services
{
    public static class SharedServiceCore
    {
        private static ILocalStorage? _localStorage;
        public static ILocalStorage LocalStorage => _localStorage ??= ServiceLocator.GetService<ILocalStorage>();

        private static IErrorHandlingService? _errorHandlingService;
        public static IErrorHandlingService ErrorHandlingService =>
            _errorHandlingService ??= ServiceLocator.GetService<IErrorHandlingService>();

        private static IAppInformation? _appInformation;
        public static IAppInformation? AppInformation => _appInformation ??= ServiceLocator.GetService<IAppInformation>();

        private static bool _loadSafe = false;
        public static bool LoadSafe => _loadSafe;
        public static void LoadSafeOn()
        {
            _loadSafe = true;
        }
        public static void LoadSafeOff()
        {
            _loadSafe = false;
        }

        public static async Task<T?> LoadDataFile<T>()
        {
            T? data = default;

            try
            {
                LocalStorage.Initialize();
                data = await LocalStorage.GetData<T>().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ErrorHandlingService.HandleException(e);
            }

            return data;
        }

        public static Task SaveData<T>(T data)
        {
            try
            {
                if (_loadSafe)
                {
                    return Task.CompletedTask;
                }

                if (PageHelper.IsFormLoading)
                {
                    return Task.CompletedTask;
                }

                Task.Run(async () =>
                {
                    await LocalStorage.SaveData(data).ConfigureAwait(false);
                }).Wait();
            }
            catch (Exception e)
            {
                ErrorHandlingService.HandleException(e);
            }

            return Task.CompletedTask;
        }
    }
}
