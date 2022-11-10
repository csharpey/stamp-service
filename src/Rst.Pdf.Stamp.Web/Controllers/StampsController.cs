using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using Rst.Pdf.Stamp.Interfaces;
using Rst.Pdf.Stamp.Web.Interfaces;
using Rst.Pdf.Stamp.Web.Models;
using Rst.Pdf.Stamp.Web.Options;

namespace Rst.Pdf.Stamp.Web.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class StampsController : ControllerBase
{
    private readonly ILogger<StampsController> _logger;
    private readonly IStampService _stampService;
    private readonly ITemplateFactory _templateFactory;
    private readonly AmazonS3Client _client;
    private readonly ISignatureService _signature;
    private readonly BucketOptions _options;
    private readonly IContentTypeProvider _contentTypeProvider;

    public StampsController(
        ILogger<StampsController> logger,
        IStampService stampService,
        IContentTypeProvider contentTypeProvider,
        AmazonS3Client client,
        IOptions<BucketOptions> options,
        ISignatureService signature,
        ITemplateFactory templateFactory)
    {
        _logger = logger;
        _stampService = stampService;
        _contentTypeProvider = contentTypeProvider;
        _client = client;
        _signature = signature;
        _templateFactory = templateFactory;
        _options = options.Value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [FeatureGate(FeatureFlags.S3)]
    [HttpPost("[action]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<IActionResult> Refs([FromBody] FileRef args, CancellationToken token)
    {
        var template = _templateFactory.Template();

        var signatures = await _signature.InfoAsync(args)
            .ContinueWith(t => t.Result.ToImmutableList(), token);

        var file = await _client.GetObjectAsync(args.Bucket, args.Key, token);
        using var archive = new ZipArchive(file.ResponseStream, ZipArchiveMode.Read);

        var stamped = await StampAsync(archive, signatures, template, token)
            .ToListAsync(cancellationToken: token);

        return Ok(stamped);
    }

    private async IAsyncEnumerable<FileRef> StampAsync(ZipArchive archive,
        IReadOnlyCollection<SignatureInfo> signatures,
        IView template,
        [EnumeratorCancellation] CancellationToken token)
    {
        foreach (var g in archive.Entries.GroupBy(e => new FileInfo(e.Name).Extension)
                     .Where(g => SignedDocument.DocumentExtensions.Contains(g.Key)))
        {
            foreach (var entry in g.ToList())
            {
                using (_logger.BeginScope("Archive entry '{Name}'", entry.Name))
                {
                    await using Stream s = new MemoryStream((int)entry.Length);
                    await entry.Open().CopyToAsync(s, token);
                    s.Seek(0, SeekOrigin.Begin);
                    var newFileName = Path.ChangeExtension(entry.Name, "pdf");
                    var contentType = ContentType(newFileName);
                    try
                    {
                        var stream = await _stampService.AddStampAsync(s, signatures, template, token);

                        await _client.PutObjectAsync(new PutObjectRequest
                        {
                            BucketName = _options.Stamped,
                            Key = newFileName,
                            InputStream = stream,
                            ContentType = contentType
                        }, token);
                    }
                    catch (IOException e)
                    {
                        _logger.LogError(e, "Invalid file type");
                        var stream = await _stampService.AddStampAsync(s, signatures, template, token);

                        await _client.PutObjectAsync(new PutObjectRequest
                        {
                            BucketName = _options.Stamped,
                            Key = newFileName,
                            InputStream = stream,
                            ContentType = contentType
                        }, token);
                    }

                    yield return new FileRef
                    {
                        Bucket = _options.Stamped,
                        Key = newFileName
                    };
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Previews([FromForm] PreviewArgs args, CancellationToken token)
    {
        var file = args.File;
        var signatures = await _signature.InfoAsync(args)
            .ContinueWith(t => t.Result.ToImmutableList(), token);

        _logger.LogInformation("Process {FileName} with size {Length}", file.FileName, file.Length);

        var newFileName = Path.ChangeExtension(file.FileName, "pdf");

        var stream = await _stampService.AddStampAsync(file.OpenReadStream(), signatures, null, token);
        var contentType = ContentType(newFileName);

        return File(stream, contentType, newFileName);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [FeatureGate(FeatureFlags.S3)]
    [HttpPost("[action]")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Files([FromForm] StampArgs args, CancellationToken token)
    {
        var signatures = await _signature.InfoAsync(args)
            .ContinueWith(t => t.Result.ToImmutableList(), token);

        var collection = new List<object>();

        foreach (var f in args.Files)
        {
            _logger.LogInformation("Process {FileName} with size {Length}", f.FileName, f.Length);

            var newFileName = Path.ChangeExtension(f.FileName, "pdf");

            var stream = await _stampService.AddStampAsync(f.OpenReadStream(), signatures, null, token);
            var contentType = ContentType(newFileName);

            var request = new PutObjectRequest
            {
                BucketName = _options.Stamped,
                Key = newFileName,
                InputStream = stream,
                ContentType = contentType,
            };
            await _client.PutObjectAsync(request, token);

            collection.Add(new
            {
                BucketName = _options.Stamped,
                Key = newFileName
            });
        }

        return Ok(collection);
    }

    private string ContentType(string name)
    {
        if (!_contentTypeProvider.TryGetContentType(name, out var contentType))
        {
            contentType = MediaTypeNames.Application.Octet;
        }

        return contentType;
    }
}