using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.Exceptions;

namespace Core.Application.Mediatr.Users.Commands
{
    public class BlockUserCommand : IRequest<BlockUserResponse>
    {
        public string ProfileIdToBlock { get; set; }
    }

    public class BlockUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string BlockedUserUid { get; set; }
        public string BlockedUsername { get; set; }
        public string BlockedFirstName { get; set; }
        public string BlockedImageUrl { get; set; }
        public DateTime? BlockedAt { get; set; }
    }

    public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, BlockUserResponse>
    {
        private readonly ILogger<BlockUserCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserBlockService _userBlockService;

        public BlockUserCommandHandler(
            ILogger<BlockUserCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserBlockService userBlockService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userBlockService = userBlockService;
        }

        public async Task<BlockUserResponse> Handle(BlockUserCommand request, CancellationToken cancellationToken)
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var currentUserProfile = await _dbContext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id, cancellationToken);

            if (currentUserProfile == null)
            {
                throw new NotFoundException("Current user profile not found.");
            }

            // Check if user is trying to block themselves
            if (currentUserProfile.Uid == request.ProfileIdToBlock)
            {
                throw new BadRequestException("You cannot block yourself.");
            }

            // Ensure target profile exists and load details
            var blockedProfile = await _dbContext.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Uid == request.ProfileIdToBlock, cancellationToken);
            if (blockedProfile == null)
            {
                throw new NotFoundException("Profile not found.");
            }

            // Check if block already exists
            var existingBlock = await _dbContext.UserBlocks
                .FirstOrDefaultAsync(b =>
                    b.BlockerProfileId == currentUserProfile.Uid &&
                    b.BlockedProfileId == request.ProfileIdToBlock,
                    cancellationToken);

            if (existingBlock != null)
            {
                throw new BadRequestException("User is already blocked.");
            }

            // Create new block
            var block = new UserBlock
            {
                BlockerProfileId = currentUserProfile.Uid,
                BlockedProfileId = request.ProfileIdToBlock,
                Uid = Guid.NewGuid().ToString(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.UserBlocks.Add(block);

            // Handle all the effects of blocking (unfollow, remove likes, etc.)
            await _userBlockService.HandleUserBlock(
                currentUserProfile.Uid,
                request.ProfileIdToBlock,
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new BlockUserResponse
            {
                Success = true,
                Message = "User blocked successfully.",
                BlockedUserUid = blockedProfile.Uid,
                BlockedUsername = blockedProfile.User?.UserName,
                BlockedFirstName = blockedProfile.User?.FirstName,
                BlockedImageUrl = blockedProfile.ImageUrl,
                BlockedAt = block.CreatedAt
            };
        }
    }
} 