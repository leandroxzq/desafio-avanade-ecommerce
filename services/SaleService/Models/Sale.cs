namespace SalesService.Models;

public enum SaleStatus { Created = 1, Confirmed = 2, Failed = 3 }

public class Sale
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<SaleHistory> History { get; set; } = new List<SaleHistory>();
}
