namespace SalesService.DTOs;

public record CreateSaleItemRequest(int ProductId, int Quantity, decimal UnitPrice);
