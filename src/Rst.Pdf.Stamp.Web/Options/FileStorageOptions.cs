namespace Rst.Pdf.Stamp.Web.Options
{
    public class FileStorageOptions
    {
        public const string Section = "ObjectStorage";

        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string ServiceUrl { get; set; }
    }
}