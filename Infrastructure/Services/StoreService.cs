using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services
{
    public class StoreService : IStoreService
    {
        private readonly ILogger<StoreService> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IConfiguration _configuration;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileUploadService _fileUploadService;
        private readonly IMapper _mapper;

        public StoreService(ILogger<StoreService> logger,
            IApplicationDbContext dbContext,
            IQueryHelperService queryHelperService,
            IConfiguration configuration,
            ICurrentUserService currentUserService,
            IFileUploadService fileUploadService,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _queryHelperService = queryHelperService;
            _configuration = configuration;
            _currentUserService = currentUserService;
            _fileUploadService = fileUploadService;
            _mapper = mapper;
        }

        public Task<PagingResponse<ProductInventoryResponse>> GetProductInventoryList(PagingParamsRequest pagingParams, string storeUid, Expression<Func<Product, bool>> predicate = null)
        {
            throw new NotImplementedException();
        }

        public Task<ProductSimilarsResponse> GetProductSimilars(string productUid)
        {
            throw new NotImplementedException();
        }

        //TODO Check with Dusan regarding this predicate in this method
        //public async Task<PagingResponse<ProductInventoryResponse>> GetProductInventoryList(
        //    PagingParamsRequest pagingParams,
        //    string storeUid,
        //    Expression<Func<Product, bool>> predicate = null)
        //{
        //    try
        //    {
        //        IQueryable<Product> query = _dbContext.Products.AsNoTracking();
        //        if (predicate != null)
        //        {
        //            query = query.Where(predicate);
        //        }

        //        query = query.Where(e =>
        //            e.IsActive == true && e.Store.Uid == storeUid && e.Store.UserId == _currentUserService.GetUserId());

        //        if (!String.IsNullOrWhiteSpace(pagingParams.Search))
        //        {
        //            query = query.Where(s =>
        //                s.Name.ToLower().Contains(pagingParams.Search.Trim().ToLower()) ||
        //                s.Description.ToLower().Contains(pagingParams.Search.Trim().ToLower())
        //            );
        //        }


        //        if (String.IsNullOrWhiteSpace(pagingParams.Order) || String.IsNullOrWhiteSpace(pagingParams.OrderBy))
        //        {
        //            query = query.OrderByDescending(u => u.Id);
        //        }
        //        else
        //        {
        //            query = _queryHelperService.AppendOrderBy(query, pagingParams.OrderBy, pagingParams.Order);
        //        }

        //        var currencyCode = await _dbContext.Stores.Where(s => s.Uid == storeUid).Select(s => s.Currency.Code)
        //            .SingleOrDefaultAsync();

        //        query = query.Select(p => new Product()
        //        {
        //            Uid = p.Uid,
        //            ArticleCode = p.ArticleCode,
        //            Name = p.Name,
        //            ProductCategory = p.ProductCategory,
        //            Price = p.Price
        //        });

        //        var list = await PagedList<Product>.ToPagedListAsync(query, pagingParams.PageNumber,
        //            pagingParams.PageSize);

        //        var mappedList = _mapper.Map<PagingResponse<ProductInventoryResponse>>(list);
        //        mappedList.Items.ForEach(i => i.CurrencyCode = currencyCode);
        //        return mappedList;
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e, e.Message);
        //        throw;
        //    }
        //}

        //TODO Consult regarding this method, is there a reason why it was named with the same name
        //public async Task<ProductSimilarsResponse> GetProductSimilars(string productUid)
        //{
        //    try
        //    {
        //        var result = new ProductSimilarsResponse();

        //        var product = await _dbContext.Products.Include(p => p.Store)
        //            .SingleOrDefaultAsync(p => p.Uid == productUid);
        //        if (product == null)
        //        {
        //            return result;
        //        }

        //        var storeId = product.Store.Id;

        //        var similarProductIds = await _dbContext.ProductSimilars.Where(ps => ps.ProductId == product.Id)
        //            .Select(ps => ps.SimilarId).ToListAsync();
        //        var pairIds = await _dbContext.ProductPairs.Where(pp => pp.ProductId == product.Id)
        //            .Select(pp => pp.PairId).ToListAsync();

        //        result.Similars = await GetProductSimilars(similarProductIds, storeId);
        //        result.Pairs = await GetProductSimilars(pairIds, storeId);

        //        return result;
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e, e.Message);
        //        throw;
        //    }
        //}

        //private async Task<List<ProductPublicResponse>> GetProductSimilars(List<int> productSimilarIdList, int storeId)
        //{
        //    try
        //    {
        //        IQueryable<Product> query = _dbContext.Products;
        //        query = query.Where(p => p.IsActive == true && productSimilarIdList.Contains(p.Id));
        //        query = query.OrderByDescending(p => p.Id).Take(4);

        //        query = query.Select(p => new Product()
        //        {
        //            Id = p.Id,
        //            Uid = p.Uid,
        //            Name = p.Name,
        //            Price = p.Price
        //        }).AsNoTracking();

        //        //var queryRaw = query.ToSql();
        //        //var currencyCode = null;

        //        var list = await query.ToListAsync();
        //        if (list.Count == 0)
        //        {
        //            return new List<ProductPublicResponse>();
        //        }

        //        var currencyCode = await _dbContext.Stores.Where(s => s.Id == storeId).Select(s => s.Currency)
        //            .SingleOrDefaultAsync();
        //        var listOfProductUids = list.Select(p => p.Uid);
        //        var productMediaFileList = await _dbContext.ProductMediaFiles.Where(pmf =>
        //                listOfProductUids.Contains(pmf.Product.Uid) &&
        //                pmf.MediaFile.Priority == 0)
        //            .Select(pmf => new ProductMediaFileDto()
        //            {
        //                MediaFileUrl = pmf.MediaFile.Url,
        //                ProductUid = pmf.Product.Uid
        //            })
        //            .AsNoTracking()
        //            .ToListAsync();

        //        var mappedList = _mapper.Map<List<ProductPublicResponse>>(list);

        //        for (int i = 0; i < mappedList.Count; i++)
        //        {
        //            var item = mappedList[i];
        //            item.CurrencyUid = currencyCode.Uid;
        //            item.CurrencyCode = currencyCode.Code;
        //            item.FeaturedImageUrl = productMediaFileList.Where(pmfl => pmfl.ProductUid == item.Uid)
        //                .Select(pmf => pmf.MediaFileUrl)
        //                .SingleOrDefault();
        //        }

        //        return mappedList;
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e, e.Message);
        //        throw;
        //    }
        //}

        public async Task<string> UpdateStoreAvatarImage(string storeUid, IFormFile image,
            ProfileImageTypeEnum profileImageType, CancellationToken cancellationToken)
        {
            try
            {
                var isAdmin = _currentUserService.HasRole(PulrRoles.Administrator);
                Store store = await _dbContext.Stores.SingleOrDefaultAsync(s =>
                    s.Uid == storeUid && (isAdmin || s.User.Id == _currentUserService.GetUserId()) && s.IsActive, cancellationToken);

                if (store == null)
                {
                    throw new BadRequestException($"Store with uid '{storeUid}' doesnt exist.");
                }

                string bucketName = _configuration[AwsLocationNames.S3UploadBucket];
                string folderPath = _configuration[AwsLocationNames.PublicUploadFolder];

                var fileConfig = new FileUploadConfigDto()
                {
                    FileName = image.FileName,
                    BucketName = bucketName,
                    FolderPath = folderPath,
                    File = image,
                    ImageWidth = profileImageType == ProfileImageTypeEnum.Avatar
                        ? PulrGlobalConfig.AvatarImage.Width
                        : PulrGlobalConfig.BannerImage.Width,
                    ImageHeight = profileImageType == ProfileImageTypeEnum.Avatar
                        ? PulrGlobalConfig.AvatarImage.Height
                        : PulrGlobalConfig.BannerImage.Height,
                };

                string path = null;

                if (profileImageType == ProfileImageTypeEnum.Avatar)
                {
                    if (store.ImageUrl != null)
                    {
                        fileConfig.OldFileName = store.ImageUrl.Substring(store.ImageUrl.LastIndexOf("/") + 1);
                        await _fileUploadService.Delete(fileConfig);
                    }

                    path = await _fileUploadService.UploadImage(fileConfig);
                    store.ImageUrl = path;
                }
                else
                {
                    if (store.BannerUrl != null)
                    {
                        fileConfig.OldFileName = store.BannerUrl.Substring(store.BannerUrl.LastIndexOf("/") + 1);
                        await _fileUploadService.Delete(fileConfig);
                    }

                    path = await _fileUploadService.UploadImage(fileConfig);
                    store.BannerUrl = path;
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return path;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}