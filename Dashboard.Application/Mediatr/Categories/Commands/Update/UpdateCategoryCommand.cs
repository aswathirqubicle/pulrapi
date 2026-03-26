using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Application.Mediatr.Categories.Commands.Update;

public class UpdateCategoryCommand : IRequest <Unit>
{
    public string? CategoryUid { get; set; }
    public string? Name { get; set; }
    public string? ParentCategoryUid { get; set; }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand,Unit>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateCategoryCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Find the category to update
        var categoryToUpdate = await _dbContext.Categories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Uid == request.CategoryUid, cancellationToken);

        if (categoryToUpdate == null)
        {
            // Handle the case where the category doesn't exist
            throw new Exception("Category not found.");
        }

        // Update the category name and generate the slug
        categoryToUpdate.Name = request.Name;
        categoryToUpdate.Slug = request?.Name?.ToLower().Replace(" ", "-");

        if (string.IsNullOrEmpty(request?.ParentCategoryUid))
        {
            throw new ArgumentException("ParentCategoryUid cannot be null or empty.");
        }

        var newParentCategory = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Uid == request.ParentCategoryUid, cancellationToken);

        if (newParentCategory == null)
        {
            throw new NotFoundException("New parent category not found");
        }

        // Check if the parent category is changing
        if (categoryToUpdate.ParentCategoryId != newParentCategory.Id)
        {
            // Update the parent category
            var oldParentCategory = categoryToUpdate.ParentCategory;
            categoryToUpdate.ParentCategoryId = newParentCategory.Id;

            // Update closure data
            await UpdateClosureData(categoryToUpdate, oldParentCategory, newParentCategory, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task UpdateClosureData(Category categoryToUpdate, Category oldParentCategory,
        Category newParentCategory, CancellationToken cancellationToken)
    {
        // Find all descendants of the category being moved
        var descendants = await _dbContext.CategoryClosures
            .Where(c => c.AncestorId == categoryToUpdate.Id)
            .Select(c => c.DescendantId)
            .ToListAsync(cancellationToken);

        // Remove closure data for the old parent-child relationship
        var closureToRemove = await _dbContext.CategoryClosures
            .Where(c => c.AncestorId == oldParentCategory.Id && descendants.Contains(c.DescendantId))
            .ToListAsync(cancellationToken);

        _dbContext.CategoryClosures.RemoveRange(closureToRemove);

        // Calculate and insert closure data for the new parent-child relationship
        var closureToAdd = new List<CategoryClosure>();

        var numLevelToUpdate = await _dbContext.CategoryClosures
            .Where(c => c.AncestorId == categoryToUpdate.Id && c.DescendantId == categoryToUpdate.Id)
            .Select(c => c.NumLevel)
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var descendantId in descendants)
        {
            closureToAdd.Add(new CategoryClosure
            {
                AncestorId = newParentCategory.Id,
                DescendantId = descendantId,
                NumLevel = numLevelToUpdate + 1
            });
        }

        _dbContext.CategoryClosures.AddRange(closureToAdd);
    }
}