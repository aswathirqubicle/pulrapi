using FileTypeChecker.Abstracts;
using FileTypeChecker.Extensions;
using FileTypeChecker;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Collections.Generic;
using Core.Application.Models;
using Core.Domain.Enums;

namespace Core.Application.Helpers
{
    public static class FileHelper
    {

        private static readonly List<(FileTypeEnum, string[])> _allowedExtensionsList = new List<(FileTypeEnum, string[])>(){
            (FileTypeEnum.Image, new string[] { "jpg", "jpeg", "png", "webp" }),
            (FileTypeEnum.Video, new string[] { "mp4", "avi", "wmv", "webm", "ogg" , "mpg", "mpeg" })};

        public static byte[] streamToByteArray(Stream input)
        {
            MemoryStream ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();

        }

        public static FileValidationInfo CheckFile(IFormFile file, FileTypeEnum[] allowedFileTypes = null, List<string> allowedExtensions = null)
        {

            var iFormFileExtension = Path.GetExtension(file.FileName).Substring(1);

            if (allowedExtensions == null)
            {
                allowedExtensions = new List<string>();
            }

            if (allowedFileTypes == null)
            {
                allowedFileTypes = new FileTypeEnum[2] { FileTypeEnum.Image, FileTypeEnum.Video };
            }

            foreach (var allowedFileType in allowedFileTypes)
            {
                var allowedExtensionIndex = _allowedExtensionsList.FindIndex(e => e.Item1 == allowedFileType);
                if (allowedExtensionIndex > -1)
                {
                    allowedExtensions.AddRange(_allowedExtensionsList[allowedExtensionIndex].Item2);
                }
            }

            using (var fileStream = file.OpenReadStream())
            {
                var fileValidationInfo = new FileValidationInfo();

                if(iFormFileExtension == "mp4")
                {
                    fileValidationInfo.IsValid = allowedExtensions.Contains(iFormFileExtension);
                    fileValidationInfo.IsValidExtension = allowedExtensions.Contains(iFormFileExtension);
                    fileValidationInfo.Extension= iFormFileExtension;
                    fileValidationInfo.FileType= FileTypeEnum.Video;
                    return fileValidationInfo;
                }

                var isRecognizableType = FileTypeValidator.IsTypeRecognizable(fileStream);
                if (!isRecognizableType)
                {
                    return fileValidationInfo;
                }

                IFileType fileType = FileTypeValidator.GetFileType(fileStream);
                fileValidationInfo.Name = fileType.Name;
                fileValidationInfo.Extension = fileType.Extension;

                if (iFormFileExtension == "webp" && !fileStream.IsArchive() && !fileStream.IsExecutable() && !fileStream.IsDocument())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Image;
                }
                else if (fileStream.IsExecutable())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Executable;
                    return fileValidationInfo;
                }
                else if (fileStream.IsArchive())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Archive;
                    return fileValidationInfo;
                }
                else if (fileStream.IsImage())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Image;
                }
                else if (FileHasVideoExtension(fileType.Extension))
                {
                    fileValidationInfo.FileType = FileTypeEnum.Video;
                }

                fileValidationInfo.IsValid = allowedFileTypes.Contains(fileValidationInfo.FileType);
                fileValidationInfo.IsValidExtension = allowedExtensions.Contains(iFormFileExtension == "webp" ? iFormFileExtension : fileType.Extension);

                return fileValidationInfo;
            }
        }

        public static bool FileHasVideoExtension(string extension)
        {
            
            return _allowedExtensionsList.Where(ae => ae.Item1 == FileTypeEnum.Video).Select(ae => ae.Item2).FirstOrDefault().Contains(extension);
        }

        public static MediaFileTypeEnum FileTypeEnumToMediaFileTypeEnum(FileTypeEnum fileTypeEnum)
        {
            if (fileTypeEnum == FileTypeEnum.Video)
            {
                return MediaFileTypeEnum.Video;
            }

            if (fileTypeEnum == FileTypeEnum.Image)
            {
                return MediaFileTypeEnum.Image;
            }

            throw new NotImplementedException();
        }
    }
}
