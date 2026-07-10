using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OrderService.Application.Messaging.Interfaces;
using RabbitMQ.Client;

namespace OrderService.Application.Messaging.Services;

public class RabbitMqEventBus : IEventBus
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqEventBus(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"]
        };

        const int maxRetries = 10;
        const int delaySeconds = 5;

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"Connecting to RabbitMQ... Attempt {attempt}/{maxRetries}");

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                Console.WriteLine("RabbitMQ connected successfully.");

                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                Console.WriteLine(
                    $"RabbitMQ not available. Retrying in {delaySeconds} seconds...");

                Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        throw new Exception(
            "Unable to connect to RabbitMQ after multiple attempts.",
            lastException);
    }

    public Task PublishAsync<T>(string queueName, T message)
    {
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var json = JsonSerializer.Serialize(message);

        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: null,
            body: body);

        Console.WriteLine($"Published message to queue '{queueName}'");

        return Task.CompletedTask;
    }
}