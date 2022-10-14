using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitMiracle.Docotic.Pdf;
using iTextSharp.text;
using iTextSharp.text.html;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;

namespace Rst.Pdf.Stamp
{
    public class PdfGenerator : IPdfGenerator
    {
        public MemoryStream FromHtml(List<string> html)
        {
            FontFactory.Register("wwwroot/Roboto-Regular.ttf");
            var style = new StyleSheet();
            style.LoadTagStyle(HtmlTags.BODY, HtmlTags.FONT, "Roboto");
            style.LoadTagStyle(HtmlTags.BODY, "encoding", BaseFont.IDENTITY_H);

            var document = new Document(PageSize.A8.Rotate(), 0, 0, 0, 0);
            MemoryStream stream = new MemoryStream();
            PdfWriter.GetInstance(document, stream);
            var worker = new HTMLWorker(document)
            {
                Style = style
            };

            document.Open();
            worker.StartDocument();

            for (int i = 0; i < html.Count; i++)
            {
                StringReader strReader = new StringReader(html.ElementAt(i));
                var tables = HTMLWorker.ParseToList(strReader, style);
                var table = (PdfPTable)tables[0];
                table.TableEvent = new CustomBorder();
                table.TotalWidth = PageSize.A10.Height;
                document.SetPageSize(new Rectangle(table.TotalHeight, table.TotalWidth).Rotate());
                document.NewPage();
                document.Add(table);
            }

            worker.EndDocument();
            document.Close();

            var memoryStream = new MemoryStream(stream.ToArray());
#if DEBUG
            using (var fileStream = new FileStream("stamp.pdf", FileMode.Create))
            {
                memoryStream.CopyTo(fileStream);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
#endif

            return memoryStream;
        }
    }
}