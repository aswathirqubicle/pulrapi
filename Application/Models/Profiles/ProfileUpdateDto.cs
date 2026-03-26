using Core.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models.Profiles;

public class ProfileUpdateDto
{
    [SafeUid(allowNullValue: false, ErrorMessage = "Profile UID contains invalid characters or format.")]
    public string Uid { get; set; }
    
    [SafeName(maxLength: 100, minLength: 1, allowNullValue: true, ErrorMessage = "Full name contains invalid characters or format.")]
    public string FullName { get; set; }
    
    [SafeName(maxLength: 30, minLength: 3, allowNullValue: true, ErrorMessage = "Username contains invalid characters or format.")]
    public string Username { get; set; }
    
    [SafeName(maxLength: 50, minLength: 1, allowNullValue: true, ErrorMessage = "First name contains invalid characters or format.")]
    public string FirstName { get; set; }
    
    [SafeName(maxLength: 50, minLength: 1, allowNullValue: true, ErrorMessage = "Last name contains invalid characters or format.")]
    public string LastName { get; set; }
    
    [SafeName(maxLength: 50, minLength: 1, allowNullValue: true, ErrorMessage = "Display name contains invalid characters or format.")]
    public string DisplayName { get; set; }
    public string PhoneNumber { get; set; }
    public string UserType { get; set; }
    public string Location { get; set; }
    public GenderEnum? Gender { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    
    [SafeName(maxLength: 100, minLength: 1, allowNullValue: true, ErrorMessage = "City name contains invalid characters or format.")]
    public string CityName { get; set; }
    public string About { get; set; }
    
    [SafeUid(allowNullValue: true, ErrorMessage = "Country UID contains invalid characters or format.")]
    public string CountryUid { get; set; }
    
    [SafeUid(allowNullValue: true, ErrorMessage = "Currency UID contains invalid characters or format.")]
    public string CurrencyUid { get; set; }
    public string WebsiteUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
    public List<ProfileSocialMediaLinkDto> SocialMediaLinks { get; set; } = new List<ProfileSocialMediaLinkDto>();
}