using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Rst.Pdf.Stamp.Web.Interfaces;

namespace Rst.Pdf.Stamp.Web
{
    public class TemplateFactory : ITemplateFactory
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly IHttpContextAccessor _accessor;
        private readonly ActionContext _actionContext;
        private readonly ApplicationPartManager _applicationPartManager;

        public TemplateFactory(IRazorViewEngine razorViewEngine, IHttpContextAccessor accessor,
            ApplicationPartManager applicationPartManager)
        {
            _razorViewEngine = razorViewEngine;
            _accessor = accessor;
            _applicationPartManager = applicationPartManager;
            var context = _accessor.HttpContext;
            _actionContext = new ActionContext(context, context.GetRouteData(), new ActionDescriptor());
        }

        public IView Template()
        {
            var context = _accessor.HttpContext;
            Debug.Assert(context != null);

            var viewResult = _razorViewEngine.FindView(_actionContext, "Stamp", false);
            if (viewResult.View == null)
            {
                throw new ArgumentNullException(nameof(viewResult.View));
            }

            return viewResult.View;
        }

        public IEnumerator<IView> GetEnumerator()
        {
            var feature = new ViewsFeature();
            _applicationPartManager.PopulateFeature(feature);

            foreach (var descriptor in feature.ViewDescriptors)
            {
                yield return _razorViewEngine.GetView(string.Empty, descriptor.RelativePath, false).View;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}