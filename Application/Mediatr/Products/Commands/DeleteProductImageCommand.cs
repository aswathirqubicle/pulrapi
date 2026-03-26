using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Products.Commands;

public class DeleteProductImageCommand : IRequest <Unit>
{
    [Required] public string ProductUid { get; set; }
    [Required] public string ImageUid { get; set; }
}

public class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand,Unit>
{
    private readonly ILogger<DeleteProductImageCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileUploadService _fileUploadService;
    private readonly IConfiguration _configuration;

    public DeleteProductImageCommandHandler(
        ILogger<DeleteProductImageCommandHandler> logger,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IFileUploadService fileUploadService,
        IConfiguration configuration)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _fileUploadService = fileUploadService;
        _configuration = configuration;
    }

    public async Task<Unit> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Product product = await _dbContext.Products.SingleOrDefaultAsync(p =>
                p.Uid == request.ProductUid &&
                p.Store.User.Id == _currentUserService.GetUserId() &&
                p.IsActive, cancellationToken);

            if (product == null)
            {
                throw new BadRequestException($"Product with uid '{request.ProductUid}' doesn't exist.");
            }

            var productMediaFile = await _dbContext.ProductMediaFiles
                .Where(pmf => pmf.Product == product && pmf.MediaFile.Uid == request.ImageUid)
                .Include(pmf => pmf.Product)
                .Include(pmf => pmf.MediaFile)
                .SingleOrDefaultAsync(cancellationToken);

            if (productMediaFile?.MediaFile == null)
            {
                throw new BadRequestException($"Image with uid '{request.ImageUid}' doesn't exist.");
            }

            var fileConfig = new FileUploadConfigDto()
            {
                BucketName = _configuration[AwsLocationNames.S3UploadBucket],
                FolderPath = _configuration[AwsLocationNames.PublicUploadFolder],
                OldFileName =
                    productMediaFile.MediaFile.Url.Substring(productMediaFile.MediaFile.Url.LastIndexOf("/") + 1),
            };
            await _fileUploadService.Delete(fileConfig);

            // Remove the ProductMediaFile relationship, which will cascade delete the MediaFile
            _dbContext.ProductMediaFiles.Remove(productMediaFile);

            await _dbContext.SaveChangesAsync(CancellationToken.None);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}