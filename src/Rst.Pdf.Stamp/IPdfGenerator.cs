using System.Collections;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text.pdf;

namespace Rst.Pdf.Stamp
{
    public interface IPdfGenerator
    {
        MemoryStream FromHtml(List<string> html);
    }
}