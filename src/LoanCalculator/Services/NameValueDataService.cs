using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.Services
{
    public class NameValueDataService(ILocalStorage localStorage) : INameValueDataService
    {
        private NameValueDataModel? _nameValueDataModel;

        public NameValueDataModel NameValueDataModel
        {
            get
            {
                if (_nameValueDataModel == null)
                {
                    // Called during App startup on the main thread — Task.Run avoids sync-context deadlock.
                    _nameValueDataModel = Task.Run(() => localStorage.GetData<NameValueDataModel>()).GetAwaiter().GetResult();
                    if (_nameValueDataModel == null)
                    {
                        _nameValueDataModel = new NameValueDataModel
                        {
                            HasShowAppLaunchDisclaimer = false,
                        };
                        SaveNameValueData(_nameValueDataModel);
                    }
                }
                return _nameValueDataModel;
            }
        }

        public void SaveNameValueData(NameValueDataModel value = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await localStorage.SaveData<NameValueDataModel>(value ?? NameValueDataModel).ConfigureAwait(false);
                }
                catch { /* non-critical metadata — swallow */ }
            });
        }
    }
}
