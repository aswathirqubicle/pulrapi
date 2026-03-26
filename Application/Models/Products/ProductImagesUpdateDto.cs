using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Core.Application.Models.Products
{
    public class ProductImagesUpdateDto
    {
        public string ProductUid { get; set; }
        public List<IFormFile> Images { get; set; }
        public List<int> ImagePriorities { get; set; }
    }
}
