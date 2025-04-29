using LoanCalculator.Core.Exts;
using LoanCalculator.Core.Models.Enums;
using LoanCalculator.Core.Models.Income;
using LoanCalculator.Core.Models.Pdf;
using LoanCalculator.Core.Services;
using Pj.Library;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf.Interactive;
using System.ComponentModel;
using System.Globalization;
using Color = Syncfusion.Drawing.Color;
using PointF = Syncfusion.Drawing.PointF;
using SizeF = Syncfusion.Drawing.SizeF;

namespace LoanCalculator.Core.Pdf
{
    public class PdfInsightsGenerator : PdfGeneratorBaseWithDisclaimer, INotifyPropertyChanged
    {
        public PdfInsightsGenerator()
        {

        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            private set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        private void UpdateProgress(int value)
        {
            Progress = value;
            // Optionally, raise an event or notify observers if needed
        }

        public async Task GeneratePdf(int taskDelay = 0)
        {
            try
            {
                _yPosition = 0;

                SharedServiceCore.LoadSafeOn();

                UpdateProgress(10); // 10% progress

                // Offload CPU-intensive tasks to a background thread
                await Task.Run(() =>
                {
                    InitializeDataSets();
                    InitializeDocumentWithPageSettings();
                });

                UpdateProgress(20); // 30% progress
                await Task.Delay(taskDelay); // Simulate work

                // Render header and footer templates
                await RenderHeaderTemplate();
                await Task.Run(() => RenderFooterTemplate());

                // Add a page to the document
                await Task.Run(() =>
                {
                    Page = Document!.Pages.Add();
                });

                UpdateProgress(40); // 50% progress
                await Task.Delay(taskDelay); // Simulate work

                // Draw header and subtitle
                await Task.Run(() =>
                {
                    PageTitle("Property Home Loan Report");
                    PageSubtitle("Comprehensive Overview (estimate ONLY)");
                    AddNewLineSpace(20);
                    GenerateDisclaimerData();
                });

                UpdateProgress(50); // 50% progress
                await Task.Delay(taskDelay); // Simulate work

                // Draw KPI boxes
                await Task.Run(() => DrawKpiBoxes());

                UpdateProgress(60); // 70% progress
                await Task.Delay(taskDelay); // Simulate work

                // Generate property insights
                await Task.Run(() => GeneratePropertyInsights());

                UpdateProgress(80); // 90% progress
                await Task.Delay(taskDelay); // Simulate work

                // Save the document to a stream
                using MemoryStream stream = new MemoryStream();
                await Task.Run(() => Document.Save(stream));

                // Save the stream as a file in the device and invoke it for viewing
                await Task.Delay(taskDelay); // Simulate work
                UpdateProgress(100); // 100% progress
                await SaveAndView("Output.pdf", stream);

                UpdateProgress(0);
            }
            catch (Exception e)
            {
                SharedServiceCore.ErrorHandlingService.HandleException(e);
            }
            finally
            {
                SharedServiceCore.LoadSafeOff();
            }
        }


        private async Task RenderHeaderTemplate()
        {
            // Header template
            RectangleF headerBounds = new RectangleF(0, 0, PageWidth, 70);
            PdfPageTemplateElement header = new PdfPageTemplateElement(headerBounds);

            await using var stream = await SharedServiceCore.LocalStorage.LoadFileFromFileSystem("Resources/AppIcon/appiconfg.png");

            // Copy the original stream to a MemoryStream
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            await memoryStream.FlushAsync();
            memoryStream.Position = 0; // Reset the position to the beginning of the stream

            // Validate the stream
            if (memoryStream.Length == 0)
            {
                throw new InvalidOperationException("The image stream is empty.");
            }

            PdfImage image = new PdfBitmap(memoryStream);

            // Calculate aspect ratio
            float originalWidth = image.Width;
            float originalHeight = image.Height;
            float fixedHeight = 50;
            float aspectRatio = originalWidth / originalHeight;
            float newWidth = fixedHeight * aspectRatio;

            // Draw the image in the header with the calculated dimensions.
            // Adjust the Y-coordinate to move the image upwards
            header.Graphics.DrawImage(image, new PointF(0, 0), new SizeF(newWidth, fixedHeight));
            // Add the header at the top.

            // Define title and subtitle
            string title = "Loan Calculator Report";
            string subtitle = "Comprehensive Overview and Estimates";

            // Set font and style for title and subtitle
            PdfFont titleFont = new PdfStandardFont(DefaultFontFamily, 16, PdfFontStyle.Bold);
            PdfFont subtitleFont = new PdfStandardFont(DefaultFontFamily, 12, PdfFontStyle.Italic);

            // Draw title
            header.Graphics.DrawString(title, titleFont, "#99a3a4".ToPdfBrush(), new PointF(newWidth + 10, 10));

            // Draw subtitle
            header.Graphics.DrawString(subtitle, subtitleFont, "#99a3a4".ToPdfBrush(), new PointF(newWidth + 10, 30));


            Document.Template.Top = header;
        }
        private void RenderFooterTemplate()
        {
            // Footer template
            RectangleF bounds = new RectangleF(0, 0, PageWidth, 50);
            PdfPageTemplateElement footer = new PdfPageTemplateElement(bounds);
            PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 7);
            PdfBrush brush = new PdfSolidBrush(Color.Black);

            // Create page number field.
            PdfPageNumberField pageNumber = new PdfPageNumberField(font, brush);
            // Create page count field.
            PdfPageCountField count = new PdfPageCountField(font, brush);
            // Add the fields in composite fields.
            PdfCompositeField compositeField = new PdfCompositeField(font, brush, "Page {0} of {1}", pageNumber, count);
            compositeField.Bounds = footer.Bounds;

            // Draw the composite field in footer.
            compositeField.Draw(footer.Graphics, new PointF(470, 20));

            // Get the current date and time
            string dateTime = DateTime.Now.ToString("f", CultureInfo.CurrentCulture);

            // Define contact information and Play Store link
            string contactInfoText = "Contact App";
            string contactEmail = "yoursimpleapps@gmail.com";
            string appInformationText = "App Information";
            string playStoreLink = "https://play.google.com/store/apps/details?id=com.yourapp";

            // Set font and style for the text
            PdfFont infoFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);

            // Calculate positions
            float margin = 10;
            float dateTimeWidth = infoFont.MeasureString(dateTime).Width;
            float emailWidth = infoFont.MeasureString(contactInfoText).Width;
            float playStoreWidth = infoFont.MeasureString(appInformationText).Width;

            float startX = 10; // Align to the left

            // Draw date and time
            footer.Graphics.DrawString(dateTime, infoFont, brush, new PointF(startX, 20));

            // Draw contact information as a hyperlink
            footer.Graphics.DrawString(contactInfoText, infoFont, PdfBrushes.Black, new PointF(startX + dateTimeWidth + margin, 20));

