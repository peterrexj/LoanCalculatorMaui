using Plugin.InAppBilling;
using System.Diagnostics;

namespace LoanCalculator.Core.Services
{
    public interface IInAppPurchaseService
    {
        /// <summary>
        /// Initiates the purchase process for the specified product.
        /// </summary>
        /// <param name="productId">The unique identifier of the product to purchase.</param>
        /// <returns>True if the purchase was successful or restored; otherwise, false.</returns>
        Task<PurchaseResult?> PurchaseProductAsync(string productId);

        /// <summary>
        /// Attempts to restore previously purchased products for the user.
        /// </summary>
        /// <returns>True if any purchases were restored; otherwise, false.</returns>
        Task<bool> RestorePurchasesAsync(string productId);

        /// <summary>
        /// Checks if the specified product has already been purchased.
        /// </summary>
        /// <param name="productId">The unique identifier of the product to check.</param>
        /// <returns>True if the product is purchased; otherwise, false.</returns>
        Task<PurchaseResult> IsProductPurchasedAsync(string productId);

        /// <summary>
        /// Checks and handles any pending purchases.
        /// </summary>
        Task<bool> CheckPendingPurchasesAsync(bool iscCheckingOnAppLoad);
    }

    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class InAppPurchaseService : IInAppPurchaseService
    {
        private const string AlertTitlePurchase = "Purchase";
        private const string AlertButtonOk = "Ok";

        private readonly IErrorHandlingService errorHandlingService;
        private readonly IAlertService alertService;
        private readonly IInAppBilling billing;

        public InAppPurchaseService(
            IErrorHandlingService errorHandlingService,
            IAlertService alertService,
            IInAppBilling? billing = null)
        {
            this.errorHandlingService = errorHandlingService;
            this.alertService = alertService;
            this.billing = billing ?? CrossInAppBilling.Current;
        }

        private string GetPurchaseResultCustomerMessage(PurchaseState purchase)
        {
            string customerMessage = purchase switch
            {
                PurchaseState.Purchased => "Purchase successful! Thank you for your support.",
                PurchaseState.Canceled => "Purchase was canceled by the user.",
                PurchaseState.Deferred => "Purchase is awaiting approval (e.g., parental approval required).",
                PurchaseState.Failed => "Purchase failed. Please try again.",
                PurchaseState.PaymentPending => "Payment is pending. Please wait for confirmation.",
                PurchaseState.Purchasing => "Purchase is being processed. Please wait...",
                PurchaseState.Unknown => "Purchase state is unknown. Please try again later.",
                PurchaseState.Restored => "Purchase restored successfully.",
                _ => "Unknown purchase state, please try again after sometime."
            };
            return customerMessage;
        }

        private async Task<PurchaseResult?> HandlePurchased(IInAppBilling billing, InAppBillingPurchase purchase)
        {
            try
            {
                var results = await billing.FinalizePurchaseAsync(
                    new[] { purchase.PurchaseToken },
                    CancellationToken.None
                );

                bool allSucceeded = results.All(r => r.Success);

                if (!allSucceeded)
                {
                    return new PurchaseResult
                    {
                        Success = false,
                        ErrorMessage = "Purchase successful but failed to acknowledge the purchase. Please try restore after a while."
                    };
                }
                else
                {
                    // Clear stored details after successful acknowledgment/consumption
                    await SecureStorage.SetAsync("PendingProductId", string.Empty);

                    return new PurchaseResult { Success = true, ErrorMessage = "Purchase successful. Thank you for your support." };
                }
            }
            catch (Exception e)
            {
                return new PurchaseResult
                {
                    Success = false,
                    ErrorMessage = "Purchase successful but failed to acknowledge the purchase. Please try restore after a while."
                };
            }
        }

        private async Task<PurchaseResult?> HandlePending(string productId)
        {
            await SecureStorage.SetAsync("PendingProductId", productId);

            return new PurchaseResult
            {
                Success = false,
                ErrorMessage = "Your payment is currently processing. You'll get access to your purchase shortly. Please check back in a few minutes."
            };
        }

        private async Task<PurchaseResult?> HandleNoNetwork()
        {
            await alertService.ShowAlertAsync(AlertTitlePurchase, "No Network!", AlertButtonOk);
            return new PurchaseResult { Success = false, ErrorMessage = "No Network!" };
        }

        /// <inheritdoc/>
        public async Task<PurchaseResult?> PurchaseProductAsync(string productId)
        {
            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    return await HandleNoNetwork();
                }

                var purchase = await billing.PurchaseAsync(productId, ItemType.InAppPurchase, "apppayload");

                var message = GetPurchaseResultCustomerMessage(purchase?.State ?? PurchaseState.Unknown);

                if (purchase == null)
                {
                    await alertService.ShowAlertAsync(AlertTitlePurchase, message, AlertButtonOk);
                    return new PurchaseResult { Success = false, ErrorMessage = message }; // user cancelled
                }

                PurchaseResult? purchaseResult = null;

                if (purchase.State is PurchaseState.PaymentPending or PurchaseState.Purchasing)
                {
                    purchaseResult = await HandlePending(productId);
                }
                else if (purchase.State == PurchaseState.Purchased)
                {
                    purchaseResult = await HandlePurchased(billing, purchase);
                }
                else
                {
                    purchaseResult = new PurchaseResult
                    {
                        Success = false,
                        ErrorMessage = message
                    };
                }

                await alertService.ShowAlertAsync(AlertTitlePurchase,
                    purchaseResult?.ErrorMessage ?? "Unknown purchase state, please try again after sometime.",
                    AlertButtonOk);
                return purchaseResult;
            }
            catch (InAppBillingPurchaseException ex)
            {
                await alertService.ShowAlertAsync(AlertTitlePurchase,
                    "Your payment couldn't be completed. Please try again. Already purchased? Tap \"Restore Purchase.\"",
                    AlertButtonOk);
                return new PurchaseResult { Success = false, ErrorMessage = ex.Message };
            }
            catch (Exception ex)
            {
                errorHandlingService.HandleException(ex, $"In-App Billing Error: {ex.GetType().Name} {ex.StackTrace}");
                return new PurchaseResult { Success = false, ErrorMessage = ex.Message };
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RestorePurchasesAsync(string productId)
        {
            InAppBillingPurchase? billingPurchase = null;
            try
            {
                var pendingProductId = await SecureStorage.GetAsync("PendingProductId");

                if (string.IsNullOrEmpty(pendingProductId) == false)
                {
                    return await CheckPendingPurchasesAsync(iscCheckingOnAppLoad: false);
                }
                else
                {
                    var connected = await billing.ConnectAsync();
                    if (!connected)
                    {
                        await HandleNoNetwork();
                        return false; // No network connection, handle it gracefully
                    }

                    var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                    billingPurchase = purchases?.FirstOrDefault(p => p.ProductId == productId);

                    if (purchases == null || purchases?.Count() == 0)
                    {
                        await alertService.ShowAlertAsync(AlertTitlePurchase, "No purchase to restore.", AlertButtonOk);
                        return false; // No purchases found, handle it gracefully
                    }
                    if (billingPurchase != null && billingPurchase.State == PurchaseState.PaymentPending)
                    {
                        // Inform the user: payment is still pending
                        await alertService.ShowAlertAsync(AlertTitlePurchase, "Your payment is currently processing. You'll get access to your purchase shortly. Please check back in a few minutes.", AlertButtonOk);
                        return false; // Payment is pending, handle it gracefully
                    }
                    if (billingPurchase != null && billingPurchase.State == PurchaseState.Purchased)
                    {
                        var result = await HandlePurchased(billing, billingPurchase);
                        if (result is { Success: true })
                        {
                            SharedServiceCore.UpdateToPremium();
                            await alertService.ShowAlertAsync(AlertTitlePurchase, "Your purchase has been successfully restored.", AlertButtonOk);
                            return true; // Purchase restored successfully
                        }
                        else
                        {
                            await alertService.ShowAlertAsync(AlertTitlePurchase, result?.ErrorMessage ?? "An error occurred while processing your restore.", AlertButtonOk);
                            return false; // Error occurred while processing purchase
                        }
                    }
                    else
                    {
                        await alertService.ShowAlertAsync(AlertTitlePurchase, "No purchase to restore.", AlertButtonOk);
                    }
                }

                return false; // No valid purchase found, handle it gracefully
            }
            catch (InAppBillingPurchaseException ex)
            {
                await alertService.ShowAlertAsync(AlertTitlePurchase,
                    "We couldn't complete your restore at this time. Please try again later.",
                    AlertButtonOk);
                return false;
            }
            catch (Exception ex)
            {
                string orderId = billingPurchase?.Id ?? "Unknown Order ID";
                errorHandlingService.HandleException(ex, $"In-App Purchase Check Failed: OrderID: {orderId}, error: {ex.Message}");
                return false;
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<PurchaseResult> IsProductPurchasedAsync(string productId)
        {
            try
            {
                var connected = await billing.ConnectAsync();

                if (!connected)
                    return new PurchaseResult { Success = false, ErrorMessage = "No Network!" };

                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                var purchaseStatus = purchases?.Any(p => p.ProductId == productId) ?? false;
                return new PurchaseResult { Success = purchaseStatus, ErrorMessage = purchaseStatus ? null : "Product not purchased." };
            }
            catch (Exception ex)
            {
                errorHandlingService.HandleException(ex, $"In-App Purchase Check Failed: {ex.Message}");
                return new PurchaseResult { Success = false, ErrorMessage = ex.Message };
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CheckPendingPurchasesAsync(bool iscCheckingOnAppLoad)
        {
            InAppBillingPurchase? billingPurchase = null;
            try
            {
                var pendingProductId = await SecureStorage.GetAsync("PendingProductId");

                if (string.IsNullOrEmpty(pendingProductId))
                    return false;

                var connected = billing.IsConnected || await billing.ConnectAsync();
                if (!connected)
                {
                    await HandleNoNetwork(); // No network connection, handle it gracefully
                    return false;
                }

                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                billingPurchase = purchases?.FirstOrDefault(p => p.ProductId == pendingProductId);

                if (billingPurchase == null)
                {
                    // No matching purchase found, clear stored details
                    await SecureStorage.SetAsync("PendingProductId", string.Empty);
                    return false;
                }

                if (billingPurchase.State == PurchaseState.PaymentPending)
                {
                    if (iscCheckingOnAppLoad == false) 
                    {
                        // Inform the user: payment is still pending
                        await alertService.ShowAlertAsync(AlertTitlePurchase, "Your payment is still pending. Access will be granted once payment completes.", AlertButtonOk);
                    }
                    return false;
                }
                else if (billingPurchase.State == PurchaseState.Purchased)
                {
                    var result = await HandlePurchased(billing, billingPurchase);
                    if (result is { Success: true })
                    {
                        SharedServiceCore.UpdateToPremium();
                        await alertService.ShowAlertAsync(AlertTitlePurchase, "Your purchase has been successfully processed.", AlertButtonOk);
                        return true;
                    }
                    else
                    {
                        await alertService.ShowAlertAsync(AlertTitlePurchase, result?.ErrorMessage ?? "An error occurred while processing your purchase.", AlertButtonOk);
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                string orderId = billingPurchase?.Id ?? "Unknown Order ID";
                errorHandlingService.HandleException(ex, $"In-App Purchase Check Failed: OrderID: {orderId}, error: {ex.Message}");
                return false;
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }
    }
}
