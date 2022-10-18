using System;
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

    public PlaceManager(ILogger<PlaceManager> logger, IPdfConverter converter)
    {
        _logger = logger;
        _converter = converter;
    }

    public async Task<FreeSpaceMap> FindNotOccupied(Stream memoryStream, CancellationToken token)
    {
        var png = await _converter.Convert(memoryStream, token);

        var b = new byte[png.Length];
        var bytesCount = await png.ReadAsync(b, token);
        Debug.Assert(bytesCount == png.Length);
        
        var mat = new Mat();
        CvInvoke.Imdecode(b, ImreadModes.Grayscale, mat);

        var img = mat.ToImage<Gray, byte>()
            .SmoothGaussian(5)
            .ThresholdBinaryInv(new Gray(250), new Gray(byte.MaxValue));

        var m = new Mat();
        VectorOfVectorOfPoint contours = new();

        var d = img.CopyBlank();
        var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(50, 50), Point.Empty);
        CvInvoke.Dilate(img, d, kernel, Point.Empty, 1, BorderType.Default, new MCvScalar(255) );
        CvInvoke.FindContours(d, contours, m, RetrType.External, ChainApproxMethod.ChainApproxSimple);
        
        var tmp = d.CopyBlank();
        CvInvoke.DrawContours(tmp, contours, -1, new MCvScalar(255));
        tmp.Save("stamp.png");
        return new FreeSpaceMap();
    }
}