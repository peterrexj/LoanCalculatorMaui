//using LoanCalculator.Core.Services;
//using Moq;
//using Plugin.InAppBilling;

//namespace LoanCalculator.Tests.Services
//{
//    [TestFixture]
//    public class InAppPurchaseServiceTests
//    {
//        private readonly Mock<IErrorHandlingService> _errorHandlingService = new();
//        private readonly Mock<IAlertService> _alertService = new();

//        private InAppPurchaseService CreateService(Mock<IInAppBilling>? billingMock = null)
//        {
//            return new InAppPurchaseService(
//                _errorHandlingService.Object,
//                _alertService.Object,
//                billingMock != null ? billingMock.Object : new Mock<IInAppBilling>().Object
//            );
//        }

//        private void SetupBillingConnect(Mock<IInAppBilling> billingMock, bool connected = true)
//        {
//            billingMock.Setup(x => x.ConnectAsync()).ReturnsAsync(connected);
//            billingMock.SetupGet(x => x.IsConnected).Returns(connected);
//        }

//        [Test]
//        public async Task PurchaseProductAsync_SuccessfulPurchase_ReturnsSuccess()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock
//                .Setup(x => x.PurchaseAsync(It.IsAny<string>(), It.IsAny<ItemType>(), It.IsAny<string>()))
//                .ReturnsAsync(new InAppBillingPurchase { State = PurchaseState.Purchased, PurchaseToken = "token" });
//            billingMock.Setup(x => x.FinalizePurchaseAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
//                .ReturnsAsync(new[] { new InAppBillingPurchase { Success = true} });

//            var service = CreateService(billingMock);
//            var result = await service.PurchaseProductAsync("product_id");

//            Assert.That(result?.Success, Is.True);
//        }

//        [Test]
//        public async Task PurchaseProductAsync_NoNetwork_ReturnsFailure()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock, false);

//            var service = CreateService(billingMock);
//            var result = await service.PurchaseProductAsync("product_id");

//            Assert.That(result?.Success, Is.False);
//            Assert.That(result?.ErrorMessage, Is.EqualTo("No Network!"));
//        }

//        [Test]
//        public async Task PurchaseProductAsync_UserCancelled_ReturnsFailure()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.PurchaseAsync(It.IsAny<string>(), ItemType.InAppPurchase, It.IsAny<string>()))
//                .ReturnsAsync((InAppBillingPurchase?)null);

//            var service = CreateService(billingMock);
//            var result = await service.PurchaseProductAsync("product_id");

//            Assert.That(result?.Success, Is.False);
//        }

//        [Test]
//        public async Task PurchaseProductAsync_PaymentPending_ReturnsPending()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.PurchaseAsync(It.IsAny<string>(), ItemType.InAppPurchase, It.IsAny<string>()))
//                .ReturnsAsync(new InAppBillingPurchase { State = PurchaseState.PaymentPending });

//            var service = CreateService(billingMock);
//            var result = await service.PurchaseProductAsync("product_id");

//            Assert.That(result?.Success, Is.False);
//            Assert.That(result?.ErrorMessage, Does.Contain("processing").IgnoreCase);
//        }

//        [Test]
//        public async Task PurchaseProductAsync_FailedPurchase_ReturnsFailure()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.PurchaseAsync(It.IsAny<string>(), ItemType.InAppPurchase, It.IsAny<string>()))
//                .ReturnsAsync(new InAppBillingPurchase { State = PurchaseState.Failed });

//            var service = CreateService(billingMock);
//            var result = await service.PurchaseProductAsync("product_id");

//            Assert.That(result?.Success, Is.False);
//            Assert.That(result?.ErrorMessage, Does.Contain("failed").IgnoreCase);
//        }

//        [Test]
//        public async Task PurchaseProductAsync_Exception_ReturnsFailure()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.PurchaseAsync(It.IsAny<string>(), ItemType.InAppPurchase, It.IsAny<string>()))
//                .ThrowsAsync(new Exception("Test exception"));

//            var service = CreateService(billingMock);
//            var result = await service.PurchaseProductAsync("product_id");

//            Assert.That(result?.Success, Is.False);
//            Assert.That(result?.ErrorMessage, Is.EqualTo("Test exception"));
//        }

//        [Test]
//        public async Task RestorePurchasesAsync_NoPurchases_ReturnsFalse()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.GetPurchasesAsync(ItemType.InAppPurchase))
//                .ReturnsAsync((IEnumerable<InAppBillingPurchase>?)null);

//            var service = CreateService(billingMock);
//            var result = await service.RestorePurchasesAsync("product_id");

//            Assert.That(result, Is.False);
//        }

//        [Test]
//        public async Task RestorePurchasesAsync_PaymentPending_ReturnsFalse()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.GetPurchasesAsync(ItemType.InAppPurchase))
//                .ReturnsAsync(new[] { new InAppBillingPurchase { ProductId = "product_id", State = PurchaseState.PaymentPending } });

//            var service = CreateService(billingMock);
//            var result = await service.RestorePurchasesAsync("product_id");

//            Assert.That(result, Is.False);
//        }

//        [Test]
//        public async Task RestorePurchasesAsync_Exception_ReturnsFalse()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.GetPurchasesAsync(ItemType.InAppPurchase))
//                .ThrowsAsync(new Exception("Test exception"));

//            var service = CreateService(billingMock);
//            var result = await service.RestorePurchasesAsync("product_id");

//            Assert.That(result, Is.False);
//        }

//        [Test]
//        public async Task IsProductPurchasedAsync_ProductNotPurchased_ReturnsFalse()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.GetPurchasesAsync(ItemType.InAppPurchase))
//                .ReturnsAsync(new List<InAppBillingPurchase>());

//            var service = CreateService(billingMock);
//            var result = await service.IsProductPurchasedAsync("product_id");

//            Assert.That(result.Success, Is.False);
//        }

//        [Test]
//        public async Task IsProductPurchasedAsync_ProductPurchased_ReturnsTrue()
//        {
//            var billingMock = new Mock<IInAppBilling>();
//            SetupBillingConnect(billingMock);
//            billingMock.Setup(x => x.GetPurchasesAsync(ItemType.InAppPurchase))
//                .ReturnsAsync(new List<InAppBillingPurchase> { new InAppBillingPurchase { ProductId = "product_id" } });

//            var service = CreateService(billingMock);
//            var result = await service.IsProductPurchasedAsync("product_id");

//            Assert.That(result.Success, Is.True);
//        }

//        [Test]
//        public async Task CheckPendingPurchasesAsync_NoPending_ReturnsFalse()
//        {
//            var service = CreateService();
//            var result = await service.CheckPendingPurchasesAsync(false);
//            Assert.That(result, Is.False);
//        }
//    }
//}