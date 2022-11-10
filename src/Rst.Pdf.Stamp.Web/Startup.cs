using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Rst.Pdf.Stamp.Web.Extensions;
using Rst.Pdf.Stamp.Web.Interfaces;
using Rst.Pdf.Stamp.Web.Options;
using Rst.Pdf.Stamp.Web.Services;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Rst.Pdf.Stamp.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        Debug.Assert(version is not null);

        services.AddFeatureManagement(Configuration.GetSection("Features"));

        services.AddControllers(options =>
            {
                options.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status500InternalServerError));
                options.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status400BadRequest));
                options.Filters.Add(
                    new ProducesResponseTypeAttribute(typeof(void), StatusCodes.Status403Forbidden));
                options.Conventions.Add(new RouteTokenTransformerConvention(new ParameterTransformer()));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            }).AddViewLocalization();

        services.AddMvc(options =>
            {
                options.Conventions.Add(new RouteTokenTransformerConvention(new ParameterTransformer()));
            })
            .AddViewLocalization();
        services.AddSwaggerGen(options =>
        {
            var info = new OpenApiInfo
            {
                Title = "Pdf stamping service",
                Version = version.ToString(),
                Description = string.Empty,
                Contact = new OpenApiContact { Name = "Nikita Kupreenkov", Email = "kupnsaloxa@gmail.com" },
                License = new OpenApiLicense { Name = "MIT" },
            };
            options.EnableAnnotations();
            options.AddServer(new OpenApiServer
            {
                Description = "Local server",
                Url = new UriBuilder(Uri.UriSchemeHttp, IPAddress.Loopback.ToString(), 5105).ToString()
            });
            options.SwaggerDoc("v1", info);
            options.SwaggerDoc("v2", info);

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = HeaderNames.Authorization,
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
            var xmlCommentsFullPath = Path.Combine(
                AppContext.BaseDirectory,
                string.Join('.', assembly.GetName().Name, "xml"));
            options.IncludeXmlComments(xmlCommentsFullPath);
        });
        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });
        services.AddVersionedApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });
        services.Configure<BucketOptions>(options =>
        {
            options.Public = "public-bucket";
            options.Stamped = nameof(BucketOptions.Stamped).ToLower(CultureInfo.CurrentCulture);
        });

        services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
        });
        services.AddFileStorage(Configuration);
        services.AddScoped<IStampService, StampService>();
        services.AddScoped<ISignatureService, SignatureService>();
        services.AddScoped<IContentTypeProvider, FileExtensionContentTypeProvider>();

        services.AddHttpContextAccessor();
        services.AddRazorPages();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<ITemplateFactory, TemplateFactory>();
        services.AddScoped<IPdfGenerator, PdfGenerator>();
        services.AddScoped<IPlaceManager, PlaceManager>();
        services.AddScoped<IPdfConverter, PngConverter>();

        services.AddHealthChecks()
            .AddRedis(Configuration.GetConnectionString("Redis"))
            .AddS3(options =>
            {
                options.AccessKey = Configuration["S3:AccessKey"];
                options.BucketName = "default";
                options.SecretKey = Configuration["S3:SecretKey"];
                options.S3Config = new AmazonS3Config
                {
                    ServiceURL = Configuration.GetConnectionString(FileStorageOptions.Section),
                    ForcePathStyle = true,
                    HttpClientFactory = new SslFactory()
                };
            }, timeout: TimeSpan.FromMinutes(2));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "v2");
            options.DefaultModelExpandDepth(3);
            options.DocExpansion(DocExpansion.None);
            options.DefaultModelRendering(ModelRendering.Example);
            options.EnableDeepLinking();
            options.DisplayOperationId();
        });

        var supportedCultures = new[] { "ru", "en" }
            .Select(c => new CultureInfo(c)).ToList();

        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(supportedCultures.First()),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
        });

        app.UseHttpLogging();
        app.UseRouting();

        // app.UseAuthentication();
        // app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/hc");
            endpoints.MapControllers();
        });
    }
}