using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Application.Exceptions
{
    public class SuccessException : BaseException
    {
        public override int StatusCode { get; } = 200;

        public override string Message { get; }

        public SuccessException(string message)
        {
            Message = message;
        }

        public SuccessException()
        {
            Message = "Operation completed successfully";
        }
    }
} 