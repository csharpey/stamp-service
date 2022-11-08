using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Rst.Pdf.Stamp.Api.Extensions;
using Rst.Pdf.Stamp.Web;

namespace Rst.Pdf.Stamp.Tests.Integrations;

public class WebApplicationFactory : WebApplicationFactory<Startup>, IDisposable
{
    private readonly Random _random;

    public WebApplicationFactory()
    {
        _random = new Random();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            const int prefixSize = 6;
            var b = new StringBuilder(prefixSize);
            b.Append('_');
            for (var i = 0; i < prefixSize - 1; i++)
            {
                b.Append((char)_random.Next('1', '9'));
            }

            // configurationBuilder.AddJsonFile(
            // Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") is not null
            // ? "appsettings.Testing.Container.json"
            // : "appsettings.Testing.json", false);

            var config = configurationBuilder.Build();
            var c = new NpgsqlConnectionStringBuilder(config.GetConnectionString("Database"));
            c.Database += b;

            configurationBuilder.AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new("ConnectionStrings:Database", c.ToString())
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddStampApi();

            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var context = scopedServices.GetService<DbContext>();

            if (context is null)
            {
                return;
            }

            var logger = scopedServices
                .GetRequiredService<ILogger<WebApplicationFactory>>();

            try
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the " +
                                    "database with test messages. Error: {Message}", ex.Message);
            }
        });
    }

    public new void Dispose()
    {
        var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var context = scopedServices.GetService<DbContext>();

        context?.Database.EnsureDeleted();
    }
}