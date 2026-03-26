using Core.Domain.Entities;

namespace Core.Domain.Entities;

public class PaymentMethod : EntityBase
{
    public string Name { get; set; }
    public string Key { get; set; }
}
