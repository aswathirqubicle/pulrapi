using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Profiles;
using Core.Application.Mediatr.Profiles.Queries;

namespace Core.Application.Mediatr.Profiles.Commands
{
    public class ProfileToggleFollowCommand : IRequest<ProfileDetailsResponse>
    {
        [Required]
        public string ProfileUid { get; set; }
    }

    public class ProfileToggleFollowCommandHandler : IRequestHandler<ProfileToggleFollowCommand, ProfileDetailsResponse>
    {
        private readonly ILogger<ProfileToggleFollowCommandHandler> _logger;
        private readonly IProfileService _profileService;
        private readonly IMediator _mediator;

        public ProfileToggleFollowCommandHandler(ILogger<ProfileToggleFollowCommandHandler> logger, 
            IProfileService profileService,
            IMediator mediator)
        {
            _logger = logger;
            _profileService = profileService;
            _mediator = mediator;
        }
        public async Task<ProfileDetailsResponse> Handle(ProfileToggleFollowCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _profileService.ProfileToggleFollow(request.ProfileUid);
                return await _mediator.Send(new GetProfileQuery() { Username = result.username });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }


}
