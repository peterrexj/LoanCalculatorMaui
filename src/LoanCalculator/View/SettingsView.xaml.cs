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
        _viewModel.IsPageBusy = true;

        BindingContext = _viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Ensure LoanViewModel reflects the latest Australian mode setting
        var loanViewModel = ServiceLocator.GetService<LoanViewModel>();
        if (loanViewModel != null)
            loanViewModel.IsAustralianModeEnabled = _viewModel.IsAustralianModeEnabled;
    }

    protected override async void OnAppearing()
    {
        try
        {
            PageHelper.PageIsLoading();

            base.OnAppearing();

            await Task.Delay(100); // Delay to allow UI to load

            await LoadDataSet();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            _viewModel.IsPageBusy = false;
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
                    _viewModel.SelectedTheme = _viewModel.Themes.First(f => f == SharedServiceCore.DefaultAppTheme.ToString());
                }
            });

            await Task.WhenAll(viewModelInitializeTask, themeHandlerTask);

            _viewModel.RefreshProperties();

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

    private void OnDefaultLoanTermDecrement(object? sender, EventArgs e)
    {
        var v = _viewModel.DefaultLoanTermYears - 1;
        if (v >= 1) _viewModel.DefaultLoanTermYears = v;
    }

    private void OnDefaultLoanTermIncrement(object? sender, EventArgs e)
    {
        var v = _viewModel.DefaultLoanTermYears + 1;
        if (v <= _viewModel.MaxLoanTermYears) _viewModel.DefaultLoanTermYears = v;
    }

    private void OnMaxLoanTermDecrement(object? sender, EventArgs e)
    {
        var v = _viewModel.MaxLoanTermYears - 1;
        if (v >= 10) _viewModel.MaxLoanTermYears = v;
    }

    private void OnMaxLoanTermIncrement(object? sender, EventArgs e)
    {
        var v = _viewModel.MaxLoanTermYears + 1;
        if (v <= 50) _viewModel.MaxLoanTermYears = v;
    }

    private void OnDefaultProjectionYearsDecrement(object? sender, EventArgs e)
    {
        var v = _viewModel.DefaultProjectionYears - 1;
        if (v >= 1) _viewModel.DefaultProjectionYears = v;
    }

    private void OnDefaultProjectionYearsIncrement(object? sender, EventArgs e)
    {
        var v = _viewModel.DefaultProjectionYears + 1;
        if (v <= _viewModel.MaxProjectionYears) _viewModel.DefaultProjectionYears = v;
    }

    private void OnMaxProjectionYearsDecrement(object? sender, EventArgs e)
    {
        var v = _viewModel.MaxProjectionYears - 1;
        if (v >= 5) _viewModel.MaxProjectionYears = v;
    }

    private void OnMaxProjectionYearsIncrement(object? sender, EventArgs e)
    {
        var v = _viewModel.MaxProjectionYears + 1;
        if (v <= 50) _viewModel.MaxProjectionYears = v;
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