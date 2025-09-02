namespace SalesService.Models;

public class SaleHistory
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public string Action { get; set; } = default!;
    public string Details { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
