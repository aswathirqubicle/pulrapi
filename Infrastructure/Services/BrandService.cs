using Core.Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Infrastructure.Services
{
    public class BrandService : IBrandService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<BrandService> _logger;

        public BrandService(IApplicationDbContext dbContext, ILogger<BrandService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<string> GetOrCreateBrandAsync(string brandName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(brandName))
                return null;

            var trimmedBrandName = brandName.Trim();
            
            // Check if brand already exists
            var existingBrand = await _dbContext.Brands
                .SingleOrDefaultAsync(b => b.Name.ToLower() == trimmedBrandName.ToLower(), cancellationToken);

            if (existingBrand != null)
            {
                return existingBrand.Name;
            }

            // Create new brand
            var newBrand = new Brand
            {
                Name = trimmedBrandName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            _dbContext.Brands.Add(newBrand);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new brand: {BrandName}", trimmedBrandName);
            
            return newBrand.Name;
        }
    }
} 