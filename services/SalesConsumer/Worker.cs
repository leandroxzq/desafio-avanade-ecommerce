using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SalesConsumer.Models;

namespace SalesConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var factory = new ConnectionFactory { HostName = rabbitHost };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "sales",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        _logger.LogInformation("📥 Consumer iniciado e aguardando mensagens...");

        var consumer = new AsyncEventingBasicConsumer(channel);
        var clientInventory = _httpClientFactory.CreateClient("Inventory");
        var salesClient = _httpClientFactory.CreateClient("Sale");

        consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    try
    {
        var sale = JsonSerializer.Deserialize<Sale>(message);

        if (sale is null)
        {
            _logger.LogWarning("⚠️ Mensagem inválida recebida.");
            await Task.Yield();
            return;
        }

        var formattedJson = JsonSerializer.Serialize(sale, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation($"📦 Mensagem recebida:\n{formattedJson}");

        var finalStatus = SaleStatus.Failed;
        var finalHistory = new SaleHistory { SaleId = sale.Id, Timestamp = DateTime.UtcNow };

        var availabilityRequest = sale.Items
          .Select(i => new { productId = i.ProductId, quantity = i.Quantity })
          .ToList();

        var checkResponse = await clientInventory.PostAsJsonAsync("Products/availability", availabilityRequest);

        if (!checkResponse.IsSuccessStatusCode)
        {
            finalStatus = SaleStatus.Failed;
            finalHistory.Action = "StockCheckFailed";
            finalHistory.Details = "Erro ao verificar disponibilidade";
            _logger.LogWarning("⚠️ Erro ao verificar disponibilidade para Sale {SaleId}", sale.Id);
        }
        else
        {
            var availability = await checkResponse.Content.ReadFromJsonAsync<AvailabilityResponse>();

            if (availability is null || !availability.Available)
            {
                finalStatus = SaleStatus.Failed;
                finalHistory.Action = "StockCheckFailed";
                finalHistory.Details = $"Estoque insuficiente para Sale {sale.Id}";
                _logger.LogWarning("❌ Estoque insuficiente para Sale {SaleId}", sale.Id);
            }
            else
            {
                var decreaseResponse = await clientInventory.PostAsJsonAsync("Products/decrease", availabilityRequest);

                if (!decreaseResponse.IsSuccessStatusCode)
                {
                    finalStatus = SaleStatus.Failed;
                    finalHistory.Action = "StockDecreaseFailed";
                    finalHistory.Details = "Falha ao decrementar estoque";
                    _logger.LogError("❌ Erro ao decrementar estoque para Sale {SaleId}", sale.Id);
                }
                else
                {
                    var decreaseResult = await decreaseResponse.Content.ReadFromJsonAsync<DecreaseResponse>();
                    if (decreaseResult is not null && decreaseResult.Success)
                    {
                        finalStatus = SaleStatus.Confirmed;
                        finalHistory.Action = "StockDecreased";
                        finalHistory.Details = "Estoque atualizado com sucesso";
                        _logger.LogInformation("✅ Venda confirmada e estoque decrementado. SaleId: {SaleId}", sale.Id);
                    }
                    else
                    {
                        finalStatus = SaleStatus.Failed;
                        finalHistory.Action = "StockDecreaseFailed";
                        finalHistory.Details = "Falha ao decrementar estoque.";
                        _logger.LogError("❌ Falha ao decrementar estoque. SaleId: {SaleId}", sale.Id);
                    }
                }
            }
        }

        var updateRequest = new SaleStatusUpdateRequest
        {
            Id = sale.Id,
            Status = finalStatus,
            History = finalHistory
        };

        var updateResponse = await salesClient.PatchAsJsonAsync("Sales/status", updateRequest, stoppingToken);

        if (updateResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("✅ Status da venda {SaleId} atualizado com sucesso.", sale.Id);
        }
        else
        {
            _logger.LogError("❌ Falha ao atualizar o status da venda {SaleId}. Status Code: {StatusCode}", sale.Id, updateResponse.StatusCode);
        }

    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Erro ao processar mensagem");
    }


    await Task.Yield();
};

        await channel.BasicConsumeAsync(
            queue: "sales",
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        await Task.Delay(-1, stoppingToken);
    }
}