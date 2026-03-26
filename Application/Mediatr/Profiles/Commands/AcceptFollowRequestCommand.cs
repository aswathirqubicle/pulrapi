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
    public class AcceptFollowRequestCommand : IRequest<ProfileDetailsResponse>
    {
        [Required]
        public string RequesterProfileUid { get; set; }
    }

    public class AcceptFollowRequestCommandHandler : IRequestHandler<AcceptFollowRequestCommand, ProfileDetailsResponse>
    {
        private readonly ILogger<AcceptFollowRequestCommandHandler> _logger;
        private readonly IProfileService _profileService;
        private readonly IMediator _mediator;

        public AcceptFollowRequestCommandHandler(ILogger<AcceptFollowRequestCommandHandler> logger, 
            IProfileService profileService,
            IMediator mediator)
        {
            _logger = logger;
            _profileService = profileService;
            _mediator = mediator;
        }

        public async Task<ProfileDetailsResponse> Handle(AcceptFollowRequestCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _profileService.AcceptFollowRequest(request.RequesterProfileUid);
                // Return the requester's profile details (the one who sent the request) with CanFollowBack = true
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

