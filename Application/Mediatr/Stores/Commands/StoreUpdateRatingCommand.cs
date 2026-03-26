using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stores.Commands;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class StoreUpdateRatingCommand : IRequest <Unit>
    {
        [Required] public string StoreUid { get; set; }
        [Required] public double Rating { get; set; }
    }

    public class StoreUpdateRatingCommandHandler : IRequestHandler<StoreUpdateRatingCommand,Unit>
    {
        private readonly ILogger<StoreUpdateRatingCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public StoreUpdateRatingCommandHandler(
            ILogger<StoreUpdateRatingCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext
        )
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<Unit> Handle(StoreUpdateRatingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser.Profile == null)
                {
                    throw new BadRequestException($"Profile doesnt exist for user '{cUser.UserName}'.");
                }

                var store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Uid == request.StoreUid, cancellationToken);
                
                if (store == null)
                {
                    throw new BadRequestException($"Store with uid '{request.StoreUid}' doesnt exist.");
                }

                var storeRating = await _dbContext.StoreRatings.SingleOrDefaultAsync(sr =>
                    sr.Store.Uid == request.StoreUid && sr.RatedById == cUser.Profile.Id, cancellationToken);
                if (storeRating == null)
                {
                    _dbContext.StoreRatings.Add(new StoreRating
                    {
                        Store = store,
                        RatedBy = cUser.Profile,
                        NumberOfStars = request.Rating
                    });
                }
                else
                {
                    storeRating.NumberOfStars = request.Rating;
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
                // await _storeService.UpdateRating(request.StoreUid, request.Rating);
                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
