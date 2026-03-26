using Core.Domain.Enums;

namespace Core.Application.Hubs
{
    public abstract class SignalrNotificationBase
    {
        public CrudOperationEnum CrudOperation { get; set; }
        public string Message { get; set; }
    }
}
