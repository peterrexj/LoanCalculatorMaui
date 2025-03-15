using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.Services;
using LoanCalculatorMaui.ViewModel;

namespace LoanCalculatorMaui.View;

public partial class SettingsView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private SettingsViewModel viewModel;

    public SettingsView(IErrorHandlingService errorHandlingService)
    {
        _errorHandlingService = errorHandlingService;
        InitializeComponent();
        viewModel = new SettingsViewModel
        {
            IsBusy = true
        };
        BindingContext ??= viewModel;
    }

    protected override async void OnAppearing()
    {
        try
        {
            PageHelper.PageIsLoading();

            await LoadDataSet();

            base.OnAppearing();

            viewModel.RefreshProperties();
        }
        catch (Exception ex)
        {
            base.OnAppearing();
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            PageHelper.PageLoadingComplete();
            viewModel.IsBusy = false;
        }
    }

    private async Task LoadDataSet()
    {
        try
        {
            //placeholder for the data to be loaded
            var data = await SharedServices.LoadDataFile<SettingsViewModel>();

            var theme = viewModel.SelectedTheme;
            if (theme == null)
            {
                viewModel.SelectedTheme = viewModel.Themes.First(f => f == AppTheme.Light.ToString());
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
}