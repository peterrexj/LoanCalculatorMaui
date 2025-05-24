using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.View;

public partial class SettingsView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly SettingsViewModel _viewModel;

    public SettingsView(
        IErrorHandlingService errorHandlingService,
        SettingsViewModel viewModel)
    {
        InitializeComponent();

        _errorHandlingService = errorHandlingService;
        _viewModel = viewModel;
        _viewModel.IsBusy = true;
    }

    protected override async void OnAppearing()
    {
        try
        {
            PageHelper.PageIsLoading();

            base.OnAppearing();

            await Task.Delay(100); // Delay to allow UI to load

            await Task.Yield();

            Dispatcher.Dispatch(async () =>
            {
                await LoadDataSet();
            });
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            _viewModel.IsBusy = false;
        }
    }

    private async Task LoadDataSet()
    {
        try
        {
            //placeholder for the data to be loaded
            var viewModelInitializeTask = Task.Run(async () =>
            {
                var data = await SharedServiceCore.LoadDataFile<SettingsViewModel>();
            });

            var themeHandlerTask = Task.Run(async () =>
            {
                var theme = _viewModel.SelectedTheme;
                if (theme == null)
                {
                    _viewModel.SelectedTheme = _viewModel.Themes.First(f => f == AppTheme.Light.ToString());
                }
            });

            await Task.WhenAll(viewModelInitializeTask, themeHandlerTask);

            _viewModel.RefreshProperties();

            BindingContext ??= _viewModel;

            _viewModel.LoadSelectedCurrency();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            PageHelper.PageLoadingComplete();
        }
    }

    private async void OnPremiumShow_Clicked(object? sender, EventArgs e)
    {
        try
        {
            await Task.Run(() => PremiumWindow.ShowPremiumBuyWindow = true);
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
}