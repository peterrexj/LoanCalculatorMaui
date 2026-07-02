using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.Controls;

public partial class PopupDisclaimerView : ContentView
{
    private readonly PopupDisclaimerViewModel _viewModel;

    public PopupDisclaimerView() : this(ServiceLocator.GetService<IErrorHandlingService>(), ServiceLocator.GetService<PopupDisclaimerViewModel>())
    {
    }

    public PopupDisclaimerView(IErrorHandlingService errorHandlingService, PopupDisclaimerViewModel viewModel)
	{
		InitializeComponent();

        _viewModel = viewModel;

        _viewModel.TriggerChange();

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
            // ignored
        }
        finally
        {
            _viewModel.IsActive = true;
        }

    }
}