using Microsoft.Extensions.DependencyInjection;
using Rst.Pdf.Stamp.Api.Clients.Generated;

namespace Rst.Pdf.Stamp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddStampApi(this IServiceCollection services)
    {
        return services.AddHttpClient<IStampClient, StampClient>();
    }
}