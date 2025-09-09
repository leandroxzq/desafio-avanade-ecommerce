using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SalesConsumer.Models;

namespace SalesConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
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
                var formattedJson = JsonSerializer.Serialize(sale, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogInformation($"📦 Mensagem recebida:\n{formattedJson}");
            
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