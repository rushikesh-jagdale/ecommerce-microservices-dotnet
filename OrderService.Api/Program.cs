using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderService.Api.Middleware;
using OrderService.Application.Features.Orders;
using OrderService.Application.Interfaces;
using OrderService.Application.Services;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using System.Text;
using System.Text.Json.Serialization;
using Mapster;
using MapsterMapper;
using OrderService.Application.Mapping;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Serilog;
using Polly;
using Polly.Extensions.Http;
using OrderService.Application.Messaging.Interfaces;
using OrderService.Application.Messaging.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<OrderManager>();

builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);

builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>

{

    client.BaseAddress = new Uri(

    builder.Configuration["ProductService:BaseUrl"]!);

})

.AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, delay, retryNumber, context) =>
            {
                Log.Warning(
                    "Retry {RetryNumber} after {Delay}s. Reason: {Reason}",
                    retryNumber,
                    delay.TotalSeconds,
                    outcome.Exception?.Message ??
                    outcome.Result.StatusCode.ToString());
            });
}

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>(
        "SQL Server");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter token like: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"]!);

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(key)
            };
    });

MapsterConfig.RegisterMappings();

Log.Information("Starting OrderService...");

var app = builder.Build();

for (int retry = 0; retry < 10; retry++)
{
    try
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<OrderDbContext>();

        db.Database.Migrate();

        Log.Information("Database migration successful.");

        break;
    }
    catch (Exception ex)
    {
        Log.Warning(ex,
            "Database not ready. Retrying...");

        Thread.Sleep(5000);
    }
}

// Configure the HTTP request pipeline.

    app.UseSwagger();
    app.UseSwaggerUI();


app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            Status = report.Status.ToString(),

            Checks = report.Entries.Select(x => new
            {
                Name = x.Key,
                Status = x.Value.Status.ToString(),
                Description = x.Value.Description
            }),

            TotalDuration = report.TotalDuration
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }));
    }
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}