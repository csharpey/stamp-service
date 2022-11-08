using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Rst.Pdf.Stamp.Tests.Unit;

[Trait("TestCategory", "Unit")]
public class PlacementTests
{
    private readonly ITestOutputHelper _output;
    private readonly IPlaceManager _manager;

    public PlacementTests(ITestOutputHelper output)
    {
        _output = output;
        _manager = new PlaceManager(new Mock<ILogger<PlaceManager>>().Object);
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
        await _manager.FindEmpty(stream, Array.Empty<Rectangle>(), source.Token);
    }

}