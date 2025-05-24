using LoanCalculator.Core.Exts;
using LoanCalculator.Core.Models.Pdf;
using LoanCalculator.Core.Models.ViewModels.PrimaryModels;
using LoanCalculator.Core.Services;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Graphics.Fonts;
using PointF = Syncfusion.Drawing.PointF;
using SizeF = Syncfusion.Drawing.SizeF;

namespace LoanCalculator.Core.Pdf
{
    public class PdfGeneratorBase
    {
        protected float _yPosition = 0;

        private readonly IFontUnicodeProvider _fontUnicodeProvider;

        #region Page Settings
        protected const float PageWidth = 595; // A4 width
        protected const float PageHeight = 842; // A4 height
        #endregion

        #region Default Values
        readonly Dictionary<PdfFontStyle, byte[]> _fontBytes = new();
        protected readonly PdfFontFamily DefaultFontFamily;
        protected const string DefaultTextColor = "#212f3c";
        protected readonly PdfSolidBrush? DefaultTextBrush;

        protected readonly PdfBrush? DefaultHeaderRowBgBrush = "#80BCBD".ToPdfBrush();
        protected readonly PdfBrush? DefaultHeaderRowTextBrush = "#092635".ToPdfBrush();
        protected readonly PdfBrush? DefaultColumnHeaderBgBrush = "#B2C8BA".ToPdfBrush();
        protected readonly PdfBrush? DefaultColumnHeaderTextBrush = "#1B4242".ToPdfBrush();
        protected readonly PdfBrush? DefaultTotalRowHeaderBgBrush = "#EBF3E8".ToPdfBrush();
        protected readonly PdfBrush? DefaultTotalRowHeaderTextBrush = "#1B4242".ToPdfBrush();
        protected readonly PdfBrush? DefaultBorderBrush = "#5C8374".ToPdfBrush();
        protected readonly PdfBrush? DefaultHighlightedCellBgBrush = "#f9e79f".ToPdfBrush();
        protected readonly PdfBrush? DefaultCellPositiveBgBrush = "#d4efdf".ToPdfBrush();
        protected readonly PdfBrush? DefaultCellNegativeBgBrush = "#fcf3cf".ToPdfBrush();

        protected PdfBrush? _defaultTextFgBasedOnValueBrush(double value) => value < 0 ? "#922b21".ToPdfBrush() : DefaulCellTextFgBrush;
        protected PdfBrush? _defaultTextFgBasedOnValueBrush(double value, PdfBrush positiveBrush) => value < 0 ? "#922b21".ToPdfBrush() : positiveBrush;
        protected PdfBrush? _defaultTextFgBasedOnValueBrush(double? value, PdfBrush positiveBrush) => value is < 0 ? "#922b21".ToPdfBrush() : positiveBrush;

        protected readonly PdfBrush? DefaulCellTextFgBrush = DefaultTextColor.ToPdfBrush();
        protected readonly PdfBrush? DefaultCellTextBgBrush = PdfBrushes.White;
        #endregion


        protected PdfDataInsightsModel DataModel;
        protected PdfDocument Document;
        protected PdfPage Page;

        public PdfGeneratorBase(IFontUnicodeProvider fontUnicodeProvider)
        {
            DefaultFontFamily = PdfFontFamily.Helvetica;
            DefaultTextBrush = DefaultTextColor.ToPdfBrush();
            _fontUnicodeProvider = fontUnicodeProvider;
            LoadFontsOnce();
        }

        #region Fonts

        public void LoadFontsOnce()
        {
            // Preload all required styles
            _fontBytes[PdfFontStyle.Regular] = LoadFontBytes("NotoSans-Regular.ttf");
            _fontBytes[PdfFontStyle.Bold] = LoadFontBytes("NotoSans-Bold.ttf");
            _fontBytes[PdfFontStyle.Italic] = LoadFontBytes("NotoSans-Italic.ttf");
        }

