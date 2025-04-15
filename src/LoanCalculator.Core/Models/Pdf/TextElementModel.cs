using Syncfusion.Pdf.Graphics;

namespace LoanCalculator.Core.Models.Pdf
{
    public class TextElementModel(
        string text,
        PdfFontFamily fontFamily,
        float fontSize,
        PdfFontStyle fontStyle,
        PdfBrush? textBrush,
        int numberOfLinesExpected)
    {
        public string Text { get; set; } = text;
        public PdfFontFamily FontFamily { get; set; } = fontFamily;
        public float FontSize { get; set; } = fontSize;
        public PdfFontStyle FontStyle { get; set; } = fontStyle;
        public PdfBrush? TextBrush { get; set; } = textBrush;
        public int NumberOfLinesExpected { get; set; } = numberOfLinesExpected;
    }
}
