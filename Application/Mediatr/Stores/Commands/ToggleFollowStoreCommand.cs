using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stores.Commands;
using Core.Domain.Entities;
using AutoMapper;
using Core.Application.Models.Stores;
using Core.Application.Mediatr.Stores.Queries;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class ToggleFollowStoreCommand : IRequest<StoreDetailsResponse>
    {
        [Required] public string StoreUid { get; set; }
    }

    public class ToggleFollowStoreCommandHandler : IRequestHandler<ToggleFollowStoreCommand, StoreDetailsResponse>
    {
        private readonly ILogger<ToggleFollowStoreCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMediator _mediator;

        public ToggleFollowStoreCommandHandler(
            ILogger<ToggleFollowStoreCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IMediator mediator)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mediator = mediator;
        }

        public async Task<StoreDetailsResponse> Handle(ToggleFollowStoreCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();

                var store = await _dbContext.Stores.SingleOrDefaultAsync(p => p.IsActive && p.Uid == request.StoreUid,
                    cancellationToken);
                if (store == null)
                {
                    throw new BadRequestException($"Store with uid '{request.StoreUid}' doesn't exist.");
                }

                var follower = await _dbContext.Profiles.SingleOrDefaultAsync(
                    p => p.IsActive && p.Uid == user.Profile.Uid,
                    cancellationToken);

                if (follower == null)
                    throw new BadRequestException($"Profile with uid '{user.Profile.Uid}' doesn't exist.");

                var sf = await _dbContext.StoreFollowers
                    .Where(sf => sf.Store.Uid == request.StoreUid && sf.Follower.Uid == user.Profile.Uid)
                    .SingleOrDefaultAsync(cancellationToken);

                if (sf != null)
                {
                    _dbContext.StoreFollowers.Remove(sf);
                }
                else
                {
                    _dbContext.StoreFollowers.Add(new StoreFollower { Store = store, Follower = follower });
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                return await _mediator.Send(new GetStoreDetailsQuery() { Uid = store.Uid });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
