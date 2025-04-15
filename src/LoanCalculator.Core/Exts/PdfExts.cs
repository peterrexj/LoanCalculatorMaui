using Syncfusion.Pdf.Graphics;
using System.Globalization;

namespace LoanCalculator.Core.Exts
{
    public static class PdfExts
    {
        public static PdfBrush ToPdfBrush(this Syncfusion.Drawing.Color color)
        {
            return new PdfSolidBrush(new PdfColor(color.R, color.G, color.B));
        }
        public static PdfSolidBrush? ToPdfBrush(this string hex)
        {
            // Remove the hash at the start if it's there
            hex = hex.Replace("#", string.Empty);

            // Parse the hex string
            byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

            // Create PdfBrush using PdfColor
            return new PdfSolidBrush(new PdfColor(r, g, b));
        }
        public static PdfColor ToPdfColor(this string hex)
        {
            return Syncfusion.Drawing.ColorTranslator.FromHtml(hex);
        }
    }
}
