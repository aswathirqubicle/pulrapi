using Amazon.S3.Model;
using System.Threading.Tasks;
using Core.Application.Models;

namespace Core.Application.Interfaces
{
    public interface IFileUploadService
    {
        Task<string> UploadImage(FileUploadConfigDto config);
        Task<string> UploadVideo(FileUploadConfigDto config);
        Task<string> UploadDocument(FileUploadConfigDto config);
        Task<(string originalUrl, string hlsMasterUrl, string hlsBasePath, int durationSeconds, int width, int height, string[] qualities)> UploadVideoWithHls(FileUploadConfigDto config, IVideoTranscodingService transcodingService);
        Task<DeleteObjectResponse> Delete(FileUploadConfigDto config);
        Task ListFilesInBucket(string bucketName, string prefixOrPath);
    }
}
