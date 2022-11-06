using System.Collections;
using System.Collections.Generic;

namespace Rst.Pdf.Stamp.Web.Options;

public class BucketOptions : IEnumerable<string>
{
    public string Public { get; set; }
    public string Stamped { get; set; }


    public IEnumerator<string> GetEnumerator()
    {
        yield return Public;
        yield return Stamped;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}