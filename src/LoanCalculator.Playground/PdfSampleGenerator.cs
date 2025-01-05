using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.Drawing;
using System.Diagnostics;
using Syncfusion.Pdf.Grid;

namespace LoanCalculator.Playground
{
    internal class PdfSampleGenerator
    {
        private float _yPosition = 0;

        public void GeneratePdf()
        {
            // Create a new PDF document
            PdfDocument document = new PdfDocument();
            // Add a page to the document
            PdfPage page = document.Pages.Add();

            // Draw header H1
            _yPosition = PageTitle(page, "Property Home Loan Report", _yPosition);

            // Draw subtitle
            _yPosition = PageSubtitle(page, "Comprehensive Overview", _yPosition);

            // Draw title
            GenerateDisclaimerData(document, ref page);
            GeneratePropertyInsights(document, ref page);

            // Draw table
            _yPosition = DrawTable(document, ref page, _yPosition);

            // Save the document to a stream
            using MemoryStream stream = new MemoryStream();
            document.Save(stream);
            // Save the stream as a file in the device and invoke it for viewing
            SaveAndView("Output.pdf", "application/pdf", stream);
        }

        private float PageTitle(PdfPage page, string text, float yPosition)
        {
            PdfStandardFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 28, PdfFontStyle.Bold);
            page.Graphics.DrawString(text, font, PdfBrushes.Black, new PointF(0, yPosition));
            return yPosition + 40; // Adjust the spacing as needed
        }

