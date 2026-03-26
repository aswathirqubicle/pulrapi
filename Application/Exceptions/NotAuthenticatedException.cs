using System;
using System.Collections.Generic;
using System.Text;
using Core.Application.Exceptions;

namespace Core.Application.Exceptions
{
    public class NotAuthenticatedException : BaseException
    {
        public override int StatusCode { get; } = 401;

        public override string Message { get; }

        public NotAuthenticatedException(string message)
        {
            Message = message;
        }

        public NotAuthenticatedException()
        {
            Message = "";
        }
    }
}
