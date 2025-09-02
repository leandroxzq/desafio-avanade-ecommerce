using SalesService.Models;

namespace SalesService.DTOs;

public record SaleResponse(Guid Id, SaleStatus Status, decimal TotalAmount, DateTime CreatedAt);
