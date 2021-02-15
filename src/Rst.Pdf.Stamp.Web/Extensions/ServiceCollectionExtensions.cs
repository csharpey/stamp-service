using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rst.Pdf.Stamp.Web.Options;

namespace Rst.Pdf.Stamp.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFileStorage(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<FileStorageOptions>(
                configuration.GetSection(FileStorageOptions.Section));

            services.AddSingleton(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<FileStorageOptions>>();
                    var config = new AmazonS3Config
                    {
                        ServiceURL = options.Value.ServiceUrl,
                        ForcePathStyle = true,
                        HttpClientFactory = new SslFactory()
                    };
                    return new AmazonS3Client(
                        options.Value.AccessKey,
                        options.Value.SecretKey,
                        config);
                }
            );
        }
    }
}