using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.ViewModel;

namespace LoanCalculatorMaui.Controls;

public partial class PopupDisclaimerView : ContentView
{
    private readonly PopupDisclaimerViewModel _viewModel;
    public PopupDisclaimerView()
	{
		InitializeComponent();

        _viewModel = new(ServiceLocator.GetService<IErrorHandlingService>(), ServiceLocator.GetService<IAlertService>());
        BindingContext = _viewModel;

        Loaded += OnLoaded;
    }
    private async void OnLoaded(object sender, EventArgs e)
    {
        try
        {
            await Task.Delay(3000);
        }
        catch (Exception exception)
        {
            
        }
        finally
        {
            _viewModel.IsActive = true;
        }
        
    }
}