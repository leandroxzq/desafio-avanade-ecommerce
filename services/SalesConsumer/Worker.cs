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
                    return;
                }

                var formattedJson = JsonSerializer.Serialize(sale, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation($"📦 Mensagem recebida:\n{formattedJson}");

                var client = _httpClientFactory.CreateClient("Inventory");

                var availabilityRequest = sale.Items
                    .Select(i => new { productId = i.ProductId, quantity = i.Quantity })
                    .ToList();

                var checkResponse = await client.PostAsJsonAsync("Products/availability", availabilityRequest);

                if (!checkResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ Erro ao verificar disponibilidade para Sale {SaleId}", sale.Id);
                    return;
                }

                var availability = await checkResponse.Content.ReadFromJsonAsync<AvailabilityResponse>();

                if (availability is null || !availability.Available)
                {
                    sale.Status = SaleStatus.Failed;
                    sale.History.Add(new SaleHistory
                    {
                        SaleId = sale.Id,
                        Action = "StockCheckFailed",
                        Details = JsonSerializer.Serialize(availability?.Missing ?? new())
                    });

                    _logger.LogWarning("❌ Estoque insuficiente para Sale {SaleId}", sale.Id);
                    return;
                }

                var decreaseResponse = await client.PostAsJsonAsync("Products/decrease", availabilityRequest);

                if (!decreaseResponse.IsSuccessStatusCode)
                {
                    sale.Status = SaleStatus.Failed;
                    sale.History.Add(new SaleHistory
                    {
                        SaleId = sale.Id,
                        Action = "StockDecreaseFailed",
                        Details = "Falha ao decrementar estoque"
                    });

                    _logger.LogError("❌ Erro ao decrementar estoque para Sale {SaleId}", sale.Id);
                    return;
                }

                var decreaseResult = await decreaseResponse.Content.ReadFromJsonAsync<DecreaseResponse>();

                if (decreaseResult is not null && decreaseResult.Success)
                {
                    sale.Status = SaleStatus.Confirmed;
                    sale.History.Add(new SaleHistory
                    {
                        SaleId = sale.Id,
                        Action = "StockDecreased",
                        Details = "Estoque atualizado com sucesso"
                    });

                    _logger.LogInformation("✅ Venda confirmada e estoque decrementado. SaleId: {SaleId}", sale.Id);
                }
                else
                {
                    sale.Status = SaleStatus.Failed;
                    sale.History.Add(new SaleHistory
                    {
                        SaleId = sale.Id,
                        Action = "StockDecreaseFailed",
                        Details = JsonSerializer.Serialize(decreaseResult?.Failed ?? new())
                    });

                    _logger.LogError("❌ Falha ao decrementar estoque. SaleId: {SaleId}", sale.Id);
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