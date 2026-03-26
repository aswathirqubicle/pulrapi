using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Models;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using Core.Application.Models.MediaFiles;

namespace Core.Application.Mediatr.Products.Queries
{
    public class ProductsToTagListQuery : PagingParamsRequest, IRequest<PagingResponse<ProductPublicResponse>>
    {
    }

    public class ProductsToTagListQueryHandler : IRequestHandler<ProductsToTagListQuery, PagingResponse<ProductPublicResponse>>
    {
        private readonly ILogger<ProductsToTagListQueryHandler> _logger;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IMapper _mapper;
        private readonly IApplicationDbContext _dbContext;

        public ProductsToTagListQueryHandler(
            ILogger<ProductsToTagListQueryHandler> logger,
            IQueryHelperService queryHelperService,
            IMapper mapper,
            IApplicationDbContext dbContext
        )
        {
            _logger = logger;
            _queryHelperService = queryHelperService;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public async Task<PagingResponse<ProductPublicResponse>> Handle(ProductsToTagListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.PageSize > 30)
                {
                    request.PageSize = 30;
                }

                var storeUids = new List<string>();
                IQueryable<Product> query = _dbContext.Products;

                if (!String.IsNullOrWhiteSpace(request.Search))
                {
                    storeUids = await _dbContext.Stores
                        .Where(s => s.IsActive 
                                    && (request.Search.Contains(s.Name) 
                                    || request.Search.Contains(s.UniqueName)))
                        .Select(s => s.Uid)
                        .ToListAsync(cancellationToken);

                    if (storeUids.Count > 0)
                    {
                        query = query.Where(p => storeUids.Contains(p.Store.Uid));
                    }

                    query = query.Where(p => p.Name.Contains(request.Search));
                }

                if (String.IsNullOrWhiteSpace(request.Order) || String.IsNullOrWhiteSpace(request.OrderBy))
                {
                    query = query.OrderByDescending(u => u.Id);
                }
                else
                {
                    query = _queryHelperService.AppendOrderBy(query, request.OrderBy, request.Order);
                }

                query = query.Where(p => p.IsActive)
                    .Select(p => new Product
                    {
                        Id = p.Id,
                        Uid = p.Uid,
                        Name = p.Name,
                        Store = p.Store,
                        MinPrice = p.MinPrice,
                        MaxPrice = p.MaxPrice,
                        Type = p.Type,
                        SellType = p.SellType
                    });

                //var queryRaw = query.ToSql(); 

                var list = await PagedList<Product>.ToPagedListAsync(query, request.PageNumber, request.PageSize);
                var listOfProductUids = list.Select(p => p.Uid);

                var productMediaFileList = await _dbContext.ProductMediaFiles
                    .Where(pmf => listOfProductUids.Contains(pmf.Product.Uid) && pmf.MediaFile.Priority == 0 && pmf.MediaFile.IsActive)
                    .Select(pmf => new ProductMediaFileDto()
                    {
                        MediaFileUrl = pmf.MediaFile.Url,
                        ProductUid = pmf.Product.Uid
                    })
                    .ToListAsync(cancellationToken);

                var mappedList = _mapper.Map<PagingResponse<ProductPublicResponse>>(list);

                for (int i = 0; i < mappedList.Items.Count; i++)
                {
                    var item = mappedList.Items[i];
                    item.ProductMediaFiles = productMediaFileList
                        .Where(pmfl => pmfl.ProductUid == item.Uid)
                        .Select(pmf => new MediaFileDetailsResponse
                        {
                            Uid = "", // We don't have Uid in the DTO, but we can use the URL as identifier
                            Url = pmf.MediaFileUrl,
                            FileType = "Image" // Assuming these are images
                        }).ToList();

                    mappedList.ItemIds.Add(item.Uid);
                }

                return mappedList;
                //return await _storeService.GetProductsToTagList(request.PagingParams);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
