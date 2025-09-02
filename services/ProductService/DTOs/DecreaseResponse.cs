namespace ProductService.Dtos;

using ProductService.DTOs;

public class DecreaseResponse
{
    public bool Success { get; set; }
    public List<UnavailableItem> Failed { get; set; } = new();
}
