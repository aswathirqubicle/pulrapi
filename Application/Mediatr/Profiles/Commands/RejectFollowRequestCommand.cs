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
    public class RejectFollowRequestCommand : IRequest<ProfileDetailsResponse>
    {
        [Required]
        public string RequesterProfileUid { get; set; }
    }

    public class RejectFollowRequestCommandHandler : IRequestHandler<RejectFollowRequestCommand, ProfileDetailsResponse>
    {
        private readonly ILogger<RejectFollowRequestCommandHandler> _logger;
        private readonly IProfileService _profileService;
        private readonly IMediator _mediator;

        public RejectFollowRequestCommandHandler(ILogger<RejectFollowRequestCommandHandler> logger, 
            IProfileService profileService,
            IMediator mediator)
        {
            _logger = logger;
            _profileService = profileService;
            _mediator = mediator;
        }

        public async Task<ProfileDetailsResponse> Handle(RejectFollowRequestCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _profileService.RejectFollowRequest(request.RequesterProfileUid);
                // Return the current user's profile details (the one who rejected the request)
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

