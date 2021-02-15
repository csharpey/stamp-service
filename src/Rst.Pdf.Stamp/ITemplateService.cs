using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Rst.Pdf.Stamp
{
    public interface ITemplateService
    {
        Task<string> RenderToString(IView view, object model);
        
        Task<string> RenderToString(SignatureInfo signature);
    }
}