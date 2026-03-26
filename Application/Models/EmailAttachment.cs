using System.IO;

namespace Core.Application.Models
{
    /// <summary>
    /// Model for email attachments with support for inline resources (CID embedding).
    /// </summary>
    public class EmailAttachment
    {
        /// <summary>
        /// File name of the attachment
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Stream containing the attachment content
        /// </summary>
        public Stream ContentStream { get; set; }

        /// <summary>
        /// Content-ID for inline attachments (CID embedding). 
        /// If set, the attachment will be embedded inline in the email.
        /// Example: "pulr-logo-id" will be referenced as cid:pulr-logo-id in HTML
        /// </summary>
        public string ContentId { get; set; }

        /// <summary>
        /// Whether this attachment should be displayed inline in the email body.
        /// When true and ContentId is set, attachment appears as part of the email content.
        /// </summary>
        public bool IsInline { get; set; }

        /// <summary>
        /// MIME type of the attachment (e.g., "image/png", "application/pdf").
        /// If not specified, will be inferred from file extension.
        /// </summary>
        public string MimeType { get; set; }
    }
}
