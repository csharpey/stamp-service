using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rst.Pdf.Stamp.Web.Interfaces;

namespace Rst.Pdf.Stamp.Web.Services;

public class TemplateService : ITemplateService
{
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IHttpContextAccessor _accessor;
    private readonly ITemplateFactory _templateFactory;

    public TemplateService(
        ITempDataProvider tempDataProvider,
        IHttpContextAccessor accessor, ITemplateFactory templateFactory)
    {
        _tempDataProvider = tempDataProvider;
        _accessor = accessor;
        _templateFactory = templateFactory;
    }

    public async Task<string> RenderToString(IView view, object model)
    {
        var actionContext = new ActionContext(_accessor.HttpContext, new RouteData(), new ActionDescriptor());

        await using var sw = new StringWriter();

        var viewDictionary =
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

        var viewContext = new ViewContext(
            actionContext,
            view,
            viewDictionary,
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            sw,
            new HtmlHelperOptions()
        );

        await view.RenderAsync(viewContext);
        return PreMailer.Net.PreMailer.MoveCssInline(sw.ToString(), true).Html;
    }

    public async Task<string> RenderToString(SignatureInfo signature)
    {
        IView view;

        view = _templateFactory.FirstOrDefault(x => x.Path.Contains("Stamp", StringComparison.Ordinal));

        var actionContext = new ActionContext(_accessor.HttpContext, new RouteData(), new ActionDescriptor());

        await using var sw = new StringWriter();

        var viewDictionary =
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = signature
            };

        var viewContext = new ViewContext(
            actionContext,
            view,
            viewDictionary,
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            sw,
            new HtmlHelperOptions()
        );

        await view.RenderAsync(viewContext);
        return PreMailer.Net.PreMailer.MoveCssInline(sw.ToString(), true).Html;
    }
}