namespace SalesService.DTOs;

public record CreateSaleRequest(List<CreateSaleItemRequest> Items);
