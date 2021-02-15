using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Rst.Pdf.Stamp.Web.Interfaces;
using Rst.Pdf.Stamp.Web.Options;

namespace Rst.Pdf.Stamp.Web
{
    public class TemplateService : ITemplateService
    {
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _accessor;
        private readonly ITemplateFactory _templateFactory;
        private readonly SertOptions _sertOptions;

        public TemplateService(
            ITempDataProvider tempDataProvider, 
            IHttpContextAccessor accessor, ITemplateFactory templateFactory, IOptions<SertOptions> options)
        {
            _tempDataProvider = tempDataProvider;
            _accessor = accessor;
            _templateFactory = templateFactory;
            _sertOptions = options.Value;
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

        public async Task<string> RenderToString(SignatureInfo model)
        {
            IView view;

            if (_sertOptions.OGRNS.Contains(model.OGRN))
            {
                view = _templateFactory.FirstOrDefault(x=>x.Path.Contains("Operator"));
            }
            else
            {
                view = _templateFactory.FirstOrDefault(x=>x.Path.Contains("Declarant"));
            }

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
    }
}