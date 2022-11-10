using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using iTextSharp.text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rst.Pdf.Stamp.Interfaces;
using Rst.Pdf.Stamp.Services;
using Xunit;
using Xunit.Abstractions;
using Rectangle = System.Drawing.Rectangle;

namespace Rst.Pdf.Stamp.Tests.Unit;

[Trait("TestCategory", "Unit")]
public class PlacementTests
{
    private readonly ITestOutputHelper _output;
    private readonly IPlaceManager _manager;

    public PlacementTests(ITestOutputHelper output)
    {
        _output = output;
        var converter = new PngConverter(new NullLogger<PngConverter>());
        _manager = new PlaceManager(new Mock<ILogger<PlaceManager>>().Object, converter);
    }

    [Theory]
    [InlineData("document_1.png")]
    [InlineData("document_2.png")]
    public async Task PlaceManagerTestCase(string path)
    {
        var source = new CancellationTokenSource();

        Assert.True(File.Exists(path));
        await using var stream = new FileStream(path, FileMode.Open);

        Assert.True(stream.CanRead);

        var stamp = new Document(PageSize.A8.Rotate());
        var rectangles = new List<Rectangle>
        {
            new(0, 0, (int)stamp.PageSize.Width, (int)stamp.PageSize.Height)
        };

        var free = await _manager.FreeSpace(stream, rectangles, source.Token);

        Assert.NotEmpty(free);
    }
}