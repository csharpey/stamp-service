using System.IO;
using BitMiracle.Docotic.Pdf;

namespace Rst.Pdf.Stamp;

public class PlaceManager : IPlaceManager
{
    public FreeSpaceMap FindNotOccupied(MemoryStream memoryStream)
    {
        var png = new MemoryStream();

        using (var pdf = new PdfDocument(memoryStream))
        {
            PdfDrawOptions options = PdfDrawOptions.Create();
            options.BackgroundColor = new PdfRgbColor(255, 255, 255);
            options.HorizontalResolution = 300;
            options.VerticalResolution = 300;

            for (int i = 0; i < pdf.PageCount; ++i)
                pdf.Save(png);
        }

        png.Seek(0, SeekOrigin.Begin);
        memoryStream.Seek(0, SeekOrigin.Begin);

#if DEBUG
        using (var fileStream = new FileStream("stamp.png", FileMode.Create))
        {
            png.CopyTo(fileStream);
        }

        png.Seek(0, SeekOrigin.Begin);
#endif
        return new FreeSpaceMap();
    }
}