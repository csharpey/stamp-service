namespace Rst.Pdf.Stamp.Web.Options
{
    public class FileStorageOptions
    {
        public const string Section = "S3";

        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
    }
}