        private float PageSubtitle(PdfPage page, string text, float yPosition)
        {
            PdfStandardFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Italic);
            page.Graphics.DrawString(text, font, PdfBrushes.Gray, new PointF(0, yPosition));
            return yPosition + 30; // Adjust the spacing as needed
        }

        private float DrawH1(PdfPage page, string text, float yPosition)
        {
            PdfStandardFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 20, PdfFontStyle.Bold);
            page.Graphics.DrawString(text, font, PdfBrushes.Black, new PointF(0, yPosition));
            return yPosition + 35; // Adjust the spacing as needed
        }

        private float DrawH2(PdfPage page, string text, float yPosition)
        {
            PdfStandardFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
            page.Graphics.DrawString(text, font, PdfBrushes.Black, new PointF(0, yPosition));
            return yPosition + 30; // Adjust the spacing as needed
        }

        private float DrawH3(PdfPage page, string text, float yPosition)
        {
            PdfStandardFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
            page.Graphics.DrawString(text, font, PdfBrushes.Black, new PointF(0, yPosition));
            return yPosition + 25; // Adjust the spacing as needed
        }

        private float DrawParagraph(PdfDocument document, ref PdfPage page, string text, float yPosition)
        {
            PdfStandardFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 12);
            PdfTextElement textElement = new PdfTextElement(text, font)
            {
                Brush = PdfBrushes.Black
            };
            // Set layout format for text wrapping
            PdfLayoutFormat layoutFormat = new PdfLayoutFormat
            {
                Layout = PdfLayoutType.Paginate, // Automatically continue to the next page if needed
                Break = PdfLayoutBreakType.FitPage // Wrap text at word boundaries
            };
            PdfLayoutResult result = textElement.Draw(page, new RectangleF(new PointF(0, yPosition), new SizeF(page.GetClientSize().Width, page.GetClientSize().Height - yPosition)), layoutFormat);
            // Check if the text was drawn on a new page
            if (result.Page != page)
            {
                page = result.Page;
                yPosition = result.Bounds.Bottom + 10; // Adjust the spacing as needed
            }
            else
            {
                yPosition = result.Bounds.Bottom + 10; // Adjust the spacing as needed
            }
            return yPosition;
        }

        private float DrawFormattedText(PdfDocument document, ref PdfPage page, string amount, string description, string additionalText, float yPosition)
        {
            PdfStandardFont amountFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
            PdfStandardFont descriptionFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
            PdfStandardFont additionalTextFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12);

            // Calculate the baseline adjustment for alignment
            float baselineAdjustment = amountFont.Height - descriptionFont.Height;

            // Draw amount
            PdfTextElement amountElement = new PdfTextElement(amount, amountFont)
            {
                Brush = PdfBrushes.Black
            };
            PdfLayoutResult amountResult = amountElement.Draw(page, new PointF(0, yPosition));

            // Draw description
            PdfTextElement descriptionElement = new PdfTextElement(description, descriptionFont)
            {
                Brush = PdfBrushes.Black
            };
            PdfLayoutResult descriptionResult = descriptionElement.Draw(page, new PointF(amountResult.Bounds.Right + 5, yPosition + baselineAdjustment));

            // Calculate the starting position for the additional text
            float additionalTextStartY = yPosition + amountFont.Height + 5; // Move to the next line with less space

            // Draw additional text
            PdfTextElement additionalTextElement = new PdfTextElement(additionalText, additionalTextFont)
            {
                Brush = PdfBrushes.Black
            };

            // Set layout format for text wrapping
            PdfLayoutFormat layoutFormat = new PdfLayoutFormat
            {
                Layout = PdfLayoutType.Paginate, // Automatically continue to the next page if needed
                Break = PdfLayoutBreakType.FitPage // Wrap text at word boundaries
            };

            // Draw the additional text starting from the left margin on the next line
            PdfLayoutResult additionalTextResult = additionalTextElement.Draw(page, new RectangleF(new PointF(0, additionalTextStartY), new SizeF(page.GetClientSize().Width, page.GetClientSize().Height - additionalTextStartY)), layoutFormat);

            // Check if the text elements fit on the current page
            if (amountResult.Page != page || descriptionResult.Page != page || additionalTextResult.Page != page)
            {
                page = additionalTextResult.Page;
                yPosition = additionalTextResult.Bounds.Bottom + 10; // Adjust the spacing as needed
            }
            else
            {
                yPosition = additionalTextResult.Bounds.Bottom + 10; // Adjust the spacing as needed
            }

            return yPosition;
        }

        private float DrawTable(PdfDocument document, ref PdfPage page, float yPosition)
        {
            // Create a PdfGrid
            PdfGrid pdfGrid = new PdfGrid();

            // Add columns to the grid
            pdfGrid.Columns.Add(4);

            // Add header
            PdfGridRow header = pdfGrid.Headers.Add(1)[0];
            header.Cells[0].Value = "Column 1";
            header.Cells[1].Value = "Column 2";
            header.Cells[2].Value = "Column 3";
            header.Cells[3].Value = "Column 4";

            // Set header style
            PdfGridCellStyle headerStyle = new PdfGridCellStyle
            {
                BackgroundBrush = PdfBrushes.LightGray,
                TextBrush = PdfBrushes.Black,
                Font = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold),
                Borders = new PdfBorders { All = new PdfPen(PdfBrushes.Black, 0.5f) },
                CellPadding = new PdfPaddings(5, 5, 5, 5) // Add padding to the header cells
            };
            foreach (PdfGridCell cell in header.Cells)
            {
                cell.Style = headerStyle;
            }

            // Add rows
            for (int i = 0; i < 5; i++)
            {
                PdfGridRow row = pdfGrid.Rows.Add();
                row.Cells[0].Value = $"Row {i + 1}, Column 1";
                row.Cells[1].Value = $"Row {i + 1}, Column 2";
                row.Cells[2].Value = $"Row {i + 1}, Column 3";
                row.Cells[3].Value = $"Row {i + 1}, Column 4";

                // Set cell style
                PdfGridCellStyle cellStyle = new PdfGridCellStyle
                {
                    BackgroundBrush = PdfBrushes.White,
                    TextBrush = PdfBrushes.Black,
                    Font = new PdfStandardFont(PdfFontFamily.Helvetica, 12),
                    Borders = new PdfBorders { All = new PdfPen(PdfBrushes.Black, 0.5f) },
                    CellPadding = new PdfPaddings(5, 5, 5, 5) // Add padding to the cells
                };
                foreach (PdfGridCell cell in row.Cells)
                {
                    cell.Style = cellStyle;
                }
            }

            // Draw the grid on the page with left padding
            float leftPadding = 20; // Adjust the left padding as needed
            PdfGridLayoutResult result = pdfGrid.Draw(page, new PointF(leftPadding, yPosition));

            // Return the new y-position after the table
            return result.Bounds.Bottom + 10; // Adjust the spacing as needed
        }









        private void SaveAndView(string fileName, string contentType, MemoryStream stream)
        {
            // Save the stream as a file in the device and invoke it for viewing
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            File.WriteAllBytes(filePath, stream.ToArray());
            // Open the file with the default PDF viewer
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }

        private void GenerateDisclaimerData(PdfDocument document, ref PdfPage page)
        {
            _yPosition = DrawH1(page, "Home Loan Calculator App Disclaimer", _yPosition);
            _yPosition = DrawH2(page, "General Disclaimer", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "The Home Loan Calculator App (referred to as \"Home Loan Calculator Insights App\") is provided for informational purposes only. The results and information generated by the App are estimates based on the inputs provided by the user and should not be considered definitive or legally binding. The App does not provide legal, financial, or professional advice and should not be relied upon for such purposes.", _yPosition);

            _yPosition = DrawH2(page, "No Legal Obligations", _yPosition);
            _yPosition = DrawH3(page, "Accuracy of Information", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "The App aims to provide accurate calculations and information. However, the accuracy, completeness, and timeliness of the data provided cannot be guaranteed. The App owner (referred to as \"we\" or \"us\") makes no representations or warranties of any kind, express or implied, about the reliability, suitability, or availability of the App or the information, products, services, or related graphics contained in the App for any purpose.", _yPosition);

            _yPosition = DrawH3(page, "No Liability", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "We will not be liable for any loss or damage, including but not limited to indirect or consequential loss or damage, or any loss or damage whatsoever arising from loss of data or profits arising out of or in connection with the use of the App. Users use the App at their own risk.", _yPosition);

            _yPosition = DrawH3(page, "No Financial Advice", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "The App is not intended to substitute professional financial advice. The information generated by the App is based on the inputs provided by the user and should be used as a guide only. It is recommended that users consult with their financial adviser before making any financial decisions. We do not guarantee the suitability or appropriateness of any financial product or transaction described in the App.", _yPosition);

            _yPosition = DrawH3(page, "Third-Party Links", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "The App may contain links to third-party websites or services not owned or controlled by us. We have no control over, and assume no responsibility for, the content, privacy policies, or practices of any third-party websites or services. By using the App, you expressly relieve us from any liability arising from your use of any third-party website or service.", _yPosition);

            _yPosition = DrawH3(page, "Changes to the App", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "We reserve the right to make changes to the App or discontinue any part of the App without notice at any time. We shall not be liable to users or any third party should we exercise such right.", _yPosition);

            _yPosition = DrawH2(page, "User Responsibilities", _yPosition);
            _yPosition = DrawH3(page, "Accuracy of Inputs", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "Users are responsible for ensuring the accuracy of the inputs they provide to the App. The results generated by the App are only as accurate as the information provided by the user.", _yPosition);

            _yPosition = DrawH3(page, "Professional Consultation", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "Users should seek advice from a qualified financial adviser to understand the financial implications of their decisions fully. The App should not be the sole basis for making financial decisions.", _yPosition);

            _yPosition = DrawH3(page, "Compliance with Laws", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "Users must comply with all applicable laws and regulations when using the App. We are not responsible for any illegal use of the App by users.", _yPosition);

            _yPosition = DrawH2(page, "Conclusion", _yPosition);
            _yPosition = DrawParagraph(document, ref page, "By using the Home Loan Calculator Insights App, you acknowledge that you have read this disclaimer, understand it, and agree to be bound by its terms and conditions. If you do not agree with any part of this disclaimer, you must not use the App.\r\nThis disclaimer is subject to change without notice. Users are recommended to review the disclaimer periodically for any updates or changes.\r\nFor any questions or concerns regarding this disclaimer, please contact us at [yoursimpleapps@gmail.com].\r\n", _yPosition);
        }

        private void GeneratePropertyInsights(PdfDocument document, ref PdfPage page)
        {
            _yPosition = DrawH1(page, "Insights", _yPosition);
            
            _yPosition = DrawH2(page, "Property", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$1,000,000", "Cost of the property", "the estimated cost of the proposed property", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$35,000", "Additional upfront costs (estimated)", "Additional upfront costs for a property can include various fees and expenses beyond the purchase price, from stamp duty, bank fees, conveyancer fees, inspection fees and more", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$1,035,000", "Total Property Cost (Estimated)", "The total investment in the property, combining both the actual purchase price and the additional upfront costs, forms the comprehensive sum that need to bed considered when planning the financial commitment", _yPosition);

            _yPosition = DrawH2(page, "Loan", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$1,000,000", "Total Loan Amount", "The aggregate loan amount to be procured from the bank", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$1,000,000", "Total Deposit Amount", "The full sum you must have available upfront as a deposit before applying for a loan or when settling the loan", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$1,000,000", "Total Loan Repayment Amount", "The total repayment to the bank comprises two main components: the principal and the interest. The principal is the original amount borrowed, while the interest is the additional cost incurred for the privilege of borrowing that amount. Together, these two elements constitute the overall repayment amount, representing the combined sum of the borrowed principal and the interest accrued over the loan term.", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$1,000,000", "Total Interest Cost", "Overall amount of interest that is required to repay to the bank over the course of a loan. This includes the interest accrued on the principal amount borrowed. It's an important factor to be aware of, as it represents the cost of borrowing and is a significant component of the total repayment.", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "30 years", "Loan Duration", "Period over which a loan is scheduled to be repaid. It is the duration during which is obligated to make regular payments toward the loan, including both principal and interest.", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "5%", "Interest Rate", "", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "fortnightly", "Repayment Frequency", "", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$2,500 fortnightly", "Repayment during the frequency", "", _yPosition);
            _yPosition = DrawFormattedText(document, ref page, "$65,000 yearly", "Annual Repayment", "", _yPosition);
        }

    }
}