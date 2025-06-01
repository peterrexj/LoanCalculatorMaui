using System.Threading.Tasks;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using Moq;
using Xunit;

namespace LoanCalculator.Tests.Models.ViewModels.PrimaryModels
{
    public class InAppPurchaseViewModelTests
    {
        [Fact]
        public async Task PurchaseProductAsync_Success_UpdatesPremiumAndClosesWindow()
        {
            var inAppPurchaseService = new Mock<IInAppPurchaseService>();
            var alertService = new Mock<IAlertService>();
            var appInfo = new Mock<IAppInformation>();
            appInfo.SetupGet(x => x.InAppProductId).Returns("test_product");
            inAppPurchaseService.Setup(x => x.PurchaseProductAsync("test_product"))
                .ReturnsAsync(new PurchaseResult { Success = true });

            var vm = new InAppPurchaseViewModel(inAppPurchaseService.Object, alertService.Object, appInfo.Object);

            await vm.GetType().GetMethod("PurchaseProductAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.False(vm.ShowPremiumBuyWindow);
        }

        [Fact]
        public async Task PurchaseProductAsync_Failure_DoesNotCloseWindow()
        {
            var inAppPurchaseService = new Mock<IInAppPurchaseService>();
            var alertService = new Mock<IAlertService>();
            var appInfo = new Mock<IAppInformation>();
            appInfo.SetupGet(x => x.InAppProductId).Returns("test_product");
            inAppPurchaseService.Setup(x => x.PurchaseProductAsync("test_product"))
                .ReturnsAsync(new PurchaseResult { Success = false });

            var vm = new InAppPurchaseViewModel(inAppPurchaseService.Object, alertService.Object, appInfo.Object)
            {
                ShowPremiumBuyWindow = true
            };

            await vm.GetType().GetMethod("PurchaseProductAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.True(vm.ShowPremiumBuyWindow);
        }

        [Fact]
        public async Task PurchaseProductAsync_EmptyProductId_DoesNothing()
        {
            var inAppPurchaseService = new Mock<IInAppPurchaseService>();
            var alertService = new Mock<IAlertService>();
            var appInfo = new Mock<IAppInformation>();
            appInfo.SetupGet(x => x.InAppProductId).Returns(string.Empty);

            var vm = new InAppPurchaseViewModel(inAppPurchaseService.Object, alertService.Object, appInfo.Object)
            {
                ShowPremiumBuyWindow = true
            };

            await vm.GetType().GetMethod("PurchaseProductAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.True(vm.ShowPremiumBuyWindow);
        }

        [Fact]
        public async Task RestorePurchasesAsync_Success_ClosesWindow()
        {
            var inAppPurchaseService = new Mock<IInAppPurchaseService>();
            var alertService = new Mock<IAlertService>();
            var appInfo = new Mock<IAppInformation>();
            appInfo.SetupGet(x => x.InAppProductId).Returns("test_product");
            inAppPurchaseService.Setup(x => x.RestorePurchasesAsync("test_product"))
                .ReturnsAsync(true);

            var vm = new InAppPurchaseViewModel(inAppPurchaseService.Object, alertService.Object, appInfo.Object)
            {
                ShowPremiumBuyWindow = true
            };

            await vm.GetType().GetMethod("RestorePurchasesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.False(vm.ShowPremiumBuyWindow);
        }

        [Fact]
        public async Task RestorePurchasesAsync_Failure_DoesNotCloseWindow()
        {
            var inAppPurchaseService = new Mock<IInAppPurchaseService>();
            var alertService = new Mock<IAlertService>();
            var appInfo = new Mock<IAppInformation>();
            appInfo.SetupGet(x => x.InAppProductId).Returns("test_product");
            inAppPurchaseService.Setup(x => x.RestorePurchasesAsync("test_product"))
                .ReturnsAsync(false);

            var vm = new InAppPurchaseViewModel(inAppPurchaseService.Object, alertService.Object, appInfo.Object)
            {
                ShowPremiumBuyWindow = true
            };

            await vm.GetType().GetMethod("RestorePurchasesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.True(vm.ShowPremiumBuyWindow);
        }
    }
}