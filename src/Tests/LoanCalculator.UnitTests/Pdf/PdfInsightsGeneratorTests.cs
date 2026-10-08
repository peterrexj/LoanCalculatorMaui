using LoanCalculator.Core.Helper;
using LoanCalculator.Core.Models;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Pdf;
using LoanCalculator.Core.Services;
using Moq;
using Syncfusion.Pdf.Parsing;

namespace LoanCalculator.UnitTests.Pdf
{
    /// <summary>
    /// Generates real PDFs from known inputs and asserts on the text extracted from them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The PDF had no automated coverage at all, which mattered because
    /// <see cref="PdfInsightsGenerator.GeneratePdf"/> wraps everything in
    /// <c>catch { ErrorHandlingService.HandleException(e); }</c> — a broken report produces no file
    /// and no error anywhere a user or a test would see. <c>Generation_Succeeded</c> is therefore
    /// the first assertion for every scenario.
    /// </para>
    /// <para>
    /// Written while fixing TECH-DEBT D1, which replaced the generator's "mutate the shared
    /// records, read them back" approach with pure arithmetic.
    /// </para>
    /// <para><b>The fixture values are deliberately awkward.</b> With round numbers (9,000 income,
    /// 2,500 expenses) the net 6,500 also appears as 78,000/12 elsewhere in the report, so an
    /// assertion on "6,500" passed even with the subtraction mutated to an addition. Values that
    /// cannot collide, plus the negative assertions below, are what give these tests teeth.</para>
    /// </remarks>
    [TestFixture]
    [NonParallelizable]
    public class PdfInsightsGeneratorTests
    {
        /// <summary>One generated report plus everything needed to assert against it.</summary>
        private sealed record Report(string Text, int Bytes, List<Exception> Errors);

        private const double LoanAmount = 560_000;

        // A property running cost, on the LOAN's own records. Needed for coverage, not colour:
        // with an empty loan record set, removing the loan recompute added in D1a changed nothing
        // and the mutation went undetected.
        private const double MonthlyRunningCost = 417;

        // Income exceeds outgoings.
        private const double SurplusIncome = 9_137;
        private const double SurplusExpense = 2_513;

        // Outgoings exceed income. A negative net is the situation this report exists to warn
        // about, and it drives a separate path in IncomeExpenseScaling (negative shares) plus the
        // table's negative-cell shading.
        private const double ShortfallIncome = 3_011;
        private const double ShortfallExpense = 7_523;

        private static Report _surplus = null!;
        private static Report _shortfall = null!;
        private static IServiceProvider? _originalProvider;

        /// <summary>
        /// Resolves a file inside the repo from the test assembly's location, so these tests use
        /// the same real fonts and assets the app ships rather than fixtures that could drift.
        /// </summary>
        private static string RepoFile(params string[] parts)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src", "LoanCalculator", "Resources")))
                dir = dir.Parent;

            if (dir == null)
                throw new DirectoryNotFoundException(
                    "Could not locate the repo's Resources folder from " + AppContext.BaseDirectory);

            return Path.Combine(new[] { dir.FullName, "src", "LoanCalculator", "Resources" }.Concat(parts).ToArray());
        }

        private sealed class RepoFontProvider : IFontUnicodeProvider
        {
            public Stream LoadFont(string fileName) => File.OpenRead(RepoFile("Fonts", fileName));
        }

