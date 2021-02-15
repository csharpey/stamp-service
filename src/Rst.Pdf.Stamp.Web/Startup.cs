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

            services.AddSwaggerGen(opts =>
            {
                opts.SwaggerDoc("Rst.Pdf.Stamp",
                    new OpenApiInfo
                    {
                        Title = "Pdf stamping service",
                        Version = version.ToString(),
                        Description = string.Empty
                    });

                var xmlCommentsFullPath = Path.Combine(
                    AppContext.BaseDirectory,
                    string.Join('.', assembly.GetName().Name, "xml"));
                opts.IncludeXmlComments(xmlCommentsFullPath);
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

            services.Configure<SertOptions>(Configuration.GetSection(SertOptions.Section));
            services.AddHttpContextAccessor();
            services.AddRazorPages();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<ITemplateFactory, TemplateFactory>();
            services.AddScoped<IPdfGenerator, PdfGenerator>();

            services.AddHealthChecks()
                .AddRedis(Configuration.GetConnectionString("Redis"))
                .AddS3(options =>
                {
                    options.AccessKey = Configuration["ObjectStorage:AccessKey"];
                    options.BucketName = "bucket";
                    options.SecretKey = Configuration["ObjectStorage:SecretKey"];
                    options.S3Config = new AmazonS3Config
                    {
                        ServiceURL = Configuration["ObjectStorage:ServiceUrl"],
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
                app.UseSwagger();
            }

            app.UseSwaggerUI(opts =>
            {
                opts.SwaggerEndpoint("/swagger.json", "Rst Stamp Service");

                opts.DefaultModelExpandDepth(3);
                opts.DocExpansion(DocExpansion.None);
                opts.DefaultModelRendering(ModelRendering.Example);
                opts.EnableDeepLinking();
                opts.DisplayOperationId();
            });

            var supportedCultures = new[] {"ru", "en"}
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