using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using Rst.Pdf.Stamp.Interfaces;

namespace Rst.Pdf.Stamp.Services;

public class PlaceManager : IPlaceManager
{
    private readonly ILogger<IPlaceManager> _logger;
    private readonly IPdfConverter _converter;

    private const int DilationIteration = 2;
    private static readonly LineSegment2DF YAxis = new(PointF.Empty, new PointF(0, 1));
    private static readonly LineSegment2DF XAxis = new(PointF.Empty, new PointF(1, 0));

    public PlaceManager(ILogger<PlaceManager> logger, IPdfConverter converter)
    {
        _logger = logger;
        _converter = converter;
    }

    public async Task<IReadOnlyCollection<Rectangle>> FreeSpace(Stream pngMemoryStream,
        IReadOnlyCollection<Rectangle> rectangles, CancellationToken token)
    {
        var img = await CreateImage(pngMemoryStream, token);
        var contours = await FindTextContours(img, token);
#if DEBUG
        var color = new MCvScalar(0);
        const int thickness = 5;
        var debug = img.Copy().Convert<Rgb, byte>();
        // CvInvoke.DrawContours(debug, contours, -1, color, thickness);
        foreach (var rectangle in rectangles)
        {
            CvInvoke.Rectangle(debug, rectangle, color, thickness);
        }
        CvInvoke.DrawContours(debug, contours, -1, color, thickness);
        debug.Save("debug.png");
#endif
        return await Filter(img, contours, rectangles, token);
    }

    private static async Task<Image<Gray, byte>> CreateImage(Stream pdfMemoryStream, CancellationToken token)
    {
        var buffer = new byte[pdfMemoryStream.Length];
        var bytesCount = await pdfMemoryStream.ReadAsync(buffer, token);
        Debug.Assert(bytesCount == pdfMemoryStream.Length);

        var src = new Mat();
        CvInvoke.Imdecode(buffer, ImreadModes.Grayscale, src);

        return src.ToImage<Gray, byte>()
            .SmoothGaussian(5)
            .ThresholdBinaryInv(new Gray(250), new Gray(byte.MaxValue));
    }

    private static Task<VectorOfVectorOfPoint> FindTextContours(Image<Gray, byte> img,
        CancellationToken token)
    {
        var hierarchy = new Mat();
        VectorOfVectorOfPoint contours = new();

        const int textSize = 50;
        var imgCopy = img.CopyBlank();
        var anchor = new Point(-1, -1);
        var size = new Size(textSize, textSize);

        var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, size, anchor);
        CvInvoke.Dilate(img, imgCopy, kernel, anchor, DilationIteration, BorderType.Default, new MCvScalar(255));
        CvInvoke.FindContours(imgCopy, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

        return Task.FromResult(ApproximatedContours(contours));
    }

    private static async Task<IReadOnlyCollection<Rectangle>> Filter(Image<Gray, byte> img,
        VectorOfVectorOfPoint contours,
        IReadOnlyCollection<Rectangle> rectangles,
        CancellationToken token)
    {
        foreach (var rectangle in rectangles)
        {
        }


        return rectangles;
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
        for (int i = 0; i < size; i++)
        {
            var rotation = new Mat();
            var line = new LineSegment2DF(contour[i], contour[i + 1]);
            var vector = new VectorOfPoint(new[] { contour[i], contour[i + 1] });

            var ax = line.GetExteriorAngleDegree(XAxis);
            var yx = line.GetExteriorAngleDegree(YAxis);

            var anchor = line.P1;
            var angle = Math.Abs(yx) > 45 ? ax : yx;

            CvInvoke.GetRotationMatrix2D(anchor, angle, 1, rotation);
            // CvInvoke.Transform(vector, vector, rotation);
            fitted.Push(vector);
        }

        return fitted;
    }
}