using System.ComponentModel.DataAnnotations;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Products.Commands.Delete;

public class DeleteProductCommand : IRequest <Unit>
{
    [Required] public string? ProductUid { get; set; }
}

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand,Unit>
{
    private readonly ILogger<DeleteProductCommandHandler> _logger;
    private readonly IFileUploadService _fileUploadService;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _dbContext;

    public DeleteProductCommandHandler(ILogger<DeleteProductCommandHandler> logger,
        IFileUploadService fileUploadService, IConfiguration configuration, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _fileUploadService = fileUploadService;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        if (request.ProductUid == null) throw new ArgumentNullException(nameof(request.ProductUid));

    //        var productUid = request.ProductUid!;

    //        var product = _dbContext.Products
    //            .AsSplitQuery()
    //            .Include(p => p.Comments)
    //            .Include(p => p.ProductLikes)
    //            .Include(p => p.ProductMoreInfos)
    //            .Include(p => p.ProductPairs)
    //            .Include(p => p.ProductMediaFiles).ThenInclude(pmf => pmf.MediaFile)
    //            .Include(p => p.ProductAttributes)
    //            .SingleOrDefault(p => p.IsActive && p.Uid == productUid);

    //        if (product == null)
    //            throw new NotFoundException("Product not found");

    //        if (product.ProductMediaFiles.Any())
    //        {
    //            string bucketName = _configuration[AwsLocationNames.S3UploadBucket]!;
    //            string folderPath = _configuration[AwsLocationNames.PublicUploadFolder]!;

    //            var fileConfig = new FileUploadConfigDto()
    //            {
    //                BucketName = bucketName,
    //                FolderPath = folderPath
    //            };

    //            foreach (var pmf in product.ProductMediaFiles)
    //            {
    //                fileConfig.OldFileName = pmf.MediaFile.Url.Substring(pmf.MediaFile.Url.LastIndexOf("/") + 1);
    //                await _fileUploadService.Delete(fileConfig);
    //            }
    //        }

    //        _dbContext.Products.Remove(product);
    //        await _dbContext.SaveChangesAsync(CancellationToken.None);
    //        return Unit.Value;
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogError(e, e.Message);
    //        throw;
    //    }
    //}
}