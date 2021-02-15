using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography.Pkcs;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Rst.Pdf.Stamp.Web.Interfaces;

namespace Rst.Pdf.Stamp.Web
{
    public class SignatureService : ISignatureService
    {
        private readonly AmazonS3Client _client;

        public SignatureService(AmazonS3Client client)
        {
            _client = client;
        }
        public async Task<IEnumerable<SignatureInfo>> Info(FileRef arg)
        {
            var obj = await _client.GetObjectAsync(arg.Bucket, arg.Key);
            return Info(obj.ResponseStream);
        }
        public Task<IEnumerable<SignatureInfo>> Info(PreviewArgs args)
        {
            if (args.Signatures is not null)
                return Task.FromResult(Info(args.Signatures));
            
            throw new InvalidOperationException();
        }

        public Task<IEnumerable<SignatureInfo>> Info(StampArgs args)
        {
            if (args.Archive is not null)
                return Task.FromResult(Info(args.Archive));
            
            if (args.Signatures is not null)
                return Task.FromResult(Info(args.Signatures));
            
            throw new InvalidOperationException();
        }

        private IEnumerable<SignatureInfo> Info(IFormFileCollection signatures)
        {
            foreach (var sig in signatures)
            {
                var read = new StreamReader(sig.OpenReadStream()).ReadToEnd();
                var signedCms = new SignedCms();
                signedCms.Decode(Convert.FromBase64String(read));
                yield return new SignatureInfo(signedCms);
            }
        }

        private IEnumerable<SignatureInfo> Info(IFormFile archive)
        {
            return Info(archive.OpenReadStream());
        }
        
        private static IEnumerable<SignatureInfo> Info(Stream stream)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            var document = new SignedDocument(zip);

            foreach (var sig in document.GetSignatures())
            {
                var read = new StreamReader(sig).ReadToEnd();
                var signedCms = new SignedCms();
                signedCms.Decode(Convert.FromBase64String(read));
                yield return new SignatureInfo(signedCms);
            }
        }
    }
}