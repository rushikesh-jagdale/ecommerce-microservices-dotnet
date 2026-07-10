using OrderProcessor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true);

builder.Services.AddHostedService<OrderCreatedConsumer>();

var host = builder.Build();

host.Run();