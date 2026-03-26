using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using FluentValidation.Results;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Core.Application.Mediatr.Profiles.Commands;

public class UpdateProfileBioCommand : IRequest<ProfileBioDto>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string Username { get; set; }
    public string About { get; set; }
    public string Location { get; set; }
    public string UserType { get; set; }
    public string PhoneNumber { get; set; }
    public string WebsiteUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
    public List<ProfileSocialMediaLinkDto> SocialMediaLinks { get; set; }
}

public class UpdateProfileBioCommandHandler : IRequestHandler<UpdateProfileBioCommand, ProfileBioDto>
{
    private readonly IMapper _mapper;
    private readonly IProfileService _profileService;

    public UpdateProfileBioCommandHandler(
        IMapper mapper,
        IProfileService profileService)
    {
        _mapper = mapper;
        _profileService = profileService;
    }

    public async Task<ProfileBioDto> Handle(UpdateProfileBioCommand request, CancellationToken cancellationToken)
    {
        // Map command to DTO
        var updateDto = _mapper.Map<ProfileUpdateDto>(request);
        string message = null;
        try
        {
            message = await _profileService.Update(updateDto);
        }
        catch (SuccessException ex)
        {
            // Optionally, you can set a message in the response
            // or handle "no changes" vs "changes updated" here
            throw new SuccessException(ex.Message);
        }
        catch (ValidationException ex)
        {
            // Handle validation errors
            throw new ValidationException(ex.Message);
        }
        catch (Exception ex)
        {
            // Log and rethrow unexpected exceptions
            // _logger.LogError(ex, "Error updating profile bio");
            throw new Exception("An error occurred while updating the profile bio", ex);
        }
        // Fetch the full user with all includes
        var user = await _profileService.GetCurrentUserWithProfile();
        if (user?.Profile == null)
        {
            throw new NotFoundException("User or profile not found");
        }
        user.Profile.User = user; // Ensure navigation property is set for mapping
        var profileBioDto = _mapper.Map<ProfileBioDto>(user.Profile);
        profileBioDto.Message = message;
        return profileBioDto;
    }
    
}