using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface IBrandService
    {
        Task<string> GetOrCreateBrandAsync(string brandName, CancellationToken cancellationToken = default);
    }
}