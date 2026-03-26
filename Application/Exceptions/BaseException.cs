using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Application.Exceptions
{
    public abstract class BaseException : Exception
    {
        public abstract int StatusCode { get; }
    }
}
