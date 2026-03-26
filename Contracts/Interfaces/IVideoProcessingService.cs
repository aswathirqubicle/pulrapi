using System.Threading.Tasks;

namespace Pulr.Contracts.Interfaces
{
    public interface IVideoProcessingService
    {
        Task ProcessHlsTranscodingAsync(int mediaFileId);
    }
}
