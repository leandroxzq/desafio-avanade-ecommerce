using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.DTOs;
using SalesService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create([FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        if (request?.Items == null || request.Items.Count == 0)
            return BadRequest("Nenhum item informado.");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("ID do usuário não encontrado no token.");
        }

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            CustomerId = userId,
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

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(ct);

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
            sale.Items.ToList(),
            sale.Status,
            sale.TotalAmount,
            sale.CreatedAt
        );

        return Ok(response);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleResponse>>> GetAllByCustomer(CancellationToken ct)
    {

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {

            return Unauthorized("ID do usuário não encontrado no token.");
        }


        var sales = await _db.Sales
            .AsNoTracking()
            .Where(s => s.CustomerId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SaleResponse(s.Id, s.Items.ToList(), s.Status, s.TotalAmount, s.CreatedAt))
            .ToListAsync(ct);

        if (sales == null || !sales.Any())
        {
            return NotFound("Nenhuma compra encontrada para este usuário.");
        }

        return Ok(sales);
    }

    [Authorize]
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

    [HttpPatch("status")]
    public async Task<ActionResult> UpdateSaleStatus([FromBody] SaleStatusUpdateRequest request)
    {
        var sale = await _db.Sales
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.Id == request.Id);

        if (sale == null)
        {
            return NotFound();
        }

        sale.Status = request.Status;
        sale.History.Add(request.History);

        await _db.SaveChangesAsync();

        return NoContent();
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
