using Core.Application.Hubs;
using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
        public interface INotificationHub
        {
            public Task SendMessage<T>(T notification) where T : SignalrNotificationBase;
        }
}
