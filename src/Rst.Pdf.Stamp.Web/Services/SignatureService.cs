using System.IO.Compression;
using System.Security.Cryptography.Pkcs;
using Amazon.S3;
using Rst.Pdf.Stamp.Web.Interfaces;
using Rst.Pdf.Stamp.Web.Models;

namespace Rst.Pdf.Stamp.Web.Services;

public class SignatureService : ISignatureService
{
    private readonly AmazonS3Client _client;

    public SignatureService(AmazonS3Client client)
    {
        _client = client;
    }

    public async Task<IEnumerable<SignatureInfo>> InfoAsync(FileRef arg)
    {
        var obj = await _client.GetObjectAsync(arg.Bucket, arg.Key);
        return Info(obj.ResponseStream);
    }

    public Task<IEnumerable<SignatureInfo>> InfoAsync(PreviewArgs args)
    {
        if (args.Signatures is not null)
            return Task.FromResult(Info(args.Signatures));

        throw new InvalidOperationException();
    }

    public Task<IEnumerable<SignatureInfo>> InfoAsync(StampArgs args)
    {
        if (args.Archive is not null)
            return Task.FromResult(Info(args.Archive));

        if (args.Signatures is not null)
            return Task.FromResult(Info(args.Signatures));

        throw new InvalidOperationException();
    }

    private static IEnumerable<SignatureInfo> Info(IFormFileCollection signatures)
    {
        foreach (var sig in signatures)
        {
            using var reader = new StreamReader(sig.OpenReadStream());
            var read = reader.ReadToEnd();
            var signedCms = new SignedCms();
            signedCms.Decode(Convert.FromBase64String(read));
            yield return new SignatureInfo(signedCms);
        }
    }

    private static IEnumerable<SignatureInfo> Info(IFormFile archive)
    {
        return Info(archive.OpenReadStream());
    }

    private static IEnumerable<SignatureInfo> Info(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var document = new SignedDocument(zip);

        foreach (var sig in document.GetSignatures())
        {
            using var reader = new StreamReader(sig);
            var read = reader.ReadToEnd();
            var signedCms = new SignedCms();
            signedCms.Decode(Convert.FromBase64String(read));
            yield return new SignatureInfo(signedCms);
        }
    }
}