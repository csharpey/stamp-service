using System.Net;
using System.Net.Http;
using Amazon.Runtime;

namespace Rst.Pdf.Stamp.Web;

public sealed class SslFactory : HttpClientFactory
{
    public override HttpClient CreateHttpClient(IClientConfig clientConfig)
    {
        var httpMessageHandler = CreateClientHandler();
        if (clientConfig.MaxConnectionsPerServer.HasValue)
            httpMessageHandler.MaxConnectionsPerServer = clientConfig.MaxConnectionsPerServer.Value;
        httpMessageHandler.AllowAutoRedirect = clientConfig.AllowAutoRedirect;

        httpMessageHandler.AutomaticDecompression = DecompressionMethods.None;

        var proxy = clientConfig.GetWebProxy();
        if (proxy != null)
        {
            httpMessageHandler.Proxy = proxy;
        }

        if (httpMessageHandler.Proxy != null && clientConfig.ProxyCredentials != null)
        {
            httpMessageHandler.Proxy.Credentials = clientConfig.ProxyCredentials;
        }
        var httpClient = new HttpClient(httpMessageHandler);

        if (clientConfig.Timeout.HasValue)
        {
            httpClient.Timeout = clientConfig.Timeout.Value;
        }
        return httpClient;
    }

    private HttpClientHandler CreateClientHandler() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
}