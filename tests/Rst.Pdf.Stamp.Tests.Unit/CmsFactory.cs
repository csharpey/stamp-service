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
            .GroupBy(s => Path.GetFileName(s).StartsWith("operator")).ToArray();

        var documents = Extensions.SelectMany(e => Directory.GetFiles(AppContext.BaseDirectory, e))
            .Where(s => !Path.GetFileName(s).StartsWith("stamped")).ToList();

        foreach (var doc in documents)
        {
            var info = new List<SignatureInfo>();
            foreach (var (@operator, user) in signs.First().Zip(signs.Last()))
            {
                {
                    var sign = new FileStream(@operator, FileMode.Open);

                    var signedCms = new SignedCms();
                    var read = new StreamReader(sign).ReadToEnd();
                    signedCms.Decode(Convert.FromBase64String(read));
                    info.Add(new SignatureInfo(signedCms));
                }

                {
                    var sign = new FileStream(user, FileMode.Open);

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