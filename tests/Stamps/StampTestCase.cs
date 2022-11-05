using System;
using System.IO;
using Rst.Pdf.Stamp;
using Xunit;

namespace Stamps;

[Trait("TestCategory", "Unit")]
public class StampTestCase
{
    [Theory]
    [ClassData(typeof(CmsFactory))]
    public void TestStamp(string name,  SignatureInfo[] info)
    {
        Assert.True(File.Exists(name));
    }
}