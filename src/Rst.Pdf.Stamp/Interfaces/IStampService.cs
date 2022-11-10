using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Rst.Pdf.Stamp.Interfaces;

public interface IStampService
{
    public Task<Stream> AddStampAsync(Stream input, IReadOnlyCollection<SignatureInfo> signatures, IView template,
        CancellationToken cancellationToken);
}