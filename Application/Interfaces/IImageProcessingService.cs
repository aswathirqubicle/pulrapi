using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface IImageProcessingService
    {
        Task<bool> IsFfmpegAvailableAsync();
        Task<string> ProcessImageAsync(string inputPath, string outputPath, int width, int height, string filterType);
    }
}
