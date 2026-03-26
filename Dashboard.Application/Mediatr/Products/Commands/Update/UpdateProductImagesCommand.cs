using System.ComponentModel.DataAnnotations;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Products;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Products.Commands.Update;

public class UpdateProductImagesCommand : IRequest<List<ProductImageUpdateResponse>>
{
    [Required]
    public string? ProductUid { get; set; }

    [Required, MinLength(1), MaxLength(5), MaxFileSize(5 * 1024 * 1024),
     PulrFileValidation(new FileTypeEnum[] { FileTypeEnum.Image })]
    public List<IFormFile>? Images { get; set; }

    [ValidFormImagePriorities]
    public List<string>? ImagePriorities { get; set; }
}

public class UpdateProductImagesCommandHandler : IRequestHandler<UpdateProductImagesCommand, List<ProductImageUpdateResponse>>
{
    private readonly ILogger<UpdateProductImagesCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileUploadService _fileUploadService;
    private readonly IConfiguration _configuration;

    public UpdateProductImagesCommandHandler(
        ILogger<UpdateProductImagesCommandHandler> logger,
        IApplicationDbContext dbContext,
        IFileUploadService fileUploadService,
        IConfiguration configuration)
    {
        _logger = logger;
        _dbContext = dbContext;
        _fileUploadService = fileUploadService;
        _configuration = configuration;
    }

    public async Task<List<ProductImageUpdateResponse>> Handle(UpdateProductImagesCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Images == null || request.ImagePriorities == null)
            {
                throw new BadRequestException("Images or ImagePriorities cannot be null.");
            }

            var product = await _dbContext.Products
                .AsSplitQuery()
                .Include(p => p.ProductMediaFiles)
                .ThenInclude(pmf => pmf.MediaFile)
                .SingleOrDefaultAsync(p =>
                    p.Uid == request.ProductUid &&
                    p.IsActive, cancellationToken);

            if (product == null)
            {
                throw new BadRequestException($"Product with uid '{request.ProductUid}' doesn't exist.");
            }

            var existingPriorities = product.ProductMediaFiles.Select(pmf => pmf.MediaFile.Priority).ToList();

            string bucketName = _configuration[AwsLocationNames.S3UploadBucket]!;
            string folderPath = _configuration[AwsLocationNames.PublicUploadFolder]!;
            var fileConfig = new FileUploadConfigDto()
            {
                BucketName = bucketName,
                FolderPath = folderPath,
                ImageWidth = PulrGlobalConfig.ProductImage.Width,
                ImageHeight = PulrGlobalConfig.ProductImage.Height,
            };

            var result = new List<ProductImageUpdateResponse>();

            for (int i = 0; i < request.Images.Count; i++)
            {
                if (request.Images[i] != null)
                {
                    fileConfig.FileName = request.Images[i].FileName;
                    fileConfig.File = request.Images[i];
                    string path = await _fileUploadService.UploadImage(fileConfig);

                    var priority = request.ImagePriorities != null ? request.ImagePriorities[i] : null;
                    var mediaFile = await _dbContext.ProductMediaFiles
                        .Where(pmf => pmf.Product == product
                                      && pmf.MediaFile.Priority.ToString() == priority
                                      && pmf.MediaFile.MediaFileType == MediaFileTypeEnum.Image)
                        .Select(pmf => pmf.MediaFile)
                        .SingleOrDefaultAsync(cancellationToken);

                    if (mediaFile != null)
                    {
                        fileConfig.OldFileName = mediaFile.Url.Substring(mediaFile.Url.LastIndexOf("/") + 1);
                        await _fileUploadService.Delete(fileConfig);
                        mediaFile.Url = path;
                    }
                    else
                    {
                        product.ProductMediaFiles.Add(new ProductMediaFile
                        {
                            Product = product,
                            MediaFile = new MediaFile
                            {
                                Url = path,
                                MediaFileType = MediaFileTypeEnum.Image,
                                Priority = Int32.Parse(priority),
                            }
                        });
                    }

                    result.Add(new ProductImageUpdateResponse
                    {
                        Priority = Int32.Parse(priority!),
                        Url = path
                    });
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}