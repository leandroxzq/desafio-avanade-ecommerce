namespace SalesConsumer.Models;

public class SaleItem
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
