using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rst.Pdf.Stamp.Interfaces;

public interface IPlaceManager
{
    Task<IReadOnlyCollection<Rectangle>> FreeSpace(Stream pngMemoryStream, IReadOnlyCollection<Rectangle> rectangles,
        CancellationToken token);
}