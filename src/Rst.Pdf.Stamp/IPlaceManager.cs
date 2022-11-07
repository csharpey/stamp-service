using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rst.Pdf.Stamp;

public interface IPlaceManager
{
    Task<FreeSpaceMap> FindEmpty(Stream pdf, IReadOnlyCollection<Rectangle> rectangles, CancellationToken token);
}