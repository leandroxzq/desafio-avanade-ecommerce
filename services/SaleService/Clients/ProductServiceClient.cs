using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using SalesService.DTOs;

namespace SalesService.Clients
{
    public class ProductServiceClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductServiceClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Checa disponibilidade de produtos
        public async Task<AvailabilityResponse> CheckAvailabilityAsync(
            IEnumerable<CreateSaleItemRequest> items,
            CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient("product");
            var response = await client.PostAsJsonAsync("/api/products/availability", items, ct);
            return await response.Content.ReadFromJsonAsync<AvailabilityResponse>(cancellationToken: ct);
        }

        // Baixa estoque
        public async Task<DecreaseResponse> DecreaseStockAsync(
            IEnumerable<CreateSaleItemRequest> items,
            CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient("product");
            var response = await client.PostAsJsonAsync("/api/products/decrease", items, ct);
            return await response.Content.ReadFromJsonAsync<DecreaseResponse>(cancellationToken: ct);
        }

        public sealed class AvailabilityResponse
        {
            public bool Available { get; set; }
            public List<object> Missing { get; set; } = new();
        }

        public sealed class DecreaseResponse
        {
            public bool Success { get; set; }
            public List<object> Failed { get; set; } = new();
        }
    }
}
