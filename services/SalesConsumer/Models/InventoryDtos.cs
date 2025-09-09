namespace SalesConsumer.Models;

public class AvailabilityResponse
{
    public bool Available { get; set; }
    public List<UnavailableItem> Missing { get; set; } = new();
}

public class DecreaseResponse
{
    public bool Success { get; set; }
    public List<UnavailableItem> Failed { get; set; } = new();
}

public record ProductStockRequest(int ProductId, int Quantity);

public record UnavailableItem(int ProductId, int AvailabilityQty);
