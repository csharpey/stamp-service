using System.Globalization;
using System.Text.RegularExpressions;

namespace Rst.Pdf.Stamp.Web;

public class ParameterTransformer : IOutboundParameterTransformer
{
    public string TransformOutbound(object value)
    {
        return value == null
            ? null
            : Regex.Replace(value.ToString() ?? string.Empty, "([a-z])([A-Z])", "$1-$2")
                .ToLower(CultureInfo.CurrentCulture);
    }
}