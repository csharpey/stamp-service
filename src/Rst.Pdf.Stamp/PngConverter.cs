using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Rst.Pdf.Stamp;

public class PngConverter : IPdfConverter
{
    private readonly ILogger<IPdfConverter> _logger;

    public PngConverter(ILogger<IPdfConverter> logger)
    {
        _logger = logger;
    }

    public async Task<Stream> Convert(Stream memoryStream, CancellationToken token)
    {
        await using var pdfStream = new FileStream(Path.GetTempFileName(), FileMode.Create);
        await memoryStream.CopyToAsync(pdfStream, token);
        await pdfStream.FlushAsync(token);

        var name = Path.GetTempFileName();
        var process = Process.Start("pdftoppm", new[] { "-png", "-gray", pdfStream.Name, name });
        memoryStream.Seek(0, SeekOrigin.Begin);

        await process.WaitForExitAsync(token);

        switch (process.ExitCode)
        {
            case 1:
                _logger.LogError("Error opening a PDF file");
                break;
            case 2:
                _logger.LogError("Error opening an output file");
                break;
            case 3:
                _logger.LogError("Error related to PDF permissions");
                break;
        }

        await using var pngStream = new FileStream(name + "-1.png", FileMode.Open, FileAccess.Read);
        var png = new MemoryStream();
        await pngStream.CopyToAsync(png, token);
        png.Seek(0, SeekOrigin.Begin);

        return png;
    }
}