using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
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

    private const int DilationIteration = 2;
    private static readonly LineSegment2DF YAxis = new(PointF.Empty, new PointF(0, 1));
    private static readonly LineSegment2DF XAxis = new(PointF.Empty, new PointF(1, 0));

    public PlaceManager(ILogger<PlaceManager> logger)
    {
        _logger = logger;
    }

    public async Task<FreeSpaceMap> FindEmpty(
        Stream memoryStream,
        IReadOnlyCollection<Rectangle> rectangles,
        CancellationToken token)
    {
        var b = new byte[memoryStream.Length];
        var bytesCount = await memoryStream.ReadAsync(b, token);
        Debug.Assert(bytesCount == memoryStream.Length);

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
        debug.Save("debug.png");
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

        var size = contour.Size - 1;
        size = 5;
        for (int i = 0; i < size; i++)
        {
            var rotation = new Mat();
            var line = new LineSegment2DF(contour[i], contour[i + 1]);
            var vector = new VectorOfPoint(new[] { contour[i], contour[i + 1] });

            var ax = line.GetExteriorAngleDegree(XAxis);
            var yx = line.GetExteriorAngleDegree(YAxis);

            ax *= Math.Sign(ax);
            yx *= Math.Sign(yx);
            
            var anchor = (PointF)((Vector2)line.P1 + (Vector2)line.P2 / 2);
            var angle = Math.Abs(yx) > 45 ? ax : yx;

            CvInvoke.GetRotationMatrix2D(anchor, angle, 1, rotation);
            CvInvoke.Transform(vector, vector, rotation);
            fitted.Push(vector);
        }

        return fitted;
    }
}