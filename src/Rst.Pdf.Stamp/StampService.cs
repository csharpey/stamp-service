using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.FeatureManagement;

namespace Rst.Pdf.Stamp;

public class StampService : IStampService
{
    private readonly IPdfGenerator _pdf;
    private readonly IPlaceManager _placeManager;
    private readonly ITemplateService _renderer;

    private readonly IFeatureManager _featureManager;
    private readonly IPdfConverter _converter;

    public StampService(ITemplateService renderer, IPdfGenerator pdf, IFeatureManager featureManager,
        IPlaceManager placeManager, IPdfConverter converter)
    {
        _renderer = renderer;
        _pdf = pdf;
        _featureManager = featureManager;
        _placeManager = placeManager;
        _converter = converter;
    }

    public async Task<Stream> AddStamp(Stream input, IReadOnlyCollection<SignatureInfo> signatures, IView template,
        CancellationToken cancellationToken)
    {
        var stream = new MemoryStream();
        var document = new Document(PageSize.A4);
        var pegPage = await _featureManager.IsEnabledAsync(FeatureFlags.PerPage);

        var rectangles = new List<System.Drawing.Rectangle>
        {
            new(0, 0, (int)document.PageSize.Width, (int)document.PageSize.Height)
        };
        
        var png = await _converter.Convert(input, cancellationToken);
        var fsm = await _placeManager.FindEmpty(png, rectangles, cancellationToken);
        var reader = new PdfReader(input);
        var stamper = new PdfStamper(reader, stream);

        var html = new List<string>();
        for (var i = 0; i < signatures.Count; i++)
        {
            var signature = signatures.ElementAt(i);
            string symbol = template == null
                ? await _renderer.RenderToString(signature)
                : await _renderer.RenderToString(template, signature);
            html.Add(HttpUtility.HtmlDecode(symbol));
        }

        var pdfStream = _pdf.FromHtml(html);
        var pdfReader = new PdfReader(pdfStream);
        var size = document.PageSize;
        for (var x = reader.NumberOfPages; x > 0; x--)
        {
            var indent = 0f;
            var content = stamper.GetOverContent(x);

            for (int i = 0; i < pdfReader.NumberOfPages; i++)
            {
                var page = stamper.GetImportedPage(pdfReader, i + 1);
                var stampSize = pdfReader.GetPageSize(i + 1);

                content.AddTemplate(
                    page, 0, -1, 1, 0,
                    size.GetRight(stampSize.Height) - 5,
                    size.GetBottom(stampSize.Width) + indent + 5);

                indent += stampSize.Width + 5;
            }

            if (!pegPage)
            {
                break;
            }
        }

        stamper.Close();

        return new MemoryStream(stream.ToArray());
    }
}