using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface IUserBlockService
    {
        Task HandleUserBlock(string blockerProfileUid, string blockedProfileUid, CancellationToken cancellationToken);
        Task HandleUserUnblock(string blockerProfileUid, string blockedProfileUid, CancellationToken cancellationToken);
    }
} 