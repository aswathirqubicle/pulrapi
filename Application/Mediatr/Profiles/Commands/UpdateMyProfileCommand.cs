using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Profiles.Commands;
using Core.Application.Models.Profiles;
using Core.Application.Security.Validation.Attributes;
using System.Collections.Generic;

namespace Core.Application.Mediatr.Profiles.Commands
{
    public class UpdateMyProfileCommand : IRequest<Unit>
    {
        [Required]
        [PulrNameValidation]
        public string FirstName { get; set; }
        [Required]
        [PulrNameValidation]
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        [Required]
        [PulrUsernameValidation]
        public string Username { get; set; }
        [Required]
        public string Gender { get; set; }
        public string About { get; set; }
        public string PhoneNumber { get; set; }
        public string UserType { get; set; }
        public string CurrencyUid { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string CityName { get; set; }
        public string CountryUid { get; set; }
        public string WebsiteUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string TikTokUrl { get; set; }
    }

    public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand,Unit>
    {
        private readonly ILogger<UpdateMyProfileCommandHandler> _logger;
        private readonly IProfileService _profileService;
        private readonly IMapper _mapper;

        public UpdateMyProfileCommandHandler(ILogger<UpdateMyProfileCommandHandler> logger,
            IProfileService profileService,
            IMapper mapper)
        {
            _logger = logger;
            _profileService = profileService;
            _mapper = mapper;
        }

        public async Task<Unit> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _profileService.Update(_mapper.Map<ProfileUpdateDto>(request));
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
