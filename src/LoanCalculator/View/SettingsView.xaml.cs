using LoanCalculatorMaui.Extensions;
using LoanCalculatorMaui.ViewModel;

namespace LoanCalculatorMaui.View;

public partial class SettingsView : ContentPage
{
    private SettingsViewModel viewModel;
    public SettingsView()
    {
        InitializeComponent();
        viewModel = new SettingsViewModel
        {
            IsBusy = true
        };
        BindingContext ??= viewModel;
    }

    protected override async void OnAppearing()
    {
        PageHelper.PageIsLoading();

        await LoadDataSet();

        base.OnAppearing();
        
        PageHelper.PageLoadingComplete();
    }

    private async Task LoadDataSet()
    {
        SettingsViewModel? data = await viewModel.LoadDataFile<SettingsViewModel>();

        if (data == null)
        {
            viewModel.SelectedTheme = viewModel.Themes.First(f => f.Name == AppTheme.Light.ToString());
        }
        else
        {
            viewModel.SelectedTheme = data.SelectedTheme;
        }
    }
}