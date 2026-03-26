using System;
using System.Threading.Tasks;
using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities;
//using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Services
{
    public class ProfileSettingsService : IProfileSettingsService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ProfileSettingsService(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ProfileSettingsDto> GetProfileSettingsAsync()
        {
            var currentUserId = _currentUserService.GetUserId();
            var settings = await _context.ProfileSettings
                .Include(s => s.Profile)
                .FirstOrDefaultAsync(s => s.Profile.UserId == currentUserId);

            if (settings == null)
            {
                throw new Exception("Profile settings not found");
            }

            return new ProfileSettingsDto
            {
                IsProfilePublic = settings.IsProfilePublic,
                ShowSocialMediaLinks = settings.ShowSocialMediaLinks,
                ShowFollowers = settings.ShowFollowers,
                ShowFollowing = settings.ShowFollowing,
                ShowLocation = settings.ShowLocation,
                ShowAbout = settings.ShowAbout
            };
        }

        public async Task<ProfileSettingsDto> UpdateProfileSettingsAsync(UpdateProfileSettingsDto settingsDto)
        {
            var currentUserId = _currentUserService.GetUserId();
            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (profile == null)
            {
                throw new Exception("Profile not found for current user");
            }

            var settings = await _context.ProfileSettings
                .Include(s => s.Profile)
                .FirstOrDefaultAsync(s => s.Profile.UserId == currentUserId);

            if (settings == null)
            {
                // Create new profile settings if they don't exist
                settings = new ProfileSettings
                {
                    ProfileId = profile.Id,
                    IsProfilePublic = settingsDto.IsProfilePublic ?? true, // Default to public
                    ShowSocialMediaLinks = settingsDto.ShowSocialMediaLinks ?? true,
                    ShowFollowers = settingsDto.ShowFollowers ?? true,
                    ShowFollowing = settingsDto.ShowFollowing ?? true,
                    ShowLocation = settingsDto.ShowLocation ?? true,
                    ShowAbout = settingsDto.ShowAbout ?? true
                };
                _context.ProfileSettings.Add(settings);
            }
            else
            {
                // Only update fields that have been changed
                if (settingsDto.IsProfilePublic.HasValue)
                {
                    settings.IsProfilePublic = settingsDto.IsProfilePublic.Value;
                }
                if (settingsDto.ShowSocialMediaLinks.HasValue)
                {
                    settings.ShowSocialMediaLinks = settingsDto.ShowSocialMediaLinks.Value;
                }
                if (settingsDto.ShowFollowers.HasValue)
                {
                    settings.ShowFollowers = settingsDto.ShowFollowers.Value;
                }
                if (settingsDto.ShowFollowing.HasValue)
                {
                    settings.ShowFollowing = settingsDto.ShowFollowing.Value;
                }
                if (settingsDto.ShowLocation.HasValue)
                {
                    settings.ShowLocation = settingsDto.ShowLocation.Value;
                }
                if (settingsDto.ShowAbout.HasValue)
                {
                    settings.ShowAbout = settingsDto.ShowAbout.Value;
                }
            }

            await _context.SaveChangesAsync(cancellationToken: default);

            return new ProfileSettingsDto
            {
                IsProfilePublic = settings.IsProfilePublic,
                ShowSocialMediaLinks = settings.ShowSocialMediaLinks,
                ShowFollowers = settings.ShowFollowers,
                ShowFollowing = settings.ShowFollowing,
                ShowLocation = settings.ShowLocation,
                ShowAbout = settings.ShowAbout
            };
        }
    }
} 