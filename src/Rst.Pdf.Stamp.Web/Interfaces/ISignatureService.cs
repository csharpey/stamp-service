using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rst.Pdf.Stamp.Web.Interfaces
{
    public interface ISignatureService
    {
        Task<IEnumerable<SignatureInfo>> Info(StampArgs args);
        Task<IEnumerable<SignatureInfo>> Info(FileRef arg);
        Task<IEnumerable<SignatureInfo>> Info(PreviewArgs args);
    }
}