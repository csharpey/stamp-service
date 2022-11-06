using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Rst.Pdf.Stamp.Web.Interfaces;

public interface ITemplateFactory : IEnumerable<IView>
{
    IView Template();
}