        private static LoanViewModel BuildLoan()
        {
            var vm = new LoanViewModel
            {
                HomeLoanInfo = new HomeLoanInformation
                {
                    HomeLoanRepaymentRequest = new HomeLoanRepaymentInput
                    { InterestRate = 5.0, LoanTermInYears = 30, TotalNumberPaymentPerYear = 12 },
                    PropertyAmount = 700_000
                },
                TransactionRecords = new Incomes { IncomeExpenseEntries = [] }
            };
            vm.HomeLoanInfo.LoanAmountDirectInput = LoanAmount;
            // Deliberately NOT summed — same reason as OneEntry.
            vm.TransactionRecords.Add("Maintenance", MonthlyRunningCost,
                TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            return vm;
        }

        private static Incomes OneEntry(string name, double monthly)
        {
            var records = new Incomes { IncomeExpenseEntries = [] };
            records.Add(name, monthly, TimeFrequencyEnum.Monthly, isCheckForExistingRequired: false);
            // Deliberately NOT summed: this is the shape a load from disk produces, and the
            // generator is responsible for recomputing it. See TECH-DEBT D1a.
            return records;
        }

        private static async Task<Report> Generate(double monthlyIncome, double monthlyExpense)
        {
            var errors = new List<Exception>();

            var errorService = new Mock<IErrorHandlingService>(MockBehavior.Loose);
            errorService.Setup(e => e.HandleException(It.IsAny<Exception>(), It.IsAny<string>()))
                .Callback((Exception ex, string _) => errors.Add(ex));

            var services = new Mock<IServiceProvider>(MockBehavior.Loose);
            services.Setup(s => s.GetService(typeof(IErrorHandlingService))).Returns(errorService.Object);
            ServiceLocator.ServiceProvider = services.Object;
            SharedServiceCore.ResetErrorHandlingService();

            var income = new IncomeViewModel { TransactionRecords = OneEntry("Salary", monthlyIncome) };
            var expense = new ExpenseViewModel { TransactionRecords = OneEntry("Rent", monthlyExpense) };

            byte[]? bytes = null;
            var storage = new Mock<ILocalStorage>(MockBehavior.Loose);
            storage.Setup(s => s.IsInitialized).Returns(true);
            storage.Setup(s => s.GetData<LoanViewModel>()).ReturnsAsync(BuildLoan());
            storage.Setup(s => s.GetData<IncomeViewModel>()).ReturnsAsync(income);
            storage.Setup(s => s.GetData<ExpenseViewModel>()).ReturnsAsync(expense);

            // RenderHeaderTemplate loads the logo through storage; a loose mock returns null and
            // the generator NREs, so serve the real asset.
            storage.Setup(s => s.LoadFileFromFileSystem(It.IsAny<string>()))
                .Returns((string name) => Task.FromResult<Stream>(File.OpenRead(RepoFile("Raw", name))));
            storage.Setup(s => s.SaveFileToFileSystem(It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .Returns((string _, MemoryStream stream) => { bytes = stream.ToArray(); return Task.CompletedTask; });

            SharedServiceCore.SetLocalStorage(storage.Object);

            await new PdfInsightsGenerator(new RepoFontProvider()).GeneratePdf("LoanCalc Test");

            var text = string.Empty;
            if (bytes is { Length: > 0 })
            {
                using var doc = new PdfLoadedDocument(bytes);
                var sb = new System.Text.StringBuilder();
                for (var i = 0; i < doc.Pages.Count; i++)
                    sb.Append(doc.Pages[i].ExtractText());
                text = sb.ToString();
            }

            return new Report(text, bytes?.Length ?? 0, errors);
        }

        private static (Report Report, double Income, double Expense) Case(string scenario) =>
            scenario == "surplus"
                ? (_surplus, SurplusIncome, SurplusExpense)
                : (_shortfall, ShortfallIncome, ShortfallExpense);

        [OneTimeSetUp]
        public async Task GenerateBothReports()
        {
            PageHelper.PageLoadingComplete();
            _originalProvider = ServiceLocator.ServiceProvider;

            _surplus = await Generate(SurplusIncome, SurplusExpense);
            _shortfall = await Generate(ShortfallIncome, ShortfallExpense);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            // Restore every static this fixture touched. ServiceLocator.ServiceProvider and the
            // cached IErrorHandlingService are process-wide, so leaving them set would serve a
            // recording mock to every fixture running afterwards — a test that should fail loudly
            // on an unhandled error would instead pass quietly.
            SharedServiceCore.ResetLocalStorage();
            SharedServiceCore.ResetErrorHandlingService();
            ServiceLocator.ServiceProvider = _originalProvider!;
            SharedServiceCore.LoadSafeOff();
            PageHelper.PageLoadingComplete();
        }

        // ── Generation itself ────────────────────────────────────────────────

        [Test]
        public void Generation_Succeeded([Values("surplus", "shortfall")] string scenario)
        {
            var report = Case(scenario).Report;

            Assert.That(report.Errors, Is.Empty,
                "GeneratePdf swallows exceptions into the error handler; one was raised: "
                + string.Join(" | ", report.Errors.Select(e => $"{e.GetType().Name}: {e.Message}")));
            Assert.That(report.Bytes, Is.GreaterThan(10_000), "no usable PDF was written");
            Assert.That(report.Text, Is.Not.Empty, "no text could be extracted from the PDF");
        }

        [Test]
        public void Report_HasItsTitleAndStaysWordedAsAnEstimate()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_surplus.Text, Does.Contain("Loan Insight Report"));
                Assert.That(_surplus.Text, Does.Contain("Estimate").IgnoreCase,
                    "store-compliance reframe: the report must not read as a verdict");
            });
        }

        [Test]
        public void Report_IncludesTheLoanAmount() =>
            Assert.That(_surplus.Text, Does.Contain($"{LoanAmount:N0}"));

        /// <summary>
        /// The loan's own running costs appear in the report.
        /// </summary>
        /// <remarks>
        /// Note this does NOT prove the loan summary was recomputed: these figures also reach the
        /// page from the table rows, which are built per entry and bypass the summary entirely.
        /// The summary-derived path is pinned by
        /// <c>PdfDataInsightsModelRecomputeTests</c> instead — a text assertion could not tell the
        /// two apart, and a test that cannot fail is worse than no test.
        /// </remarks>
        [Test]
        public void Report_IncludesThePropertyRunningCosts()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_surplus.Text, Does.Contain($"{MonthlyRunningCost:N0}"),
                    "the monthly running cost must appear");
                Assert.That(_surplus.Text, Does.Contain($"{MonthlyRunningCost * 12:N0}"),
                    "the annualised running cost must appear");
            });
        }

        // ── The figures the user entered ─────────────────────────────────────

        [Test]
        public void Report_IncludesTheIncomeAndExpenseAsEnteredAndAnnualised(
            [Values("surplus", "shortfall")] string scenario)
        {
            var (report, income, expense) = Case(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(report.Text, Does.Contain($"{income:N0}"), "monthly income");
                Assert.That(report.Text, Does.Contain($"{expense:N0}"), "monthly expense");
                Assert.That(report.Text, Does.Contain($"{income * 12:N0}"), "yearly income");
                Assert.That(report.Text, Does.Contain($"{expense * 12:N0}"),
                    "yearly expense — before D1a this came straight off disk and was never "
                    + "recomputed on the PDF path. Removing that recompute fails this assertion.");
            });
        }

        // ── The derived "after expenses" figures ─────────────────────────────

        /// <summary>
        /// The number D1 moved from mutate-then-read to plain subtraction. Mutating that
        /// subtraction to an addition is confirmed to fail this test.
        /// </summary>
        [Test]
        public void Report_IncludesIncomeAfterExpenses([Values("surplus", "shortfall")] string scenario)
        {
            var (report, income, expense) = Case(scenario);
            var netMonthly = income - expense;

            // Absolute values: a negative is rendered with the minus on the currency symbol
            // ("-$4,512"), so the digits appear unsigned. Report_ShowsAShortfallWithItsSign
            // covers the sign, and Report_NeverPrintsIncomePlusExpenses covers sign errors.
            Assert.Multiple(() =>
            {
                Assert.That(report.Text, Does.Contain($"{Math.Abs(netMonthly):N0}"),
                    $"income after expenses ({netMonthly:N0}) must appear");
                Assert.That(report.Text, Does.Contain($"{Math.Abs(netMonthly * 12):N0}"),
                    $"yearly income after expenses ({netMonthly * 12:N0}) must appear");
            });
        }

        /// <summary>
        /// "Contains the right number" is not enough on its own: the right figure can be present
        /// on one path while a wrong one is printed on another, which is exactly how the first
        /// version of this fixture passed against a mutated subtraction.
        /// </summary>
        [Test]
        public void Report_NeverPrintsIncomePlusExpenses([Values("surplus", "shortfall")] string scenario)
        {
            var (report, income, expense) = Case(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(report.Text, Does.Not.Contain($"{income + expense:N0}"),
                    "income plus expenses is a sign error, never a figure to print");
                Assert.That(report.Text, Does.Not.Contain($"{(income + expense) * 12:N0}"),
                    "ditto annualised");
            });
        }

        /// <summary>
        /// A shortfall must be reported as one. If the sign were dropped the report would tell
        /// someone they can afford a loan they cannot.
        /// </summary>
        [Test]
        public void Report_ShowsAShortfallWithItsSign()
        {
            var netMonthly = ShortfallIncome - ShortfallExpense;
            Assume.That(netMonthly, Is.LessThan(0), "fixture precondition");

            // The minus sits with the currency symbol so it reads "-$4,512" rather than
            // "$-4,512" — the same convention as the affordability box.
            var signed = $"-{Helper.CurrencySymbol}{Math.Abs(netMonthly):N0}";

            Assert.That(_shortfall.Text, Does.Contain(signed),
                $"a shortfall must be shown as {signed}; dropping the sign would tell someone "
                + "they can afford a loan they cannot");
            Assert.That(_surplus.Text, Does.Not.Contain(
                $"-{Helper.CurrencySymbol}{Math.Abs(SurplusIncome - SurplusExpense):N0}"),
                "the surplus report must not render its net as negative");
        }

        /// <summary>
        /// The two reports must differ. Guards against the whole fixture asserting against a
        /// stale or shared document, which would make every case above vacuous.
        /// </summary>
        [Test]
        public void TheTwoScenariosProduceDifferentReports()
        {
            Assert.That(_shortfall.Text, Is.Not.EqualTo(_surplus.Text));
            Assert.That(_surplus.Text, Does.Not.Contain($"{ShortfallIncome:N0}"),
                "the surplus report must not contain the shortfall fixture's income");
        }
    }
}
