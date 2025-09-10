using SalesService.Models;

namespace SalesService.DTOs;

public record SaleResponse(Guid Id, List<SaleItem> Items, SaleStatus Status, decimal TotalAmount, DateTime CreatedAt);
