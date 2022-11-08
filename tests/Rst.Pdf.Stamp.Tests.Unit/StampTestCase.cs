using System;
using System.IO;
using Rst.Pdf.Stamp;
using Xunit;

namespace Rst.Pdf.Stamp.Tests.Unit;

[Trait("TestCategory", "Unit")]
public class StampTestCase
{
    [Theory]
    [ClassData(typeof(CmsFactory))]
    public void TestStamp(string name)
    {
        Assert.True(File.Exists(name));
    }
}