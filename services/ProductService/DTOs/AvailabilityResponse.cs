namespace ProductService.Dtos;

using ProductService.DTOs;

public class AvailabilityResponse
{
    public bool Available { get; set; }
    public List<UnavailableItem> Missing { get; set; } = new();
}
