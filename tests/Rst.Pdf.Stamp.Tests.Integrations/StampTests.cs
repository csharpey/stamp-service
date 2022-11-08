using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Rst.Pdf.Stamp.Api.Clients.Generated;
using Xunit;
using Xunit.Abstractions;

namespace Rst.Pdf.Stamp.Tests.Integrations;

[Trait("TestCategory", "Integration")]
public class StampTests : IClassFixture<WebApplicationFactory>
{
    private readonly WebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public StampTests(WebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task ApiClientTestCase()
    {
        var source = new CancellationTokenSource();
        await using var scope = _factory.Services.CreateAsyncScope();

        var httpClient = _factory.CreateClient();
        var client = new StampClient(httpClient);

        Assert.NotNull(client);
    }
    
}