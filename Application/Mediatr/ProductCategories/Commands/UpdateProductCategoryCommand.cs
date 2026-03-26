using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.ProductCategories.Commands;

public class UpdateProductCategoryCommand : IRequest <Unit>
{
    [Required] public string Uid { get; set; }
    [Required] public string Title { get; set; }
    public string ParentCategoryUid { get; set; }
    [Required] public string StoreUid { get; set; }
}

public class UpdateProductCategoryCommandHandler : IRequestHandler<UpdateProductCategoryCommand, Unit>
{
    private readonly ILogger<UpdateProductCategoryCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStoreService _storeService;

    public UpdateProductCategoryCommandHandler(
        ILogger<UpdateProductCategoryCommandHandler> logger,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IStoreService storeService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _storeService = storeService;
    }

    public Task<Unit> Handle(UpdateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            throw new NotImplementedException();
            //Store store = await _dbContext.Stores.SingleOrDefaultAsync(s =>
            //        s.Uid == request.StoreUid && s.User.Id == _currentUserService.GetUserId() && s.IsActive == true,
            //    cancellationToken);
            //if (store == null)
            //{
            //    throw new BadRequestException("Store doesn't exist.");
            //}

            //var category =
            //    await _dbContext.ProductCategories.SingleOrDefaultAsync(c =>
            //        c.Uid == request.Uid && c.Store == store, cancellationToken);

            //if (category == null)
            //{
            //    throw new BadRequestException("Category doesn't exist.");
            //}

            //var parentCategory =
            //    await _dbContext.Categories.SingleOrDefaultAsync(c => c.Uid == request.ParentCategoryUid,
            //        cancellationToken);

            //category.Title = request.Title;
            //category.Slug = request.Title.Slugify();
            //if (parentCategory != null)
            //{
            //    category.ParentCategory = parentCategory;
            //}

            //_dbContext.ProductCategories.Add(category);
            //await _dbContext.SaveChangesAsync(cancellationToken);

            //await _storeService.UpdateCategory(new CategoryUpdateDto() { Title = request.Title, ParentCategoryUid = request.ParentCategoryUid});
            //return Unit.Value;
        }
        catch (Exception e)
        {
            throw new BadRequestException(e.Message);
        }
    }
}
