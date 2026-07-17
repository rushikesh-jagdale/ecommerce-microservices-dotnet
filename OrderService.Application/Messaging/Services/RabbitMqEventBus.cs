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
        var host =
            Environment.GetEnvironmentVariable("RabbitMQ__Host")
            ?? configuration["RabbitMQ:Host"];

        var username =
            Environment.GetEnvironmentVariable("RabbitMQ__Username")
            ?? configuration["RabbitMQ:Username"];

        var password =
            Environment.GetEnvironmentVariable("RabbitMQ__Password")
            ?? configuration["RabbitMQ:Password"];

        var virtualHost =
            Environment.GetEnvironmentVariable("RabbitMQ__VirtualHost")
            ?? configuration["RabbitMQ:VirtualHost"];

        var port = int.Parse(
            Environment.GetEnvironmentVariable("RabbitMQ__Port")
            ?? configuration["RabbitMQ:Port"]
            ?? "5672");

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
            factory.Ssl.Enabled = true;
        }

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