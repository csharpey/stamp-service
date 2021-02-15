using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using Rst.Pdf.Stamp;

namespace Rst.Pdf.Cli
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No files have been provided");
                Environment.Exit(1);
            }

            foreach (var filename in args)
            {
                CheckFile(filename);
            }

            var originalFilename = args[0];
            var signatures = args.Skip(1).Select(f =>
            {
                var signedCms = new SignedCms();
                signedCms.Decode(Convert.FromBase64String(File.ReadAllText(f)));
                return new SignatureInfo(signedCms);
            }).ToList();

            var builder = new StringBuilder();
            foreach (var info in signatures)
            {
                builder.AppendFormat("Organization: {0}", info.Organization);
                builder.AppendFormat("Name: {0}", info.Name);
                builder.AppendFormat("SigningDate: {0}", info.SigningDate);
            }
            
            Console.WriteLine(builder.ToString());

            await using var original = File.Open(originalFilename, FileMode.Open);
            await using var processed = File.Open("test.pdf", FileMode.OpenOrCreate);
        }

        private static void CheckFile(string filename)
        {
            if (File.Exists(filename)) return;
            
            Console.WriteLine($"The file '{filename}' does not exist");
            Environment.Exit(1);
        }
    }
}
