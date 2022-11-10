using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using Moq;
using Rst.Pdf.Stamp;
using Rst.Pdf.Stamp.Interfaces;
using Rst.Pdf.Stamp.Services;
using Xunit;

namespace Rst.Pdf.Stamp.Tests.Unit;

[Trait("TestCategory", "Unit")]
public class ConverterTests
{
    private readonly IPdfConverter _converter;

    public ConverterTests()
    {
        _converter = new PngConverter(new Mock<ILogger<PngConverter>>().Object);
    }

    [Theory]
    [InlineData("document_1.pdf")]
    [InlineData("document_2.pdf")]
    public async Task ConvertTestCase(string path)
    {
        var source = new CancellationTokenSource();
        Assert.True(File.Exists(path));
        await using var stream = new FileStream(path, FileMode.Open);
        var png = await _converter.Convert(stream, source.Token);
        Assert.True(png.CanRead);
        Assert.True(png.Length > 0);
    }
}