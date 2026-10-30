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

    protected override void OnAppearing()
    {
        try
        {
            PageHelper.PageIsLoading();

            base.OnAppearing();

            // No Task.Delay here any more. It was "Delay to allow UI to load", which made sense
            // while LoadDataSet went to disk — it let the busy flag render before the read. That
            // read was dead code (see LoadDataSet), so the delay was 100 ms of latency added to
            // every single visit to guard work that no longer happens.
            //
            // It could not have helped the symptom it looked like it was for either: this page is
            // declared in AppShell.xaml as ContentTemplate="{DataTemplate localview:SettingsView}",
            // so Shell constructs the whole page on the first tab tap and OnAppearing does not run
            // until that construction is already finished. Anything slow about the first visit has
            // happened before the first line here executes.
            LoadDataSet();

#if IOS || MACCATALYST
            // Syncfusion 33.2.6 stacks a touch-enabled drawing layer over expander headers on
            // iOS, so taps are accepted and dropped. Same defect family as the tab strip and the
            // segmented control — see SyncfusionIosTouchFix. None of these expanders have an
            // x:Name, so the fix walks the page for them.
            LoanCalculatorMaui.Extensions.SyncfusionIosTouchFix.ApplyToExpanders(this);

#endif
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

    private void LoadDataSet()
    {
        try
        {
            // This used to read settingsdata.json via SharedServiceCore.LoadDataFile<SettingsViewModel>()
            // on every appearance and then DISCARD the result — the local was never used, under a
            // comment reading "placeholder for the data to be loaded". LoadDataFile has no side
            // effect beyond LocalStorage.Initialize(), so it was a file read plus a full
            // deserialisation of this view model bought for nothing. SettingsViewModel is a DI
            // singleton already populated by the time the page is reachable.
            //
            // With that gone nothing here touches disk, so the two Task.Runs and the Task.WhenAll
            // went with it: scheduling thread-pool work to assign one string costs more than the
            // assignment.
            if (_viewModel.SelectedTheme == null)
            {
                _viewModel.SelectedTheme = _viewModel.Themes.First(f => f == SharedServiceCore.DefaultAppTheme.ToString());
            }

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
        if (v >= SharedServiceCore.MinLoanTermYears) _viewModel.DefaultLoanTermYears = v;
    }

    private void OnDefaultLoanTermIncrement(object? sender, EventArgs e)
    {
        var v = _viewModel.DefaultLoanTermYears + 1;
        if (v <= _viewModel.MaxLoanTermYears) _viewModel.DefaultLoanTermYears = v;
    }

    private void OnMaxLoanTermDecrement(object? sender, EventArgs e)
    {
        var v = _viewModel.MaxLoanTermYears - 1;
        if (v >= SharedServiceCore.MinConfigurableMaxLoanTermYears) _viewModel.MaxLoanTermYears = v;
    }

    private void OnMaxLoanTermIncrement(object? sender, EventArgs e)
    {
        var v = _viewModel.MaxLoanTermYears + 1;
        if (v <= SharedServiceCore.AbsoluteMaxLoanTermYears) _viewModel.MaxLoanTermYears = v;
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