            // Draw Play Store link as a hyperlink
            footer.Graphics.DrawString(appInformationText, infoFont, PdfBrushes.Black, new PointF(startX + dateTimeWidth + emailWidth + 3 * margin, 20));

            // Add the footer template at the bottom.
            Document.Template.Bottom = footer;

            Document.Pages.PageAdded += (sender, args) =>
            {
                // Calculate the footer's Y position on the page
                float footerYPosition = args.Page.GetClientSize().Height;

                // Add contact email link
                PdfUriAnnotation emailLinkAnnotation = new PdfUriAnnotation(new RectangleF(dateTimeWidth - 2, footerYPosition + 17, emailWidth, infoFont.Height + 6), $"mailto:{contactEmail}");
                //Set border color(e.g., Blue)
                emailLinkAnnotation.Color = new PdfColor(ColorTranslator.FromHtml("#95a5a6"));

                // Optional: set border width or style if needed
                emailLinkAnnotation.Border = new PdfAnnotationBorder(0.5f); // 0.5pt border
                emailLinkAnnotation.Border.HorizontalRadius = 0;
                emailLinkAnnotation.Border.VerticalRadius = 0;

                args.Page.Annotations.Add(emailLinkAnnotation);

                PdfUriAnnotation playStoreLinkAnnotation = new PdfUriAnnotation(new RectangleF(dateTimeWidth + emailWidth + 5, footerYPosition + 17, playStoreWidth + 4, infoFont.Height + 6), playStoreLink);
                //Set border color(e.g., Blue)
                playStoreLinkAnnotation.Color = new PdfColor(ColorTranslator.FromHtml("#95a5a6"));

                // Optional: set border width or style if needed
                playStoreLinkAnnotation.Border = new PdfAnnotationBorder(0.5f); // 0.5pt border
                playStoreLinkAnnotation.Border.HorizontalRadius = 0;
                playStoreLinkAnnotation.Border.VerticalRadius = 0;
                args.Page.Annotations.Add(playStoreLinkAnnotation);
            };
        }
        private void RenderGradientBackground(PdfPage page)
        {
            // Define the gradient colors
            PdfColor startColor = new PdfColor(255, 255, 255); // White
            PdfColor endColor = new PdfColor(0, 128, 255); // Blue

            // Create a linear gradient brush
            PdfLinearGradientBrush gradientBrush = new PdfLinearGradientBrush(
                new PointF(0, 0),
                new PointF(0, PageHeight), // Use the full page height
                startColor,
                endColor);

            // Draw a rectangle with the gradient brush to cover the entire page
            page.Graphics.DrawRectangle(gradientBrush, new RectangleF(0, 0, PageWidth, PageHeight));
        }

