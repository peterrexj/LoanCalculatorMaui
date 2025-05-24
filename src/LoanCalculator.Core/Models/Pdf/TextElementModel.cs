using Syncfusion.Pdf.Graphics;

namespace LoanCalculator.Core.Models.Pdf
{
    public class TextElementModel(
        string text,
        PdfFont font,
        PdfBrush? textBrush,
        int numberOfLinesExpected)
    {
        public string Text { get; set; } = text;
        public PdfFont Font { get; set; } = font;
        public PdfBrush? TextBrush { get; set; } = textBrush;
        public int NumberOfLinesExpected { get; set; } = numberOfLinesExpected;
    }
}
