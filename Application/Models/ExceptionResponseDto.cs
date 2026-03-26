
namespace Core.Application.Models
{
    public class ExceptionResponseDto
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public object Details { get; set; }
        public object Errors { get; set; }
    }
}
