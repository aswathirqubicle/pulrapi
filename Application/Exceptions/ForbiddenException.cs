using System;
using System.Collections.Generic;
using System.Text;
using Core.Application.Exceptions;

namespace Core.Application.Exceptions
{
    public class ForbiddenException : BaseException
    {
        public override int StatusCode { get; } = 403;

        public override string Message { get; }

        public ForbiddenException(
            string message = ""
            )
        {
            Message = message;
        }
    }
}
