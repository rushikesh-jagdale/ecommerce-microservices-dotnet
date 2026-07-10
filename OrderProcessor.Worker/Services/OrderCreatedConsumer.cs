using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;

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

    protected override async Task ExecuteAsync(
    CancellationToken stoppingToken)
    {
        IConnection? connection = null;
        IModel? channel = null;

        _logger.LogInformation(
      "RabbitMQ Host = {Host}",
      _configuration["RabbitMQ:Host"]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"]
                };

                connection = factory.CreateConnection();

                channel = connection.CreateModel();

                _logger.LogInformation("Connected to RabbitMQ.");

                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "RabbitMQ not ready. Retrying in 5 seconds...");

                _logger.LogWarning(ex.Message);

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

            var order =
                JsonSerializer.Deserialize<OrderCreatedEvent>(json);

            _logger.LogInformation("Order Received");

            _logger.LogInformation(
                "OrderId: {OrderId}",
                order!.OrderId);

            _logger.LogInformation(
                "UserId: {UserId}",
                order.UserId);

            _logger.LogInformation(
                "ProductId: {ProductId}",
                order.ProductId);

            _logger.LogInformation(
                "Quantity: {Quantity}",
                order.Quantity);

            _logger.LogInformation(
                "Total Price: {Price}",
                order.TotalPrice);

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