using System;
using System.Threading.Tasks;
using Core.Application.DTOs;
using Core.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileSettingsController : ControllerBase
    {
        private readonly IProfileSettingsService _profileSettingsService;

        public ProfileSettingsController(IProfileSettingsService profileSettingsService)
        {
            _profileSettingsService = profileSettingsService;
        }

        [HttpGet]
        public async Task<ActionResult<ProfileSettingsDto>> GetProfileSettings()
        {
            try
            {
                var settings = await _profileSettingsService.GetProfileSettingsAsync();
                return Ok(settings);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut]
        public async Task<ActionResult<ProfileSettingsDto>> UpdateProfileSettings(UpdateProfileSettingsDto settings)
        {
            try
            {
                var updatedSettings = await _profileSettingsService.UpdateProfileSettingsAsync(settings);
                return Ok(updatedSettings);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
} 