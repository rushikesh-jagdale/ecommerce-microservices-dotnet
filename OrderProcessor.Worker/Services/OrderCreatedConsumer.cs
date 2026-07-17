using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderProcessor.Worker.Services;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection? connection = null;
        IModel? channel = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var host =
                    Environment.GetEnvironmentVariable("RabbitMQ__Host")
                    ?? _configuration["RabbitMQ:Host"];

                var username =
                    Environment.GetEnvironmentVariable("RabbitMQ__Username")
                    ?? _configuration["RabbitMQ:Username"];

                var password =
                    Environment.GetEnvironmentVariable("RabbitMQ__Password")
                    ?? _configuration["RabbitMQ:Password"];

                var virtualHost =
                    Environment.GetEnvironmentVariable("RabbitMQ__VirtualHost")
                    ?? _configuration["RabbitMQ:VirtualHost"];

                var port = int.Parse(
                    Environment.GetEnvironmentVariable("RabbitMQ__Port")
                    ?? _configuration["RabbitMQ:Port"]
                    ?? "5671");

                var factory = new ConnectionFactory
                {
                    HostName = host,
                    UserName = username,
                    Password = password,
                    VirtualHost = virtualHost,
                    Port = port,
                    DispatchConsumersAsync = false
                };

                // Enable SSL only for CloudAMQP
                if (!string.Equals(host, "rabbitmq", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    factory.Ssl = new SslOption
                    {
                        Enabled = true,
                        Version = SslProtocols.Tls12,
                        AcceptablePolicyErrors =
                            SslPolicyErrors.RemoteCertificateChainErrors |
                            SslPolicyErrors.RemoteCertificateNameMismatch
                    };
                }

                connection = factory.CreateConnection();
                channel = connection.CreateModel();

                _logger.LogInformation("Connected to RabbitMQ.");

                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ not ready. Retrying in 5 seconds...");

                await Task.Delay(5000, stoppingToken);
            }
        }

        channel!.QueueDeclare(
            queue: "order-created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();

            var json = Encoding.UTF8.GetString(body);

            var order = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

            _logger.LogInformation("========== ORDER RECEIVED ==========");
            _logger.LogInformation("OrderId: {OrderId}", order!.OrderId);
            _logger.LogInformation("UserId: {UserId}", order.UserId);
            _logger.LogInformation("ProductId: {ProductId}", order.ProductId);
            _logger.LogInformation("Quantity: {Quantity}", order.Quantity);
            _logger.LogInformation("Total Price: {Price}", order.TotalPrice);

            channel.BasicAck(eventArgs.DeliveryTag, false);
        };

        channel.BasicConsume(
            queue: "order-created",
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ Consumer Started.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}