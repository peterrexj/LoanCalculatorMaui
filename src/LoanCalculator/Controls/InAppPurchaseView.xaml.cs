using System.ComponentModel;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;

namespace LoanCalculatorMaui.Controls;

public partial class InAppPurchaseView : ContentView
{
    public event EventHandler? Dismissed;

    private readonly InAppPurchaseViewModel _viewModel;


    public InAppPurchaseView() : this(ServiceLocator.GetService<IErrorHandlingService>(), ServiceLocator.GetService<InAppPurchaseViewModel>())
    {
    }

    public InAppPurchaseView(IErrorHandlingService errorHandlingService, InAppPurchaseViewModel viewModel)
	{
		InitializeComponent();

        _viewModel = viewModel;

        BindingContext = _viewModel;
    }

    // Define the bindable property
    public static readonly BindableProperty ShowPremiumBuyWindowProperty =
        BindableProperty.Create(
            nameof(ShowPremiumBuyWindow), // Property name
            typeof(bool),                 // Property type
            typeof(InAppPurchaseView),    // Declaring type
            false,             // Default value
            propertyChanged: OnShowPremiumBuyWindowChanged // Property changed callback
        );

    // CLR property wrapper
    public bool ShowPremiumBuyWindow
    {
        get => (bool)GetValue(ShowPremiumBuyWindowProperty);
        set => SetValue(ShowPremiumBuyWindowProperty, value);
    }

    // Property changed callback
    private static void OnShowPremiumBuyWindowChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is InAppPurchaseView view && newValue is bool showPremiumBuyWindow)
        {
            view._viewModel.ShowPremiumBuyWindow = showPremiumBuyWindow;
            // Handle the property change logic here
            // For example, show or hide a UI element based on the value
            if (showPremiumBuyWindow)
            {

                // Logic to show the premium buy window
            }
            else
            {
                // Logic to hide the premium buy window
            }
        }
    }

    private void SfPopup_OnClosing(object? sender, CancelEventArgs e)
    {
        _viewModel.ShowPremiumBuyWindow = false;
        ShowPremiumBuyWindow = false;
        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    private void IgnoreOffer_OnClicked(object? sender, EventArgs e)
    {
        _viewModel.ShowPremiumBuyWindow = false;
    }
}