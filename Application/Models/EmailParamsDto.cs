using System.Collections.Generic;

namespace Core.Application.Models
{
    public class EmailParamsDto
    {
        public bool IsTemplateFromFile { get; set; }
        public List<string> To { get; set; } = new List<string>();
        public List<string> Cc { get; set; } = new List<string>();
        public List<string> Bcc { get; set; } = new List<string>();
        public string From { get; set; }
        public string ReplyTo { get; set; }
        public string Subject { get; set; } = "";
        public string Content { get; set; } = "";
        public string TemplatePath { get; set; }
        public object TemplateModel { get; set; }
        public IList<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    }
}
