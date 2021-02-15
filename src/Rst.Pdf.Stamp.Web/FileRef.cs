using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Rst.Pdf.Stamp.Web
{
    public class FileRef : IValidatableObject
    {
        [Required]
        public string Bucket { get; set; }
        
        [Required]
        public string Key { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var client = validationContext.GetRequiredService<AmazonS3Client>();

            var bucket = client.ListBucketsAsync().GetAwaiter().GetResult()
                .Buckets.Find(b => b.BucketName.Equals(Bucket));

            if (bucket is null)
            {
                yield return new ValidationResult("Bucket not found", new[] { nameof(Bucket) });
                yield break;
            }
            
            var response = client.GetObjectMetadataAsync(Bucket, Key)
                .GetAwaiter().GetResult();
                
            if (response.HttpStatusCode == HttpStatusCode.NotFound)
            {
                yield return new ValidationResult("File not found", new []{ nameof(Key) });
                yield break;
            }
            
            if (response.Headers.ContentType != MediaTypeNames.Application.Zip)
            {
                yield return new ValidationResult("File must be zip", new []{ nameof(Key) });
            }
        }

        public static implicit operator GetObjectMetadataRequest(FileRef b)
        {
            return new GetObjectMetadataRequest
            {
                Key = b.Key,
                BucketName = b.Bucket
            };
        }
    }
}