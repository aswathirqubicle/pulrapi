using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Profiles.Queries;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class ProfileGetHandlesQuery : IRequest<List<string>>
    {
        [Required]
        [MinLength(3)]
        public string Search { get; set; }
    }

    public class ProfileGetHandlesQueryHandler : IRequestHandler<ProfileGetHandlesQuery, List<string>>
    {
        private readonly ILogger<ProfileGetHandlesQueryHandler> _logger;
        private readonly IProfileService _profileService;

        public ProfileGetHandlesQueryHandler(ILogger<ProfileGetHandlesQueryHandler> logger,
            IProfileService profileService)
        {
            _logger = logger;
            _profileService = profileService;
        }

        public async Task<List<string>> Handle(ProfileGetHandlesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _profileService.SearchHandles(request.Search);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
