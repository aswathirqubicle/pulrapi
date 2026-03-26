using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface IVideoProcessingService
    {
        Task ProcessHlsTranscodingAsync(int mediaFileId);
    }
}
