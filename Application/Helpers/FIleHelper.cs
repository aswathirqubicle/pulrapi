using FileTypeChecker.Abstracts;
using FileTypeChecker.Extensions;
using FileTypeChecker;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Collections.Generic;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Domain.Enums;

namespace Core.Application.Helpers
{
    public static class FileHelper
    {

        private static readonly List<(FileTypeEnum, string[])> _allowedExtensionsList = new List<(FileTypeEnum, string[])>(){
            (FileTypeEnum.Image, new string[] { "jpg", "jpeg", "png", "webp", "avif" }),
            (FileTypeEnum.Video, new string[] { "mp4", "avi", "wmv", "webm", "ogg" , "mpg", "mpeg" }),
            (FileTypeEnum.Document, new string[] { "pdf" })};

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
                allowedFileTypes = new FileTypeEnum[3] { FileTypeEnum.Image, FileTypeEnum.Video, FileTypeEnum.Document };
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
                    var allowed = allowedExtensions.Contains(iFormFileExtension) && HasMp4Signature(fileStream);
                    fileValidationInfo.IsValid = allowed;
                    fileValidationInfo.IsValidExtension = allowed;
                    fileValidationInfo.Extension= iFormFileExtension;
                    fileValidationInfo.FileType= FileTypeEnum.Video;
                    return fileValidationInfo;
                }

                if(iFormFileExtension == "pdf")
                {
                    var allowed = allowedExtensions.Contains(iFormFileExtension) && HasPdfSignature(fileStream);
                    fileValidationInfo.IsValid = allowed;
                    fileValidationInfo.IsValidExtension = allowed;
                    fileValidationInfo.Extension = iFormFileExtension;
                    fileValidationInfo.FileType = FileTypeEnum.Document;
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
                else if (iFormFileExtension == "avif" && !fileStream.IsArchive() && !fileStream.IsExecutable() && !fileStream.IsDocument())
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
                else if (fileStream.IsDocument())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Document;
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
                fileValidationInfo.IsValidExtension = allowedExtensions.Contains(iFormFileExtension == "webp" || iFormFileExtension == "avif" ? iFormFileExtension : fileType.Extension);

                return fileValidationInfo;
            }
        }

        public static FileValidationInfo CheckFile(StreamingMediaFile file, FileTypeEnum[] allowedFileTypes = null, List<string> allowedExtensions = null)
        {

            var iFormFileExtension = Path.GetExtension(file.FileName).Substring(1);

            if (allowedExtensions == null)
            {
                allowedExtensions = new List<string>();
            }

            if (allowedFileTypes == null)
            {
                allowedFileTypes = new FileTypeEnum[3] { FileTypeEnum.Image, FileTypeEnum.Video, FileTypeEnum.Document };
            }

            foreach (var allowedFileType in allowedFileTypes)
            {
                var allowedExtensionIndex = _allowedExtensionsList.FindIndex(e => e.Item1 == allowedFileType);
                if (allowedExtensionIndex > -1)
                {
                    allowedExtensions.AddRange(_allowedExtensionsList[allowedExtensionIndex].Item2);
                }
            }

            // Validate directly against the (already buffered, seekable) source stream
            // to avoid an extra full-size copy in memory.
            var fileCopy = file.Stream;
            try
            {
                fileCopy.Position = 0;

                var fileValidationInfo = new FileValidationInfo();

                if(iFormFileExtension == "mp4")
                {
                    var allowed = allowedExtensions.Contains(iFormFileExtension) && HasMp4Signature(fileCopy);
                    fileValidationInfo.IsValid = allowed;
                    fileValidationInfo.IsValidExtension = allowed;
                    fileValidationInfo.Extension= iFormFileExtension;
                    fileValidationInfo.FileType= FileTypeEnum.Video;
                    return fileValidationInfo;
                }

                if(iFormFileExtension == "pdf")
                {
                    var allowed = allowedExtensions.Contains(iFormFileExtension) && HasPdfSignature(fileCopy);
                    fileValidationInfo.IsValid = allowed;
                    fileValidationInfo.IsValidExtension = allowed;
                    fileValidationInfo.Extension = iFormFileExtension;
                    fileValidationInfo.FileType = FileTypeEnum.Document;
                    return fileValidationInfo;
                }

                var isRecognizableType = FileTypeValidator.IsTypeRecognizable(fileCopy);
                if (!isRecognizableType)
                {
                    return fileValidationInfo;
                }

                IFileType fileType = FileTypeValidator.GetFileType(fileCopy);
                fileValidationInfo.Name = fileType.Name;
                fileValidationInfo.Extension = fileType.Extension;

                if (iFormFileExtension == "webp" && !fileCopy.IsArchive() && !fileCopy.IsExecutable() && !fileCopy.IsDocument())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Image;
                }
                else if (iFormFileExtension == "avif" && !fileCopy.IsArchive() && !fileCopy.IsExecutable() && !fileCopy.IsDocument())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Image;
                }
                else if (fileCopy.IsExecutable())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Executable;
                    return fileValidationInfo;
                }
                else if (fileCopy.IsArchive())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Archive;
                    return fileValidationInfo;
                }
                else if (fileCopy.IsDocument())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Document;
                }
                else if (fileCopy.IsImage())
                {
                    fileValidationInfo.FileType = FileTypeEnum.Image;
                }
                else if (FileHasVideoExtension(fileType.Extension))
                {
                    fileValidationInfo.FileType = FileTypeEnum.Video;
                }

                fileValidationInfo.IsValid = allowedFileTypes.Contains(fileValidationInfo.FileType);
                fileValidationInfo.IsValidExtension = allowedExtensions.Contains(iFormFileExtension == "webp" || iFormFileExtension == "avif" ? iFormFileExtension : fileType.Extension);

                return fileValidationInfo;
            }
            finally
            {
                // Leave the source stream rewound; the upload handler reads it next.
                fileCopy.Position = 0;
            }
        }

        private static bool HasPdfSignature(Stream stream)
        {
            // %PDF-
            return HasSignatureAt(stream, 0, new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });
        }

        private static bool HasMp4Signature(Stream stream)
        {
            // ISO Base Media File Format: bytes 4..7 == "ftyp"
            return HasSignatureAt(stream, 4, new byte[] { 0x66, 0x74, 0x79, 0x70 });
        }

        private static bool HasSignatureAt(Stream stream, int offset, byte[] signature)
        {
            if (stream == null || !stream.CanRead)
            {
                return false;
            }

            long originalPosition = stream.CanSeek ? stream.Position : 0;
            try
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                int total = offset + signature.Length;
                var header = new byte[total];
                int read = 0;
                while (read < total)
                {
                    int n = stream.Read(header, read, total - read);
                    if (n == 0)
                    {
                        return false; // file shorter than the expected header
                    }
                    read += n;
                }

                for (int i = 0; i < signature.Length; i++)
                {
                    if (header[offset + i] != signature[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
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

            if (fileTypeEnum == FileTypeEnum.Document)
            {
                return MediaFileTypeEnum.Document;
            }

            throw new NotImplementedException();
        }
    }
}
