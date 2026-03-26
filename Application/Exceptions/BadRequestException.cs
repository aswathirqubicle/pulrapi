using Core.Application.Exceptions;
namespace Core.Application.Exceptions
{
    public class BadRequestException : BaseException
    {
        public override int StatusCode { get; } = 400;

        public override string Message { get; }

        public BadRequestException(string message)
        {
            Message = message;
        }
    }
}
