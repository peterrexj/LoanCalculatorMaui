using Syncfusion.Pdf.Graphics;

namespace LoanCalculator.Core.Models.Pdf
{
    public class BoxModel(
        string header,
        string value,
        string secondValue,
        PdfColor backgroundStartColor,
        PdfColor backgroundEndColor,
        PdfColor borderColor)
    {
        public string Header { get; set; } = header;
        public string Value { get; set; } = value;
        public string SecondValue { get; set; } = secondValue;
        public PdfColor BackgroundStartColor { get; set; } = backgroundStartColor;
        public PdfColor BackgroundEndColor { get; set; } = backgroundEndColor;
        public PdfColor BorderColor { get; set; } = borderColor;

        //public PdfColor GetBackgroundStartColor() => BackgroundStartColor.ToPdfColor();
        //public PdfColor GetBackgroundEndColor() => BackgroundEndColor.ToPdfColor();
        //public PdfColor GetBorderColor() => BorderColor.ToPdfColor();
    }
}
