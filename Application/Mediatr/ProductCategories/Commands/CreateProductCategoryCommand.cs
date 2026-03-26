using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Core.Application.Mediatr.ProductCategories.Commands
{
    public class CreateProductCategoryCommand : IRequest<string>
    {
        [Required] public string Title { get; set; }
        [Required] public string StoreUid { get; set; }
        [Required] public string ParentCategoryUid { get; set; }
    }

    public class CreateProductCategoryCommandHandler : IRequestHandler<CreateProductCategoryCommand, string>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateProductCategoryCommandHandler> _logger;

        public CreateProductCategoryCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            ILogger<CreateProductCategoryCommandHandler> logger
        )
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public Task<string> Handle(CreateProductCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                throw new NotImplementedException();
                //Store store = await _dbContext.Stores.SingleOrDefaultAsync(s =>
                //    s.Uid == request.StoreUid && s.User.Id == _currentUserService.GetUserId() && s.IsActive == true, cancellationToken);
                //if (store == null)
                //{
                //    throw new BadRequestException("Store doesn't exist.");
                //}

                //var parentCategory =
                //    await _dbContext.Categories.SingleOrDefaultAsync(c => c.Uid == request.ParentCategoryUid, cancellationToken);
                //if (parentCategory == null)
                //{
                //    throw new BadRequestException("ParentCategory doesn't exist.");
                //}

                //var slug = request.Title.Slugify();

                //if (_dbContext.ProductCategories.Any(c =>
                //        c.Slug == slug && c.Store == store && c.ParentCategory == parentCategory))
                //{
                //    throw new ValidationException("Category with same title already exists");
                //}

                //if (_dbContext.ProductCategories.Where(c => c.Store == store).Count() >= 300)
                //{
                //    throw new ForbiddenException("Limit of 300 created ProductCategories reached.");
                //}

                //var category = new ProductCategory()
                //{
                //    Title = request.Title,
                //    Slug = slug,
                //    Store = store,
                //    ParentCategory = parentCategory
                //};

                //_dbContext.ProductCategories.Add(category);
                //await _dbContext.SaveChangesAsync(cancellationToken);
                //return category.Uid;


                //var uid = await _storeService.CreateCategory(new CategoryCreateDto() { Title = request.Title, StoreUid = request.StoreUid, ParentCategoryUid = request.ParentCategoryUid});
                //return uid;
            }
            catch (Exception e)
            {
                throw new BadRequestException(e.Message);
            }
        }
    }
}
