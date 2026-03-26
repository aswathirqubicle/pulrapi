using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Categories.Queries;

public class GetCategoriesQuery : IRequest<List<CategoryResponse>>
{
    public string Search { get; set; }
}

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryResponse>>
{
    private readonly ILogger<GetCategoriesQueryHandler> _logger;
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(ILogger<GetCategoriesQueryHandler> logger, IApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<List<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            List<Category> categories = await _context.Categories.ToListAsync(cancellationToken);

            var categoryTree = BuildCategoryTree(categories);
            var filtered = FilterCategoryTree(categoryTree, request.Search);
            var categoryTreeResponse = ConvertToCategoryTreeResponse(filtered);

            return categoryTreeResponse;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error fetching categories");
            throw;
        }
    }

    public static List<CategoryTree> BuildCategoryTree(List<Category> allCategories)
    {
        var rootCategories = allCategories.Where(c => c.ParentCategoryId == null).ToList();
        List<CategoryTree> tree = new List<CategoryTree>();

        foreach (var rootCategory in rootCategories)
        {
            var rootNode = BuildSubtree(rootCategory, 1, allCategories);
            tree.Add(rootNode);
        }

        return tree;
    }

    private static CategoryTree BuildSubtree(Category category, int numLevel, List<Category> allCategories)
    {
        var node = new CategoryTree(category, numLevel);

        var childCategories = allCategories
            .Where(c => c.ParentCategoryId == category.Id)
            .ToList();

        foreach (var childCategory in childCategories)
        {
            var childNode = BuildSubtree(childCategory, numLevel + 1, allCategories);
            node.SubCategories.Add(childNode);
        }

        return node;
    }

    public static List<CategoryTree> FilterCategoryTree(List<CategoryTree> categoryTree, string searchTerm)
    {
        return categoryTree
            .Select(root => FilterTree(root, searchTerm))
            .Where(filteredRoot => filteredRoot != null)
            .ToList();
    }

    private static CategoryTree FilterTree(CategoryTree categoryNode, string searchTerm)
    {
        if (String.IsNullOrEmpty(searchTerm))
        {
            var filtered = categoryNode.SubCategories
                .Where(filteredSubCategory => filteredSubCategory != null)
                .ToList();
            
            return filtered.Any() ? new CategoryTree(categoryNode.Category, categoryNode.NumLevel, filtered) : null;
        }

        if (categoryNode.Category.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            var filteredNode = new CategoryTree(categoryNode.Category, categoryNode.NumLevel);
            foreach (var subCategory in categoryNode.SubCategories)
            {
                var filteredSubCategory = FilterTree(subCategory, searchTerm);
                if (filteredSubCategory != null)
                {
                    filteredNode.SubCategories.Add(filteredSubCategory);
                }
            }

            return filteredNode;
        }

        // If the current node doesn't match the search term, check its children
        var filteredChildren = categoryNode.SubCategories
            .Select(subCategory => FilterTree(subCategory, searchTerm))
            .Where(filteredSubCategory => filteredSubCategory != null)
            .ToList();

        // If any child matches, include the current node in the filtered tree
        return filteredChildren.Any() ? new CategoryTree(categoryNode.Category, categoryNode.NumLevel, filteredChildren) : null;
    }

    private List<CategoryResponse> ConvertToCategoryTreeResponse(List<CategoryTree> categoryTree)
    {
        var result = new List<CategoryResponse>();

        foreach (var rootCategory in categoryTree)
        {
            var rootViewModel = ConvertToViewModel(rootCategory);
            result.Add(rootViewModel);
        }

        return result;
    }

    private CategoryResponse ConvertToViewModel(CategoryTree categoryNode)
    {
        var viewModel = new CategoryResponse
        {
            Uid = categoryNode.Category.Uid,
            Name = categoryNode.Category.Name,
            Level = categoryNode.NumLevel,
            SubCategories = new List<CategoryResponse>()
        };

        foreach (var subCategory in categoryNode.SubCategories)
        {
            var subCategoryViewModel = ConvertToViewModel(subCategory);
            viewModel.SubCategories.Add(subCategoryViewModel);
        }

        return viewModel;
    }
}