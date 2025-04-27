using Plugin.InAppBilling;
using System.Diagnostics;

namespace LoanCalculator.Core.Services
{
    public interface IInAppPurchaseService
    {
        Task<bool> PurchaseProductAsync(string productId);
        Task<bool> RestorePurchasesAsync();
        Task<bool> IsProductPurchasedAsync(string productId);

    }

    public class InAppPurchaseService : IInAppPurchaseService
    {
        public async Task<bool> PurchaseProductAsync(string productId)
        {
            try
            {
                var billing = CrossInAppBilling.Current;

                var connected = await billing.ConnectAsync();
                if (!connected)
                    return false;

                var purchase = await billing.PurchaseAsync(productId, ItemType.InAppPurchase, "apppayload");

                if (purchase == null)
                    return false; // user cancelled

                return purchase.State is PurchaseState.Purchased or PurchaseState.Restored;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"In-App Billing Error: {ex.Message}");
                return false;
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }

        public async Task<bool> RestorePurchasesAsync()
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var connected = await billing.ConnectAsync();
                if (!connected)
                    return false;

                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                return purchases?.Any() ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IAP] Restore Failed: {ex.Message}");
                return false;
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }

        public async Task<bool> IsProductPurchasedAsync(string productId)
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var connected = await billing.ConnectAsync();
                if (!connected)
                    return false;
                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                return purchases?.Any(p => p.ProductId == productId) ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IAP] Check Purchase Failed: {ex.Message}");
                return false;
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }
    }

}
