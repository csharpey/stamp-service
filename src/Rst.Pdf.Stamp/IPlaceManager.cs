using System.IO;

namespace Rst.Pdf.Stamp;

public interface IPlaceManager
{
    FreeSpaceMap FindNotOccupied(MemoryStream pdf);
}