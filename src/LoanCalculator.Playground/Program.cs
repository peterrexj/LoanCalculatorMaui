// See https://aka.ms/new-console-template for more information

using LoanCalculator.Core.Pdf;
using LoanCalculator.Core.Services;
using LoanCalculator.Playground;

Console.WriteLine("Hello, Loan Calculator Playground service!");

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWH1ccXVSQ2dcV0Z0W0A=");

// Assign the mock ServiceProvider
ServiceLocator.ServiceProvider = MockServiceProvider.CreateMockServiceProvider();

// Example usage
var errorHandlingService = ServiceLocator.GetService<IErrorHandlingService>();
errorHandlingService.HandleException(new Exception("test error!"));

Console.WriteLine("Mock services are ready!");

var fontProvider = new FileSystemFontProvider();

await new PdfInsightsGenerator(fontProvider).GeneratePdf("Loan Affordability Calculator");