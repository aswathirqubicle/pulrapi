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
    public class  ProductPreferencesCreateCommand : IRequest<string>
    {
        [Required]
        public string ProductUid { get; set; }
        [MaxLength(3)]
        public ICollection<string> PreferenceUids { get; set; } = new List<string>();
    }

    public class ProductPreferencesCreateCommandHandler : IRequestHandler<ProductPreferencesCreateCommand,string>
    {
        private readonly ILogger<ProductPreferencesCreateCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public ProductPreferencesCreateCommandHandler(
            ILogger<ProductPreferencesCreateCommandHandler> logger, 
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }
        public async Task<string> Handle(ProductPreferencesCreateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if(request.PreferenceUids.Distinct().Count() > 3)
                {
                    throw new BadRequestException("Only 3 preferences per product allowed.");
                }

                var product = await _dbContext.Products
                    //.Include(p => p.ProductOnboardingPreferences)
                    .SingleOrDefaultAsync(p => p.Store.UserId == _currentUserService.GetUserId() && p.Uid == request.ProductUid);

                if(product == null)
                {
                    throw new BadRequestException("Product not found.");
                }

                //if (product.ProductOnboardingPreferences.Any())
                //{
                //    throw new BadRequestException("Product already has onboarding preferences.");
                //}

                if (request.PreferenceUids.Any())
                {
                    foreach (var preferenceUid in request.PreferenceUids.Distinct())
                    {
                        var newPreference = new ProductOnboardingPreference
                        {
                            Product = product,
                            OnboardingPreference = await _dbContext.OnboardingPreferences.SingleOrDefaultAsync(p => p.Uid == preferenceUid)
                        };

                        _dbContext.ProductOnboardingPreferences.Add(newPreference);
                    }
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

