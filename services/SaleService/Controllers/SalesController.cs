using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.DTOs;
using SalesService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace SalesService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly SalesDbContext _db;


    public SalesController(SalesDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create([FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        if (request?.Items == null || request.Items.Count == 0)
            return BadRequest("Nenhum item informado.");

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            CustomerId = "anon",
            Items = request.Items.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            TotalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity),
            Status = SaleStatus.Created,
            CreatedAt = DateTime.UtcNow,
            History = new List<SaleHistory>
            {
                new SaleHistory
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "Pedido criado",
                    Details = "Pedido em processamento"
                }
            }
        };

        var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var factory = new ConnectionFactory { HostName = rabbitHost };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "sales",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        string message = JsonSerializer.Serialize(sale);
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "sales",
            body: body
        );

        var response = new SaleResponse(
            sale.Id,
            sale.Status,
            sale.TotalAmount,
            sale.CreatedAt
        );

        return Ok(response);
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