        private byte[] LoadFontBytes(string fileName)
        {
            using var stream = _fontUnicodeProvider.LoadFont(fileName);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        protected PdfFont GetNumberFont(float size, PdfFontStyle style)
        {
            if (!_fontBytes.ContainsKey(style))
                style = PdfFontStyle.Regular;

            var memoryStream = new MemoryStream(_fontBytes[style]);
            return new PdfTrueTypeFont(memoryStream, size, style);
        }

        protected PdfFont GetTextFont(float size, PdfFontStyle style)
        {
            return GetNumberFont(size, style);
            // Uncomment the line below if you want to use a standard font instead of a custom one
            //return new PdfStandardFont(DefaultFontFamily, size, style);
        }

        #endregion

        protected async Task SaveAndView(string fileName, MemoryStream stream)
        {
            await SharedServiceCore.LocalStorage.SaveFileToFileSystem(fileName, stream);
        }

        protected void InitializeDocumentWithPageSettings()
        {
            // Create a new PDF document
            Document = new PdfDocument();

            // Add metadata
            Document.DocumentInformation.Author = "Your Simple Apps \u00ae";
            Document.DocumentInformation.Title = "Property Home Loan Report";
            Document.DocumentInformation.Subject = " Comprehensive Overview (estimate ONLY)";
            Document.DocumentInformation.Keywords = "Loan, YourSimpleApp, Insights, Calculator";
            Document.DocumentInformation.Creator = "Your Simple Apps \u00ae";
            Document.DocumentInformation.Producer = "Your Simple Apps\u00ae";

            // Set page margins
            PdfPageSettings pageSettings = new PdfPageSettings
            {
                Margins = new PdfMargins
                {
                    Top = 10, // Adjust the top margin as needed
                    Bottom = 10,
                    Left = 50,
                    Right = 40
                }
            };
            pageSettings.Size = PdfPageSize.A4;
            pageSettings.Orientation = PdfPageOrientation.Portrait;

            Document.PageSettings = pageSettings;
        }

        protected void InitializeDataSets()
        {
            try
            {
                LoanViewModel? loanViewModel = null;
                IncomeViewModel incomeModel = null;
                ExpenseViewModel expenseModel = null;

                Task.Run(async () =>
                {
                    try
                    {
                        loanViewModel = await SharedServiceCore.LoadDataFile<LoanViewModel>();
                        incomeModel = await SharedServiceCore.LoadDataFile<IncomeViewModel>();
                        expenseModel = await SharedServiceCore.LoadDataFile<ExpenseViewModel>();
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions
                        Console.WriteLine(ex.Message);
                    }
                }).Wait();

                DataModel = new PdfDataInsightsModel(loanViewModel, incomeModel, expenseModel);
                DataModel.InitializeLocalDataSet();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected PdfSolidBrush? GetBrushFromHex(string hex)
        {
            return hex == DefaultTextColor ? DefaultTextBrush : hex.ToPdfBrush();
        }

        #region Renderer

        protected void InsertBlankPage()
        {
            Page = Document!.Pages.Add();

            _yPosition = 0; // Reset Y position for the new page
        }

        protected void PageTitle(string text, string textColor = DefaultTextColor)
        {
            RenderContent(text, 28, PdfFontStyle.Bold, GetBrushFromHex(textColor));
        }

        protected void PageSubtitle(string text, string textColor = DefaultTextColor)
        {
            RenderContent(text, 14, PdfFontStyle.Italic, GetBrushFromHex(textColor));
        }

        protected void DrawH1(string text, string textColor = DefaultTextColor)
        {
            RenderContent(text, 22, PdfFontStyle.Bold, GetBrushFromHex(textColor));
        }

        protected void DrawH2(string text, string textColor = DefaultTextColor)
        {
            RenderContent(text, 18, PdfFontStyle.Bold, GetBrushFromHex(textColor));
        }

        protected void DrawH3(string text, string textColor = DefaultTextColor)
        {
            RenderContent(text, 16, PdfFontStyle.Bold, GetBrushFromHex(textColor));
        }

        protected void DrawParagraph(string text, string textColor = DefaultTextColor)
        {
            RenderContent(text, 12, PdfFontStyle.Regular, GetBrushFromHex(textColor));
        }

        protected void AddNewLineSpace(int defaultSpace = 10)
        {
            _yPosition += defaultSpace;
        }

        protected void RenderContent(string text, float size, PdfFontStyle style, PdfBrush? brush, int bottomSpaceAdjustment = 10)
        {
            var font = GetTextFont(size, style);
            var textElement = new PdfTextElement(text, font) { Brush = brush };
            var layoutFormat = new PdfLayoutFormat
            {
                Layout = PdfLayoutType.Paginate, // Automatically continue to the next page if needed
                Break = PdfLayoutBreakType.FitPage // Wrap text at word boundaries
            };

            PdfLayoutResult result = textElement.Draw(Page, new RectangleF(new PointF(0, _yPosition), new SizeF(Page.GetClientSize().Width, Page.GetClientSize().Height - _yPosition)), layoutFormat);
            // Check if the text was drawn on a new page
            if (result.Page != null && result.Page != Page)
            {
                Page = result.Page;
            }
            _yPosition = result.Bounds.Bottom + bottomSpaceAdjustment; // Adjust the spacing as needed
        }

        protected void DrawFormattedText(string amount, string description, string additionalText,
            PdfBrush? amountTextBrush = null,
            PdfBrush? descriptionTextBrush = null,
            PdfBrush? additionalTextBrush = null)
        {
            amountTextBrush ??= DefaultTextBrush;
            descriptionTextBrush ??= DefaultTextBrush;
            additionalTextBrush ??= DefaultTextBrush;

            //AddNewLineSpace();

            var textElementsRow1 = new List<TextElementModel>
            {
                new(amount, GetNumberFont(14, PdfFontStyle.Bold), amountTextBrush, 1),
                new($"{description}", GetTextFont(12, PdfFontStyle.Bold), descriptionTextBrush, 1),
            };
            DrawTextElements(textElementsRow1, _yPosition, updateYPosition: true);

            var textElementsRow2 = new List<TextElementModel>
            {
                new(additionalText, GetTextFont(12, PdfFontStyle.Regular), additionalTextBrush, 2),
            };
            DrawTextElements(textElementsRow2, _yPosition, updateYPosition: true);

            AddNewLineSpace();
        }

        protected float DrawTextElements(List<TextElementModel> textElements, float yPosition, float xPosition = 0, bool updateYPosition = false)
        {
            float maxWidth = Page.Graphics.ClientSize.Width - 10; // Right margin
            float lineHeight = 0; // Track max height of the line
            var innerXPosition = xPosition;

            var maxFontSize = textElements.Max(x => x.Font.Size);
            var maxFontData = GetTextFont(maxFontSize, PdfFontStyle.Bold); //fix the bold font issue

            foreach (var element in textElements)
            {
                // Calculate the baseline adjustment for alignment
                float baselineAdjustment = 0;
                if (element.Font.Size != maxFontSize)
                {
                    baselineAdjustment = maxFontData.Height - element.Font.Height;
                }

                string[] words = element.Text.Split(' '); // Split text into words
                foreach (string word in words)
                {
                    string wordWithSpace = word + " "; // Preserve spaces

                    float wordWidth = element.Font.MeasureString(wordWithSpace).Width;
                    float wordHeight = element.Font.MeasureString(wordWithSpace).Height;

                    // If the text exceeds the width, move to the next line
                    if (innerXPosition + wordWidth > maxWidth)
                    {
                        innerXPosition = xPosition; // Reset X position to left margin
                        //yPosition += font.Size + 5; // Move to the next line
                        yPosition += lineHeight + 5; // Move to the next line
                        lineHeight = 0; // Reset line height
                    }

                    // Draw the word at the current position
                    PdfTextElement text = new PdfTextElement(wordWithSpace, element.Font, element.TextBrush);

                    PdfLayoutResult result = text.Draw(Page, new PointF(innerXPosition, yPosition + baselineAdjustment));
                    if (result.Page != null && result.Page != Page)
                    {
                        Page = result.Page;
                        yPosition = 0;
                    }

                    //_page.Graphics.DrawString(wordWithSpace, font, PdfBrushes.Black, new PointF(innerXPosition, yPosition + baselineAdjustment));

                    // Move X position forward
                    innerXPosition += wordWidth + 3;

                    // Track the tallest text in the line for proper spacing
                    lineHeight = Math.Max(lineHeight, wordHeight);
                }
            }

            if (updateYPosition)
            {
                _yPosition = yPosition + lineHeight + 5; // Adjust the spacing as needed
            }

            return yPosition + lineHeight + 5; // Adjust the spacing as needed
        }

        protected void DrawBulletPoints(List<List<TextElementModel>> bulletPoints, string bulletCharacter = "- ", float additionalSpacingAfterLastBullet = 10)
        {
            float bulletIndent = 20; // Indent for the bullet points
            float textIndent = 30; // Indent for the text after the bullet
            float yPosition = _yPosition;

            foreach (var point in bulletPoints)
            {
                // Draw bullet
                PdfTextElement bulletElement = new PdfTextElement(bulletCharacter, GetTextFont(12, PdfFontStyle.Regular))
                {
                    Brush = GetBrushFromHex(DefaultTextColor)
                };
                PdfLayoutResult bulletResult = bulletElement.Draw(Page, new PointF(bulletIndent, yPosition));

                // Draw formatted text elements
                yPosition = DrawTextElements(point, yPosition, textIndent);
            }

            // Add additional spacing after the last bullet point
            yPosition += additionalSpacingAfterLastBullet;

            _yPosition = yPosition;
        }

        #endregion
    }
}
