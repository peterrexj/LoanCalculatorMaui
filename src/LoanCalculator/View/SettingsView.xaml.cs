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
            var data = await SharedServices.LoadDataFile<SettingsViewModel>();

            if (data == null)
            {
                viewModel.SelectedTheme = viewModel.Themes.First(f => f.Name == AppTheme.Light.ToString());
            }
            else
            {
                viewModel.SelectedTheme = data.SelectedTheme;
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
}