using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Products;
using Core.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetProductOnboardingPreferencesQuery : IRequest<ProductOnboardingPreferencesResponse>
    {
        [Required]
        public string ProductUid { get; set; }
    }

    public class GetProductOnboardingPreferencesQueryHandler : IRequestHandler<GetProductOnboardingPreferencesQuery, ProductOnboardingPreferencesResponse>
    {
        private readonly ILogger<GetProductOnboardingPreferencesQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public GetProductOnboardingPreferencesQueryHandler(ILogger<GetProductOnboardingPreferencesQueryHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<ProductOnboardingPreferencesResponse> Handle(GetProductOnboardingPreferencesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var product = await _dbContext.Products
                    //.Include(p => p.ProductOnboardingPreferences)
                    .SingleOrDefaultAsync(p => p.Store.UserId == _currentUserService.GetUserId() && p.Uid == request.ProductUid);

                if (product == null)
                {
                    throw new BadRequestException("Product not found.");
                }
                var preferences = await _dbContext.ProductOnboardingPreferences.Where(pop => pop.Product.Uid == request.ProductUid).Select(pop => new ProductOnboardingPreferenceResponse()
                {
                    Uid = pop.OnboardingPreference.Uid,
                    Name = pop.OnboardingPreference.Name,
                }).ToListAsync(cancellationToken);

                return new ProductOnboardingPreferencesResponse()
                {
                    ProductUid = product.Uid,
                    Preferences = preferences
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
