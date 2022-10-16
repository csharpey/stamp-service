using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Amazon.S3;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Microsoft.OpenApi.Models;
using Rst.Pdf.Stamp.Web.Extensions;
using Rst.Pdf.Stamp.Web.Interfaces;
using Rst.Pdf.Stamp.Web.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Rst.Pdf.Stamp.Web
{
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

            services.AddControllers();

            services.AddMvc(options =>
                {
                    options.Conventions.Add(new RouteTokenTransformerConvention(new ParameterTransformer()));
                })
                .AddViewLocalization();
            services.AddVersionedApiExplorer(setup =>
            {
                setup.GroupNameFormat = "'v'VVV";
                setup.SubstituteApiVersionInUrl = true;
            });
            services.AddSwaggerGen(opts =>
            {
                var info = new OpenApiInfo
                {
                    Title = "Pdf stamping service",
                    Version = version.ToString(),
                    Description = string.Empty,
                    Contact = new OpenApiContact { Name = "Nikita Kupreenkov", Email = "kupnsaloxa@gmail.com" },
                    License = new OpenApiLicense { Name = "MIT" },
                };
                opts.SwaggerDoc("v1", info);
                opts.SwaggerDoc("v2", info);

                var xmlCommentsFullPath = Path.Combine(
                    AppContext.BaseDirectory,
                    string.Join('.', assembly.GetName().Name, "xml"));
                opts.IncludeXmlComments(xmlCommentsFullPath);
            });
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });
            services.Configure<BucketOptions>(options =>
            {
                options.Public = "public-bucket";
                options.Stamped = nameof(BucketOptions.Stamped).ToLower();
            });

            services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestBody |
                                        HttpLoggingFields.ResponseBody |
                                        HttpLoggingFields.ResponseStatusCode;
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
}