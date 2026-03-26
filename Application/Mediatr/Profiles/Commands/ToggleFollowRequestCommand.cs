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
    public class ToggleFollowRequestCommand : IRequest<ProfileDetailsResponse>
    {
        [Required]
        public string TargetProfileUid { get; set; }
    }

    public class ToggleFollowRequestCommandHandler : IRequestHandler<ToggleFollowRequestCommand, ProfileDetailsResponse>
    {
        private readonly ILogger<ToggleFollowRequestCommandHandler> _logger;
        private readonly IProfileService _profileService;
        private readonly IMediator _mediator;

        public ToggleFollowRequestCommandHandler(ILogger<ToggleFollowRequestCommandHandler> logger, 
            IProfileService profileService,
            IMediator mediator)
        {
            _logger = logger;
            _profileService = profileService;
            _mediator = mediator;
        }

        public async Task<ProfileDetailsResponse> Handle(ToggleFollowRequestCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _profileService.ToggleFollowRequest(request.TargetProfileUid);
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
