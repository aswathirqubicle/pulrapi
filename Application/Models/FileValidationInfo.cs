using Core.Domain.Enums;

namespace Core.Application.Models
{
    public class FileValidationInfo
    {
        public bool IsValid { get; set; } = false;
        public bool IsValidExtension { get; set; } = false;
        public FileTypeEnum FileType { get; set; } = FileTypeEnum.Unknown;
        public string Name { get; set; } = null;
        public string Extension { get; set; } = null;
    }
}
