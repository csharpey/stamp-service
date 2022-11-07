using System;
using System.Collections.Generic;
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

    private const int DilationIteration = 3;
    private static readonly LineSegment2D YAxis = new(Point.Empty, new Point(0, 1));
    private static readonly LineSegment2D XAxis = new(Point.Empty, new Point(1, 0));

    public PlaceManager(ILogger<PlaceManager> logger, IPdfConverter converter)
    {
        _logger = logger;
        _converter = converter;
    }

    public async Task<FreeSpaceMap> FindEmpty(
        Stream memoryStream,
        IReadOnlyCollection<Rectangle> rectangles,
        CancellationToken token)
    {
        var png = await _converter.Convert(memoryStream, token);

        var b = new byte[png.Length];
        var bytesCount = await png.ReadAsync(b, token);
        Debug.Assert(bytesCount == png.Length);

        var src = new Mat();
        CvInvoke.Imdecode(b, ImreadModes.Grayscale, src);

        var img = src.ToImage<Gray, byte>()
            .SmoothGaussian(5)
            .ThresholdBinaryInv(new Gray(250), new Gray(byte.MaxValue));

        var dst = new Mat();
        VectorOfVectorOfPoint contours = new();

        const int textSize = 50;
        var imgCopy = img.CopyBlank();
        var anchor = new Point(-1, -1);
        var size = new Size(textSize, textSize);

        var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, size, anchor);
        CvInvoke.Dilate(img, imgCopy, kernel, anchor, DilationIteration, BorderType.Default, new MCvScalar(255));
        CvInvoke.FindContours(imgCopy, contours, dst, RetrType.External, ChainApproxMethod.ChainApproxSimple);

        var approximatedContours = ApproximatedContours(contours);
#if DEBUG
        var color = new MCvScalar(0);
        const int thickness = 5;

        var debug = src.ToImage<Rgb, byte>();
        // CvInvoke.DrawContours(debug, contours, -1, color, thickness);
        CvInvoke.DrawContours(debug, approximatedContours, -1, color, thickness);
        debug.Save("stamp.png");
#endif
        return new FreeSpaceMap();
    }

    private static VectorOfVectorOfPoint ApproximatedContours(VectorOfVectorOfPoint contours)
    {
        var result = new VectorOfVectorOfPoint();
        for (var i = 0; i < contours.Size; i++)
        {
            var contour = contours[i];
            var length = CvInvoke.ArcLength(contour, true);
            var approximation = new VectorOfPoint();

            CvInvoke.ApproxPolyDP(contour, approximation, 0.003 * length, true);

            result.Push(FitToAxis(approximation));
        }

        return result;
    }

    private static VectorOfPoint FitToAxis(VectorOfPoint contour)
    {
        var fitted = new VectorOfPoint();

        for (int i = 0; i < contour.Size - 1; i++)
        {
            var rotation = new Mat();
            var line = new LineSegment2D(contour[i], contour[i + 1]);
            var vector = new VectorOfPoint(new[] { line.P1, line.P2 });

            var ax = line.GetExteriorAngleDegree(XAxis);
            var yx = line.GetExteriorAngleDegree(YAxis);
            var angle = Math.Abs(ax) > 45 ? -yx : ax;

            CvInvoke.GetRotationMatrix2D(new PointF(0, 0), angle, 1, rotation);
            CvInvoke.Transform(vector, vector, rotation);
            fitted.Push(vector);
        }

        return fitted;
    }
}