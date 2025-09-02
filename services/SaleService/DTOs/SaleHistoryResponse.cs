namespace SalesService.DTOs;

public record SaleHistoryResponse(DateTime Timestamp, string Action, string Details);
