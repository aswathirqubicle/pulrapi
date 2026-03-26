using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Extensions.Logging;

namespace Core.Application.Extensions
{
    /// <summary>
    /// Extension methods for EmailParamsDto to add logo attachments easily.
    /// Follows Open/Closed Principle - extends functionality without modifying existing classes.
    /// </summary>
    public static class EmailParamsDtoExtensions
    {
        /// <summary>
        /// Adds the PULR logo attachment to the email parameters using the logo service.
        /// </summary>
        /// <param name="emailParams">Email parameters to add logo to</param>
        /// <param name="logoService">Logo service instance</param>
        /// <returns>True if logo was added, false otherwise</returns>
        public static async Task<bool> AddLogoAsync(this EmailParamsDto emailParams, IEmailLogoService logoService)
        {
            if (logoService == null)
            {
                return false;
            }

            return await logoService.AddLogoAttachmentAsync(emailParams);
        }
    }
}
