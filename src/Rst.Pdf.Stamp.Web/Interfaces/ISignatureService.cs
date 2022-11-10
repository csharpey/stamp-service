using System.Collections.Generic;
using System.Threading.Tasks;
using Rst.Pdf.Stamp.Web.Models;

namespace Rst.Pdf.Stamp.Web.Interfaces;

public interface ISignatureService
{
    Task<IEnumerable<SignatureInfo>> InfoAsync(StampArgs args);
    Task<IEnumerable<SignatureInfo>> InfoAsync(FileRef arg);
    Task<IEnumerable<SignatureInfo>> InfoAsync(PreviewArgs args);
}