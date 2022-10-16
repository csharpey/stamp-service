using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;

namespace Rst.Pdf.Stamp;

public class PlaceManager : IPlaceManager
{
    private readonly ILogger<IPlaceManager> _logger;
    private readonly IPdfConverter _converter;

    public PlaceManager(ILogger<IPlaceManager> logger, IPdfConverter converter)
    {
        _logger = logger;
        _converter = converter;
    }

    public async Task<FreeSpaceMap> FindNotOccupied(Stream memoryStream, CancellationToken token)
    {
        var png = await _converter.Convert(memoryStream, token);
        var mat = new Mat();
#if DEBUG
        await using (var fileStream = new FileStream("stamp.png", FileMode.Create))
        {
            await png.CopyToAsync(fileStream, token);
        }

        png.Seek(0, SeekOrigin.Begin);
#endif
        var b = new byte[png.Length];
        var bytesCount = await png.ReadAsync(b, token);
        Debug.Assert(bytesCount == png.Length);
        
        CvInvoke.Imdecode(b, ImreadModes.Grayscale, mat);

        var img = mat.ToImage<Gray, byte>()
            .SmoothGaussian(5)
            .ThresholdBinary(new Gray(250), new Gray(byte.MaxValue));

        VectorOfPoint c = new();
        Mat h = new();

        CvInvoke.FindContours(img, c, h, RetrType.External, ChainApproxMethod.ChainApproxSimple);
        return new FreeSpaceMap();
    }
}