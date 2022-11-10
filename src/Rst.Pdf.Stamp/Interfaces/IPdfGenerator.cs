using System.Collections.Generic;
using System.IO;

namespace Rst.Pdf.Stamp.Interfaces;

public interface IPdfGenerator
{
    MemoryStream FromHtml(IReadOnlyCollection<string> html);
}