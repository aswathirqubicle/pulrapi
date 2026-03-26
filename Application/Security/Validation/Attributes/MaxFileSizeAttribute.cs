using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutoMapper.Internal;
using Core.Application.Helpers;
using Core.Domain.Enums;
using System;

namespace Core.Application.Security.Validation.Attributes
{
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;
        private readonly string _fileType;

        public MaxFileSizeAttribute(int maxFileSize, string fileType = null)
        {
            _maxFileSize = maxFileSize;
            _fileType = fileType;
            ErrorMessage = $"Maximum allowed file size is {maxFileSize / (1024 * 1024)}MB.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            try
            {
                // Handle null value
                if (value == null)
                {
                    return ValidationResult.Success; // Let Required attribute handle null validation
                }

                // Handle list of files
                if (value.GetType().IsListType())
                {
                    var files = value as List<IFormFile>;
                    if (files == null)
                    {
                        return ValidationResult.Success; // Let other validators handle this case
                    }

                    foreach (var file in files)
                    {
                        if (file != null)
                        {
                            var fileTypeInfo = FileHelper.CheckFile(file);
                            var maxSize = !string.IsNullOrEmpty(_fileType) && fileTypeInfo.FileType.ToString() == _fileType ? 
                                (fileTypeInfo.FileType == FileTypeEnum.Video ? 30 * 1024 * 1024 : _maxFileSize) : 
                                _maxFileSize;

                            if (file.Length > maxSize)
                            {
                                return new ValidationResult($"File '{file.FileName}' exceeds the maximum allowed size of {maxSize / (1024 * 1024)}MB.");
                            }
                        }
                    }
                }
                else
                {
                    // Handle single file
                    var file = value as IFormFile;
                    if (file != null)
                    {
                        var fileTypeInfo = FileHelper.CheckFile(file);
                        var maxSize = !string.IsNullOrEmpty(_fileType) && fileTypeInfo.FileType.ToString() == _fileType ? 
                            (fileTypeInfo.FileType == FileTypeEnum.Video ? 30 * 1024 * 1024 : _maxFileSize) : 
                            _maxFileSize;

                        if (file.Length > maxSize)
                        {
                            return new ValidationResult($"File '{file.FileName}' exceeds the maximum allowed size of {maxSize / (1024 * 1024)}MB.");
                        }
                    }
                }

                return ValidationResult.Success;
            }
            catch (Exception ex)
            {
                return new ValidationResult($"Error validating file size: {ex.Message}");
            }
        }
    }
}
