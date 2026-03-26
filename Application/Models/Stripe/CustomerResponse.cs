using System;

namespace Core.Application.Models.Stripe;

public class CustomerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string Description { get; set; } = string.Empty;
}
