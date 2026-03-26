using Core.Domain.Common;
using Core.Domain.Entities;

namespace Core.Domain.Events;

public class StoreCreatedEvent : BaseEvent
{
    public StoreCreatedEvent(Store store)
    {
        Store = store;
    }

    public Store Store { get; }
}