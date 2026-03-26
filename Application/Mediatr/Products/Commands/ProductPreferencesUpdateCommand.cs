using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Products.Commands
{
    public class ProductPreferencesUpdateCommand : IRequest<string>
    {
        [Required]
        public string ProductUid { get; set; }
        [MaxLength(3)]
        public ICollection<string> PreferenceUids { get; set; } = new List<string>();
    }

    public class ProductPreferencesUpdateCommandHandler : IRequestHandler<ProductPreferencesUpdateCommand, string>
    {
        private readonly ILogger<ProductPreferencesUpdateCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public ProductPreferencesUpdateCommandHandler(
            ILogger<ProductPreferencesUpdateCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }
        public async Task<string> Handle(ProductPreferencesUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.PreferenceUids.Distinct().Count() > 3)
                {
                    throw new BadRequestException("Only 3 preferences per product allowed.");
                }

                var product = await _dbContext.Products.SingleOrDefaultAsync(p => p.Store.UserId == _currentUserService.GetUserId() && p.Uid == request.ProductUid);

                if (product == null)
                {
                    throw new BadRequestException("Product not found.");
                }

                var productPreferencesToRemove = await _dbContext.ProductOnboardingPreferences
                                .Where(pp => pp.Product == product && request.PreferenceUids.Contains(pp.OnboardingPreference.Uid) == false)
                                .ToListAsync(cancellationToken);

                if (productPreferencesToRemove.Any())
                {
                    _dbContext.ProductOnboardingPreferences.RemoveRange(productPreferencesToRemove);
                }

                foreach (var preferenceUid in request.PreferenceUids)
                {
                    var productPreferencesExisting = await _dbContext.ProductOnboardingPreferences.SingleOrDefaultAsync(pop => 
                                                pop.Product == product &&
                                                pop.OnboardingPreference.Uid == preferenceUid, cancellationToken);

                    if (productPreferencesExisting == null)
                    {
                        _dbContext.ProductOnboardingPreferences.Add(new ProductOnboardingPreference()
                        {
                            Product = product,
                            OnboardingPreference = await _dbContext.OnboardingPreferences.SingleOrDefaultAsync(p => p.Uid == preferenceUid)
                        });
                    }
                }

                var preferencesCount = await _dbContext.ProductOnboardingPreferences.Where(pop => pop.Product == product).CountAsync(cancellationToken);

                if (preferencesCount > 3)
                {
                    throw new BadRequestException("Only 3 preferences per product allowed.");
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return product.Uid;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }


}

