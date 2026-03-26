using Core.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Core.Application.Hubs
{
    public class NotificationHub : Hub
    {
        public CrudOperationEnum CrudOperation { get; set; }
        public string Message { get; set; }
    }
}
