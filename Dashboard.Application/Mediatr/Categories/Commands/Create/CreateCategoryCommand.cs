using System.ComponentModel.DataAnnotations;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Categories.Commands.Create;

public class CreateCategoryCommand : IRequest<string>
{
    [Required] public string? Name { get; set; }
    [Required] public string? ParentUid { get; set; }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, string>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<CreateCategoryCommandHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var parentCategory =
                await _dbContext.Categories.FirstOrDefaultAsync(c => c.Uid == request.ParentUid, cancellationToken);

            var isRootCategory = parentCategory == null;

            if (isRootCategory)
            {
                // Adding a new root category
                parentCategory = new Category { ParentCategoryId = null }; // Set parentCategory to null
            }
            else if (parentCategory == null)
            {
                throw new Exception("Parent category not found.");
            }

            var newCategory = new Category
            {
                Name = request.Name,
                Slug = request?.Name?.ToLower().Replace(" ", "-"),
                ParentCategoryId = parentCategory.Id
            };

            _dbContext.Categories.Add(newCategory);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await CalculateAndInsertClosureData(newCategory, parentCategory, cancellationToken);

            return newCategory.Uid;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating a new category with message {Message}", e.Message);
            throw;
        }
    }

    private async Task CalculateAndInsertClosureData(Category newCategory, Category parentCategory,
        CancellationToken cancellationToken)
    {
        try
        {
            var closureData = new List<CategoryClosure>();

            if (parentCategory != null)
            {
                // Calculate and insert closure data for the new category based on the existing hierarchy
                await AddClosureRecordsForAncestors(closureData, parentCategory, newCategory, cancellationToken);
            }
            else
            {
                // Adding a new root category
                closureData.Add(new CategoryClosure
                {
                    AncestorId = newCategory.Id,
                    DescendantId = newCategory.Id,
                    NumLevel = 0
                });
            }
            _dbContext.CategoryClosures.AddRange(closureData);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error calculating category insert data {Message}", e.Message);
            throw;
        }
    }

    private async Task AddClosureRecordsForAncestors(List<CategoryClosure> closureData, Category currentCategory,
        Category newCategory, CancellationToken cancellationToken)
    {
        try
        {
            if (currentCategory == null || currentCategory.Id == newCategory.Id)
            {
                return;
            }

            var numLevel = closureData.Count(c => c.DescendantId == currentCategory.Id);

            closureData.Add(new CategoryClosure
                { AncestorId = currentCategory.Id, DescendantId = newCategory.Id, NumLevel = numLevel + 1 });

            await AddClosureRecordsForAncestors(closureData, currentCategory.ParentCategory, newCategory,
                cancellationToken);
        }

        catch (Exception e)
        {
            _logger.LogError(e, "Error calculating category insert data {Message}", e.Message);
            throw;
        }
    }
}