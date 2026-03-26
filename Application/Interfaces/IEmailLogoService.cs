using System.Threading.Tasks;
using Core.Application.Models;

namespace Core.Application.Interfaces
{
    /// <summary>
    /// Service interface for email logo handling.
    /// Handles logo conversion and attachment creation for emails.
    /// </summary>
    public interface IEmailLogoService
    {
        /// <summary>
        /// Creates a logo attachment for CID embedding in emails.
        /// </summary>
        /// <returns>EmailAttachment with ContentId="logo" for CID embedding, or null if conversion fails</returns>
        Task<EmailAttachment> CreateLogoAttachmentAsync();

        /// <summary>
        /// Adds the logo attachment to email parameters if not already present.
        /// </summary>
        /// <param name="emailParams">Email parameters to add logo attachment to</param>
        /// <returns>True if logo was added, false otherwise</returns>
        Task<bool> AddLogoAttachmentAsync(EmailParamsDto emailParams);

        /// <summary>
        /// Pre-loads the logo into cache. Call this during application startup to ensure logo is available.
        /// </summary>
        /// <returns>True if logo was successfully loaded and cached, false otherwise</returns>
        Task<bool> PreloadLogoAsync();
    }
}
