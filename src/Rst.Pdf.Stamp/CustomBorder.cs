using iTextSharp.text.pdf;

namespace Rst.Pdf.Stamp
{
    public class CustomBorder : IPdfPTableEvent
    {
        public void TableLayout(PdfPTable table, float[][] widths, float[] heights, int headerRows, int rowStart, PdfContentByte[] canvases)
        {
            PdfContentByte cb = canvases[PdfPTable.BACKGROUNDCANVAS];
            cb.Rectangle(
                0.1f,
                0.1f,
                table.TotalWidth-0.2f,
                table.TotalHeight-0.2f
            );
            cb.SetLineWidth(0.3f);
            cb.Stroke();
        }
    }
}