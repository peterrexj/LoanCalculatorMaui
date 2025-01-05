using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointF = Syncfusion.Drawing.PointF;

namespace LoanCalculatorMaui.Services
{
    public class PdfGenerator
    {
        public void GeneratePdf()
        {
            // Create a new PDF document
            PdfDocument document = new PdfDocument();
            // Add a page to the document
            PdfPage page = document.Pages.Add();
            // Create a font
            PdfStandardFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 12);
            // Draw text on the page
            page.Graphics.DrawString("Hello, World!", font, PdfBrushes.Black, new PointF(0,0));
            // Save the document to a stream
            using MemoryStream stream = new MemoryStream();
            document.Save(stream);
            // Save the stream as a file in the device and invoke it for viewing
            SaveAndView("Output.pdf", "application/pdf", stream);
        }

        private void SaveAndView(string fileName, string contentType, MemoryStream stream)
        {
            // Save the stream as a file in the device and invoke it for viewing
            string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            File.WriteAllBytes(filePath, stream.ToArray());
            // Invoke the file for viewing
            Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
        }
    }
}
