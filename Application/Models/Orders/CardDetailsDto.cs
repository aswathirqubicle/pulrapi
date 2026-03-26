using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Orders;

public class CardDetailsDto
{
    [Required]
    public string CardNumber { get; set; }
    [Required]
    public DateTime CardExpDate { get; set; }
    [Required]
    public string CardCvc { get; set; }
}
