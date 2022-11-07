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

    private const int DilationIteration = 5;

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

        const int textSize = 50;

        var image = img.CopyBlank();
        var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(textSize, textSize), Point.Empty);
        CvInvoke.Dilate(img, image, kernel, Point.Empty, DilationIteration, BorderType.Default, new MCvScalar(255));
        CvInvoke.FindContours(image, contours, m, RetrType.External, ChainApproxMethod.ChainApproxSimple);

        double perimetr = CvInvoke.ArcLength(contours[0], true);
        VectorOfPoint approximation = new VectorOfPoint();
        CvInvoke.ApproxPolyDP(contours[0], approximation, 0.004 * perimetr, true);
#if DEBUG
        var tmp = image.CopyBlank();
        CvInvoke.DrawContours(tmp, new VectorOfVectorOfPoint(approximation), -1, new MCvScalar(255));
        tmp.Save("stamp.png");
#endif
        return new FreeSpaceMap();
    }
}