        private void DrawKpiBoxes()
        {
            InsertBlankPage();

            DrawH1("Key Insights");

            AddNewLineSpace();

            var listOfKpiExplain = new List<TextElementModel>
            {
                new("Affordability", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new("is the money you have left after paying for all your expenses, including this new loan. It shows how much you can comfortably manage while still covering your financial needs.", DefaultFontFamily, 12,
                    PdfFontStyle.Regular, DefaultTextBrush, 1),
            };

            DrawTextElements(listOfKpiExplain, _yPosition, updateYPosition: true);
            AddNewLineSpace(5);

            listOfKpiExplain = new List<TextElementModel>
            {
                new("Loan", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new("is the amount borrowed and deposit paid for the property purchase.", DefaultFontFamily, 12,
                    PdfFontStyle.Regular, DefaultTextBrush, 1),
            };

            DrawTextElements(listOfKpiExplain, _yPosition, updateYPosition: true);
            AddNewLineSpace(5);

            listOfKpiExplain = new List<TextElementModel>
            {
                new("Total Repayment", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new("is the complete amount you'll pay back, including the loan principal and interest.", DefaultFontFamily, 12,
                    PdfFontStyle.Regular, DefaultTextBrush, 1),
            };

            DrawTextElements(listOfKpiExplain, _yPosition, updateYPosition: true);
            AddNewLineSpace(5);

            listOfKpiExplain = new List<TextElementModel>
            {
                new("Term Payment", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new("is the regular payments made monthly or yearly to repay the loan.", DefaultFontFamily, 12,
                    PdfFontStyle.Regular, DefaultTextBrush, 1),
            };

            DrawTextElements(listOfKpiExplain, _yPosition, updateYPosition: true);
            AddNewLineSpace(5);

            listOfKpiExplain = new List<TextElementModel>
            {
                new("Total Income", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new("is the total earnings from all sources on a monthly and yearly basis.", DefaultFontFamily, 12,
                    PdfFontStyle.Regular, DefaultTextBrush, 1),
            };

            DrawTextElements(listOfKpiExplain, _yPosition, updateYPosition: true);
            AddNewLineSpace(5);

            listOfKpiExplain = new List<TextElementModel>
            {
                new("Total Expenses", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new("is your overall financial outflows, covering loan repayments and other expenses.", DefaultFontFamily, 12,
                    PdfFontStyle.Regular, DefaultTextBrush, 1),
            };

            DrawTextElements(listOfKpiExplain, _yPosition, updateYPosition: true);
            AddNewLineSpace(5);

            AddNewLineSpace(20);

            var listOfBoxValueDefinitions = new List<BoxModel>
            {
                new("Affordability", $"{DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly.ToCurrency()} (monthly)",
                    $"{DataModel.Income.TotalAfterExpenseIncludingPropertyYearly.ToCurrency()} (yearly)",
                    (DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly <= 0 ? "#edbb99" : "#e8f6f3").ToPdfColor(),
                    (DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly <= 0 ?"#e59866" : "#a3e4d7").ToPdfColor(),
                    (DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly <= 0 ? "#aeb6bf" : "#45b39d").ToPdfColor()),
                new("Loan",
                    $"{DataModel.Loan.LoanAmount.ToCurrency()} (loan)",
                    $"{DataModel.Loan.DepositAmount.ToCurrency()} (deposit)",
                    "#ebedef".ToPdfColor(), "#aeb6bf".ToPdfColor(), "#a6acaf".ToPdfColor()),
                new("Total Repayment",
                    DataModel.Loan.TotalRepayment.ToCurrency(),
                    $"{DataModel.Loan.TotalInterest.ToCurrency()} (interest)",
                    "#ebedef".ToPdfColor(), "#aeb6bf".ToPdfColor(), "#a6acaf".ToPdfColor()),
                new("Term Payment", $"{DataModel.Loan.MonthlyRepayment.ToCurrency()} (monthly)",
                    $"{DataModel.Loan.YearlyRepayment.ToCurrency()} (yearly)",
                    "#ebedef".ToPdfColor(), "#aeb6bf".ToPdfColor(), "#a6acaf".ToPdfColor()),
                new("Total Income",
                    $"{DataModel.Income.TotalMonthly.ToCurrency()} (monthly)",
                    $"{DataModel.Income.TotalYearly.ToCurrency()} (yearly)",
                    "#a2d9ce".ToPdfColor(), "#73c6b6".ToPdfColor(), "#d5dbdb".ToPdfColor()),
                new("Total Expenses",
                    $"{DataModel.Income.TotalExpenseIncludingPropertyMonthly.ToCurrency()} (monthly)",
                    $"{DataModel.Income.TotalExpenseIncludingPropertyYearly.ToCurrency()} (yearly)",
                    "#a9cce3".ToPdfColor(), "#7fb3d5".ToPdfColor(), "#d5dbdb".ToPdfColor()),
            };

            // Draw highlighter boxes
            float boxWidth = 200; // Reduced width to fit within margins
            float boxHeight = 90;
            float margin = 20;
            float startX = 20; // Move boxes to the left
            float startY = _yPosition;

            for (int i = 0; i < 6; i++)
            {
                float x = startX + (i % 2) * (boxWidth + margin);
                float y = startY + (i / 2) * (boxHeight + margin);
                DrawHighlighterBox(Page, listOfBoxValueDefinitions[i].Header, listOfBoxValueDefinitions[i].Value, listOfBoxValueDefinitions[i].SecondValue,
                    x, y, boxWidth, boxHeight,
                    listOfBoxValueDefinitions[i].BackgroundStartColor,
                    listOfBoxValueDefinitions[i].BackgroundEndColor,
                    listOfBoxValueDefinitions[i].BorderColor);
            }

            // Update _yPosition to push the content down after drawing the boxes
            _yPosition = startY + (6 / 2) * (boxHeight + margin) + margin;
        }

        private void DrawHighlighterBox(PdfPage page, string header, string value, string secondValue, float xPosition, float yPosition, float boxWidth, float boxHeight, PdfColor startColor, PdfColor endColor, PdfColor borderColor)
        {
            // Define the bounds of the box using xPosition and yPosition
            RectangleF bounds = new RectangleF(xPosition, yPosition, boxWidth, boxHeight);

            // Create a linear gradient brush
            PdfLinearGradientBrush gradientBrush = new PdfLinearGradientBrush(
                new PointF(bounds.Left, bounds.Top),
                new PointF(bounds.Left, bounds.Bottom),
                startColor,
                endColor);

            // Draw the gradient rectangle
            page.Graphics.DrawRectangle(gradientBrush, bounds);

            // Draw the border
            PdfPen borderPen = new PdfPen(borderColor, 1);
            page.Graphics.DrawRectangle(borderPen, bounds);

            // Draw the header text
            PdfFont headerFont = new PdfStandardFont(DefaultFontFamily, 12, PdfFontStyle.Bold);
            PdfTextElement headerElement = new PdfTextElement(header, headerFont)
            {
                Brush = PdfBrushes.Black
            };
            headerElement.Draw(page, new PointF(bounds.Left + 5, bounds.Top + 5));

            // Draw the value text
            PdfFont valueFont = new PdfStandardFont(DefaultFontFamily, 16, PdfFontStyle.Bold);
            PdfTextElement valueElement = new PdfTextElement(value, valueFont)
            {
                Brush = PdfBrushes.Black
            };
            valueElement.Draw(page, new PointF(bounds.Left + 5, bounds.Top + 25));

            // Draw the value text
            PdfFont secondValueFont = new PdfStandardFont(DefaultFontFamily, 12, PdfFontStyle.Regular);
            PdfTextElement secondValueElement = new PdfTextElement(secondValue, secondValueFont)
            {
                Brush = PdfBrushes.Black
            };
            secondValueElement.Draw(page, new PointF(bounds.Left + 5, bounds.Top + 25 + 25));
        }

        private PdfGridCellStyle CustomTableCellStyle(PdfBrush? bgBrush, PdfBrush? fgBrush, PdfBrush? borderBrush, int fontSize, PdfFontStyle fontStyle)
        {
            return new PdfGridCellStyle
            {
                BackgroundBrush = bgBrush,
                TextBrush = fgBrush,
                Font = new PdfStandardFont(DefaultFontFamily, fontSize, fontStyle),
                Borders = new PdfBorders { All = new PdfPen(borderBrush, 0.5f) },
                CellPadding = new PdfPaddings(5, 5, 5, 5) // Add padding to the header cells
            };
        }

        private void DrawAmortisationTable()
        {
            var headerStyle = CustomTableCellStyle(DefaultHeaderRowBgBrush, DefaultHeaderRowTextBrush, DefaultBorderBrush, 12, PdfFontStyle.Bold);
            var defaultCellStyle = CustomTableCellStyle(DefaultCellTextBgBrush, DefaulCellTextFgBrush, DefaultBorderBrush, 11, PdfFontStyle.Regular);

            // Create a PdfGrid
            PdfGrid pdfGrid = new PdfGrid();

            // Add columns to the grid
            pdfGrid.Columns.Add(4);

            //

            // Add header
            PdfGridRow header = pdfGrid.Headers.Add(1)[0];
            header.Cells[0].Value = "Period";
            header.Cells[1].Value = "Principal";
            header.Cells[2].Value = "Interest";
            header.Cells[3].Value = "Balance";

            foreach (PdfGridCell cell in header.Cells)
            {
                cell.Style = headerStyle;
            }

            // Add rows
            for (int i = 0; i < DataModel.Loan.PaymentAmortization.Count; i++)
            {
                PdfGridRow row = pdfGrid.Rows.Add();
                row.Cells[0].Value = DataModel.Loan.PaymentAmortization[i].PaymentPeriod;
                row.Cells[1].Value = DataModel.Loan.PaymentAmortization[i].PrincipalAmount.ToCurrency();
                row.Cells[2].Value = DataModel.Loan.PaymentAmortization[i].InterestAmount.ToCurrency();
                row.Cells[3].Value = DataModel.Loan.PaymentAmortization[i].BalanceAmount.ToCurrency();

                foreach (PdfGridCell cell in row.Cells)
                {
                    cell.Style = defaultCellStyle;
                }
            }

            // Draw the grid on the page with left padding
            float leftPadding = 20; // Adjust the left padding as needed
            PdfGridLayoutResult result = pdfGrid.Draw(Page, new PointF(leftPadding, _yPosition));

            // Check if the text was drawn on a new page
            if (result.Page != null && result.Page != Page)
            {
                Page = result.Page;
            }
            _yPosition = result.Bounds.Bottom + 10; // Adjust the spacing as needed

            //_yPosition += result.Bounds.Bottom + 10; // Adjust the spacing as needed
        }
        private void DrawExpenseOnNewPropertyTable(
            string headerRowBgColor = "", string headerRowTextColor = "",
            string columnHeaderBgColor = "", string columnHeaderTextColor = "",
            string borderBrushColor = "")
        {
            var headerRowBgBrush = headerRowBgColor.IsEmpty() ? DefaultHeaderRowBgBrush : headerRowBgColor.ToPdfBrush();
            var headerRowTextBrush = headerRowTextColor.IsEmpty() ? DefaultHeaderRowTextBrush : headerRowTextColor.ToPdfBrush();
            var columnHeaderBgBrush = columnHeaderBgColor.IsEmpty() ? DefaultColumnHeaderBgBrush : columnHeaderBgColor.ToPdfBrush();
            var columnHeaderTextBrush = columnHeaderTextColor.IsEmpty() ? DefaultColumnHeaderTextBrush : columnHeaderTextColor.ToPdfBrush();
            var borderBrush = borderBrushColor.IsEmpty() ? DefaultBorderBrush : borderBrushColor.ToPdfBrush();

            // Set default style
            var headerStyle = CustomTableCellStyle(headerRowBgBrush, headerRowTextBrush, borderBrush, 12, PdfFontStyle.Bold);
            var boldCellStyle = CustomTableCellStyle(columnHeaderBgBrush, columnHeaderTextBrush, borderBrush, 11, PdfFontStyle.Bold);
            var defaultCellStyle = CustomTableCellStyle(DefaultCellTextBgBrush, DefaulCellTextFgBrush, borderBrush, 11, PdfFontStyle.Regular);

            // Create a PdfGrid
            PdfGrid pdfGrid = new PdfGrid();

            // Add columns to the grid
            pdfGrid.Columns.Add(5);

            PdfGridRow header = pdfGrid.Headers.Add(1)[0];
            header.Cells[0].Value = "Expense";
            header.Cells[1].Value = "Weekly";
            header.Cells[2].Value = "Fortnightly";
            header.Cells[3].Value = "Monthly";
            header.Cells[4].Value = "Yearly";

            foreach (PdfGridCell cell in header.Cells)
            {
                cell.Style = headerStyle;
            }

            PdfGridRow repaymentRow = pdfGrid.Rows.Add();
            repaymentRow.Cells[0].Value = "Repayment";
            repaymentRow.Cells[1].Value = DataModel.Loan.WeeklyRepayment.ToCurrency();
            repaymentRow.Cells[2].Value = DataModel.Loan.FortnightlyRepayment.ToCurrency();
            repaymentRow.Cells[3].Value = DataModel.Loan.MonthlyRepayment.ToCurrency();
            repaymentRow.Cells[4].Value = DataModel.Loan.YearlyRepayment.ToCurrency();

            repaymentRow.Cells[0].Style = boldCellStyle;
            repaymentRow.Cells[1].Style = defaultCellStyle;
            repaymentRow.Cells[2].Style = defaultCellStyle;
            repaymentRow.Cells[3].Style = defaultCellStyle;
            repaymentRow.Cells[4].Style = defaultCellStyle;


            PdfGridRow additionalExpenseRow = pdfGrid.Rows.Add();
            additionalExpenseRow.Cells[0].Value = "Additional expense";
            additionalExpenseRow.Cells[1].Value = DataModel.Loan.Transactions?.IncomeExpenseSummary?.TotalWeekly.ToCurrency();
            additionalExpenseRow.Cells[2].Value = DataModel.Loan.Transactions?.IncomeExpenseSummary?.TotalFortnightly.ToCurrency();
            additionalExpenseRow.Cells[3].Value =
                DataModel.Loan.Transactions?.IncomeExpenseSummary?.TotalMonthly.ToCurrency();
            additionalExpenseRow.Cells[4].Value =
                DataModel.Loan.Transactions?.IncomeExpenseSummary?.TotalYearly.ToCurrency();

            additionalExpenseRow.Cells[0].Style = boldCellStyle;
            additionalExpenseRow.Cells[1].Style = defaultCellStyle;
            additionalExpenseRow.Cells[2].Style = defaultCellStyle;
            additionalExpenseRow.Cells[3].Style = defaultCellStyle;
            additionalExpenseRow.Cells[4].Style = defaultCellStyle;

            PdfGridRow totalExpenseRow = pdfGrid.Rows.Add();
            totalExpenseRow.Cells[0].Value = "Total";
            totalExpenseRow.Cells[1].Value = Math.Round(
                DataModel.Loan.Transactions?.IncomeExpenseSummary?.TotalWeekly +
                DataModel.Loan.WeeklyRepayment ?? 0, 0).ToCurrency();
            totalExpenseRow.Cells[2].Value = Math.Round(
                DataModel.Loan.Transactions?.IncomeExpenseSummary?.TotalFortnightly +
                DataModel.Loan.FortnightlyRepayment ?? 0, 0).ToCurrency();
            totalExpenseRow.Cells[3].Value =
                Math.Round(
                    DataModel.Loan.Transactions?.IncomeExpenseSummary?.TotalMonthly +
                    DataModel.Loan.MonthlyRepayment ?? 0, 0).ToCurrency();
            totalExpenseRow.Cells[4].Value =
                Math.Round(
                    DataModel.Loan.Transactions?.IncomeExpenseSummary?.TotalYearly +
                    DataModel.Loan.YearlyRepayment ?? 0, 0).ToCurrency();

            totalExpenseRow.Cells[0].Style = CustomTableCellStyle(DefaultCellTextBgBrush, DefaulCellTextFgBrush, borderBrush, 10, PdfFontStyle.Bold);
            totalExpenseRow.Cells[1].Style = defaultCellStyle;
            totalExpenseRow.Cells[2].Style = defaultCellStyle;
            totalExpenseRow.Cells[3].Style = defaultCellStyle;
            totalExpenseRow.Cells[4].Style = defaultCellStyle;

            // Draw the grid on the page with left padding
            float leftPadding = 20; // Adjust the left padding as needed
            PdfGridLayoutResult result = pdfGrid.Draw(Page, new PointF(leftPadding, _yPosition));

            // Check if the text was drawn on a new page
            if (result.Page != null && result.Page != Page)
            {
                Page = result.Page;
            }
            _yPosition = result.Bounds.Bottom + 10; // Adjust the spacing as needed

            //_yPosition += result.Bounds.Bottom + 10; // Adjust the spacing as needed
        }

        private void DrawTransactionRecordsTable(IncomeExpenseBase? records,
            string headerColumnValue,
            string headerRowBgColor = "", string headerRowTextColor = "",
            string columnHeaderBgColor = "", string columnHeaderTextColor = "",
            string totalRowHeaderBgColor = "", string totalRowHeaderTextColor = "",
            string borderBrushColor = "",
            bool colorShadeTopValue = false, PdfBrush? cellHighlightBgBrush = null)
        {
            if (records?.IncomeExpenseEntries == null || !records.IncomeExpenseEntries.Any())
            {
                return;
            }

            var headerRowBgBrush = headerRowBgColor.IsEmpty() ? DefaultHeaderRowBgBrush : headerRowBgColor.ToPdfBrush();
            var headerRowTextBrush = headerRowTextColor.IsEmpty() ? DefaultHeaderRowTextBrush : headerRowTextColor.ToPdfBrush();
            var columnHeaderBgBrush = columnHeaderBgColor.IsEmpty() ? DefaultColumnHeaderBgBrush : columnHeaderBgColor.ToPdfBrush();
            var columnHeaderTextBrush = columnHeaderTextColor.IsEmpty() ? DefaultColumnHeaderTextBrush : columnHeaderTextColor.ToPdfBrush();
            var totalRowHeaderBgBrush = totalRowHeaderBgColor.IsEmpty() ? DefaultTotalRowHeaderBgBrush : totalRowHeaderBgColor.ToPdfBrush();
            var totalRowHeaderTextBrush = totalRowHeaderTextColor.IsEmpty() ? DefaultTotalRowHeaderTextBrush : totalRowHeaderTextColor.ToPdfBrush();
            var highlightedCellBgBrush = cellHighlightBgBrush == null ? DefaultHighlightedCellBgBrush : cellHighlightBgBrush;
            var borderBrush = borderBrushColor.IsEmpty() ? DefaultBorderBrush : borderBrushColor.ToPdfBrush();

            // Set default style
            var headerStyle = CustomTableCellStyle(headerRowBgBrush, headerRowTextBrush, borderBrush, 11, PdfFontStyle.Bold);
            var boldCellStyle = CustomTableCellStyle(columnHeaderBgBrush, columnHeaderTextBrush, borderBrush, 10, PdfFontStyle.Bold);

            Func<double?, PdfGridCellStyle> totalRowHeaderStyle = (cellValue) => CustomTableCellStyle(totalRowHeaderBgBrush,
                _defaultTextFgBasedOnValueBrush(cellValue, positiveBrush: totalRowHeaderTextBrush),
                borderBrush, 10, PdfFontStyle.Bold);

            Func<double, PdfGridCellStyle> defaultCellStyle = (cellValue) => CustomTableCellStyle(DefaultCellTextBgBrush,
                _defaultTextFgBasedOnValueBrush(cellValue),
                borderBrush, 11, PdfFontStyle.Regular);

            records.CalculatePercentages();

            AddNewLineSpace();

            // Create a PdfGrid
            PdfGrid pdfGrid = new PdfGrid();

            // Add columns to the grid
            pdfGrid.Columns.Add(6);

            PdfGridRow header = pdfGrid.Headers.Add(1)[0];
            header.Cells[0].Value = headerColumnValue;
            header.Cells[1].Value = "Weekly";
            header.Cells[2].Value = "Fortnightly";
            header.Cells[3].Value = "Monthly";
            header.Cells[4].Value = "Yearly";
            header.Cells[5].Value = "% of Total";

            foreach (PdfGridCell cell in header.Cells)
            {
                cell.Style = headerStyle;
            }

            // Find the entry with the highest percentage
            double highestPercentage = records.IncomeExpenseEntries.Max(entry => entry.Percentage);

            foreach (var item in records.IncomeExpenseEntries)
            {
                PdfGridRow row = pdfGrid.Rows.Add();
                row.Cells[0].Value = item.Name;
                row.Cells[1].Value = item.AmountWeekly.ToCurrency();
                row.Cells[2].Value = item.AmountFortnightly.ToCurrency();
                row.Cells[3].Value = item.AmountMonthly.ToCurrency();
                row.Cells[4].Value = item.AmountYearly.ToCurrency();
                row.Cells[5].Value = item.Percentage.ToString("0.00") + "%";

                // Apply default styles
                row.Cells[0].Style = boldCellStyle;
                row.Cells[1].Style = defaultCellStyle(item.AmountWeekly);
                row.Cells[2].Style = defaultCellStyle(item.AmountFortnightly);
                row.Cells[3].Style = defaultCellStyle(item.AmountMonthly);
                row.Cells[4].Style = defaultCellStyle(item.AmountYearly);
                row.Cells[5].Style = defaultCellStyle(item.Percentage);

                // Highlight the row with the highest percentage
                if (colorShadeTopValue && item.Percentage == highestPercentage)
                {
                    row.Cells[4].Style = CustomTableCellStyle(highlightedCellBgBrush, _defaultTextFgBasedOnValueBrush(item.AmountYearly),
                        borderBrush, 11, PdfFontStyle.Bold); ; // Yearly column
                    row.Cells[5].Style = CustomTableCellStyle(highlightedCellBgBrush, DefaulCellTextFgBrush,
                        borderBrush, 11, PdfFontStyle.Bold); ; // Percentage column
                }
            }

            PdfGridRow totalExpenseRow = pdfGrid.Rows.Add();
            totalExpenseRow.Cells[0].Value = "Total";
            totalExpenseRow.Cells[1].Value = records?.IncomeExpenseSummary?.TotalWeekly.ToCurrency();
            totalExpenseRow.Cells[2].Value = records?.IncomeExpenseSummary?.TotalFortnightly.ToCurrency();
            totalExpenseRow.Cells[3].Value = records?.IncomeExpenseSummary?.TotalMonthly.ToCurrency();
            totalExpenseRow.Cells[4].Value = records?.IncomeExpenseSummary?.TotalYearly.ToCurrency();

            totalExpenseRow.Cells[0].Style = CustomTableCellStyle(DefaultCellTextBgBrush, DefaulCellTextFgBrush, borderBrush, 10, PdfFontStyle.Bold);
            totalExpenseRow.Cells[1].Style = totalRowHeaderStyle(records?.IncomeExpenseSummary?.TotalWeekly);
            totalExpenseRow.Cells[2].Style = totalRowHeaderStyle(records?.IncomeExpenseSummary?.TotalFortnightly);
            totalExpenseRow.Cells[3].Style = totalRowHeaderStyle(records?.IncomeExpenseSummary?.TotalMonthly);
            totalExpenseRow.Cells[4].Style = totalRowHeaderStyle(records?.IncomeExpenseSummary?.TotalYearly);
            totalExpenseRow.Cells[5].Style = defaultCellStyle(0);

            // Draw the grid on the page with left padding
            float leftPadding = 20; // Adjust the left padding as needed
            PdfGridLayoutResult result = pdfGrid.Draw(Page, new PointF(leftPadding, _yPosition));

            // Check if the text was drawn on a new page
            if (result.Page != null && result.Page != Page)
            {
                Page = result.Page;
            }
            _yPosition = result.Bounds.Bottom + 10; // Adjust the spacing as needed

            AddNewLineSpace();
        }

        //private async Task DrawPieChart()
        //{
        //    // Create the Chart
        //    var chart = new SfCircularChart
        //    {
        //        Legend = new ChartLegend
        //        {
        //            Placement = LegendPlacement.Bottom,
        //            ToggleSeriesVisibility = true,
        //            LabelStyle = new ChartLegendLabelStyle
        //            {
        //                FontFamily = "Arial", // Replace with actual font
        //                TextColor = Colors.Gray
        //            }
        //        }
        //    };

        //    // Define Data
        //    var chartData = new List<ChartDataModel>
        //    {
        //        new ChartDataModel("Principal", 150000),
        //        new ChartDataModel("Interest", 50000),
        //        new ChartDataModel("Taxes", 10000),
        //        new ChartDataModel("Insurance", 5000)
        //    };

        //    // Hardcoded Color Palette
        //    var customColors = new List<Brush>
        //    {
        //        new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromRgb(52, 152, 219)),  // Blue
        //        new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromRgb(231, 76, 60)),   // Red
        //        new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromRgb(46, 204, 113)),  // Green
        //        new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromRgb(241, 196, 15))   // Yellow
        //    };

        //    // Create Doughnut Series
        //    var series = new DoughnutSeries
        //    {
        //        ItemsSource = chartData,
        //        XBindingPath = "Category",
        //        YBindingPath = "Value",
        //        InnerRadius = 0.5,
        //        ShowDataLabels = true,
        //        LegendIcon = ChartLegendIconType.Circle,
        //        PaletteBrushes = customColors,
        //        ExplodeAll = false,
        //        ExplodeOnTouch = false,

        //        // Data Label Settings
        //        DataLabelSettings = new CircularDataLabelSettings
        //        {
        //            LabelPosition = ChartDataLabelPosition.Inside,
        //            UseSeriesPalette = true,
        //            LabelStyle = new ChartDataLabelStyle
        //            {
        //                FontFamily = "Arial", // Replace with actual font
        //                FontSize = 10,
        //                LabelFormat = "c"
        //            }
        //        }
        //    };

        //    // Add Center View with Custom Label
        //    series.CenterView = new StackLayout
        //    {
        //        HorizontalOptions = LayoutOptions.FillAndExpand,
        //        VerticalOptions = LayoutOptions.FillAndExpand,
        //        Children =
        //        {
        //            new Microsoft.Maui.Controls.Label
        //            {
        //                FormattedText = new FormattedString
        //                {
        //                    Spans =
        //                    {
        //                        new Span { Text = "$", FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Colors.Black },
        //                        new Span { Text = "215,000", FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Colors.Black }
        //                    }
        //                },
        //                HorizontalTextAlignment = TextAlignment.Center
        //            }
        //        }
        //    };

        //    // Add Series to Chart
        //    chart.Series.Add(series);

        //    try
        //    {
        //        await using Stream imageStream = await chart.GetStreamAsync(ImageFileFormat.Png);
        //        {
        //            // Step 2: Create a PDF Document
        //            PdfGraphics graphics = Page.Graphics;

        //            // Step 3: Load Image into PDF
        //            PdfBitmap chartImage = new PdfBitmap(imageStream);

        //            // Step 4: Draw the Image in the PDF
        //            graphics.DrawImage(chartImage,
        //                new RectangleF(50, _yPosition, Page.Graphics.ClientSize.Width - 100, 300)); // Adjust size & position

        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }

        //}

        private void GeneratePropertyInsights()
        {
            InsertBlankPage();

            DrawH1("Property Insights");

            RenderProperty();

            RenderLoan();

            RenderExpense();

            RenderIncome();
            RenderIncomeAfterExpense();
            RenderIncomeAfterExpenseIncludingProperty();
        }
        private void RenderProperty()
        {
            AddNewLineSpace();
            DrawH2("Property");
            DrawFormattedText($"{DataModel.Loan.PropertyAmount.ToCurrency()}", "Property Purchase Price", "The settled amount required to acquire ownership of the property.");
            DrawFormattedText($"{DataModel.Loan.OtherExpenseTotalAmount.ToCurrency()}", "Additional Upfront Costs (estimated)", "These are essential expenses associated with the purchase, including but not limited to:");

            List<List<TextElementModel>> otherExpensesBullets = new()
            {
                new()
                {
                    new("Stamp Duty", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new("estimated", DefaultFontFamily, 8, PdfFontStyle.Italic, DefaultTextBrush, 1),
                    new($"({DataModel.Loan.StampDuty.ToCurrency()}): A government-imposed fee tied to the property transaction.", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 2)
                },
                new()
                {
                    new("Mortgage Charges", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"({DataModel.Loan.MortgageCharges.ToCurrency()}): Includes fees like Lender's Mortgage Insurance (LMI), application charges, property valuation costs, account maintenance fees.", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 1)
                },
                new()
                {
                    new("Bank Fees", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"({DataModel.Loan.BankSettlementFee.ToCurrency()}): Costs related to loan application or approval processes.", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 1)
                },
                new()
                {
                    new("Conveyancer Fees", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"({DataModel.Loan.ConveyancerFee.ToCurrency()}): Professional charges for overseeing the legal aspects of the property transfer.", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 1)
                },
                new()
                {
                    new("Inspection Charges", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"({DataModel.Loan.InspectionFee.ToCurrency()}): Fees incurred for property evaluations to ensure its condition and compliance.", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 1)
                },
                new()
                {
                    new("Other Expenses", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"({DataModel.Loan.OtherExpenses.ToCurrency()}): Covers moving costs, utility setup fees, renovations, council rates, and home insurance expenses.", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 1)
                }
            };
            DrawBulletPoints(otherExpensesBullets);

            DrawFormattedText(DataModel.Loan.TotalPropertyAmount.ToCurrency(), "Total Property Cost (estimated)", "The total investment in the property combines the actual purchase price and additional upfront costs. This comprehensive sum should be carefully considered when planning your financial commitment.");
        }
        private void RenderLoan()
        {
            InsertBlankPage();
            DrawH2("Loan");
            DrawFormattedText(DataModel.Loan.LoanAmount.ToCurrency(), "Total Loan Amount", "The aggregate loan amount to be procured from the bank");
            DrawFormattedText(DataModel.Loan.DepositAmount.ToCurrency(), "Total Deposit Amount", "The full sum you must have available upfront as a deposit before applying for a loan or when settling the loan");
            DrawFormattedText(DataModel.Loan.TotalRepayment.ToCurrency(), "Total Loan Repayment Amount", "The total repayment to the bank comprises two main components: the principal and the interest. The principal is the original amount borrowed, while the interest is the additional cost incurred for the privilege of borrowing that amount. Together, these two elements constitute the overall repayment amount, representing the combined sum of the borrowed principal and the interest accrued over the loan term.");
            DrawFormattedText(DataModel.Loan.TotalInterest.ToCurrency(), "Total Interest Cost", "Overall amount of interest that is required to repay to the bank over the course of a loan. This includes the interest accrued on the principal amount borrowed. It's an important factor to be aware of, as it represents the cost of borrowing and is a significant component of the total repayment.");
            DrawFormattedText($"{DataModel.Loan.LoanTermInYears} years", "Loan Duration", "Period over which a loan is scheduled to be repaid. It is the duration during which is obligated to make regular payments toward the loan, including both principal and interest.");
            DrawFormattedText($"{DataModel.Loan.InterestRate}%", "Interest Rate", "The cost of borrowing is set at 5% per annum.");
            DrawFormattedText(DataModel.Loan.RepaymentFrequency, "Repayment Frequency", "Loan repayments are scheduled fortnightly.");
            DrawFormattedText($"{DataModel.Loan.TermPayment.ToCurrency()} {DataModel.Loan.RepaymentFrequency}", "Repayment during the frequency", "A payment of $2,500 is required every fortnight.");
            DrawFormattedText($"{DataModel.Loan.YearlyRepayment.ToCurrency()} yearly", "Annual Repayment", "The yearly repayment totals $65,000.");

            DrawH3("Amortization");

            DrawParagraph("Amortization refers to the process of spreading loan repayments over a set period, typically including both principal and interest, in regular installments. As repayments are made, the loan balance gradually decreases, with an increasing portion of each payment applied to the principal over time.");

            AddNewLineSpace(20);
            // Draw table
            DrawAmortisationTable();
        }
        private void RenderCostOfNewPropertyOwnership()
        {
            AddNewLineSpace();
            DrawH2("Costs of New Property Ownership");

            DrawFormattedText($"{DataModel.Loan.TotalMonthlyRunningExpense.ToCurrency()}", "Monthly additional expense for this property (estimated)", "This refers to the recurring monthly costs associated with the property, which might include maintenance charges, utility bills, and other expenses directly tied to its ownership. These expenses are a crucial consideration for budgeting after the initial purchase.");

            DrawFormattedText($"{DataModel.Loan.TotalYearlyRunningExpense.ToCurrency()}", "Yearly Additional Expense for This Property (estimated)", "Yearly additional expense for this property encompasses total utility costs, maintenance fees, insurance premiums, and other recurring annual charges.");

            DrawTransactionRecordsTable(DataModel.Loan.Transactions, "Expense", colorShadeTopValue: true, cellHighlightBgBrush: DefaultCellNegativeBgBrush);

            AddNewLineSpace();
            DrawH2("Property Financial Commitments");

            DrawFormattedText($"{DataModel.Loan.MonthlyRepaymentWithExpenses.ToCurrency()}", "Monthly Total Expense for This Property (estimated)", "Includes the mortgage payment and recurring costs such as utilities, maintenance, and insurance, giving a comprehensive view of monthly financial commitments.");

            List<List<TextElementModel>> monthlyPropertyTotalBullets = new()
            {
                new()
                {
                    new($"{DataModel.Loan.MonthlyRepayment.ToCurrency()}", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"Repayment on a monthly basis", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 2)
                },
                new()
                {
                    new($"{DataModel.Loan.TotalMonthlyRunningExpense.ToCurrency()}", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"Monthly additional expense for this property (estimated)", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 2)
                }
            };
            DrawBulletPoints(monthlyPropertyTotalBullets);

            DrawFormattedText($"{DataModel.Loan.YearlyRepaymentWithExpenses.ToCurrency()}", "Yearly Total Expense for This Property (estimated)", "Combines the yearly mortgage repayment with all recurring annual costs such as utilities, maintenance, and insurance, to provide a comprehensive summary of total yearly obligations.");

            List<List<TextElementModel>> yearlyPropertyTotalBullets = new()
            {
                new()
                {
                    new($"{DataModel.Loan.YearlyRepayment.ToCurrency()}", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"Repayment on a yearly basis", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 2)
                },
                new()
                {
                    new($"{DataModel.Loan.TotalYearlyRunningExpense.ToCurrency()}", DefaultFontFamily, 12, PdfFontStyle.Bold, DefaultTextBrush, 1),
                    new($"Yearly additional expense for this property (estimated)", DefaultFontFamily, 12, PdfFontStyle.Regular, DefaultTextBrush, 2)
                }
            };
            DrawBulletPoints(yearlyPropertyTotalBullets);

            AddNewLineSpace();
            DrawExpenseOnNewPropertyTable();
        }
        private void RenderExpense()
        {
            InsertBlankPage();

            DrawH1("Your Expenses");

            RenderCostOfNewPropertyOwnership();

            AddNewLineSpace(20);
            DrawH2("Current Financial Outflows");

            DrawFormattedText($"{DataModel.Expense.TotalMonthly.ToCurrency()}", "Monthly Expenses Recorded", "Represents the total recurring costs incurred on a monthly basis, including utility bills, maintenance charges, and other recurring expenses.");

            DrawFormattedText($"{DataModel.Expense.TotalYearly.ToCurrency()}", "Yearly Expenses Recorded", "Represents the cumulative recurring costs incurred over a year, including utility bills, maintenance charges, and other recurring annual expenses.");

            DrawTransactionRecordsTable(DataModel.Expense.Transactions, "Expense", colorShadeTopValue: true, cellHighlightBgBrush: DefaultCellNegativeBgBrush);
        }
        private void RenderIncome()
        {
            InsertBlankPage();
            DrawH1("Your Income");

            DrawFormattedText($"{DataModel.Income.TotalMonthly.ToCurrency()}", "Monthly Income earned", "Represents the total income generated on a monthly basis, reflecting the earnings recorded");

            DrawFormattedText($"{DataModel.Income.TotalYearly.ToCurrency()}", "Yearly Total Income", "Represents the cumulative income earned over a year, giving a comprehensive view of annual financial inflow");

            DataModel.Income.ResetTransactions();
            DrawTransactionRecordsTable(DataModel.Income.Transactions, "Income", colorShadeTopValue: true, cellHighlightBgBrush: DefaultCellPositiveBgBrush);
        }
        private void RenderIncomeAfterExpense()
        {
            int fontSize = 11;

            AddNewLineSpace();
            AddNewLineSpace();
            DrawH2("Income After Expense");

            DrawFormattedText($"{DataModel.Income.TotalAfterExpenseMonthly.ToCurrency()}",
                $"Monthly Net Income",
                $"{DataModel.Income.TotalAfterExpenseMonthly.ToCurrency()} This represents the net earnings remaining after deducting {DataModel.Expense.TotalMonthly.ToCurrency()} in monthly expenses from the total monthly income of {DataModel.Income.TotalMonthly.ToCurrency()}.",
                amountTextBrush: _defaultTextFgBasedOnValueBrush(DataModel.Income.TotalAfterExpenseMonthly));

            var incomeAfterExpenseMonthlyDistributionText = new List<TextElementModel>
            {
                new("(", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("Calculation:", DefaultFontFamily, fontSize - 1, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new($"{DataModel.Income.TotalMonthly.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("income", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new($"- {DataModel.Expense.TotalMonthly.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("expenses", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new("=", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new($"{DataModel.Income.TotalAfterExpenseMonthly.ToCurrency()}",
                    DefaultFontFamily, fontSize, PdfFontStyle.Bold, _defaultTextFgBasedOnValueBrush(DataModel.Income.TotalAfterExpenseMonthly), 1),
                new("net income", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new(")", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
            };
            DrawTextElements(incomeAfterExpenseMonthlyDistributionText, _yPosition, updateYPosition: true);
            AddNewLineSpace();

            DrawFormattedText($"{DataModel.Income.TotalAfterExpenseYearly.ToCurrency()}",
                $"Yearly Net Income",
                $"{DataModel.Income.TotalAfterExpenseYearly.ToCurrency()} This reflects the total annual income after subtracting {DataModel.Expense.TotalYearly.ToCurrency()} in yearly expenses from the total annual income of {DataModel.Income.TotalYearly.ToCurrency()}.",
                amountTextBrush: _defaultTextFgBasedOnValueBrush(DataModel.Income.TotalAfterExpenseYearly));

            var incomeAfterExpenseYearlyDistributionText = new List<TextElementModel>
            {
                new("(", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("Calculation:", DefaultFontFamily, fontSize - 1, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new($"{DataModel.Income.TotalYearly.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("income", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new($"- {DataModel.Expense.TotalYearly.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("expenses", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new("=", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new($"{DataModel.Income.TotalAfterExpenseYearly.ToCurrency()}",
                    DefaultFontFamily, fontSize, PdfFontStyle.Bold, _defaultTextFgBasedOnValueBrush(DataModel.Income.TotalAfterExpenseYearly), 1),
                new("net income", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new(")", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
            };
            DrawTextElements(incomeAfterExpenseYearlyDistributionText, _yPosition, updateYPosition: true);
            AddNewLineSpace();

            var transactionsCopy = DataModel.Income.TransactionRecordsWithExpense.DeepCloneObject();
            if (transactionsCopy?.IncomeExpenseEntries != null)
            {
                transactionsCopy?.CalculatePercentages();
                foreach (var inc in transactionsCopy?.IncomeExpenseEntries)
                {
                    inc.Amount = (inc.Percentage / 100) * transactionsCopy.IncomeExpenseSummary.TotalYearly;
                    inc.Frequency = TimeFrequencyEnum.Yearly;
                }

                transactionsCopy.CalculatePercentages();

                DrawTransactionRecordsTable(transactionsCopy, "Income", colorShadeTopValue: true, cellHighlightBgBrush: DefaultCellNegativeBgBrush);
            }
        }
        private void RenderIncomeAfterExpenseIncludingProperty()
        {
            int fontSize = 11;

            AddNewLineSpace();
            AddNewLineSpace();
            DrawH2("Income After Expenses and New Investment on This Loan");

            DrawFormattedText($"{DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly.ToCurrency()}",
                $"Monthly Net Income",
                $"{DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly.ToCurrency()} represents the remaining income after deducting monthly expenses, loan repayments, and investment expenses from the total monthly income of {DataModel.Income.TotalMonthly.ToCurrency()}.",
                amountTextBrush: _defaultTextFgBasedOnValueBrush(DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly));

            var incomeAfterExpenseMonthlyDistributionText = new List<TextElementModel>
            {
                new("(", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("Calculation:", DefaultFontFamily, fontSize - 1, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new($"{DataModel.Income.TotalMonthly.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("income", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new($"- {DataModel.Expense.TotalMonthly.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("expenses", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new($"- {DataModel.Loan.MonthlyRepayment.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new($"loan repayment", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new($"- {DataModel.Loan.TotalMonthlyRunningExpense.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new($"investment expenses", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),

                new("=", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new($"{DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly.ToCurrency()}",
                    DefaultFontFamily, fontSize, PdfFontStyle.Bold, _defaultTextFgBasedOnValueBrush(DataModel.Income.TotalAfterExpenseIncludingPropertyMonthly), 1),
                new("net income", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new(")", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
            };
            DrawTextElements(incomeAfterExpenseMonthlyDistributionText, _yPosition, updateYPosition: true);
            AddNewLineSpace();

            DrawFormattedText($"{DataModel.Income.TotalAfterExpenseIncludingPropertyYearly.ToCurrency()}",
                $"Yearly Net Total",
                $"{DataModel.Income.TotalAfterExpenseIncludingPropertyYearly.ToCurrency()} reflects the total annual income after subtracting yearly expenses, loan repayments, and investment expenses from the total annual income of {DataModel.Income.TotalYearly.ToCurrency()}.",
                amountTextBrush: _defaultTextFgBasedOnValueBrush(DataModel.Income.TotalAfterExpenseIncludingPropertyYearly));

            var incomeAfterExpenseYearlyDistributionText = new List<TextElementModel>
            {
                new("(", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("Calculation:", DefaultFontFamily, fontSize - 1, PdfFontStyle.Bold, DefaultTextBrush, 1),
                new($"{DataModel.Income.TotalYearly.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("income", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new($"- {DataModel.Expense.TotalYearly.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new("expenses", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new($"- {DataModel.Loan.YearlyRepayment.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new($"loan repayment", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new($"- {DataModel.Loan.TotalYearlyRunningExpense.ToCurrency()}", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new($"investment expenses", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),

                new("=", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
                new($"{DataModel.Income.TotalAfterExpenseIncludingPropertyYearly.ToCurrency()}",
                    DefaultFontFamily, fontSize, PdfFontStyle.Bold, _defaultTextFgBasedOnValueBrush(DataModel.Income.TotalAfterExpenseIncludingPropertyYearly), 1),
                new("net income", DefaultFontFamily, fontSize, PdfFontStyle.Italic, DefaultTextBrush, 1),
                new(")", DefaultFontFamily, fontSize, PdfFontStyle.Regular, DefaultTextBrush, 1),
            };
            DrawTextElements(incomeAfterExpenseYearlyDistributionText, _yPosition, updateYPosition: true);
            AddNewLineSpace();

            var transactionsCopy = DataModel.Income.TransactionRecordsWithExpenseIncludingProperty.DeepCloneObject();
            if (transactionsCopy?.IncomeExpenseEntries != null)
            {
                transactionsCopy?.CalculatePercentages();
                foreach (var inc in transactionsCopy?.IncomeExpenseEntries)
                {
                    inc.Amount = (inc.Percentage / 100) * transactionsCopy.IncomeExpenseSummary.TotalYearly;
                    inc.Frequency = TimeFrequencyEnum.Yearly;
                }

                transactionsCopy.CalculatePercentages();
                DrawTransactionRecordsTable(transactionsCopy, "Income", colorShadeTopValue: true, cellHighlightBgBrush: DefaultCellNegativeBgBrush);
            }
        }
    }
}
