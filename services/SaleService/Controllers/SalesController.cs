using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.DTOs;
using SalesService.Models;

namespace SalesService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly SalesDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public SalesController(SalesDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create([FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        if (request?.Items == null || request.Items.Count == 0)
            return BadRequest("Nenhum item informado.");

        var client = _httpClientFactory.CreateClient("product");

        // 1) Checar disponibilidade
        var availabilityResp = await client.PostAsJsonAsync("/api/products/availability", request.Items, ct);
        if (!availabilityResp.IsSuccessStatusCode)
            return StatusCode((int)availabilityResp.StatusCode, "Erro ao checar estoque.");

        var availability = await availabilityResp.Content.ReadFromJsonAsync<AvailabilityResponse>(cancellationToken: ct);
        if (availability == null || !availability.Available)
            return Conflict(new { message = "Estoque insuficiente", availability?.Missing });

        // 2) Baixar estoque
        var decreaseResp = await client.PostAsJsonAsync("/api/products/decrease", request.Items, ct);
        if (!decreaseResp.IsSuccessStatusCode)
            return StatusCode((int)decreaseResp.StatusCode, "Erro ao baixar estoque.");

        var decrease = await decreaseResp.Content.ReadFromJsonAsync<DecreaseResponse>(cancellationToken: ct);
        if (decrease == null || !decrease.Success)
            return Conflict(new { message = "Falha ao baixar estoque", decrease?.Failed });

        // 3) Criar venda
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            CustomerId = "anon", // futuramente vem do JWT via Gateway
            Items = request.Items.Select(i => new SaleItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            TotalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity),
            Status = SaleStatus.Confirmed
        };

        sale.History.Add(new SaleHistory
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            Action = "SALE_CREATED",
            Details = "Venda criada e estoque baixado"
        });

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = sale.Id },
            new SaleResponse(sale.Id, sale.Status, sale.TotalAmount, sale.CreatedAt));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var sale = await _db.Sales.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sale is null) return NotFound();

        return new SaleResponse(sale.Id, sale.Status, sale.TotalAmount, sale.CreatedAt);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IEnumerable<SaleHistoryResponse>>> GetHistory([FromRoute] Guid id, CancellationToken ct)
    {
        var hist = await _db.SaleHistories
            .Where(h => h.SaleId == id)
            .OrderBy(h => h.Timestamp)
            .Select(h => new SaleHistoryResponse(h.Timestamp, h.Action, h.Details))
            .ToListAsync(ct);

        if (hist.Count == 0) return NotFound();
        return hist;
    }

    private sealed class AvailabilityResponse
    {
        public bool Available { get; set; }
        public List<object> Missing { get; set; } = new();
    }

    private sealed class DecreaseResponse
    {
        public bool Success { get; set; }
        public List<object> Failed { get; set; } = new();
    }
}
