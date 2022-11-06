using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Rst.Pdf.Stamp;

public class SignedDocument
{
    public const string SignatureExtension = ".sgn";
    public const string ArchiveExtension = ".zip";
    public const string OperatorPrefix = "operator_";
    public const string UserPrefix = "user_";
    public static readonly string[] DocumentExtensions =
    {
        ".pdf", ".doc", ".docx"
    };
    private const string InvalidSignedDocumentExceptionTemplate = "Cannot find an entry [{0}]";

    private readonly ZipArchive _archive;

    public SignedDocument(ZipArchive archive)
    {
        _archive = archive;
    }

    public IEnumerable<Stream> GetSignatures()
    {
        return FindEntries(SignatureExtension);
    }

    private static Stream GetStreamEntry(ZipArchiveEntry entry)
    {
        var data = new MemoryStream();
        using var stream = entry.Open();
        stream.CopyTo(data);
        data.Seek(0, SeekOrigin.Begin);
        return data;
    }

    private IEnumerable<Stream> FindEntries(string extension)
    {
        foreach (var entry in _archive.Entries)
        {
            if (entry.FullName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                yield return GetStreamEntry(entry);
            }
        }
    }
}