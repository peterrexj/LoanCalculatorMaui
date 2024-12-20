using LoanCalculatorMaui.ViewModel;

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
                    Task.Run(async () => _nameValueDataModel = await localStorage.GetData<NameValueDataModel>()).Wait();
                    if (_nameValueDataModel == null )
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
            Task.Run(async () => await localStorage.SaveData<NameValueDataModel>(value ?? NameValueDataModel)).Wait();
        }
    }
}
