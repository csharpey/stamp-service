using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rst.Pdf.Stamp;

public interface IPdfConverter
{
    Task<Stream> Convert(Stream memoryStream, CancellationToken token);

}