using AutoMapper.Internal;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Core.Application.Helpers;
using Core.Application.Models;
using Core.Domain.Enums;

namespace Core.Application.Security.Validation.Attributes
{
    public class PulrFileValidationAttribute : ValidationAttribute
    {
        private readonly FileTypeEnum[] _allowedFileTypes;
        private readonly string[] _extensions;
        public PulrFileValidationAttribute(FileTypeEnum[] allowedFileTypes = null, string[] extensions = null)
        {
            _allowedFileTypes = allowedFileTypes;
            _extensions = extensions;
        }

        protected override ValidationResult IsValid(
            object value, ValidationContext validationContext)
        {
            // Check if value is null
            if (value == null)
            {
                return new ValidationResult("File is required.");
            }

            IFormFile file = null;
            if (value.GetType().IsListType())
            {
                var files = value as List<IFormFile>;
                if (files == null || files.Count == 0)
                {
                    return new ValidationResult("At least one file is required.");
                }
                
                ValidationResult validationResult = null;
                for (int i = 0; i < files.Count; i++)
                {
                    file = files[i];
                    validationResult = CheckFile(file);
                    if(validationResult != ValidationResult.Success)
                    {
                        return validationResult;
                    }
                }
                return validationResult;
            }

            file = value as IFormFile;
            if (file == null)
            {
                return new ValidationResult("Invalid file format.");
            }
            
            return CheckFile(file);
        }

        private ValidationResult CheckFile(IFormFile file)
        {
            FileValidationInfo fileInfo = FileHelper.CheckFile(file, _allowedFileTypes, _extensions != null ? _extensions.ToList() : null);

            if (!fileInfo.IsValid || !fileInfo.IsValidExtension)
            {
                return new ValidationResult(GetErrorMessage(fileInfo));
            }

            return ValidationResult.Success;
        }

        private string GetErrorMessage(FileValidationInfo fileInfo)
        {
            return $"File is not valid. Extension: {fileInfo.Extension} , File type: {fileInfo.FileType}";
        }
    }
}
