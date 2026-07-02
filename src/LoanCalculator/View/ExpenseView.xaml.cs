using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculator.Core.Themes;
using LoanCalculatorMaui.Extensions;
using Pj.Library;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.TabView;

namespace LoanCalculatorMaui.View;

public partial class ExpenseView : ContentPage
{
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly IAlertService _alertService;
    private ExpenseViewModel _viewModel;
    private readonly IThemeHandler _themeHandler;

    public ExpenseView(
        IErrorHandlingService errorHandlingService,
        IAlertService alertService,
        ExpenseViewModel expenseViewModel,
        IThemeHandler themeHandler)
    {
        InitializeComponent();

        _errorHandlingService = errorHandlingService;
        _alertService = alertService;
        _themeHandler = themeHandler;
        _viewModel = expenseViewModel;
        _viewModel.IsUpdating = true;
        _viewModel.IsPageBusy = true;

        BindingContext = _viewModel;

        //_viewModel = new ExpenseViewModel(_errorHandlingService, _alertService) { IsUpdating = true };
    }

    private bool _hasLoadedOnce;



    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
        SharedServiceCore.MarkExpenseDirty();
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
                // Subsequent visits — re-fetch chart colors (theme may have changed), re-fire UI
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
                //if (!_viewModel.HasInitialized)
                //{
                var data = await SharedServiceCore.LoadDataFile<ExpenseViewModel>();

                if (data == null)
                {
                    _viewModel.AddDefaultToExpenses();
                }
                else if (data is { TransactionRecords: null })
                {
                    _viewModel.AddDefaultToExpenses();
                }
                else
                {
                    _viewModel.CopyPropertiesFrom(data);
                }
                //}
                if (_viewModel?.TransactionRecords == null)
                {
                    _viewModel!.AddDefaultToExpenses();
                }

                _viewModel.InitializeViewData();
                _viewModel.IncomeExpenseEntry.Frequency = TimeFrequencyEnum.Monthly;
                _viewModel.IncomeExpenseFrequencySelectedIndex = TimeFrequencyEnum.Monthly.ToString();
            });


            var incomeSummaryTask = SharedServiceCore.GetIncomeSummaryAsync();
            var loanDataTask = SharedServiceCore.GetLoanViewModelAsync();
            var chartColorsTask = Task.Run(() => _themeHandler.GetChartColors());

            await Task.WhenAll(viewModelInitializeTask, incomeSummaryTask, loanDataTask, chartColorsTask);

            _viewModel.CustomChartColors = chartColorsTask.Result;
            _viewModel.IncomeSummary = incomeSummaryTask.Result;

            var loanData = loanDataTask.Result;
            _viewModel.PropertyExpenseSummary = loanData.Item1;
            _viewModel.PropertyPayment = loanData.Item2;

            if (SharedServiceCore.IsTrialUser && await SharedServiceCore.IsCurrentDayAsync() == false)
            {
                _viewModel.AddDefaultToExpenses();
            }

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
            // Autocomplete now uses plain strings — just pass text directly to the filter
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

    #region Transaction Entry



    // FAB drag — upward only, max travel = double the button height (112px)
    private double _fabY;

    private void OnFabPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        const double maxUp = -112;  // 56 * 2
        const double maxDown = 0;   // cannot go below default position

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                FabAddExpense.TranslationY = Math.Clamp(_fabY + e.TotalY, maxUp, maxDown);
                break;
            case GestureStatus.Completed:
                _fabY = FabAddExpense.TranslationY;
                break;
        }
    }

    private void OnAddExpenseFab_Clicked(object sender, EventArgs e)
    {
        // Reset form to add mode then open popup
        _viewModel.ResetTransactionEntryData();
        _viewModel.IsAddFormVisible = true;
    }

    private void AddNewIncome_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Surface inline validation errors only after a submit attempt.
            _viewModel.ShowValidationErrors = true;
            if (_viewModel.HasErrorIncomeDescription || _viewModel.HasErrorIncomeAmount) return;

            if (_viewModel.AddOrUpdateEntryFromView() == false) return;

            _viewModel.FlushPendingSave(() => SharedServiceCore.SaveData(_viewModel));
            SharedServiceCore.MarkExpenseDirty();

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
            _viewModel.RefreshTransactionEntry();
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
            _viewModel.RefreshTransactionEntry();

            // Open popup first, then re-notify frequency so the DataTemplate picks up the value
            // after SfPopup has inflated its ContentTemplate content.
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
            SharedServiceCore.MarkExpenseDirty();
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
        // Nothing needed — no keyboard input on this tab anymore
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

    private void TabView_OnSelectionChanging(object? sender, SelectionChangingEventArgs e)    {
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