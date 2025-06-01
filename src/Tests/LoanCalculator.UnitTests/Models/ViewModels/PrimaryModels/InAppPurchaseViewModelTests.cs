using System.Threading.Tasks;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using LoanCalculatorMaui.Services;
using Moq;
using NUnit.Framework;

namespace LoanCalculator.Tests.Models.ViewModels.PrimaryModels
{
    [TestFixture]
    public class InAppPurchaseViewModelTests
    {
        [Test]
        public async Task PurchaseProductAsync_Success_UpdatesPremiumAndClosesWindow()
        {
            var inAppPurchaseService = new Mock<IInAppPurchaseService>();
            var alertService = new Mock<IAlertService>();
            var appInfo = new Mock<IAppInformation>();
            appInfo.SetupGet(x => x.InAppProductId).Returns("test_product");
            inAppPurchaseService.Setup(x => x.PurchaseProductAsync("test_product"))
                .ReturnsAsync(new PurchaseResult { Success = true });

            var vm = new InAppPurchaseViewModel(inAppPurchaseService.Object, alertService.Object, appInfo.Object);

            await (Task)vm.GetType().GetMethod("PurchaseProductAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.That(vm.ShowPremiumBuyWindow, Is.False);
        }

        [Test]
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

            await (Task)vm.GetType().GetMethod("PurchaseProductAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.That(vm.ShowPremiumBuyWindow, Is.True);
        }

        [Test]
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

            await (Task)vm.GetType().GetMethod("PurchaseProductAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.That(vm.ShowPremiumBuyWindow, Is.True);
        }

        [Test]
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

            await (Task)vm.GetType().GetMethod("RestorePurchasesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.That(vm.ShowPremiumBuyWindow, Is.False);
        }

        [Test]
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

            await (Task)vm.GetType().GetMethod("RestorePurchasesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(vm, null);

            Assert.That(vm.ShowPremiumBuyWindow, Is.True);
        }
    }
}