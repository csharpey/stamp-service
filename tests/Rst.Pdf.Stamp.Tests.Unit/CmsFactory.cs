using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using Rst.Pdf.Stamp;

namespace Rst.Pdf.Stamp.Tests.Unit;

public class CmsFactory : IEnumerable<object[]>
{
    private static readonly string[] Extensions = { "*.pdf", "*.doc", "*.docx" };
    public IEnumerator<object[]> GetEnumerator()
    {
        var signs = Directory.GetFiles(AppContext.BaseDirectory, "*.sgn")
            .Select(Path.GetFileName).ToArray();

        var documents = Extensions.SelectMany(e => Directory.GetFiles(AppContext.BaseDirectory, e)).ToList();

        foreach (var doc in documents)
        {
            var info = new List<SignatureInfo>();
            foreach (var s in signs)
            {
                {
                    var sign = new FileStream(s, FileMode.Open);

                    var signedCms = new SignedCms();
                    var read = new StreamReader(sign).ReadToEnd();
                    signedCms.Decode(Convert.FromBase64String(read));
                    info.Add(new SignatureInfo(signedCms));
                }
            }
            yield return new object[] { doc, info.ToArray() };
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}