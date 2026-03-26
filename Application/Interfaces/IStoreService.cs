using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Models;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Core.Application.Interfaces
{
    public interface IStoreService
    {
        Task<PagingResponse<ProductInventoryResponse>> GetProductInventoryList(PagingParamsRequest pagingParams, string storeUid, Expression<Func<Product, bool>> predicate = null);
        Task<ProductSimilarsResponse> GetProductSimilars(string productUid);
        Task<string> UpdateStoreAvatarImage(string storeUid, IFormFile image, ProfileImageTypeEnum profileImageType, CancellationToken cancellationToken);
    }
}
