using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Rst.Pdf.Stamp
{
    public interface IStampService
    {
        public Task<Stream> AddStamp(Stream input, IReadOnlyCollection<SignatureInfo> signatures, IView template);
    }
}