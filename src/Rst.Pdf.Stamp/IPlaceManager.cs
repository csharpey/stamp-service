using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rst.Pdf.Stamp;

public interface IPlaceManager
{
    Task<FreeSpaceMap> FindNotOccupied(Stream pdf, CancellationToken token);
}