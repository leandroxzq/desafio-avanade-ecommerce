namespace SalesService.DTOs;
using SalesService.Models;

public class SaleStatusUpdateRequest
{
    public Guid Id { get; set; }
    public SaleStatus Status { get; set; }
    public SaleHistory History { get; set; } = default!;
}