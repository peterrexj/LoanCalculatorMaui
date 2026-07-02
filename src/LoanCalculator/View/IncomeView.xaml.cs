using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.TabView;

namespace LoanCalculatorMaui.View;

public partial class IncomeView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly IAlertService _alertService;
    private IncomeViewModel _viewModel;
    private readonly IThemeHandler _themeHandler;

    // FAB drag — upward only, max travel = double the button height (112px)
    private double _fabY;

    public IncomeView(
        IErrorHandlingService errorHandlingService,
        IAlertService alertService,
        IncomeViewModel viewModel,
        IThemeHandler themeHandler)
    {
        InitializeComponent();

        _errorHandlingService = errorHandlingService;
        _alertService = alertService;
        _themeHandler = themeHandler;
        _viewModel = viewModel;
        _viewModel.IsUpdating = true;
        _viewModel.IsPageBusy = true;

        BindingContext = _viewModel;
    }

    private bool _hasLoadedOnce;



    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
        SharedServiceCore.MarkIncomeDirty();
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            await Task.Delay(100);

            if (!_hasLoadedOnce)
            {
                _hasLoadedOnce = true;
                await LoadDataSet();
            }
            else
            {
                // Re-fetch chart colors so a theme change is reflected on return.
                _viewModel.CustomChartColors = _themeHandler.GetChartColors();
                _viewModel.RefreshIncomePropertyChanged();
                _viewModel.TriggerPropertyChangedOnProjectionTab();
            }

            // Re-apply slider colors after a possible theme change (Syncfusion caches these).
            LoanCalculatorMaui.Extensions.SliderThemeRefresher.Refresh(this);
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private async Task LoadDataSet()
    {
        try
        {
            PageHelper.PageIsLoading();

            var viewModelInitializeTask = Task.Run(async () =>
            {
                var data = await SharedServiceCore.LoadDataFile<IncomeViewModel>();

                if (data == null)
                    _viewModel.AddDefaultToExpenses();
                else if (data is { TransactionRecords: null })
                    _viewModel.AddDefaultToExpenses();
                else
                    _viewModel.CopyPropertiesFrom(data);

                if (_viewModel?.TransactionRecords == null)
                    _viewModel.AddDefaultToExpenses();

                _viewModel.InitializeViewData();
                _viewModel.IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
                _viewModel.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
            });

            var expenseSummaryTask = SharedServiceCore.GetExpenseSummaryAsync();
            var loanDataTask = SharedServiceCore.GetLoanViewModelAsync();
            var chartColorsTask = Task.Run(() => _themeHandler.GetChartColors());

            await Task.WhenAll(viewModelInitializeTask, expenseSummaryTask, loanDataTask, chartColorsTask);

            _viewModel.CustomChartColors = chartColorsTask.Result;
            _viewModel.ExpenseSummary = expenseSummaryTask.Result;

            var loanData = loanDataTask.Result;
            _viewModel.PropertyExpenseSummary = loanData.Item1;
            _viewModel.PropertyPayment = loanData.Item2;

            if (SharedServiceCore.IsTrialUser && await SharedServiceCore.IsCurrentDayAsync() == false)
                _viewModel.AddDefaultToExpenses();

            _viewModel.CurrencySymbol = Helper.CurrencySymbol;
            _viewModel.MarkInitializationComplete();

            PageHelper.PageLoadingComplete();

            _viewModel.TriggerOneTimeUpdateOnPage();
            _viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
        finally
        {
            _viewModel.IsUpdating = false;
            _viewModel.IsPageBusy = false;
            PageHelper.PageLoadingComplete();
        }
    }

    private void autoComplete_Completed(object sender, EventArgs e) { }

    private void autoComplete_SelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        try
        {
            var text = autoComplete.SelectedItem as string ?? autoComplete.Text ?? string.Empty;
            _viewModel.SearchExpenseIncomeName = text.Trim();

            if (string.IsNullOrWhiteSpace(text))
                autoComplete.IsDropDownOpen = false;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    #region FAB

    private void OnFabPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        const double maxUp = -112;
        const double maxDown = 0;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                FabAddIncome.TranslationY = Math.Clamp(_fabY + e.TotalY, maxUp, maxDown);
                break;
            case GestureStatus.Completed:
                _fabY = FabAddIncome.TranslationY;
                break;
        }
    }

    private void OnAddIncomeFab_Clicked(object sender, EventArgs e)
    {
        _viewModel.ResetTransactionEntryData();
        _viewModel.IsAddFormVisible = true;
    }

    #endregion



    #region Transaction Entry

    private void AddNewIncome_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Surface inline validation errors only after a submit attempt.
            _viewModel.ShowValidationErrors = true;
            if (_viewModel.HasErrorIncomeDescription || _viewModel.HasErrorIncomeAmount) return;

            if (_viewModel.AddOrUpdateEntryFromView() == false) return;

            _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
            SharedServiceCore.MarkIncomeDirty();

            _viewModel.RefreshIncomePropertyChanged();
            _viewModel.UpdateProjectionData();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void ResetButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            _viewModel.ResetTransactionEntryData();
            _viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private async void btnEditEntry_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

            var entryData = _viewModel.TransactionRecords?.Get(Guid.Parse(button.AutomationId))?.DeepClone();
            if (entryData == null)
            {
                await _alertService.ShowAlertAsync("Error", "Unable to find the entry to edit.", "OK");
                return;
            }

            _viewModel.ShowValidationErrors = false;
            _viewModel.IncomeExpenseEntry = entryData;
            _viewModel.IncomeExpenseFrequencySelectedIndex = entryData.Frequency.ToString();
            _viewModel.RefreshIncomePropertyChanged();

            _viewModel.IsAddFormVisible = true;
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    private void btnDeleteEntry_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not SfButton button || !button.AutomationId.HasValue()) return;

            _viewModel.TransactionRecords.Delete(Guid.Parse(button.AutomationId));
            _viewModel.ResetTransactionEntryData();
            _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
            SharedServiceCore.MarkIncomeDirty();
            _viewModel.RefreshIncomePropertyChanged();
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }

    #endregion

    private void OnProjectionScrolled(object sender, ScrolledEventArgs e)
    {
        // No keyboard input on this tab — nothing to dismiss
    }

    private void OnGrowthRateIncrease(object sender, EventArgs e)
    {
        if (_viewModel.AnnualGrowthRate < 100)
            _viewModel.AnnualGrowthRate = Math.Min(100, Math.Round(_viewModel.AnnualGrowthRate + 1, 0));
    }

    private void OnGrowthRateDecrease(object sender, EventArgs e)
    {
        if (_viewModel.AnnualGrowthRate > 0)
            _viewModel.AnnualGrowthRate = Math.Max(0, Math.Round(_viewModel.AnnualGrowthRate - 1, 0));
    }

    private void TabView_OnSelectionChanging(object? sender, SelectionChangingEventArgs e)
    {
        try
        {
            if (e.Index == 1)
            {
                if (SharedServiceCore.IsTrialUser)
                {
                    PremiumWindow.ShowPremiumBuyWindow = true;
                    e.Cancel = true;
                }
                else
                {
                    _viewModel.UpdateProjectionData();
                    _viewModel.TriggerPropertyChangedOnProjectionTab();
                }
            }
        }
        catch (Exception ex)
        {
            _errorHandlingService.HandleException(ex);
        }
    }
}
